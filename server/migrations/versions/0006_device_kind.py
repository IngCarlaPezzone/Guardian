"""classify operational and synthetic devices

Revision ID: 0006_device_kind
Revises: 0005_device_timezone
Create Date: 2026-08-29
"""

import json
import uuid

from alembic import op
import sqlalchemy as sa


revision = "0006_device_kind"
down_revision = "0005_device_timezone"
branch_labels = None
depends_on = None

SEED_NAMESPACE = uuid.UUID("4a47d9b4-5d2f-4ba4-82b3-b2c389d1b5dc")
IMPORTED_MARKER = "prod-telemetry-sanitized-v1"


def seed_device_id(key: str) -> str:
    return str(uuid.uuid5(SEED_NAMESPACE, key))


def payload_value(value):
    if isinstance(value, dict):
        return value
    if isinstance(value, str):
        try:
            return json.loads(value)
        except json.JSONDecodeError:
            return {}
    return {}


def upgrade():
    op.add_column("devices", sa.Column("device_kind", sa.String(length=32), nullable=False, server_default="operational"))
    connection = op.get_bind()
    seed_ids = [seed_device_id(key) for key in ("stg-online-active", "stg-online-paused", "stg-offline")]
    connection.execute(
        sa.text("UPDATE devices SET device_kind = 'stg_demo' WHERE id IN :ids").bindparams(sa.bindparam("ids", expanding=True)),
        {"ids": seed_ids},
    )
    imported_ids = {
        row.device_id
        for row in connection.execute(sa.text("SELECT device_id, payload FROM device_events"))
        if payload_value(row.payload).get("import_source") == IMPORTED_MARKER
    }
    if imported_ids:
        connection.execute(
            sa.text("UPDATE devices SET device_kind = 'stg_imported_telemetry' WHERE id IN :ids").bindparams(sa.bindparam("ids", expanding=True)),
            {"ids": sorted(imported_ids)},
        )


def downgrade():
    op.drop_column("devices", "device_kind")
