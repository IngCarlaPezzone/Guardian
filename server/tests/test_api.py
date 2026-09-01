import os
from datetime import datetime, timedelta, timezone

os.environ["DATABASE_URL"] = "sqlite:///:memory:"
os.environ["DEVICE_BOOTSTRAP_TOKEN"] = "test-bootstrap"
os.environ["GUARDIAN_ADMIN_INITIAL_PASSWORD"] = "test-password"
os.environ["GUARDIAN_SESSION_SECRET"] = "test-session-secret"

from fastapi.testclient import TestClient

from server.app.admin import ADMIN_CSS_VERSION, admin_title_with_section, sign_username
from server.app.bootstrap import ensure_admin
from server.app.db import SessionLocal, engine
from server.app.main import app
from server.app.metrics import QUESTION_LABELS, dashboard_data
from server.app.models import DEVICE_KIND_STG_DEMO, DEVICE_KIND_STG_IMPORTED_TELEMETRY, Base, Device, DeviceCommand, DeviceConfiguration, DeviceEvent, DeviceMissionProfile, Release, UpdateCommand
from server.app.security import hash_secret, utcnow


def setup_module():
    Base.metadata.create_all(bind=engine)
    with SessionLocal() as db:
        ensure_admin(db)


def register(client):
    response = client.post("/api/v1/devices/register", json={
        "device_id": "00000000-0000-4000-8000-000000000001",
        "machine_name": "Sample-PC",
        "client_version": "0.2.0",
        "bootstrap_token": "test-bootstrap",
    })
    assert response.status_code == 200
    return response.json()["device_token"]


def test_register_rejects_invalid_bootstrap():
    client = TestClient(app)
    response = client.post("/api/v1/devices/register", json={
        "device_id": "00000000-0000-4000-8000-000000000002",
        "machine_name": "Sample-PC",
        "client_version": "0.2.0",
        "bootstrap_token": "bad",
    })
    assert response.status_code == 401


def test_admin_css_is_served_with_a_content_version():
    client = admin_client()

    response = client.get("/admin/")
    assert response.status_code == 200
    assert f'/admin/static/admin.css?v={ADMIN_CSS_VERSION}' in response.text

    css = client.get(f"/admin/static/admin.css?v={ADMIN_CSS_VERSION}")
    assert css.status_code == 200
    assert css.headers["content-type"].startswith("text/css")
    assert ".card-grid" in css.text

    icon = client.get("/admin/static/guardian.png")
    assert icon.status_code == 200
    assert icon.headers["content-type"].startswith("image/png")
    assert 'rel="icon" type="image/png" href="/admin/static/guardian.png?v=' in response.text
    assert 'class="admin-brand"' in response.text
    assert admin_title_with_section("Dispositivos").endswith("Dispositivos")


def test_dashboard_shows_operational_devices_and_hides_synthetic_stg_records_by_default():
    client = admin_client()
    operational_id = "00000000-0000-4000-8000-000000000111"
    demo_id = "00000000-0000-4000-8000-000000000112"
    imported_id = "00000000-0000-4000-8000-000000000113"
    with SessionLocal() as db:
        db.add_all([
            Device(id=operational_id, machine_name="Operational-Test-PC", display_name="PC TEST", token_hash=hash_secret("operational-token"), client_version="0.4.4-staging-stage3", last_seen_at=utcnow()),
            Device(id=demo_id, machine_name="STG-ONLINE-ACTIVE", display_name="Demo Online Activo", token_hash=hash_secret("demo-token"), device_kind=DEVICE_KIND_STG_DEMO),
            Device(id=imported_id, machine_name="STG-IMPORTED-TELEMETRY-1", display_name="Telemetría importada STG 1", token_hash=hash_secret("import-token"), device_kind=DEVICE_KIND_STG_IMPORTED_TELEMETRY),
            DeviceConfiguration(device_id=operational_id, interval_seconds=900, version=1),
        ])
        db.commit()

    dashboard = client.get("/admin/")
    assert dashboard.status_code == 200
    assert "PC TEST" in dashboard.text
    assert "Operational-Test-PC" in dashboard.text
    assert "Demo Online Activo" not in dashboard.text
    assert "Telemetría importada STG 1" not in dashboard.text
    assert "Online · Activo" in dashboard.text
    assert "Versión:" in dashboard.text
    assert "0.4.4-staging-stage3" in dashboard.text
    assert "Intervalo:" in dashboard.text
    assert "15 min" in dashboard.text
    assert "Pausar misiones" not in dashboard.text
    assert "Reanudar misiones" not in dashboard.text
    assert "Probar misión ahora" not in dashboard.text
    assert f'<a class="button" href="/admin/devices/{operational_id}/missions">Configuración</a>' in dashboard.text
    assert f'<a class="button secondary" href="/admin/devices/{operational_id}/missions">Configuración</a>' not in dashboard.text
    assert f"/admin/devices/{operational_id}/missions" in dashboard.text
    assert f"/admin/devices/{operational_id}/activity" in dashboard.text
    assert f"/admin/devices/{operational_id}/metrics" in dashboard.text
    assert 'name="release_id"' not in dashboard.text
    assert "Distribución" not in dashboard.text
    assert 'href="/admin/releases"' in dashboard.text

    synthetic = client.get("/admin/?show_synthetic=true")
    assert "Demo Online Activo" in synthetic.text
    assert "Telemetría importada STG 1" in synthetic.text


def test_device_flow_config_and_update_status():
    client = TestClient(app)
    token = register(client)
    headers = {"Authorization": f"Bearer {token}"}

    heartbeat = client.post("/api/v1/devices/00000000-0000-4000-8000-000000000001/heartbeat", headers=headers, json={
        "machine_name": "Sample-PC",
        "client_version": "0.2.0",
        "effective_interval_seconds": 900,
    })
    assert heartbeat.status_code == 200
    assert heartbeat.json()["pending_update"] is False

    config = client.get("/api/v1/devices/00000000-0000-4000-8000-000000000001/config", headers=headers)
    assert config.status_code == 200
    assert config.json()["interval_seconds"] == 900

    with SessionLocal() as db:
        cfg = db.query(DeviceConfiguration).filter(DeviceConfiguration.device_id == "00000000-0000-4000-8000-000000000001").one()
        cfg.interval_seconds = 120
        cfg.version += 1
        release = Release(version="0.2.1", filename="Guardian-0.2.1.zip", sha256="a" * 64, file_size=10)
        db.add(release)
        db.flush()
        db.add(UpdateCommand(device_id=cfg.device_id, release_id=release.id, target_version=release.version))
        db.commit()

    config = client.get("/api/v1/devices/00000000-0000-4000-8000-000000000001/config", headers=headers)
    assert config.json()["interval_seconds"] == 120

    pending = client.get("/api/v1/devices/00000000-0000-4000-8000-000000000001/updates/pending", headers=headers)
    assert pending.json()["pending"] is True
    command_id = pending.json()["command_id"]

    status = client.post(f"/api/v1/devices/00000000-0000-4000-8000-000000000001/updates/{command_id}/status", headers=headers, json={"status": "acknowledged"})
    assert status.status_code == 200


