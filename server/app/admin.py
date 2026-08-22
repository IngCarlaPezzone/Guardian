import hashlib
import hmac
import json
from datetime import date as date_cls, datetime, time, timedelta, timezone
from pathlib import Path
from zoneinfo import ZoneInfo, ZoneInfoNotFoundError

from fastapi import APIRouter, Depends, Form, HTTPException, Query, Request
from fastapi.responses import HTMLResponse, RedirectResponse
from fastapi.templating import Jinja2Templates
from sqlalchemy.orm import Session

from server.app.config import settings
from server.app.db import get_db
from server.app.models import AdminUser, Device, DeviceCommand, DeviceConfiguration, DeviceEvent, DeviceMissionProfile, Release, UpdateCommand
from server.app.security import VALID_DEVICE_COMMAND_TYPES, current_admin, utcnow, valid_interval, valid_semver, verify_secret
from server.app.update_queue import active_update, cleanup_update_queue, command_last_change, latest_update_by_device

router = APIRouter(prefix="/admin")
templates = Jinja2Templates(directory=str(Path(__file__).parent / "templates"))

MISSION_LEVELS = [
    ("math", "basic_operations_1", "Operaciones básicas", "Operaciones matemáticas básicas.", [
        ("math.basic_operations_1.addition", "Sumas", "Resolver sumas básicas."),
        ("math.basic_operations_1.subtraction", "Restas", "Resolver restas básicas."),
        ("math.basic_operations_1.multiplication", "Multiplicaciones", "Resolver multiplicaciones básicas."),
    ]),
    ("comprehension", "functional_1", "Comprensión funcional", "Preguntas cotidianas sobre identidad, edad, fechas, calendario y estaciones.", [
        ("comprehension.functional_1.identity", "Identidad", "Comprender distintas formas de solicitar información básica de identificación."),
        ("comprehension.functional_1.age_birth", "Edad y nacimiento", "Reconocer preguntas sobre edad, nacimiento y cumpleaños."),
        ("comprehension.functional_1.current_date", "Fecha actual", "Diferenciar día, mes, año y fecha actual."),
        ("comprehension.functional_1.temporal_relations", "Relaciones temporales", "Comprender referencias como ayer, mañana, mes anterior y mes siguiente."),
        ("comprehension.functional_1.calendar", "Calendario", "Reconocer días, meses y su secuencia."),
        ("comprehension.functional_1.seasons", "Estaciones", "Reconocer estaciones, características y secuencia."),
    ]),
]


def sign_username(username: str) -> str:
    return hmac.new(settings.guardian_session_secret.encode("utf-8"), username.encode("utf-8"), hashlib.sha256).hexdigest()


def is_device_online(device: Device) -> bool:
    if device.last_seen_at is None:
        return False
    last_seen = device.last_seen_at
    if last_seen.tzinfo is None:
        last_seen = last_seen.replace(tzinfo=timezone.utc)
    return (utcnow() - last_seen).total_seconds() <= settings.online_threshold_seconds


EVENT_GROUPS = {
    "missions": ["MissionStarted", "MissionFailed", "MissionSolved"],
    "config": ["RemoteConfigFetched", "RemoteConfigReceived", "RemoteConfigApplied", "RemoteConfigFailed"],
    "updates": [
        "UpdateCommandReceived",
        "UpdateDownloadStarted",
        "UpdateDownloadCompleted",
        "UpdateInstallStarted",
        "UpdateCompleted",
        "UpdateFailed",
    ],
    "control": [
        "MonitoringPauseCommandReceived",
        "MonitoringPaused",
        "MonitoringResumeCommandReceived",
        "MonitoringResumed",
        "TriggerMissionCommandReceived",
        "RemoteMissionTriggered",
    ],
    "errors": ["Error", "UnhandledError", "HeartbeatFailed", "UpdateFailed", "RemoteConfigFailed"],
}


def parse_day(value: str | None) -> date_cls | None:
    if not value:
        return None
    try:
        return date_cls.fromisoformat(value)
    except ValueError:
        return None


def admin_timezone():
    name = (settings.guardian_admin_timezone or "").strip()
    if name:
        try:
            return ZoneInfo(name)
        except ZoneInfoNotFoundError:
            return timezone.utc
    return datetime.now().astimezone().tzinfo or timezone.utc


def admin_now():
    return utcnow().astimezone(admin_timezone())


def admin_day_range_utc(day: date_cls):
    local_tz = admin_timezone()
    start_local = datetime.combine(day, time.min, tzinfo=local_tz)
    end_local = start_local + timedelta(days=1)
    return start_local.astimezone(timezone.utc), end_local.astimezone(timezone.utc)


