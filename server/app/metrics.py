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
    "comprehension": {"label": "Comprensión", "levels": {"functional_1": {"label": "Comprensión funcional", "skills": {"identity": "Identidad", "age_birth": "Edad y nacimiento", "instruction_vocabulary": "Vocabulario de consignas", "current_date": "Fecha actual", "temporal_relations": "Relaciones temporales", "calendar": "Calendario", "seasons": "Estaciones"}}}},
}

QUESTION_LABELS = {
    "generated": "Operación matemática",
    "vocab_how_many": "¿Cuántas estrellas hay?", "vocab_quantity": "Hay 3 lápices. ¿Cuántos lápices hay?",
    "vocab_before": "Lunes, martes, miércoles. ¿Qué día está antes de miércoles?",
    "vocab_after": "Enero, febrero, marzo. ¿Qué mes está después de febrero?",
    "vocab_next": "Uno, dos, tres... ¿qué número es el siguiente?",
    "vocab_previous": "Uno, dos, tres... ¿qué número es el anterior a tres?",
    "vocab_first": "Rojo, azul, verde. ¿Cuál está primero?", "vocab_last": "Rojo, azul, verde. ¿Cuál está último?",
    "identity_name_ask_1": "¿Cuál es tu nombre?", "identity_name_ask_2": "¿Cómo te llamás?", "identity_name_field": "Nombre", "identity_last_name_ask": "¿Cuál es tu apellido?", "identity_last_name_field": "Apellido", "identity_name_last_name_ask": "¿Cuál es tu nombre y apellido?", "identity_name_last_name_field": "Nombre y apellido", "identity_full_name_ask": "¿Cuál es tu nombre completo?",
    "age_ask_1": "¿Cuántos años tenés?", "age_ask_2": "¿Qué edad tenés?", "age_field": "Edad", "birth_year_ask": "¿En qué año naciste?", "birth_year_field": "Año de nacimiento", "birthday_ask": "¿Cuándo es tu cumpleaños?", "birth_date_ask": "¿Cuál es tu fecha de nacimiento?",
    "current_year_ask_1": "¿En qué año estamos?", "current_year_ask_2": "¿Qué año es?", "current_month_ask_1": "¿En qué mes estamos?", "current_month_ask_2": "¿Qué mes es?", "current_weekday": "¿Qué día de la semana es hoy?", "current_day_of_month": "¿Qué día del mes es hoy?", "current_full_date": "¿Qué fecha es hoy?",
    "tomorrow_weekday": "¿Qué día será mañana?", "yesterday_weekday": "¿Qué día fue ayer?", "next_month_ask_1": "¿Cuál es el mes que viene?", "previous_month": "¿Cuál fue el mes pasado?",
    "days_in_week": "¿Cuántos días tiene una semana?", "months_in_year": "¿Cuántos meses tiene un año?", "weekday_after": "¿Qué día viene después de un día dado?", "weekday_before": "¿Qué día viene antes de un día dado?", "month_after": "¿Qué mes viene después de un mes dado?", "month_before": "¿Qué mes viene antes de un mes dado?",
    "season_cold": "¿En qué estación hace mucho frío?", "season_hot": "¿En qué estación hace mucho calor?", "season_falling_leaves": "¿En qué estación se caen muchas hojas?", "season_flowers": "¿En qué estación crecen muchas flores?", "season_after": "¿Qué estación viene después de otra?",
}

MISSION_EVENTS = {"MissionStarted", "MissionFailed", "MissionHelpRequested", "MissionWritingHintShown", "MissionSolved"}


def device_timezone(device: Device) -> tzinfo:
    name = (device.configuration.timezone if device.configuration else "UTC") or "UTC"
    if name.startswith("UTC") and len(name) == 9 and name[3] in {"+", "-"} and name[4:6].isdigit() and name[6] == ":" and name[7:].isdigit():
        minutes = int(name[4:6]) * 60 + int(name[7:])
        if name[3] == "-":
            minutes = -minutes
        return timezone(timedelta(minutes=minutes), name)
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