def test_heartbeat_persists_monitoring_state_and_device_command_lifecycle():
    client = TestClient(app)
    token = "control-token"
    headers = {"Authorization": f"Bearer {token}"}
    device_id = "00000000-0000-4000-8000-000000000091"
    command_id = "55555555-5555-4555-8555-555555555555"
    with SessionLocal() as db:
        db.add(Device(id=device_id, machine_name="Control-PC", token_hash=hash_secret(token), client_version="0.3.2", last_seen_at=utcnow()))
        db.add(DeviceCommand(id=command_id, device_id=device_id, command_type="pause_monitoring", status="pending"))
        db.commit()

    heartbeat = client.post(f"/api/v1/devices/{device_id}/heartbeat", headers=headers, json={
        "machine_name": "Control-PC", "client_version": "0.3.2", "effective_interval_seconds": 900, "monitoring_enabled": False,
    })
    assert heartbeat.status_code == 200
    assert heartbeat.json()["monitoring_enabled"] is False
    pending = client.get(f"/api/v1/devices/{device_id}/commands/pending", headers=headers)
    assert pending.json() == {"pending": True, "command_id": command_id, "command_type": "pause_monitoring"}
    acknowledged = client.post(f"/api/v1/devices/{device_id}/commands/{command_id}/status", headers=headers, json={"status": "acknowledged"})
    completed = client.post(f"/api/v1/devices/{device_id}/commands/{command_id}/status", headers=headers, json={"status": "success"})
    assert acknowledged.status_code == 200
    assert completed.status_code == 200
    with SessionLocal() as db:
        assert db.get(Device, device_id).monitoring_enabled is False
        command = db.get(DeviceCommand, command_id)
        assert command.status == "success"
        assert command.acknowledged_at is not None
        assert command.completed_at is not None


def test_admin_named_pause_and_resume_commands_follow_heartbeat_state():
    client = admin_client()
    device_id = "00000000-0000-4000-8000-000000000088"
    token = "resume-token"
    headers = {"Authorization": f"Bearer {token}"}
    with SessionLocal() as db:
        db.add(Device(id=device_id, machine_name="Resume-PC", token_hash=hash_secret(token), client_version="0.3.3", last_seen_at=utcnow(), monitoring_enabled=True))
        db.commit()

    pause = client.post(f"/admin/devices/{device_id}/commands/pause_monitoring", follow_redirects=False)
    assert pause.status_code == 303
    assert pause.headers["location"] == f"/admin/devices/{device_id}/missions"
    pending_page = client.get(pause.headers["location"])
    assert "Acción pendiente. El estado se actualizará automáticamente al confirmarse en el dispositivo." in pending_page.text
    assert "window.location.reload()" in pending_page.text
    pending_pause = client.get(f"/api/v1/devices/{device_id}/commands/pending", headers=headers).json()
    assert pending_pause["command_type"] == "pause_monitoring"
    client.post(f"/api/v1/devices/{device_id}/commands/{pending_pause['command_id']}/status", headers=headers, json={"status": "success"})
    with SessionLocal() as db:
        assert db.get(Device, device_id).monitoring_enabled is False
    client.post(f"/api/v1/devices/{device_id}/heartbeat", headers=headers, json={"machine_name": "Resume-PC", "client_version": "0.3.3", "effective_interval_seconds": 900, "monitoring_enabled": False})

    resume = client.post(f"/admin/devices/{device_id}/commands/resume_monitoring", follow_redirects=False)
    assert resume.status_code == 303
    assert resume.headers["location"] == f"/admin/devices/{device_id}/missions"
    pending_resume = client.get(f"/api/v1/devices/{device_id}/commands/pending", headers=headers).json()
    assert pending_resume["command_type"] == "resume_monitoring"
    client.post(f"/api/v1/devices/{device_id}/commands/{pending_resume['command_id']}/status", headers=headers, json={"status": "success"})
    with SessionLocal() as db:
        assert db.get(Device, device_id).monitoring_enabled is True
    client.post(f"/api/v1/devices/{device_id}/heartbeat", headers=headers, json={"machine_name": "Resume-PC", "client_version": "0.3.3", "effective_interval_seconds": 900, "monitoring_enabled": True})

    with SessionLocal() as db:
        assert db.get(Device, device_id).monitoring_enabled is True


def test_device_auth_rejects_bad_token():
    client = TestClient(app)
    register(client)
    response = client.get(
        "/api/v1/devices/00000000-0000-4000-8000-000000000001/config",
        headers={"Authorization": "Bearer bad-token"},
    )
    assert response.status_code == 401


def test_pending_update_skips_redundant_current_version_command():
    client = TestClient(app)
    token = "queue-token"
    headers = {"Authorization": f"Bearer {token}"}
    device_id = "00000000-0000-4000-8000-000000000092"

    with SessionLocal() as db:
        db.add(Device(id=device_id, machine_name="Queue-PC", token_hash=hash_secret(token), client_version="0.9.1", last_seen_at=utcnow()))
        current = Release(version="0.9.1", filename="Guardian-0.9.1.zip", sha256="c" * 64, file_size=10)
        rollback = Release(version="0.9.0", filename="Guardian-0.9.0.zip", sha256="d" * 64, file_size=10)
        db.add(current)
        db.add(rollback)
        db.flush()
        stale = UpdateCommand(id="33333333-3333-4333-8333-333333333333", device_id=device_id, release_id=current.id, target_version=current.version, status="pending")
        wanted = UpdateCommand(id="44444444-4444-4444-8444-444444444444", device_id=device_id, release_id=rollback.id, target_version=rollback.version, status="pending")
        db.add(stale)
        db.add(wanted)
        db.commit()
        stale_id = stale.id
        wanted_id = wanted.id

    pending = client.get(f"/api/v1/devices/{device_id}/updates/pending", headers=headers)
    assert pending.status_code == 200
    assert pending.json()["pending"] is True
    assert pending.json()["command_id"] == wanted_id
    assert pending.json()["version"] == "0.9.0"

    with SessionLocal() as db:
        assert db.get(UpdateCommand, stale_id).status == "success"
        assert db.get(UpdateCommand, wanted_id).status == "pending"


