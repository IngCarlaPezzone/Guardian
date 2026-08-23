from __future__ import annotations

from collections import defaultdict
from dataclasses import dataclass
from datetime import date, datetime, time, timedelta, timezone, tzinfo
from statistics import median
from zoneinfo import ZoneInfo, ZoneInfoNotFoundError

from sqlalchemy.orm import Session

from server.app.models import Device, DeviceEvent


CATALOG = {
    "math": {"label": "Matemática", "levels": {"basic_operations_1": {"label": "Operaciones básicas", "skills": {"addition": "Sumas", "subtraction": "Restas", "multiplication": "Multiplicaciones"}}}},
    "comprehension": {"label": "Comprensión", "levels": {"functional_1": {"label": "Comprensión funcional", "skills": {"identity": "Identidad", "age_birth": "Edad y nacimiento", "current_date": "Fecha actual", "temporal_relations": "Relaciones temporales", "calendar": "Calendario", "seasons": "Estaciones"}}}},
}

MISSION_EVENTS = {"MissionStarted", "MissionFailed", "MissionSolved"}


def device_timezone(device: Device) -> tzinfo:
    name = (device.configuration.timezone if device.configuration else "UTC") or "UTC"
    try:
        return ZoneInfo(name)
    except ZoneInfoNotFoundError:
        # Mantiene el Admin operativo si un cliente histórico trae una zona ya no
        # disponible. tzdata es dependencia del servidor para los nombres IANA.
        return timezone.utc


def scope_label(category: str | None = None, level: str | None = None, skill: str | None = None) -> str:
    if skill and category and level:
        return CATALOG.get(category, {}).get("levels", {}).get(level, {}).get("skills", {}).get(skill, skill)
    if level and category:
        return CATALOG.get(category, {}).get("levels", {}).get(level, {}).get("label", level)
    if category:
        return CATALOG.get(category, {}).get("label", category)
    return "Global"


def date_range(period: str, tz: ZoneInfo, start_value: str | None = None, end_value: str | None = None, now: datetime | None = None):
    now = (now or datetime.now(timezone.utc)).astimezone(tz)
    today = now.date()
    if period == "today":
        start, end = today, today + timedelta(days=1)
    elif period == "7d":
        start, end = today - timedelta(days=6), today + timedelta(days=1)
    elif period == "30d":
        start, end = today - timedelta(days=29), today + timedelta(days=1)
    elif period == "range":
        try:
            start = date.fromisoformat(start_value or "")
            end = date.fromisoformat(end_value or "") + timedelta(days=1)
        except ValueError:
            start, end = today - timedelta(days=6), today + timedelta(days=1)
    else:
        return None, None
    return datetime.combine(start, time.min, tzinfo=tz).astimezone(timezone.utc), datetime.combine(end, time.min, tzinfo=tz).astimezone(timezone.utc)


def payload_value(payload: dict, key: str, legacy_key: str | None = None):
    return payload.get(key) or (payload.get(legacy_key) if legacy_key else None)


@dataclass
class MissionRecord:
    mission_id: str
    category_id: str | None
    level_id: str | None
    skill_id: str | None
    variant_id: str | None
    started_at: datetime | None = None
    solved_at: datetime | None = None
    solved_attempt: int | None = None
    failed_attempts: set[int] | None = None

    def __post_init__(self):
        if self.failed_attempts is None:
            self.failed_attempts = set()

    @property
    def attempts(self) -> int:
        if self.solved_attempt and self.solved_attempt > 0:
            return self.solved_attempt
        return max(1, len(self.failed_attempts) + 1)

    @property
    def duration_seconds(self) -> float | None:
        if not self.started_at or not self.solved_at:
            return None
        seconds = (self.solved_at - self.started_at).total_seconds()
        return seconds if seconds >= 0 else None


def mission_records(events: list[DeviceEvent]) -> list[MissionRecord]:
    records: dict[str, MissionRecord] = {}
    for event in events:
        payload = event.payload or {}
        mission_id = payload_value(payload, "mission_id", "missionId")
        if not mission_id:
            continue
        category = payload.get("category_id")
        level = payload.get("level_id")
        skill = payload.get("skill_id")
        variant = payload.get("variant_id")
        record = records.get(mission_id)
        if record is None:
            record = MissionRecord(str(mission_id), category, level, skill, variant)
            records[str(mission_id)] = record
        else:
            record.category_id = record.category_id or category
            record.level_id = record.level_id or level
            record.skill_id = record.skill_id or skill
            record.variant_id = record.variant_id or variant
        attempt = payload.get("attempt")
        try:
            attempt = int(attempt)
        except (TypeError, ValueError):
            attempt = None
        if event.event_type == "MissionStarted":
            if record.started_at is None or event.occurred_at < record.started_at:
                record.started_at = event.occurred_at
        elif event.event_type == "MissionFailed" and attempt:
            record.failed_attempts.add(attempt)
        elif event.event_type == "MissionSolved":
            if record.solved_at is None or event.occurred_at < record.solved_at:
                record.solved_at = event.occurred_at
                record.solved_attempt = attempt
    return [record for record in records.values() if record.solved_at is not None]