def resolved_period(period: str, tz: tzinfo, start_value: str | None = None, end_value: str | None = None, now: datetime | None = None):
    now = (now or datetime.now(timezone.utc)).astimezone(tz)
    today = now.date()
    if period == "today":
        start, end = today, today
    elif period == "yesterday":
        start = end = today - timedelta(days=1)
    elif period == "7d":
        start, end = today - timedelta(days=6), today
    elif period == "30d":
        start, end = today - timedelta(days=29), today
    elif period == "range":
        try:
            start = date.fromisoformat(start_value or "")
            end = date.fromisoformat(end_value or "")
        except ValueError:
            return period, None, None, None, None, "Ingresá fechas válidas para el rango personalizado."
        if start > end:
            return period, start, end, None, None, "La fecha Desde no puede ser posterior a Hasta."
    elif period == "all":
        return period, None, None, None, None, None
    else:
        return "today", today, today, *period_bounds(today, today, tz), None
    start_utc, end_utc = period_bounds(start, end, tz)
    return period, start, end, start_utc, end_utc, None


def period_bounds(start: date, end: date, tz: tzinfo):
    return (
        datetime.combine(start, time.min, tzinfo=tz).astimezone(timezone.utc),
        datetime.combine(end + timedelta(days=1), time.min, tzinfo=tz).astimezone(timezone.utc),
    )


def date_range(period: str, tz: tzinfo, start_value: str | None = None, end_value: str | None = None, now: datetime | None = None):
    _, _, _, start_utc, end_utc, _ = resolved_period(period, tz, start_value, end_value, now)
    return start_utc, end_utc


def payload_value(payload: dict, key: str, legacy_key: str | None = None):
    if key in payload:
        return payload[key]
    return payload.get(legacy_key) if legacy_key and legacy_key in payload else None


def as_utc(value: datetime) -> datetime:
    # SQLite de tests devuelve timestamps sin zona, mientras PostgreSQL conserva UTC.
    return value.replace(tzinfo=timezone.utc) if value.tzinfo is None else value.astimezone(timezone.utc)


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
    max_help_level: int | None = None
    had_orthographic_error: bool | None = None
    writing_correction_count: int | None = None
    writing_answer_revealed: bool | None = None
    question_text: str | None = None
    events: list[DeviceEvent] | None = None

    def __post_init__(self):
        if self.events is None:
            self.events = []

    @property
    def attempts(self) -> int | None:
        if self.solved_attempt is not None and self.solved_attempt > 0:
            return self.solved_attempt
        return None

    @property
    def question_label(self) -> str | None:
        return QUESTION_LABELS.get(self.variant_id or "")

    @property
    def orthographic_support(self) -> bool | None:
        if self.had_orthographic_error is not None:
            return self.had_orthographic_error
        if self.writing_correction_count is not None:
            return self.writing_correction_count > 0
        if self.writing_answer_revealed is not None:
            return self.writing_answer_revealed
        return None

    @property
    def writing_max_level(self) -> str | None:
        if self.writing_answer_revealed is True or (self.writing_correction_count is not None and self.writing_correction_count >= 3):
            return "revealed"
        if self.writing_correction_count == 2:
            return "level_2"
        if self.writing_correction_count == 1:
            return "level_1"
        if self.writing_correction_count == 0 or self.writing_answer_revealed is False:
            return "none"
        return None

    @property
    def duration_seconds(self) -> float | None:
        if not self.started_at or not self.solved_at:
            return None
        seconds = (self.solved_at - self.started_at).total_seconds()
        return seconds if seconds >= 0 else None