def to_admin_time(value: datetime | None):
    if value is None:
        return None
    if value.tzinfo is None:
        value = value.replace(tzinfo=timezone.utc)
    return value.astimezone(admin_timezone())


def event_summary(event: DeviceEvent) -> str:
    payload = event.payload or {}
    keys = [
        "version",
        "targetVersion",
        "target_version",
        "previousVersion",
        "previous_version",
        "releaseId",
        "release_id",
        "commandId",
        "command_id",
        "missionId",
        "mission_id",
        "category_id",
        "level_id",
        "skill_id",
        "variant_id",
        "attempt",
        "reason",
        "message",
        "result",
    ]
    parts = []
    for key in keys:
        value = payload.get(key)
        if value is not None and value != "":
            parts.append(f"{key}: {value}")
    return " | ".join(parts)


def update_view_model(command: UpdateCommand | None):
    if command is None:
        return None
    return {
        "command": command,
        "last_change": command_last_change(command),
        "is_active": command.status in {"pending", "acknowledged", "downloading", "installing"},
    }


def active_device_command(db: Session, device_id: str) -> DeviceCommand | None:
    return (
        db.query(DeviceCommand)
        .filter(DeviceCommand.device_id == device_id, DeviceCommand.status.in_({"pending", "acknowledged"}))
        .order_by(DeviceCommand.requested_at.asc())
        .first()
    )


def supports_remote_controls(device: Device) -> bool:
    try:
        return tuple(int(part) for part in (device.client_version or "").split(".")) >= (0, 3, 2)
    except ValueError:
        return False


def device_guardian_state(device: Device) -> str:
    if not is_device_online(device):
        return "offline"
    return "active" if device.monitoring_enabled else "paused"


def render_mission_config(request: Request, device: Device, selected: list[str] | None = None, error: str | None = None, status_code: int = 200):
    if selected is None:
        selected = ((device.configuration.mission_config if device.configuration else {}) or {}).get(
            "enabledSkills",
            ["math.basic_operations_1.addition", "math.basic_operations_1.subtraction", "math.basic_operations_1.multiplication"],
        )
    return templates.TemplateResponse("missions.html", {
        "request": request,
        "device": device,
        "levels": MISSION_LEVELS,
        "selected": selected,
        "profile": device.mission_profile,
        "error": error,
    }, status_code=status_code)


@router.get("/", response_class=HTMLResponse)
def dashboard(request: Request, db: Session = Depends(get_db), admin: AdminUser = Depends(current_admin)):
    devices = db.query(Device).order_by(Device.registered_at.desc()).all()
    for device in devices:
        cleanup_update_queue(db, device)
    db.commit()
    releases = db.query(Release).filter(Release.is_active.is_(True)).order_by(Release.created_at.desc()).all()
    latest = releases[0] if releases else None
    latest_updates = latest_update_by_device(db)
    update_status_by_device = {device.id: update_view_model(latest_updates.get(device.id)) for device in devices}
    command_status_by_device = {device.id: active_device_command(db, device.id) for device in devices}
    now = utcnow()
    return templates.TemplateResponse("dashboard.html", {
        "request": request,
        "devices": devices,
        "releases": releases,
        "latest": latest,
        "update_status_by_device": update_status_by_device,
        "command_status_by_device": command_status_by_device,
        "guardian_state_by_device": {device.id: device_guardian_state(device) for device in devices},
        "remote_controls_supported_by_device": {device.id: supports_remote_controls(device) for device in devices},
        "online_by_device": {device.id: is_device_online(device) for device in devices},
        "now": now,
        "online_threshold_seconds": settings.online_threshold_seconds,
    })


