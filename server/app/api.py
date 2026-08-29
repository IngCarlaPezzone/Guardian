from pathlib import Path
from datetime import datetime, timezone
from typing import Any

from fastapi import APIRouter, Depends, HTTPException, Request
from fastapi.responses import FileResponse
from pydantic import BaseModel, Field
from sqlalchemy.orm import Session

from server.app.config import settings
from server.app.db import get_db
from server.app.models import Device, DeviceCommand, DeviceConfiguration, DeviceEvent, DeviceMissionProfile, Release, UpdateCommand
from server.app.security import current_device, hash_secret, new_token, utcnow, verify_secret, VALID_DEVICE_COMMAND_STATUSES, VALID_UPDATE_STATUSES
from server.app.update_queue import cleanup_update_queue, next_pending_update

router = APIRouter(prefix="/api/v1")


class RegisterPayload(BaseModel):
    device_id: str
    machine_name: str
    client_version: str
    bootstrap_token: str
    timezone_offset_minutes: int | None = Field(default=None, ge=-840, le=840)


class HeartbeatPayload(BaseModel):
    machine_name: str
    client_version: str
    effective_interval_seconds: int
    monitoring_enabled: bool = True
    timezone_offset_minutes: int | None = Field(default=None, ge=-840, le=840)


def timezone_from_offset(offset_minutes: int | None) -> str | None:
    if offset_minutes is None:
        return None
    sign = "+" if offset_minutes >= 0 else "-"
    absolute = abs(offset_minutes)
    return f"UTC{sign}{absolute // 60:02d}:{absolute % 60:02d}"


class UpdateStatusPayload(BaseModel):
    status: str
    previous_version: str | None = None
    error_message: str | None = None


class DeviceCommandStatusPayload(BaseModel):
    status: str
    error_message: str | None = None


class EventPayload(BaseModel):
    event_id: str = Field(min_length=1, max_length=80)
    occurred_at: datetime
    event_type: str = Field(min_length=1, max_length=120)
    client_version: str | None = None
    payload: dict[str, Any] = Field(default_factory=dict)


class EventBatchPayload(BaseModel):
    device_id: str
    events: list[EventPayload]


def normalize_utc(value: datetime) -> datetime:
    if value.tzinfo is None:
        value = value.replace(tzinfo=timezone.utc)
    return value.astimezone(timezone.utc)


def authenticate_payload_device(payload: EventBatchPayload, request: Request, db: Session) -> Device:
    auth = request.headers.get("authorization", "")
    if not auth.lower().startswith("bearer "):
        raise HTTPException(status_code=401, detail="missing bearer token")
    token = auth.split(" ", 1)[1].strip()
    device = db.get(Device, payload.device_id)
    if device is None or not device.is_active or not verify_secret(token, device.token_hash):
        raise HTTPException(status_code=401, detail="invalid device token")
    return device


@router.post("/devices/register")
def register_device(payload: RegisterPayload, db: Session = Depends(get_db)):
    if payload.bootstrap_token != settings.device_bootstrap_token:
        raise HTTPException(status_code=401, detail="invalid bootstrap token")
    token = new_token()
    device = db.get(Device, payload.device_id)
    if device is None:
        device = Device(
            id=payload.device_id,
            machine_name=payload.machine_name,
            display_name=payload.machine_name,
            token_hash=hash_secret(token),
            client_version=payload.client_version,
            last_seen_at=utcnow(),
        )
        db.add(device)
        db.flush()
        db.add(DeviceConfiguration(device_id=device.id, interval_seconds=900, timezone=timezone_from_offset(payload.timezone_offset_minutes) or "UTC", version=1, mission_config={"enabledSkills": ["math.basic_operations_1.addition", "math.basic_operations_1.subtraction", "math.basic_operations_1.multiplication"]}))
    else:
        device.machine_name = payload.machine_name
        device.client_version = payload.client_version
        device.last_seen_at = utcnow()
        device.token_hash = hash_secret(token)
        if device.configuration and timezone_from_offset(payload.timezone_offset_minutes):
            device.configuration.timezone = timezone_from_offset(payload.timezone_offset_minutes)
    db.commit()
    return {"device_id": device.id, "device_token": token}


@router.post("/devices/{device_id}/heartbeat")
def heartbeat(payload: HeartbeatPayload, device: Device = Depends(current_device), db: Session = Depends(get_db)):
    device.machine_name = payload.machine_name
    device.client_version = payload.client_version
    device.monitoring_enabled = payload.monitoring_enabled
    device.last_seen_at = utcnow()
    timezone_name = timezone_from_offset(payload.timezone_offset_minutes)
    if device.configuration and timezone_name:
        device.configuration.timezone = timezone_name
    cleanup_update_queue(db, device)
    db.commit()
    pending = db.query(UpdateCommand).filter(UpdateCommand.device_id == device.id, UpdateCommand.status == "pending").first() is not None
    return {
        "server_time": utcnow().isoformat(),
        "config_version": device.configuration.version if device.configuration else 1,
        "pending_update": pending,
        "monitoring_enabled": device.monitoring_enabled,
    }