def test_event_ingest_persists_and_deduplicates_by_event_id():
    client = TestClient(app)
    token = register(client)
    headers = {"Authorization": f"Bearer {token}"}
    device_id = "00000000-0000-4000-8000-000000000001"
    payload = {
        "device_id": device_id,
        "events": [
            {
                "event_id": "11111111-1111-4111-8111-111111111111",
                "occurred_at": "2026-08-16T12:00:00Z",
                "event_type": "MissionFailed",
                "client_version": "0.3.0",
                "payload": {"version": "0.3.0", "answer": "sample answer", "attempt": 1, "helpLevel": 0, "failureReason": "wrong_answer"},
            },
            {
                "event_id": "11111111-1111-4111-8111-111111111112",
                "occurred_at": "2026-08-16T12:00:01Z",
                "event_type": "MissionStarted",
                "client_version": "0.4.6",
                "payload": {"mission_id": "question-text-test", "question_text": "Hoy es martes. ¿Qué día fue ayer?", "attempt": 1},
            },
        ],
    }

    first = client.post("/api/v1/events", headers=headers, json=payload)
    second = client.post("/api/v1/events", headers=headers, json=payload)

    assert first.status_code == 200
    assert first.json()["accepted_event_ids"] == ["11111111-1111-4111-8111-111111111111", "11111111-1111-4111-8111-111111111112"]
    assert second.status_code == 200
    assert second.json()["accepted_event_ids"] == ["11111111-1111-4111-8111-111111111111", "11111111-1111-4111-8111-111111111112"]
    with SessionLocal() as db:
        events = db.query(DeviceEvent).filter(DeviceEvent.event_id == "11111111-1111-4111-8111-111111111111").all()
        assert len(events) == 1
        assert events[0].event_type == "MissionFailed"
        assert events[0].payload["version"] == "0.3.0"
        assert events[0].payload["answer"] == "sample answer"
        assert events[0].payload["attempt"] == 1
        assert events[0].payload["helpLevel"] == 0
        assert events[0].payload["failureReason"] == "wrong_answer"
        started = db.query(DeviceEvent).filter(DeviceEvent.event_id == "11111111-1111-4111-8111-111111111112").one()
        assert started.payload["question_text"] == "Hoy es martes. ¿Qué día fue ayer?"


def test_event_ingest_rejects_bad_device_token():
    client = TestClient(app)
    register(client)
    response = client.post("/api/v1/events", headers={"Authorization": "Bearer bad-token"}, json={
        "device_id": "00000000-0000-4000-8000-000000000001",
        "events": [],
    })
    assert response.status_code == 401


def admin_client():
    client = TestClient(app)
    client.cookies.set("guardian_admin", "admin")
    client.cookies.set("guardian_admin_sig", sign_username("admin"))
    return client


def test_admin_deletes_offline_device_and_dependencies():
    client = admin_client()
    device_id = "00000000-0000-4000-8000-000000000099"
    other_id = "00000000-0000-4000-8000-000000000098"

    with SessionLocal() as db:
        release = Release(version="0.9.9", filename="Guardian-0.9.9.zip", sha256="b" * 64, file_size=10)
        db.add(release)
        db.flush()
        db.add(Device(id=device_id, machine_name="Old-Test-PC", token_hash=hash_secret("old-token"), client_version="0.2.0", last_seen_at=None))
        db.add(Device(id=other_id, machine_name="Keep-PC", token_hash=hash_secret("keep-token"), client_version="0.2.0", last_seen_at=None))
        db.add(DeviceConfiguration(device_id=device_id, interval_seconds=900, version=1))
        db.add(UpdateCommand(device_id=device_id, release_id=release.id, target_version=release.version))
        db.commit()

    response = client.post(f"/admin/devices/{device_id}/delete", data={"confirm_device_id": device_id, "confirm_delete": "ELIMINAR"}, follow_redirects=False)
    assert response.status_code == 303

    with SessionLocal() as db:
        assert db.get(Device, device_id) is None
        assert db.query(DeviceConfiguration).filter(DeviceConfiguration.device_id == device_id).count() == 0
        assert db.query(UpdateCommand).filter(UpdateCommand.device_id == device_id).count() == 0
        assert db.get(Device, other_id) is not None


def test_admin_refuses_to_delete_online_device():
    client = admin_client()
    device_id = "00000000-0000-4000-8000-000000000097"

    with SessionLocal() as db:
        db.add(Device(id=device_id, machine_name="Online-PC", token_hash=hash_secret("online-token"), client_version="0.2.0", last_seen_at=utcnow()))
        db.commit()

    response = client.post(f"/admin/devices/{device_id}/delete", data={"confirm_device_id": device_id, "confirm_delete": "ELIMINAR"}, follow_redirects=False)
    assert response.status_code == 303

    with SessionLocal() as db:
        assert db.get(Device, device_id) is not None


def test_admin_updates_display_name_without_changing_machine_identity():
    client = admin_client()
    device_id = "00000000-0000-4000-8000-000000000094"

    with SessionLocal() as db:
        db.add(Device(id=device_id, machine_name="Hostname-PC", display_name="Old Name", token_hash=hash_secret("display-token"), client_version="0.3.1", last_seen_at=utcnow()))
        db.add(DeviceConfiguration(device_id=device_id, interval_seconds=900, version=1))
        db.commit()

    response = client.post(
        f"/admin/devices/{device_id}/config",
        data={"display_name": "PC Test", "interval_minutes": "20"},
        follow_redirects=False,
    )
    assert response.status_code == 303

    with SessionLocal() as db:
        device = db.get(Device, device_id)
        assert device.display_name == "PC Test"
        assert device.machine_name == "Hostname-PC"
        assert device.configuration.interval_seconds == 1200


def test_heartbeat_preserves_admin_display_name_and_dashboard_uses_it():
    client = admin_client()
    device_id = "00000000-0000-4000-8000-000000000087"
    token = "display-heartbeat-token"
    headers = {"Authorization": f"Bearer {token}"}
    with SessionLocal() as db:
        db.add(Device(id=device_id, machine_name="Original-PC", token_hash=hash_secret(token), client_version="0.3.3", last_seen_at=utcnow()))
        db.add(DeviceConfiguration(device_id=device_id, interval_seconds=900, version=1))
        db.commit()

    client.post(f"/admin/devices/{device_id}/config", data={"display_name": "PC Test", "interval_minutes": "15"}, follow_redirects=False)
    client.post(f"/api/v1/devices/{device_id}/heartbeat", headers=headers, json={"machine_name": "Renamed-Hostname", "client_version": "0.3.3", "effective_interval_seconds": 900, "monitoring_enabled": True})
    dashboard = client.get("/admin/")
    assert "PC Test" in dashboard.text
    with SessionLocal() as db:
        device = db.get(Device, device_id)
        assert device.display_name == "PC Test"
        assert device.machine_name == "Renamed-Hostname"


