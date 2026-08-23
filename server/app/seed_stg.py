"""Idempotent, fictional STG data. Never run this against PROD."""
import uuid
from datetime import timedelta

from server.app.config import settings
from server.app.db import SessionLocal
from server.app.models import Device, DeviceConfiguration, DeviceEvent, DeviceMissionProfile
from server.app.security import hash_secret, utcnow


SEED_NAMESPACE = uuid.UUID("4a47d9b4-5d2f-4ba4-82b3-b2c389d1b5dc")
DEVICES = [
    ("stg-online-active", "STG-ONLINE-ACTIVE", "Demo Online Activo", True, 0, 300,
     ["math.basic_operations_1.addition", "comprehension.functional_1.calendar"]),
    ("stg-online-paused", "STG-ONLINE-PAUSED", "Demo Online Pausado", False, 1, 900,
     ["math.basic_operations_1.subtraction", "comprehension.functional_1.identity"]),
    ("stg-offline", "STG-OFFLINE", "Demo Offline", True, 7200, 1800,
     ["math.basic_operations_1.multiplication", "comprehension.functional_1.seasons"]),
]


def stable_id(value: str) -> str:
    return str(uuid.uuid5(SEED_NAMESPACE, value))


def event(device_id: str, index: int, occurred_at, event_type: str, payload: dict) -> DeviceEvent:
    return DeviceEvent(
        id=stable_id(f"row:{device_id}:{index}"), event_id=stable_id(f"event:{device_id}:{index}"),
        device_id=device_id, occurred_at=occurred_at, received_at=occurred_at,
        event_type=event_type, client_version="0.4.1", payload=payload,
    )


def main() -> None:
    if settings.guardian_environment.strip().upper() != "STG":
        raise SystemExit("Refusing to seed: GUARDIAN_ENVIRONMENT must be STG.")

    now = utcnow()
    seeded_ids = [stable_id(key) for key, *_ in DEVICES]
    with SessionLocal() as db:
        # Replace only the fixed fictional seed fixtures, making reruns deterministic.
        db.query(DeviceEvent).filter(DeviceEvent.device_id.in_(seeded_ids)).delete(synchronize_session=False)
        db.query(DeviceMissionProfile).filter(DeviceMissionProfile.device_id.in_(seeded_ids)).delete(synchronize_session=False)
        db.query(DeviceConfiguration).filter(DeviceConfiguration.device_id.in_(seeded_ids)).delete(synchronize_session=False)
        db.query(Device).filter(Device.id.in_(seeded_ids)).delete(synchronize_session=False)

        for ordinal, (key, hostname, display, enabled, age_seconds, interval, skills) in enumerate(DEVICES):
            device_id = stable_id(key)
            last_seen = now - timedelta(seconds=age_seconds)
            db.add(Device(
                id=device_id, machine_name=hostname, display_name=display,
                token_hash=hash_secret(f"seed-token-{key}"), client_version="0.4.1",
                last_seen_at=last_seen, registered_at=now - timedelta(days=7 + ordinal),
                monitoring_enabled=enabled,
            ))
            db.add(DeviceConfiguration(device_id=device_id, version=ordinal + 2, interval_seconds=interval,
                                       mission_config={"enabledSkills": skills}))
            db.add(DeviceMissionProfile(device_id=device_id, preferred_name=f"Usuario Demo {ordinal + 1}"))

            index = 0
            for day in range(5):
                base = now - timedelta(days=day, hours=ordinal)
                skill = skills[day % len(skills)]
                category, level, variant = skill.split(".", 2)
                mission = stable_id(f"mission:{key}:{day}")
                payload = {"mission_id": mission, "category_id": category, "level_id": level,
                           "skill_id": skill, "variant_id": f"{variant}_v{day % 3 + 1}"}
                db.add(event(device_id, index, base - timedelta(minutes=5), "MissionStarted", payload)); index += 1
                attempts = (day % 3) + 1
                for attempt in range(1, attempts):
                    db.add(event(device_id, index, base - timedelta(minutes=4 - attempt), "MissionFailed", {**payload, "attempt": attempt})); index += 1
                db.add(event(device_id, index, base, "MissionSolved", {**payload, "attempt": attempts})); index += 1
                db.add(event(device_id, index, base + timedelta(minutes=1), "Heartbeat", {"monitoring_enabled": enabled})); index += 1

            technical = [
                ("RemoteConfigApplied", {"version": ordinal + 2}),
                ("MonitoringPaused" if not enabled else "MonitoringResumed", {"source": "seed"}),
                ("TriggerMissionCommandReceived", {"command_id": stable_id(f"command:{key}")}),
                ("UpdateCompleted", {"previous_version": "0.4.0", "target_version": "0.4.1"}),
            ]
            for event_type, payload in technical:
                db.add(event(device_id, index, now - timedelta(hours=index + ordinal), event_type, payload)); index += 1
        db.commit()
    print("STG seed completed with three fictional devices and synthetic activity.")


if __name__ == "__main__":
    main()