@router.get("/devices/{device_id}/activity", response_class=HTMLResponse)
def device_activity(
    device_id: str,
    request: Request,
    period: str = Query("today"),
    group: str = Query("all"),
    event_date: str | None = Query(None, alias="date"),
    db: Session = Depends(get_db),
    admin: AdminUser = Depends(current_admin),
):
    device = db.get(Device, device_id)
    if device is None:
        raise HTTPException(status_code=404)

    query = db.query(DeviceEvent).filter(DeviceEvent.device_id == device.id)
    selected_day = None
    if period == "today":
        selected_day = admin_now().date()
    elif period == "yesterday":
        selected_day = admin_now().date() - timedelta(days=1)
    elif period == "date":
        selected_day = parse_day(event_date)
    elif period != "all":
        period = "today"
        selected_day = admin_now().date()

    if selected_day is not None:
        start, end = admin_day_range_utc(selected_day)
        query = query.filter(DeviceEvent.occurred_at >= start, DeviceEvent.occurred_at < end)

    if group != "all":
        event_types = EVENT_GROUPS.get(group)
        if event_types is None:
            group = "all"
        else:
            query = query.filter(DeviceEvent.event_type.in_(event_types))

    events = query.order_by(DeviceEvent.occurred_at.desc()).limit(300).all()
    event_rows = [
        {
            "event": event,
            "summary": event_summary(event),
            "occurred_local": to_admin_time(event.occurred_at),
            "payload_json": json.dumps(event.payload or {}, ensure_ascii=False, indent=2, sort_keys=True),
        }
        for event in events
    ]
    return templates.TemplateResponse("activity.html", {
        "request": request,
        "device": device,
        "events": event_rows,
        "period": period,
        "group": group,
        "event_date": (selected_day.isoformat() if selected_day is not None else (event_date or "")),
        "selected_day": selected_day,
        "timezone_label": str(admin_timezone()),
        "groups": [
            ("all", "Todos"),
            ("missions", "Misiones"),
            ("config", "Configuracion"),
            ("updates", "Actualizaciones"),
            ("control", "Control"),
            ("errors", "Errores"),
        ],
    })


@router.get("/login", response_class=HTMLResponse)
def login_form(request: Request):
    return templates.TemplateResponse("login.html", {"request": request, "error": None})


@router.post("/login")
def login(request: Request, username: str = Form(...), password: str = Form(...), db: Session = Depends(get_db)):
    user = db.query(AdminUser).filter(AdminUser.username == username.strip(), AdminUser.is_active.is_(True)).first()
    if not user or not verify_secret(password, user.password_hash):
        return templates.TemplateResponse("login.html", {"request": request, "error": "Credenciales invalidas"}, status_code=401)
    user.last_login_at = utcnow()
    db.commit()
    response = RedirectResponse("/admin/", status_code=303)
    secure = request.url.scheme == "https"
    response.set_cookie("guardian_admin", user.username, httponly=True, secure=secure, samesite="lax", max_age=60 * 60 * 8)
    response.set_cookie("guardian_admin_sig", sign_username(user.username), httponly=True, secure=secure, samesite="lax", max_age=60 * 60 * 8)
    return response


@router.post("/logout")
def logout():
    response = RedirectResponse("/admin/login", status_code=303)
    response.delete_cookie("guardian_admin")
    response.delete_cookie("guardian_admin_sig")
    return response


@router.post("/devices/{device_id}/config")
def update_device_config(device_id: str, request: Request, display_name: str = Form(""), interval_minutes: int = Form(...), missions_submitted: str = Form(""), enabled_skills: list[str] = Form([]), preferred_name: str = Form(""), first_name: str = Form(""), middle_name: str = Form(""), last_name: str = Form(""), birth_date: str = Form(""), db: Session = Depends(get_db), admin: AdminUser = Depends(current_admin)):
    device = db.get(Device, device_id)
    if device is None:
        raise HTTPException(status_code=404)
    seconds = int(interval_minutes) * 60
    if not valid_interval(seconds):
        raise HTTPException(status_code=422, detail="interval out of range")
    selected = None
    if missions_submitted == "1":
        valid_skills = {key for _, _, _, _, skills in MISSION_LEVELS for key, _, _ in skills}
        selected = [key for key in enabled_skills if key in valid_skills]
        if not selected:
            return render_mission_config(request, device, selected=[], error="Seleccioná al menos una habilidad antes de guardar.", status_code=422)
    device.display_name = display_name.strip() or None
    config = device.configuration
    changed = False
    if config is None:
        config = DeviceConfiguration(device_id=device.id, interval_seconds=seconds, version=1)
        db.add(config)
    else:
        if config.interval_seconds != seconds:
            config.interval_seconds = seconds
            changed = True
    if missions_submitted == "1":
        mission_config = config.mission_config or {}
        if mission_config.get("enabledSkills") != selected:
            config.mission_config = {"enabledSkills": selected}
            changed = True
        clean = {"preferred_name": preferred_name.strip() or None, "first_name": first_name.strip() or None, "middle_name": middle_name.strip() or None, "last_name": last_name.strip() or None, "birth_date": birth_date.strip() or None}
        if clean["birth_date"]:
            try:
                date_cls.fromisoformat(clean["birth_date"])
            except ValueError:
                raise HTTPException(status_code=422, detail="birth_date must be a real ISO date")
        profile = device.mission_profile
        if profile is None and any(clean.values()):
            profile = DeviceMissionProfile(device_id=device.id, **clean)
            db.add(profile)
            changed = True
        elif profile is not None:
            for field, value in clean.items():
                if getattr(profile, field) != value:
                    setattr(profile, field, value)
                    changed = True
    if changed:
        config.version += 1
    db.commit()
    return RedirectResponse("/admin/", status_code=303)