@router.get("/devices/{device_id}/config")
def get_config(device: Device = Depends(current_device)):
    config = device.configuration
    if config is None:
        return {"version": 1, "interval_seconds": 900, "timezone": "UTC", "updated_at": utcnow().isoformat(), "mission_config": {"EnabledSkills": [], "PrivateProfile": {}}}
    profile = device.mission_profile
    mission_config = config.mission_config or {}
    return {
        "version": config.version,
        "interval_seconds": config.interval_seconds,
        "timezone": config.timezone,
        "updated_at": config.updated_at.isoformat(),
        "mission_config": {
            "EnabledSkills": mission_config.get("enabledSkills", ["math.basic_operations_1.addition", "math.basic_operations_1.subtraction", "math.basic_operations_1.multiplication"]),
            "PrivateProfile": {
                "PreferredName": profile.preferred_name if profile else "",
                "FirstName": profile.first_name if profile else "",
                "MiddleName": profile.middle_name if profile else "",
                "LastName": profile.last_name if profile else "",
                "BirthDate": profile.birth_date if profile else "",
            },
        },
    }


@router.get("/devices/{device_id}/updates/pending")
def pending_update(device: Device = Depends(current_device), db: Session = Depends(get_db)):
    command = next_pending_update(db, device)
    db.commit()
    if command is None:
        return {"pending": False}
    release = command.release
    return {
        "pending": True,
        "command_id": command.id,
        "release_id": release.id,
        "version": release.version,
        "sha256": release.sha256,
        "file_size": release.file_size,
        "download_url": f"/api/v1/releases/{release.id}/download",
    }


@router.get("/devices/{device_id}/commands/pending")
def pending_device_command(device: Device = Depends(current_device), db: Session = Depends(get_db)):
    command = (
        db.query(DeviceCommand)
        .filter(DeviceCommand.device_id == device.id, DeviceCommand.status == "pending")
        .order_by(DeviceCommand.requested_at.asc())
        .first()
    )
    if command is None:
        return {"pending": False}
    return {"pending": True, "command_id": command.id, "command_type": command.command_type}


@router.post("/devices/{device_id}/commands/{command_id}/status")
def device_command_status(command_id: str, payload: DeviceCommandStatusPayload, device: Device = Depends(current_device), db: Session = Depends(get_db)):
    if payload.status not in VALID_DEVICE_COMMAND_STATUSES:
        raise HTTPException(status_code=422, detail="invalid device command status")
    command = db.get(DeviceCommand, command_id)
    if command is None or command.device_id != device.id:
        raise HTTPException(status_code=404, detail="device command not found")
    command.status = payload.status
    command.error_message = payload.error_message
    if payload.status == "acknowledged" and command.acknowledged_at is None:
        command.acknowledged_at = utcnow()
    if payload.status in {"success", "failed"}:
        command.completed_at = utcnow()
    if payload.status == "success" and command.command_type in {"pause_monitoring", "resume_monitoring"}:
        device.monitoring_enabled = command.command_type == "resume_monitoring"
    db.commit()
    return {"ok": True}


@router.post("/devices/{device_id}/updates/{command_id}/status")
def update_status(command_id: str, payload: UpdateStatusPayload, device: Device = Depends(current_device), db: Session = Depends(get_db)):
    if payload.status not in VALID_UPDATE_STATUSES:
        raise HTTPException(status_code=422, detail="invalid status")
    command = db.get(UpdateCommand, command_id)
    if command is None or command.device_id != device.id:
        raise HTTPException(status_code=404, detail="update command not found")
    command.status = payload.status
    command.error_message = payload.error_message
    if payload.previous_version:
        command.previous_version = payload.previous_version
    if payload.status in {"acknowledged", "downloading", "installing"} and command.started_at is None:
        command.started_at = utcnow()
    if payload.status in {"success", "failed", "rolled_back"}:
        command.completed_at = utcnow()
    db.commit()
    return {"ok": True}


@router.post("/events")
def ingest_events(payload: EventBatchPayload, request: Request, db: Session = Depends(get_db)):
    device = authenticate_payload_device(payload, request, db)
    accepted_event_ids: list[str] = []
    for event in payload.events:
        existing = db.query(DeviceEvent).filter(DeviceEvent.event_id == event.event_id).first()
        if existing is not None:
            if existing.device_id == device.id:
                accepted_event_ids.append(event.event_id)
            continue
        db.add(DeviceEvent(
            event_id=event.event_id,
            device_id=device.id,
            occurred_at=normalize_utc(event.occurred_at),
            received_at=utcnow(),
            event_type=event.event_type,
            client_version=event.client_version,
            payload=event.payload or {},
        ))
        accepted_event_ids.append(event.event_id)
    db.commit()
    return {"ok": True, "accepted_event_ids": accepted_event_ids}


@router.get("/releases")
def list_releases(db: Session = Depends(get_db)):
    releases = db.query(Release).filter(Release.is_active.is_(True)).order_by(Release.created_at.desc()).all()
    return [{"id": r.id, "version": r.version, "sha256": r.sha256, "file_size": r.file_size, "created_at": r.created_at.isoformat()} for r in releases]


@router.get("/releases/{release_id}/download")
def download_release(release_id: str, request: Request, db: Session = Depends(get_db)):
    auth = request.headers.get("authorization", "")
    if not auth.lower().startswith("bearer "):
        raise HTTPException(status_code=401, detail="missing bearer token")
    release = db.get(Release, release_id)
    if release is None or not release.is_active:
        raise HTTPException(status_code=404, detail="release not found")
    path = Path(settings.releases_dir) / release.filename
    if not path.exists():
        raise HTTPException(status_code=404, detail="release file not found")
    return FileResponse(path, filename=release.filename, media_type="application/zip")