def matches_scope(record: MissionRecord, category: str | None, level: str | None, skill: str | None) -> bool:
    return (not category or record.category_id == category) and (not level or record.level_id == level) and (not skill or record.skill_id == skill)


def summarize(records: list[MissionRecord]) -> dict:
    counts = {"missions": len(records), "first_attempt": 0, "second_attempt": 0, "third_plus": 0}
    durations = []
    total_attempts = 0
    for record in records:
        attempts = record.attempts
        total_attempts += attempts
        if attempts == 1:
            counts["first_attempt"] += 1
        elif attempts == 2:
            counts["second_attempt"] += 1
        else:
            counts["third_plus"] += 1
        if record.duration_seconds is not None:
            durations.append(record.duration_seconds)
    missions = counts["missions"]
    counts["total_attempts"] = total_attempts
    counts["average_attempts"] = round(total_attempts / missions, 2) if missions else 0
    counts["first_attempt_rate"] = round((counts["first_attempt"] / missions) * 100, 1) if missions else 0
    counts["retry_rate"] = round(((missions - counts["first_attempt"]) / missions) * 100, 1) if missions else 0
    counts["median_seconds"] = round(median(durations), 1) if durations else None
    return counts


def group_rows(records: list[MissionRecord], dimension: str) -> list[dict]:
    grouped: dict[tuple, list[MissionRecord]] = defaultdict(list)
    for record in records:
        if dimension == "category":
            key = (record.category_id or "legacy",)
            label = scope_label(record.category_id) if record.category_id else "Histórico sin clasificar"
        elif dimension == "level":
            key = (record.category_id or "legacy", record.level_id or "legacy")
            label = scope_label(record.category_id, record.level_id) if record.category_id and record.level_id else "Histórico sin clasificar"
        elif dimension == "skill":
            key = (record.category_id or "legacy", record.level_id or "legacy", record.skill_id or "legacy")
            label = scope_label(record.category_id, record.level_id, record.skill_id) if record.category_id and record.level_id and record.skill_id else "Histórico sin clasificar"
        else:
            key = (record.variant_id or "legacy",)
            label = record.variant_id or "Histórico sin variante"
        grouped[(key, label)].append(record)
    rows = []
    for (key, label), row_records in grouped.items():
        row = {"key": key, "label": label, **summarize(row_records)}
        rows.append(row)
    return sorted(rows, key=lambda row: (-row["missions"], row["label"]))


def daily_rows(records: list[MissionRecord], tz: ZoneInfo) -> list[dict]:
    grouped: dict[tuple[str, str], int] = defaultdict(int)
    for record in records:
        day = record.solved_at.astimezone(tz).date().isoformat()
        grouped[(day, record.category_id or "legacy")] += 1
    by_day: dict[str, list[dict]] = defaultdict(list)
    for (day, category), count in sorted(grouped.items()):
        by_day[day].append({
            "category_id": category,
            "category": scope_label(category) if category != "legacy" else "Histórico",
            "missions": count,
        })
    return [
        {"day": day, "categories": categories, "missions": sum(item["missions"] for item in categories)}
        for day, categories in by_day.items()
    ]


def dashboard_data(db: Session, device: Device, period: str, start: str | None, end: str | None, category: str | None, level: str | None, skill: str | None) -> dict:
    tz = device_timezone(device)
    start_utc, end_utc = date_range(period, tz, start, end)
    # Los intentos de una misión pueden cruzar el límite del período. Se cargan sólo
    # eventos de misión del dispositivo y luego se filtra por la fecha de resolución,
    # conservando el MissionStarted necesario para la métrica experimental de tiempo.
    query = db.query(DeviceEvent).filter(DeviceEvent.device_id == device.id, DeviceEvent.event_type.in_(MISSION_EVENTS))
    records = mission_records(query.order_by(DeviceEvent.occurred_at.asc()).all())
    if start_utc is not None:
        records = [record for record in records if start_utc <= record.solved_at < end_utc]
    records = [record for record in records if matches_scope(record, category, level, skill)]
    dimension = "category" if not category else "level" if not level else "skill" if not skill else "variant"
    return {
        "timezone": str(tz),
        "summary": summarize(records),
        "rows": group_rows(records, dimension),
        "variants": group_rows(records, "variant") if skill else [],
        "daily": daily_rows(records, tz),
        "scope_label": scope_label(category, level, skill),
        "record_count": len(records),
    }