@router.get("/devices/{device_id}/missions", response_class=HTMLResponse)
def mission_config_page(device_id: str, request: Request, db: Session = Depends(get_db), admin: AdminUser = Depends(current_admin)):
    device = db.get(Device, device_id)
    if device is None:
        raise HTTPException(status_code=404)
    return render_mission_config(request, device)


@router.post("/devices/{device_id}/updates")
def request_update(device_id: str, release_id: str = Form(...), db: Session = Depends(get_db), admin: AdminUser = Depends(current_admin)):
    device = db.get(Device, device_id)
    release = db.get(Release, release_id)
    if device is None or release is None:
        raise HTTPException(status_code=404)
    cleanup_update_queue(db, device)
    db.flush()
    if release.version == device.client_version:
        db.commit()
        return RedirectResponse("/admin/", status_code=303)
    if active_update(db, device.id) is not None:
        db.commit()
        return RedirectResponse("/admin/", status_code=303)
    db.add(UpdateCommand(device_id=device.id, release_id=release.id, target_version=release.version, status="pending"))
    db.commit()
    return RedirectResponse("/admin/", status_code=303)


@router.post("/devices/{device_id}/updates/{command_id}/cancel")
def cancel_pending_update(device_id: str, command_id: str, db: Session = Depends(get_db), admin: AdminUser = Depends(current_admin)):
    command = db.get(UpdateCommand, command_id)
    if command is None or command.device_id != device_id:
        raise HTTPException(status_code=404)
    if command.status == "pending":
        command.status = "cancelled"
        command.completed_at = utcnow()
        command.error_message = "cancelled by administrator before device acknowledgement"
        db.commit()
    return RedirectResponse("/admin/", status_code=303)


def queue_device_command(device_id: str, command_type: str, db: Session) -> None:
    device = db.get(Device, device_id)
    if device is None:
        raise HTTPException(status_code=404)
    if command_type not in VALID_DEVICE_COMMAND_TYPES or not is_device_online(device) or not supports_remote_controls(device):
        return
    if active_device_command(db, device.id) is None:
        db.add(DeviceCommand(device_id=device.id, command_type=command_type, status="pending"))
        db.commit()


@router.post("/devices/{device_id}/commands/{command_type}")
def request_named_device_command(device_id: str, command_type: str, db: Session = Depends(get_db), admin: AdminUser = Depends(current_admin)):
    queue_device_command(device_id, command_type, db)
    return RedirectResponse("/admin/", status_code=303)


@router.post("/devices/{device_id}/commands")
def request_device_command(device_id: str, command_type: str = Form(...), db: Session = Depends(get_db), admin: AdminUser = Depends(current_admin)):
    queue_device_command(device_id, command_type, db)
    return RedirectResponse("/admin/", status_code=303)


@router.post("/devices/{device_id}/delete")
def delete_device(device_id: str, confirm_device_id: str = Form(""), confirm_delete: str = Form(""), db: Session = Depends(get_db), admin: AdminUser = Depends(current_admin)):
    device = db.get(Device, device_id)
    if device is None:
        raise HTTPException(status_code=404)
    if confirm_device_id.strip() != device.id or confirm_delete.strip() != "ELIMINAR":
        return RedirectResponse("/admin/", status_code=303)
    if is_device_online(device):
        return RedirectResponse("/admin/", status_code=303)

    db.query(UpdateCommand).filter(UpdateCommand.device_id == device.id).delete(synchronize_session=False)
    db.query(DeviceCommand).filter(DeviceCommand.device_id == device.id).delete(synchronize_session=False)
    db.query(DeviceEvent).filter(DeviceEvent.device_id == device.id).delete(synchronize_session=False)
    db.query(DeviceMissionProfile).filter(DeviceMissionProfile.device_id == device.id).delete(synchronize_session=False)
    db.query(DeviceConfiguration).filter(DeviceConfiguration.device_id == device.id).delete(synchronize_session=False)
    db.delete(device)
    db.commit()
    return RedirectResponse("/admin/", status_code=303)


@router.get("/releases", response_class=HTMLResponse)
def releases_page(request: Request, db: Session = Depends(get_db), admin: AdminUser = Depends(current_admin)):
    releases = db.query(Release).order_by(Release.created_at.desc()).all()
    return templates.TemplateResponse("releases.html", {"request": request, "releases": releases})