def mission_records(events: list[DeviceEvent]) -> list[MissionRecord]:
    records: dict[str, MissionRecord] = {}
    for event in sorted(events, key=lambda item: (item.occurred_at, item.event_id)):
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
        record.events.append(event)
        attempt = payload.get("attempt")
        try:
            attempt = int(attempt)
        except (TypeError, ValueError):
            attempt = None
        if event.event_type == "MissionStarted":
            if record.started_at is None or event.occurred_at < record.started_at:
                record.started_at = event.occurred_at
            if "question_text" in payload:
                record.question_text = payload["question_text"]
        elif event.event_type == "MissionSolved":
            if record.solved_at is None or event.occurred_at < record.solved_at:
                record.solved_at = event.occurred_at
                record.solved_attempt = attempt
        if "max_help_level" in payload:
            record.max_help_level = integer_or_none(payload["max_help_level"])
        if "had_orthographic_error" in payload:
            record.had_orthographic_error = boolean_or_none(payload["had_orthographic_error"])
        if "writing_correction_count" in payload:
            record.writing_correction_count = integer_or_none(payload["writing_correction_count"])
        if "writing_answer_revealed" in payload:
            record.writing_answer_revealed = boolean_or_none(payload["writing_answer_revealed"])
    return [record for record in records.values() if record.solved_at is not None]


def matches_scope(record: MissionRecord, category: str | None, level: str | None, skill: str | None) -> bool:
    return (not category or record.category_id == category) and (not level or record.level_id == level) and (not skill or record.skill_id == skill)


def integer_or_none(value) -> int | None:
    try:
        return int(value)
    except (TypeError, ValueError):
        return None


def boolean_or_none(value) -> bool | None:
    return value if isinstance(value, bool) else None


def percentage_metric(records: list[MissionRecord], value_getter, positive) -> dict:
    values = [value_getter(record) for record in records]
    valid = [value for value in values if value is not None]
    numerator = sum(1 for value in valid if positive(value))
    return {"numerator": numerator, "valid_missions": len(valid), "percentage": round(numerator * 100 / len(valid), 1) if valid else None}


def summarize(records: list[MissionRecord], category: str | None = None) -> dict:
    counts = {"missions": len(records), "first_attempt": 0, "second_attempt": 0, "third_plus": 0}
    durations = []
    total_attempts = 0
    for record in records:
        attempts = record.attempts
        if attempts is not None:
            total_attempts += attempts
        if attempts == 1:
            counts["first_attempt"] += 1
        elif attempts == 2:
            counts["second_attempt"] += 1
        elif attempts is not None:
            counts["third_plus"] += 1
        if record.duration_seconds is not None:
            durations.append(record.duration_seconds)
    missions = counts["missions"]
    valid_attempts = [record.attempts for record in records if record.attempts is not None]
    counts["total_attempts"] = total_attempts if valid_attempts else None
    counts["attempt_valid_missions"] = len(valid_attempts)
    counts["average_attempts"] = round(total_attempts / len(valid_attempts), 2) if valid_attempts else None
    counts["first_attempt_rate"] = round((counts["first_attempt"] / len(valid_attempts)) * 100, 1) if valid_attempts else None
    counts["retry_rate"] = round(((len(valid_attempts) - counts["first_attempt"]) / len(valid_attempts)) * 100, 1) if valid_attempts else None
    if category == "comprehension":
        counts["comprehension_help"] = percentage_metric(records, lambda record: record.max_help_level, lambda value: value >= 1)
        counts["orthographic_support"] = percentage_metric(records, lambda record: record.orthographic_support, lambda value: value)
        counts["help_distribution"] = distribution(records, lambda record: record.max_help_level, {0: "Sin ayuda", 1: "Reformulación", 2: "Pista", 3: "Guía"})
        counts["writing_distribution"] = distribution(records, lambda record: record.writing_max_level, {"none": "Sin apoyo ortográfico", "level_1": "Nivel 1", "level_2": "Nivel 2", "revealed": "Respuesta escrita revelada"})
    counts["median_seconds"] = round(median(durations), 1) if durations else None
    return counts