def test_admin_does_not_create_update_when_active_command_exists():
    client = admin_client()
    device_id = "00000000-0000-4000-8000-000000000093"

    with SessionLocal() as db:
        device = Device(id=device_id, machine_name="Update-PC", display_name="Update PC", token_hash=hash_secret("update-token"), client_version="0.8.0", last_seen_at=utcnow())
        release_a = Release(version="0.8.1", filename="Guardian-0.8.1.zip", sha256="e" * 64, file_size=10)
        release_b = Release(version="0.8.2", filename="Guardian-0.8.2.zip", sha256="f" * 64, file_size=10)
        db.add(device)
        db.add(release_a)
        db.add(release_b)
        db.flush()
        db.add(UpdateCommand(device_id=device_id, release_id=release_a.id, target_version=release_a.version, status="pending"))
        db.commit()
        release_b_id = release_b.id

    response = client.post(f"/admin/devices/{device_id}/updates", data={"release_id": release_b_id}, follow_redirects=False)
    assert response.status_code == 303

    with SessionLocal() as db:
        assert db.query(UpdateCommand).filter(UpdateCommand.device_id == device_id).count() == 1


def test_admin_cancels_only_pending_update_and_prevents_duplicate_device_commands():
    client = admin_client()
    device_id = "00000000-0000-4000-8000-000000000090"
    with SessionLocal() as db:
        device = Device(id=device_id, machine_name="Cancel-PC", token_hash=hash_secret("cancel-token"), client_version="0.3.2", last_seen_at=utcnow())
        release = Release(version="0.9.8", filename="Guardian-0.9.8.zip", sha256="a" * 64, file_size=10)
        db.add_all([device, release])
        db.flush()
        pending = UpdateCommand(device_id=device_id, release_id=release.id, target_version=release.version, status="pending")
        started = UpdateCommand(device_id=device_id, release_id=release.id, target_version="0.9.7", status="installing")
        db.add_all([pending, started])
        db.commit()
        pending_id = pending.id
        started_id = started.id

    client.post(f"/admin/devices/{device_id}/updates/{pending_id}/cancel", follow_redirects=False)
    client.post(f"/admin/devices/{device_id}/updates/{started_id}/cancel", follow_redirects=False)
    client.post(f"/admin/devices/{device_id}/commands", data={"command_type": "pause_monitoring"}, follow_redirects=False)
    client.post(f"/admin/devices/{device_id}/commands", data={"command_type": "pause_monitoring"}, follow_redirects=False)
    with SessionLocal() as db:
        assert db.get(UpdateCommand, pending_id).status == "cancelled"
        assert db.get(UpdateCommand, started_id).status == "installing"
        assert db.query(DeviceCommand).filter(DeviceCommand.device_id == device_id).count() == 1


def test_configuration_keeps_offline_quick_actions_visible_but_disabled():
    client = admin_client()
    device_id = "00000000-0000-4000-8000-000000000089"
    with SessionLocal() as db:
        device = Device(id=device_id, machine_name="Offline-PC", token_hash=hash_secret("offline-update-token"), client_version="0.3.2", last_seen_at=None)
        release = Release(version="0.9.6", filename="Guardian-0.9.6.zip", sha256="f" * 64, file_size=10)
        db.add_all([device, release])
        db.flush()
        db.add(UpdateCommand(device_id=device_id, release_id=release.id, target_version=release.version, status="pending"))
        db.commit()

    response = client.get(f"/admin/devices/{device_id}/missions")
    assert response.status_code == 200
    assert "Pausar misiones" in response.text
    assert "Probar misión ahora" in response.text
    assert "disabled" in response.text
    assert 'action="/admin/devices/' + device_id + '/commands/pause_monitoring"' in response.text


def test_configuration_quick_actions_follow_guardian_state_with_prerelease_versions():
    client = admin_client()
    active_id = "00000000-0000-4000-8000-000000000181"
    paused_id = "00000000-0000-4000-8000-000000000182"
    offline_id = "00000000-0000-4000-8000-000000000183"
    with SessionLocal() as db:
        db.add_all([
            Device(id=active_id, machine_name="Quick-Active-PC", display_name="Acciones activas", token_hash=hash_secret("quick-active-token"), client_version="0.4.5-staging-stage3-iteration4", last_seen_at=utcnow(), monitoring_enabled=True),
            Device(id=paused_id, machine_name="Quick-Paused-PC", display_name="Acciones pausadas", token_hash=hash_secret("quick-paused-token"), client_version="0.4.5-staging-stage3-iteration4", last_seen_at=utcnow(), monitoring_enabled=False),
            Device(id=offline_id, machine_name="Quick-Offline-PC", display_name="Acciones offline", token_hash=hash_secret("quick-offline-token"), client_version="0.4.5-staging-stage3-iteration4", last_seen_at=None, monitoring_enabled=True),
        ])
        db.commit()

    active_page = client.get(f"/admin/devices/{active_id}/missions")
    assert "Online · Activo" in active_page.text
    assert '<button type="submit" >Pausar misiones</button>' in active_page.text
    assert '<button type="submit" >Probar misión ahora</button>' in active_page.text

    paused_page = client.get(f"/admin/devices/{paused_id}/missions")
    assert "Online · Pausado" in paused_page.text
    assert '<button type="submit" >Reanudar misiones</button>' in paused_page.text
    assert '<button type="submit" >Probar misión ahora</button>' in paused_page.text

    offline_page = client.get(f"/admin/devices/{offline_id}/missions")
    assert "Offline" in offline_page.text
    assert '<button type="submit" disabled>Pausar misiones</button>' in offline_page.text
    assert '<button type="submit" disabled>Probar misión ahora</button>' in offline_page.text
    actions_start = offline_page.text.index('device-actions')
    actions_end = offline_page.text.index('<details class="card config-card"', actions_start)
    assert 'form="device-config-form"' not in offline_page.text[actions_start:actions_end]


