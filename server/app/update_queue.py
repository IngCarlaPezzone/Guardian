from datetime import timezone

from sqlalchemy.orm import Session

from server.app.config import settings
from server.app.models import Device, UpdateCommand
from server.app.security import utcnow

ACTIVE_UPDATE_STATUSES = {"pending", "acknowledged", "downloading", "installing"}
IN_PROGRESS_UPDATE_STATUSES = {"acknowledged", "downloading", "installing"}
TERMINAL_UPDATE_STATUSES = {"success", "failed", "rolled_back"}


def cleanup_update_queue(db: Session, device: Device) -> None:
    now = utcnow()
    active_commands = (
        db.query(UpdateCommand)
        .filter(UpdateCommand.device_id == device.id, UpdateCommand.status.in_(ACTIVE_UPDATE_STATUSES))
        .order_by(UpdateCommand.requested_at.asc())
        .all()
    )
    for command in active_commands:
        if command.target_version == device.client_version:
            command.status = "success"
            if command.started_at is None:
                command.started_at = now
            command.completed_at = now
            command.error_message = "already running target version"
            continue

        if command.status in IN_PROGRESS_UPDATE_STATUSES and is_timed_out(command, now):
            command.status = "failed"
            command.completed_at = now
            command.error_message = "update command timed out before terminal status"


def next_pending_update(db: Session, device: Device) -> UpdateCommand | None:
    cleanup_update_queue(db, device)
    db.flush()
    return (
        db.query(UpdateCommand)
        .filter(UpdateCommand.device_id == device.id, UpdateCommand.status == "pending")
        .order_by(UpdateCommand.requested_at.asc())
        .first()
    )


def active_update(db: Session, device_id: str) -> UpdateCommand | None:
    return (
        db.query(UpdateCommand)
        .filter(UpdateCommand.device_id == device_id, UpdateCommand.status.in_(ACTIVE_UPDATE_STATUSES))
        .order_by(UpdateCommand.requested_at.asc())
        .first()
    )


def latest_update_by_device(db: Session) -> dict[str, UpdateCommand]:
    commands = db.query(UpdateCommand).order_by(UpdateCommand.requested_at.desc()).all()
    latest: dict[str, UpdateCommand] = {}
    for command in commands:
        if command.device_id not in latest:
            latest[command.device_id] = command
    return latest


def command_last_change(command: UpdateCommand):
    return command.completed_at or command.started_at or command.requested_at


def is_timed_out(command: UpdateCommand, now) -> bool:
    reference = command.started_at or command.requested_at
    if reference is None:
        return False
    if reference.tzinfo is None:
        reference = reference.replace(tzinfo=timezone.utc)
    return (now - reference).total_seconds() > settings.update_command_timeout_seconds
