import pytest
from pathlib import Path

from server.app.import_sanitized_telemetry import DATASET_MARKER, normalize_event, stg_device_id


def test_normalize_event_keeps_only_educational_whitelist():
    event = normalize_event({
        "slot": 2,
        "occurred_at": "2026-08-23T12:00:00Z",
        "event_type": "MissionSolved",
        "client_version": "0.4.1",
        "payload": {"mission_id": "m1", "attempt": 2, "answer": "private", "preferred_name": "private"},
    }, 4)
    assert event["payload"] == {"mission_id": "m1", "attempt": 2, "import_source": DATASET_MARKER}
    assert event["event_id"]
    assert stg_device_id(2) != stg_device_id(3)


def test_normalize_event_rejects_invalid_slot():
    with pytest.raises(ValueError, match="slot"):
        normalize_event({"slot": 0, "occurred_at": "2026-08-23T12:00:00Z", "event_type": "MissionStarted"}, 0)


def test_windows_export_uses_stdin_not_python_code_argument():
    script = (Path(__file__).parents[2] / "scripts" / "import-prod-telemetry-to-stg.ps1").read_text(encoding="utf-8")
    assert "$exportCode | & docker compose" in script
    assert "guardian-app python -" in script
    assert "python -c $exportCode" not in script