def test_configuration_update_status_uses_device_state_and_human_messages():
    client = admin_client()
    offline_id = "00000000-0000-4000-8000-000000000184"
    completed_id = "00000000-0000-4000-8000-000000000185"
    with SessionLocal() as db:
        release = Release(version="0.4.5-staging-update-status", filename="Guardian-0.4.5-staging-update-status.zip", sha256="7" * 64, file_size=10)
        offline = Device(id=offline_id, machine_name="Update-Offline-PC", token_hash=hash_secret("update-offline-token"), client_version="0.4.4", last_seen_at=None)
        completed = Device(id=completed_id, machine_name="Update-Completed-PC", token_hash=hash_secret("update-completed-token"), client_version="0.4.5-staging-update-status", last_seen_at=utcnow())
        db.add_all([release, offline, completed])
        db.flush()
        db.add_all([
            UpdateCommand(device_id=offline_id, release_id=release.id, target_version=release.version, status="pending"),
            UpdateCommand(device_id=completed_id, release_id=release.id, target_version=release.version, status="success", error_message="already running target version"),
        ])
        db.commit()

    pending = client.get(f"/admin/devices/{offline_id}/missions")
    assert "Estado:" in pending.text
    assert "Offline" in pending.text
    assert "Estado actual de actualización: Pendiente" in pending.text
    assert "Pendiente — el dispositivo está offline. La actualización comenzará cuando vuelva a conectarse." in pending.text
    assert "window.setTimeout(function () { window.location.reload(); }, 3000);" in pending.text

    completed = client.get(f"/admin/devices/{completed_id}/missions")
    assert "Online · Activo" in completed.text
    assert "Esta release ya está instalada." in completed.text
    assert "Estado actual de actualización" not in completed.text
    assert "Última actualización registrada: Completada" in completed.text
    assert "already running target version" not in completed.text
    assert "window.setTimeout(function () { window.location.reload(); }, 3000);" not in completed.text


def test_configuration_update_state_is_scoped_to_selected_release():
    client = admin_client()
    device_id = "00000000-0000-4000-8000-000000000186"
    with SessionLocal() as db:
        device = Device(id=device_id, machine_name="Update-State-PC", token_hash=hash_secret("update-state-token"), client_version="9.8.51", last_seen_at=utcnow())
        installed = Release(version="9.8.51", filename="Guardian-9.8.51.zip", sha256="9" * 64, file_size=10)
        next_release = Release(version="9.8.52", filename="Guardian-9.8.52.zip", sha256="a" * 64, file_size=10)
        historical = Release(version="9.8.50", filename="Guardian-9.8.50.zip", sha256="b" * 64, file_size=10)
        db.add_all([device, installed, next_release, historical])
        db.flush()
        db.add(UpdateCommand(device_id=device_id, release_id=historical.id, target_version=historical.version, status="success"))
        db.commit()
        installed_id = installed.id
        next_release_id = next_release.id

    current_page = client.get(f"/admin/devices/{device_id}/missions?release_id={installed_id}")
    assert "Esta release ya está instalada." in current_page.text
    assert '<button id="update-button" disabled>Actualizar</button>' in current_page.text
    assert "Estado actual de actualización" not in current_page.text
    assert "Última actualización registrada: Completada" in current_page.text
    assert "Versión destino: 9.8.50" in current_page.text

    same_release = client.post(f"/admin/devices/{device_id}/updates", data={"release_id": installed_id}, follow_redirects=False)
    assert same_release.headers["location"] == f"/admin/devices/{device_id}/missions?release_id={installed_id}"
    with SessionLocal() as db:
        assert db.query(UpdateCommand).filter(UpdateCommand.device_id == device_id).count() == 1

    with SessionLocal() as db:
        db.add(UpdateCommand(device_id=device_id, release_id=next_release_id, target_version="9.8.52", status="downloading"))
        db.commit()

    other_release_page = client.get(f"/admin/devices/{device_id}/missions?release_id={installed_id}")
    assert "Estado actual de actualización" not in other_release_page.text
    assert "window.setTimeout(function () { window.location.reload(); }, 3000);" not in other_release_page.text

    active_page = client.get(f"/admin/devices/{device_id}/missions?release_id={next_release_id}")
    assert 'data-update-active="true"' in active_page.text
    assert "Estado actual de actualización: Descargando" in active_page.text
    assert "Versión destino: 9.8.52" in active_page.text
    assert "Última actualización registrada: Completada" in active_page.text
    assert "window.setTimeout(function () { window.location.reload(); }, 3000);" in active_page.text


def test_admin_keeps_release_details_outside_dashboard():
    client = admin_client()
    device_id = "00000000-0000-4000-8000-000000000086"
    with SessionLocal() as db:
        device = Device(id=device_id, machine_name="Release-PC", token_hash=hash_secret("release-token"), client_version="0.9.4", last_seen_at=utcnow())
        release = Release(version="0.9.5", filename="Guardian-0.9.5.zip", sha256="d" * 64, file_size=10, release_notes="Short release description")
        db.add_all([device, release])
        db.flush()
        db.add(UpdateCommand(device_id=device_id, release_id=release.id, target_version=release.version, status="success", error_message="already running target version"))
        db.commit()

    dashboard = client.get("/admin/")
    releases = client.get("/admin/releases")
    assert "Short release description" not in dashboard.text
    assert "Short release description" in releases.text
    assert "Actualización success" not in dashboard.text


def test_admin_bad_delete_confirmation_redirects_without_deleting():
    client = admin_client()
    device_id = "00000000-0000-4000-8000-000000000096"

    with SessionLocal() as db:
        db.add(Device(id=device_id, machine_name="Confirm-PC", token_hash=hash_secret("confirm-token"), client_version="0.2.0", last_seen_at=None))
        db.commit()

    response = client.post(f"/admin/devices/{device_id}/delete", data={"confirm_device_id": "bad", "confirm_delete": "ELIMINAR"}, follow_redirects=False)
    assert response.status_code == 303

    with SessionLocal() as db:
        assert db.get(Device, device_id) is not None


def test_admin_activity_filters_device_events():
    client = admin_client()
    device_id = "00000000-0000-4000-8000-000000000095"

    with SessionLocal() as db:
        db.add(Device(id=device_id, machine_name="Activity-PC", token_hash=hash_secret("activity-token"), client_version="0.3.1", last_seen_at=utcnow()))
        db.add(DeviceEvent(
            event_id="22222222-2222-4222-8222-222222222222",
            device_id=device_id,
            occurred_at=utcnow(),
            received_at=utcnow(),
            event_type="UpdateCompleted",
            client_version="0.3.1",
            payload={"targetVersion": "0.3.1", "result": "success"},
        ))
        db.commit()

    response = client.get(f"/admin/devices/{device_id}/activity?period=all&group=updates")
    assert response.status_code == 200
    assert "UpdateCompleted" in response.text
    assert "targetVersion" in response.text
    assert "Hora local" in response.text