def distribution(records: list[MissionRecord], value_getter, labels: dict) -> list[dict]:
    valid_values = [value_getter(record) for record in records if value_getter(record) is not None]
    rows = [{"value": value, "label": label, "missions": valid_values.count(value), "percentage": round(valid_values.count(value) * 100 / len(valid_values), 1) if valid_values else None} for value, label in labels.items()]
    return rows + ([{"value": None, "label": "Sin dato", "missions": len(records) - len(valid_values), "percentage": None}] if len(records) != len(valid_values) else [])


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
            variant_id = record.variant_id or ""
            label = QUESTION_LABELS.get(variant_id) or "Sin dato"
        grouped[(key, label)].append(record)
    rows = []
    for (key, label), row_records in grouped.items():
        categories = {record.category_id for record in row_records}
        row_category = next(iter(categories)) if len(categories) == 1 else None
        row = {"key": key, "label": label, **summarize(row_records, row_category)}
        rows.append(row)
    return sorted(rows, key=lambda row: (-row["missions"], row["label"]))


def daily_rows(records: list[MissionRecord], tz: ZoneInfo) -> list[dict]:
    grouped: dict[tuple[str, str], int] = defaultdict(int)
    for record in records:
        day = as_utc(record.solved_at).astimezone(tz).date().isoformat()
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


def trend_dimension(category: str | None, level: str | None) -> str | None:
    if not category:
        return "category"
    if not level:
        return "level"
    return "skill"


def trend_key_and_label(record: MissionRecord, dimension: str) -> tuple[str, str]:
    if dimension == "category":
        key = record.category_id or "legacy"
        return key, scope_label(record.category_id) if record.category_id else "Histórico sin clasificar"
    if dimension == "level":
        key = record.level_id or "legacy"
        return key, scope_label(record.category_id, record.level_id) if record.category_id and record.level_id else "Histórico sin clasificar"
    key = record.skill_id or "legacy"
    return key, scope_label(record.category_id, record.level_id, record.skill_id) if record.category_id and record.level_id and record.skill_id else "Histórico sin clasificar"


def trend_rows(records: list[MissionRecord], tz: ZoneInfo, start: date | None, end: date | None, dimension: str | None) -> list[dict]:
    if not dimension or not start or not end or start >= end:
        return []
    series_labels: dict[str, str] = {}
    values: dict[tuple[str, str], dict] = defaultdict(lambda: {"missions": 0, "attempts": 0, "attempt_valid_missions": 0})
    for record in records:
        day = as_utc(record.solved_at).astimezone(tz).date().isoformat()
        key, label = trend_key_and_label(record, dimension)
        series_labels[key] = label
        bucket = values[(day, key)]
        bucket["missions"] += 1
        if record.attempts is not None:
            bucket["attempts"] += record.attempts
            bucket["attempt_valid_missions"] += 1
    rows = []
    current = start
    while current <= end:
        day = current.isoformat()
        series = [
            {"key": key, "label": label, **values[(day, key)]}
            for key, label in sorted(series_labels.items(), key=lambda item: item[1])
        ]
        rows.append({
            "day": day,
            "label": current.strftime("%d/%m/%y"),
            "missions": sum(item["missions"] for item in series),
            "attempts": sum(item["attempts"] for item in series),
            "attempt_valid_missions": sum(item["attempt_valid_missions"] for item in series),
            "series": series,
        })
        current += timedelta(days=1)
    return rows


def help_label(level: int | None) -> str | None:
    return {1: "Reformulación", 2: "Pista", 3: "Guía"}.get(level)


