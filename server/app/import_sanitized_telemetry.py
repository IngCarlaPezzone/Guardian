"""Receive a pre-sanitized telemetry dataset into STG only."""
import argparse
import json
import sys
import uuid
from datetime import datetime, timezone

from server.app.config import settings
from server.app.db import SessionLocal
from server.app.models import DEVICE_KIND_STG_IMPORTED_TELEMETRY, Device, DeviceConfiguration, DeviceEvent, DeviceMissionProfile
from server.app.security import hash_secret


DATASET_MARKER = "prod-telemetry-sanitized-v1"
NAMESPACE = uuid.UUID("bb81f8b6-8d5f-4e11-9ba7-ec73425bb650")
SAFE_PAYLOAD_KEYS = {
    "mission_id", "missionId", "category_id", "level_id", "skill_id", "variant_id",
    "attempt", "result", "categoryId", "levelId", "skillId", "variantId",
    "visual_primer_applied", "visual_primer_skill_id",
}


def stg_device_id(slot: int) -> str:
    return str(uuid.uuid5(NAMESPACE, f"stg-imported-telemetry-device:{slot}"))


def normalize_event(value: dict, index: int) -> dict:
    event_type = value.get("event_type")
    occurred_at = value.get("occurred_at")
    slot = value.get("slot")
    if not isinstance(event_type, str) or not event_type:
        raise ValueError("event_type is required")
    if not isinstance(occurred_at, str) or not occurred_at:
        raise ValueError("occurred_at is required")
    if not isinstance(slot, int) or slot < 1:
        raise ValueError("slot must be a positive integer")
    parsed = datetime.fromisoformat(occurred_at.replace("Z", "+00:00"))
    if parsed.tzinfo is None:
        parsed = parsed.replace(tzinfo=timezone.utc)
    payload = value.get("payload") or {}
    if not isinstance(payload, dict):
        raise ValueError("payload must be an object")
    safe_payload = {
        key: item for key, item in payload.items()
        if key in SAFE_PAYLOAD_KEYS and (item is None or isinstance(item, (str, int, float, bool)))
    }
    safe_payload["import_source"] = DATASET_MARKER
    return {
        "slot": slot,
        "event_type": event_type,
        "occurred_at": parsed.astimezone(timezone.utc),
        "client_version": value.get("client_version") if isinstance(value.get("client_version"), str) else None,
        "payload": safe_payload,
        "event_id": str(uuid.uuid5(NAMESPACE, f"{DATASET_MARKER}:{index}")),
    }


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--replace", action="store_true", help="replace the previous sanitized import")
    parser.add_argument("--target-device-id", help="existing STG test device that will receive only sanitized events")
    args = parser.parse_args()
    if settings.guardian_environment.strip().upper() != "STG":
        raise SystemExit("Refusing import: GUARDIAN_ENVIRONMENT must be STG.")
    try:
        document = json.load(sys.stdin)
    except json.JSONDecodeError as error:
        raise SystemExit(f"Invalid sanitized dataset: {error}")
    if not isinstance(document, dict) or document.get("schema") != DATASET_MARKER:
        raise SystemExit("Refusing import: unexpected dataset schema.")
    raw_events = document.get("events")
    if not isinstance(raw_events, list):
        raise SystemExit("Refusing import: events must be a list.")
    events = [normalize_event(raw, index) for index, raw in enumerate(raw_events)]
    slots = sorted({event["slot"] for event in events})

    with SessionLocal() as db:
        target_device_id = args.target_device_id
        if target_device_id and db.get(Device, target_device_id) is None:
            raise SystemExit("Refusing import: target STG device does not exist.")
        target_ids = [target_device_id] if target_device_id else [stg_device_id(slot) for slot in slots]
        if args.replace:
            # Las importaciones anteriores nunca deben multiplicar dispositivos
            # ni quedar visibles junto al dispositivo TEST elegido.
            stale_devices = db.query(Device).filter(Device.device_kind == DEVICE_KIND_STG_IMPORTED_TELEMETRY).all()
            for stale in stale_devices:
                db.query(DeviceEvent).filter(DeviceEvent.device_id == stale.id).delete(synchronize_session=False)
                db.query(DeviceMissionProfile).filter(DeviceMissionProfile.device_id == stale.id).delete(synchronize_session=False)
                db.query(DeviceConfiguration).filter(DeviceConfiguration.device_id == stale.id).delete(synchronize_session=False)
                db.delete(stale)
            if target_device_id:
                for previous in db.query(DeviceEvent).filter(DeviceEvent.device_id == target_device_id).all():
                    if (previous.payload or {}).get("import_source") == DATASET_MARKER:
                        db.delete(previous)
        if not target_device_id:
            for slot in slots:
                device_id = stg_device_id(slot)
                if db.get(Device, device_id) is None:
                    db.add(Device(
                        id=device_id, machine_name=f"STG-IMPORTED-TELEMETRY-{slot}",
                        display_name=f"Telemetría importada STG {slot}",
                        token_hash=hash_secret(f"stg-import-only-{slot}"), client_version=None,
                        monitoring_enabled=True, device_kind=DEVICE_KIND_STG_IMPORTED_TELEMETRY,
                    ))
                    db.add(DeviceConfiguration(device_id=device_id, interval_seconds=900, version=1, mission_config={"enabledSkills": []}))
        db.flush()
        inserted = 0
        for event in events:
            if db.query(DeviceEvent).filter(DeviceEvent.event_id == event["event_id"]).first() is None:
                db.add(DeviceEvent(
                    id=str(uuid.uuid4()), event_id=event["event_id"], device_id=target_device_id or stg_device_id(event["slot"]),
                    occurred_at=event["occurred_at"], received_at=event["occurred_at"],
                    event_type=event["event_type"], client_version=event["client_version"], payload=event["payload"],
                ))
                inserted += 1
        db.commit()
    print(f"Imported {inserted} sanitized telemetry events into STG.")


if __name__ == "__main__":
    main()