def test_admin_activity_includes_progressive_help_events_without_schema_change():
    client = admin_client()
    device_id = "00000000-0000-4000-8000-00000000a103"
    payload = {"mission_id": "sample-mission", "skill_level_id": "functional_1", "skill_id": "instruction_vocabulary", "variant_id": "vocab_before", "attempt": 2, "max_help_level": 2, "help_requests_count": 2, "writing_correction_count": 0}
    with SessionLocal() as db:
        db.add(Device(id=device_id, machine_name="Help-Activity-PC", token_hash=hash_secret("help-activity-token"), client_version="0.4.3-staging-comprehension-help", last_seen_at=utcnow()))
        db.add(DeviceEvent(event_id="33333333-3333-4333-8333-333333333333", device_id=device_id, occurred_at=utcnow(), received_at=utcnow(), event_type="MissionHelpRequested", client_version="0.4.3-staging-comprehension-help", payload={**payload, "help_level": 2}))
        db.add(DeviceEvent(event_id="44444444-4444-4444-8444-444444444444", device_id=device_id, occurred_at=utcnow(), received_at=utcnow(), event_type="MissionWritingHintShown", client_version="0.4.3-staging-comprehension-help", payload={**payload, "writing_hint_stage": 1, "had_orthographic_error": True, "writing_answer_revealed": False}))
        db.commit()

    response = client.get(f"/admin/devices/{device_id}/activity?period=all&group=missions")
    assert response.status_code == 200
    assert "MissionHelpRequested" in response.text
    assert "MissionWritingHintShown" in response.text
    assert "&quot;input&quot;" not in response.text


def test_admin_mission_configuration_and_private_profile_are_device_scoped():
    client = admin_client()
    device_id = "00000000-0000-4000-8000-00000000abc1"
    token = "mission-profile-token"
    with SessionLocal() as db:
        db.add(Device(id=device_id, machine_name="Mission-Test-PC", token_hash=hash_secret(token), client_version="0.3.3", last_seen_at=utcnow()))
        db.add(DeviceConfiguration(device_id=device_id, interval_seconds=900, version=1))
        db.commit()

    page = client.get(f"/admin/devices/{device_id}/missions")
    assert page.status_code == 200
    assert "Comprensión funcional" in page.text
    assert "data-tooltip" in page.text
    assert "Vocabulario de consignas" in page.text
    assert "aria-label" in page.text

    response = client.post(f"/admin/devices/{device_id}/config", data={
        "display_name": "Mission Test", "interval_minutes": "15", "missions_submitted": "1",
        "enabled_skills": ["math.basic_operations_1.subtraction", "comprehension.functional_1.identity", "comprehension.functional_1.instruction_vocabulary"],
        "preferred_name": "Tomi", "first_name": "Tomás", "middle_name": "", "last_name": "Pérez", "birth_date": "2010-08-23",
    }, follow_redirects=False)
    assert response.status_code == 303
    assert response.headers["location"] == f"/admin/devices/{device_id}/missions?saved=1"
    saved_page = client.get(response.headers["location"])
    assert "✓ Guardado" in saved_page.text
    assert "data-config-section=\"device\"" in saved_page.text
    assert "window.sessionStorage.getItem" in saved_page.text
    assert "window.sessionStorage.removeItem" in saved_page.text
    assert "form.addEventListener('submit'" in saved_page.text
    assert 'id="save-confirmation"' in saved_page.text
    assert "clearSaveConfirmation" in saved_page.text
    assert "querySelector('summary').addEventListener('pointerdown'" in saved_page.text

    headers = {"Authorization": f"Bearer {token}"}
    remote = client.get(f"/api/v1/devices/{device_id}/config", headers=headers)
    assert remote.status_code == 200
    assert remote.json()["mission_config"]["EnabledSkills"] == ["math.basic_operations_1.subtraction", "comprehension.functional_1.identity", "comprehension.functional_1.instruction_vocabulary"]
    assert remote.json()["mission_config"]["PrivateProfile"]["FirstName"] == "Tomás"

    with SessionLocal() as db:
        profile = db.get(DeviceMissionProfile, device_id)
        assert profile is not None
        assert profile.birth_date == "2010-08-23"


def test_configuration_update_controls_are_stable_and_reuse_update_command_flow():
    client = admin_client()
    device_id = "00000000-0000-4000-8000-00000000abc8"
    with SessionLocal() as db:
        device = Device(id=device_id, machine_name="Configuration-Update-PC", token_hash=hash_secret("configuration-update-token"), client_version="0.4.4", last_seen_at=utcnow())
        db.add(device)
        db.add(DeviceConfiguration(device_id=device_id, interval_seconds=900, version=1))
        previous_active = [(release.id, release.is_active) for release in db.query(Release).all()]
        for release in db.query(Release).all():
            release.is_active = False
        db.commit()

    no_releases = client.get(f"/admin/devices/{device_id}/missions")
    assert no_releases.status_code == 200
    assert 'aria-current="page">Configuración' not in no_releases.text
    assert "Releases disponibles" in no_releases.text
    assert "Release objetivo" not in no_releases.text
    assert "Dispositivo:" not in no_releases.text
    assert "No hay releases disponibles" in no_releases.text
    assert "No hay releases publicadas para este entorno." in no_releases.text
    assert "<button disabled>Actualizar</button>" in no_releases.text
    assert 'class="skill-label-tooltip" tabindex="0"' in no_releases.text
    assert 'class="check-control skill-control" tabindex="0"' not in no_releases.text

    with SessionLocal() as db:
        for release_id, is_active in previous_active:
            db.get(Release, release_id).is_active = is_active
        release = Release(version="0.4.5-staging-configuration", filename="Guardian-0.4.5-staging-configuration.zip", sha256="8" * 64, file_size=10, release_notes="Release de prueba para Configuración")
        db.add(release)
        db.commit()
        release_id = release.id

    with_release = client.get(f"/admin/devices/{device_id}/missions")
    assert f'value="{release_id}"' in with_release.text
    assert "Release de prueba para Configuración" in with_release.text
    assert '<button disabled>Actualizar</button>' not in with_release.text

    response = client.post(f"/admin/devices/{device_id}/updates", data={"release_id": release_id}, follow_redirects=False)
    assert response.status_code == 303
    with SessionLocal() as db:
        command = db.query(UpdateCommand).filter(UpdateCommand.device_id == device_id).one()
        assert command.release_id == release_id
        assert command.target_version == "0.4.5-staging-configuration"


def test_admin_rejects_zero_enabled_mission_skills_without_saving():
    client = admin_client()
    device_id = "00000000-0000-4000-8000-00000000abc2"
    with SessionLocal() as db:
        db.add(Device(id=device_id, machine_name="Zero-Skills-Test-PC", token_hash=hash_secret("zero-skills-token"), client_version="0.4.0", last_seen_at=utcnow()))
        db.add(DeviceConfiguration(device_id=device_id, interval_seconds=900, version=4, mission_config={"enabledSkills": ["math.basic_operations_1.subtraction"]}))
        db.commit()

    response = client.post(f"/admin/devices/{device_id}/config", data={
        "display_name": "Zero Skills", "interval_minutes": "15", "missions_submitted": "1",
    })
    assert response.status_code == 422
    assert "Seleccioná al menos una habilidad antes de guardar." in response.text

    with SessionLocal() as db:
        config = db.query(DeviceConfiguration).filter(DeviceConfiguration.device_id == device_id).one()
        assert config.mission_config == {"enabledSkills": ["math.basic_operations_1.subtraction"]}
        assert config.version == 4