def execution_detail(record: MissionRecord, tz: tzinfo) -> dict:
    timeline = []
    for event in sorted(record.events or [], key=lambda item: (item.occurred_at, item.event_id)):
        payload = event.payload or {}
        attempt = integer_or_none(payload.get("attempt")) if "attempt" in payload else None
        if event.event_type == "MissionFailed":
            reason = payload.get("failureReason") if "failureReason" in payload else None
            result = {"wrong_answer": "Incorrecta", "orthographic_error": "Error ortográfico", "invalid_input": "Respuesta inválida"}.get(reason, "Sin dato")
            timeline.append({"kind": "attempt", "attempt": attempt, "answer": payload.get("answer") if "answer" in payload else None, "result": result, "failure_reason": reason, "timestamp": event.occurred_at})
        elif event.event_type == "MissionSolved":
            timeline.append({"kind": "attempt", "attempt": attempt, "answer": payload.get("answer") if "answer" in payload else None, "result": "Correcta", "failure_reason": None, "timestamp": event.occurred_at})
        elif event.event_type == "MissionHelpRequested":
            level = integer_or_none(payload.get("help_level")) if "help_level" in payload else None
            timeline.append({"kind": "comprehension_help", "level": level, "label": help_label(level) or "Sin dato", "timestamp": event.occurred_at})
        elif event.event_type == "MissionWritingHintShown":
            stage = integer_or_none(payload.get("writing_hint_stage")) if "writing_hint_stage" in payload else None
            writing_label = {1: "Apoyo de escritura", 2: "Segundo apoyo de escritura", 3: "Respuesta escrita revelada"}.get(stage, "Sin dato")
            timeline.append({"kind": "writing_help", "level": stage, "label": writing_label, "timestamp": event.occurred_at})
    attempts = record.attempts
    help_text = "Sin dato" if record.max_help_level is None else help_label(record.max_help_level) or "Sin dato"
    return {
        "mission_id": record.mission_id,
        "started_at": record.started_at.astimezone(tz).strftime("%d/%m/%Y %H:%M") if record.started_at else None,
        "question_text": record.question_text,
        "question_label": record.question_label,
        "attempts": attempts,
        "max_help_label": help_text,
        "orthographic_support": record.orthographic_support,
        "timeline": timeline,
    }


def dashboard_data(db: Session, device: Device, period: str, start: str | None, end: str | None, category: str | None, level: str | None, skill: str | None) -> dict:
    tz = device_timezone(device)
    _, resolved_start, resolved_end, start_utc, end_utc, _ = resolved_period(period, tz, start, end)
    # Los intentos de una misión pueden cruzar el límite del período. Se cargan sólo
    # eventos de misión del dispositivo y luego se filtra por la fecha de resolución,
    # conservando el MissionStarted necesario para la métrica experimental de tiempo.
    query = db.query(DeviceEvent).filter(DeviceEvent.device_id == device.id, DeviceEvent.event_type.in_(MISSION_EVENTS))
    records = mission_records(query.order_by(DeviceEvent.occurred_at.asc(), DeviceEvent.event_id.asc()).all())
    if start_utc is not None:
        records = [record for record in records if start_utc <= as_utc(record.solved_at) < end_utc]
    records = [record for record in records if matches_scope(record, category, level, skill)]
    dimension = "category" if not category else "level" if not level else "skill" if not skill else "variant"
    trend_dimension_value = trend_dimension(category, level) if not skill else None
    return {
        "timezone": str(tz),
        "summary": summarize(records, category),
        "rows": group_rows(records, dimension),
        "variants": group_rows(records, "variant") if skill else [],
        "executions_by_variant": {variant: [execution_detail(record, tz) for record in sorted([candidate for candidate in records if candidate.variant_id == variant], key=lambda candidate: (candidate.solved_at, candidate.mission_id), reverse=True)] for variant in {record.variant_id for record in records if record.variant_id}} if skill else {},
        "daily": daily_rows(records, tz),
        "trends": trend_rows(records, tz, resolved_start, resolved_end, trend_dimension_value),
        "trend_dimension": trend_dimension_value,
        "scope_label": scope_label(category, level, skill),
        "record_count": len(records),
    }