def test_admin_configuration_persists_display_interval_and_timezone():
    client = admin_client()
    device_id = "00000000-0000-4000-8000-00000000abc3"
    token = "timezone-config-token"
    with SessionLocal() as db:
        db.add(Device(id=device_id, machine_name="Timezone-PC", token_hash=hash_secret(token), client_version="0.4.1", last_seen_at=utcnow()))
        db.add(DeviceConfiguration(device_id=device_id, interval_seconds=900, version=1))
        db.commit()

    response = client.post(f"/admin/devices/{device_id}/config", data={
        "display_name": "PC de prueba", "interval_minutes": "20", "timezone_name": "America/Argentina/Buenos_Aires",
        "missions_submitted": "1", "enabled_skills": ["math.basic_operations_1.addition"],
    }, follow_redirects=False)
    assert response.status_code == 303
    remote = client.get(f"/api/v1/devices/{device_id}/config", headers={"Authorization": f"Bearer {token}"})
    assert remote.status_code == 200
    assert remote.json()["timezone"] == "UTC"
    with SessionLocal() as db:
        device = db.get(Device, device_id)
        assert device.display_name == "PC de prueba"
        assert device.configuration.interval_seconds == 1200


def test_activity_uses_device_timezone_and_hides_technical_events_by_default():
    client = admin_client()
    device_id = "00000000-0000-4000-8000-00000000abc4"
    with SessionLocal() as db:
        db.add(Device(id=device_id, machine_name="Activity-Timezone-PC", token_hash=hash_secret("activity-timezone-token"), client_version="0.4.1", last_seen_at=utcnow()))
        db.add(DeviceConfiguration(device_id=device_id, interval_seconds=900, timezone="America/Argentina/Buenos_Aires", version=1))
        db.add_all([
            DeviceEvent(event_id="30000000-0000-4000-8000-000000000001", device_id=device_id, occurred_at=datetime(2026, 8, 23, 3, 30, tzinfo=timezone.utc), received_at=utcnow(), event_type="MissionSolved", payload={"mission_id": "activity-1", "category_id": "math", "level_id": "basic_operations_1", "skill_id": "addition", "attempt": 1}),
            DeviceEvent(event_id="30000000-0000-4000-8000-000000000002", device_id=device_id, occurred_at=datetime(2026, 8, 23, 3, 31, tzinfo=timezone.utc), received_at=utcnow(), event_type="HeartbeatSent", payload={}),
        ])
        db.commit()

    default = client.get(f"/admin/devices/{device_id}/activity?period=all")
    technical = client.get(f"/admin/devices/{device_id}/activity?period=all&technical=true")
    assert default.status_code == 200
    assert "America/Argentina/Buenos_Aires" in default.text
    assert "23/08/2026" in default.text
    assert "HeartbeatSent" not in default.text
    assert "HeartbeatSent" in technical.text


def test_metrics_count_unique_missions_attempts_scopes_legacy_and_variants():
    client = admin_client()
    assert QUESTION_LABELS["generated"] == "Operación matemática"
    device_id = "00000000-0000-4000-8000-00000000abc5"
    with SessionLocal() as db:
        db.add(Device(id=device_id, machine_name="Metrics-PC", token_hash=hash_secret("metrics-token"), client_version="0.4.1", last_seen_at=utcnow()))
        db.add(DeviceConfiguration(device_id=device_id, interval_seconds=900, timezone="UTC", version=1))
        events = [
            ("40000000-0000-4000-8000-000000000001", "MissionStarted", "m-first", 1, "math", "basic_operations_1", "addition", "a1", datetime(2026, 8, 20, 10, 0, tzinfo=timezone.utc)),
            ("40000000-0000-4000-8000-000000000002", "MissionSolved", "m-first", 1, "math", "basic_operations_1", "addition", "a1", datetime(2026, 8, 20, 10, 0, 12, tzinfo=timezone.utc)),
            ("40000000-0000-4000-8000-000000000003", "MissionStarted", "m-third", 1, "comprehension", "functional_1", "calendar", "c1", datetime(2026, 8, 21, 10, 0, tzinfo=timezone.utc)),
            ("40000000-0000-4000-8000-000000000004", "MissionFailed", "m-third", 1, "comprehension", "functional_1", "calendar", "c1", datetime(2026, 8, 21, 10, 0, 2, tzinfo=timezone.utc)),
            ("40000000-0000-4000-8000-000000000005", "MissionFailed", "m-third", 2, "comprehension", "functional_1", "calendar", "c1", datetime(2026, 8, 21, 10, 0, 4, tzinfo=timezone.utc)),
            # Reintento retransmitido: no debe aumentar la cantidad de intentos.
            ("40000000-0000-4000-8000-000000000006", "MissionFailed", "m-third", 2, "comprehension", "functional_1", "calendar", "c1", datetime(2026, 8, 21, 10, 0, 5, tzinfo=timezone.utc)),
            ("40000000-0000-4000-8000-000000000007", "MissionSolved", "m-third", 3, "comprehension", "functional_1", "calendar", "c1", datetime(2026, 8, 21, 10, 0, 8, tzinfo=timezone.utc)),
            ("40000000-0000-4000-8000-000000000008", "MissionSolved", "legacy-mission", 1, "math", "basic_operations_1", "subtraction", "s1", datetime(2026, 8, 22, 10, 0, tzinfo=timezone.utc)),
        ]
        for event_id, event_type, mission_id, attempt, category, level, skill, variant, occurred_at in events:
            payload = {"missionId": mission_id, "attempt": attempt, "category_id": category, "level_id": level, "skill_id": skill, "variant_id": variant} if mission_id == "legacy-mission" else {"mission_id": mission_id, "attempt": attempt, "category_id": category, "level_id": level, "skill_id": skill, "variant_id": variant}
            db.add(DeviceEvent(event_id=event_id, device_id=device_id, occurred_at=occurred_at, received_at=utcnow(), event_type=event_type, payload=payload))
        db.commit()

    with SessionLocal() as db:
        data = dashboard_data(db, db.get(Device, device_id), "all", None, None, None, None, None)
    assert data["summary"]["missions"] == 3
    assert data["summary"]["first_attempt"] == 2
    assert data["summary"]["total_attempts"] == 5
    assert data["summary"]["average_attempts"] == round(5 / 3, 2)
    assert "comprehension_help" not in data["summary"]
    assert data["summary"]["median_seconds"] == 10.0
    assert {row["label"] for row in data["rows"]} == {"Matemática", "Comprensión"}

    response = client.get(f"/admin/devices/{device_id}/metrics?period=all")
    assert response.status_code == 200
    assert "Misiones resueltas" in response.text
    assert "Categoría" in response.text
    assert "Misiones por categoría" not in response.text
    assert "Ayuda de comprensión" not in response.text
    assert "1.er intento" not in response.text
    assert "Comprensión" in response.text
    skill = client.get(f"/admin/devices/{device_id}/metrics?period=all&category=comprehension&level=functional_1&skill=calendar")
    assert skill.status_code == 200
    assert "Preguntas o consignas" not in skill.text
    assert "Ayuda de comprensión" in skill.text
    assert "Máximo apoyo de comprensión" not in skill.text
    assert "Máximo apoyo ortográfico" not in skill.text
    comprehension = client.get(f"/admin/devices/{device_id}/metrics?period=all&category=comprehension")
    assert "Máximo apoyo de comprensión" in comprehension.text
    assert "Máximo apoyo ortográfico" in comprehension.text

    with SessionLocal() as db:
        math_data = dashboard_data(db, db.get(Device, device_id), "all", None, None, "math", None, None)
    assert math_data["summary"]["first_attempt"] == 2
    assert math_data["summary"]["second_attempt"] == 0
    assert math_data["summary"]["third_plus"] == 0
    math = client.get(f"/admin/devices/{device_id}/metrics?period=all&category=math&level=basic_operations_1&skill=addition")
    assert "2do intento" in math.text
    assert "3+ intentos" in math.text
    assert "Ayuda de comprensión" not in math.text
    assert "Apoyo ortográfico" not in math.text
    assert "Ayuda máxima" not in math.text


def test_metrics_keep_historical_fields_unknown_and_rebuild_real_executions():
    device_id = "00000000-0000-4000-8000-00000000abc6"
    base = datetime(2026, 9, 1, 18, 42, tzinfo=timezone.utc)
    common = {"category_id": "comprehension", "level_id": "functional_1", "skill_id": "temporal_relations", "variant_id": "yesterday_weekday"}
    with SessionLocal() as db:
        db.add(Device(id=device_id, machine_name="Metrics-detail-PC", token_hash=hash_secret("metrics-detail-token"), client_version="0.4.6", last_seen_at=utcnow()))
        db.add(DeviceConfiguration(device_id=device_id, interval_seconds=900, timezone="UTC", version=1))
        items = [
            ("501", "MissionStarted", "historic", 0, {**common, "mission_id": "historic"}),
            ("502", "MissionSolved", "historic", 1, {**common, "mission_id": "historic", "attempt": 1}),
            ("503", "MissionStarted", "help", 2, {**common, "mission_id": "help", "question_text": "Hoy es martes. ¿Qué día fue ayer?", "attempt": 1, "max_help_level": 0, "had_orthographic_error": False, "writing_correction_count": 0, "writing_answer_revealed": False}),
            ("504", "MissionFailed", "help", 4, {**common, "mission_id": "help", "attempt": 1, "answer": "domingo", "failureReason": "wrong_answer", "max_help_level": 0, "had_orthographic_error": False, "writing_correction_count": 0, "writing_answer_revealed": False}),
            ("505", "MissionHelpRequested", "help", 5, {**common, "mission_id": "help", "attempt": 2, "help_level": 1, "max_help_level": 1, "help_requests_count": 1, "had_orthographic_error": False, "writing_correction_count": 0, "writing_answer_revealed": False}),
            ("506", "MissionSolved", "help", 7, {**common, "mission_id": "help", "attempt": 2, "answer": "lunes", "max_help_level": 1, "help_requests_count": 1, "had_orthographic_error": False, "writing_correction_count": 0, "writing_answer_revealed": False}),
            ("507", "MissionStarted", "writing", 8, {**common, "mission_id": "writing", "attempt": 1, "max_help_level": 0, "had_orthographic_error": False, "writing_correction_count": 0, "writing_answer_revealed": False}),
            ("508", "MissionFailed", "writing", 9, {**common, "mission_id": "writing", "attempt": 1, "answer": "lundes", "failureReason": "orthographic_error", "max_help_level": 0, "had_orthographic_error": True, "writing_correction_count": 1, "writing_answer_revealed": False}),
            ("509", "MissionWritingHintShown", "writing", 10, {**common, "mission_id": "writing", "attempt": 1, "writing_hint_stage": 1, "max_help_level": 0, "had_orthographic_error": True, "writing_correction_count": 1, "writing_answer_revealed": False}),
            ("510", "MissionSolved", "writing", 11, {**common, "mission_id": "writing", "attempt": 2, "answer": "lunes", "max_help_level": 0, "had_orthographic_error": True, "writing_correction_count": 1, "writing_answer_revealed": False}),
        ]
        for suffix, event_type, _, seconds, payload in items:
            db.add(DeviceEvent(event_id=f"50000000-0000-4000-8000-000000000{suffix}", device_id=device_id, occurred_at=base + timedelta(seconds=seconds), received_at=utcnow(), event_type=event_type, payload=payload))
        db.commit()

    with SessionLocal() as db:
        data = dashboard_data(db, db.get(Device, device_id), "all", None, None, "comprehension", "functional_1", "temporal_relations")
    assert data["summary"]["comprehension_help"] == {"numerator": 1, "valid_missions": 2, "percentage": 50.0}
    assert data["summary"]["orthographic_support"] == {"numerator": 1, "valid_missions": 2, "percentage": 50.0}
    assert next(row for row in data["summary"]["help_distribution"] if row["label"] == "Sin dato")["missions"] == 1
    assert data["rows"][0]["label"] == "¿Qué día fue ayer?"
    executions = data["executions_by_variant"]["yesterday_weekday"]
    help_execution = next(item for item in executions if item["mission_id"] == "help")
    assert help_execution["question_text"] == "Hoy es martes. ¿Qué día fue ayer?"
    assert [item["kind"] for item in help_execution["timeline"]] == ["attempt", "comprehension_help", "attempt"]
    assert [item.get("answer") for item in help_execution["timeline"] if item["kind"] == "attempt"] == ["domingo", "lunes"]
    writing_execution = next(item for item in executions if item["mission_id"] == "writing")
    assert writing_execution["question_text"] is None
    assert writing_execution["question_label"] == "¿Qué día fue ayer?"
    assert writing_execution["timeline"][1]["label"] == "Apoyo de escritura"
    historical = next(item for item in executions if item["mission_id"] == "historic")
    assert historical["question_text"] is None
    assert historical["max_help_label"] == "Sin dato"
