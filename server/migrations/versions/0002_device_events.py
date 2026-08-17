"""add device events

Revision ID: 0002_device_events
Revises: 0001_initial
Create Date: 2026-08-16
"""

from alembic import op
import sqlalchemy as sa
from sqlalchemy.dialects import postgresql

revision = "0002_device_events"
down_revision = "0001_initial"
branch_labels = None
depends_on = None


def upgrade():
    bind = op.get_bind()
    payload_type = postgresql.JSONB() if bind.dialect.name == "postgresql" else sa.JSON()
    op.create_table(
        "device_events",
        sa.Column("id", sa.String(length=36), primary_key=True),
        sa.Column("event_id", sa.String(length=36), nullable=False),
        sa.Column("device_id", sa.String(length=36), sa.ForeignKey("devices.id"), nullable=False),
        sa.Column("occurred_at", sa.DateTime(timezone=True), nullable=False),
        sa.Column("received_at", sa.DateTime(timezone=True), nullable=False),
        sa.Column("event_type", sa.String(length=120), nullable=False),
        sa.Column("client_version", sa.String(length=40), nullable=True),
        sa.Column("payload", payload_type, nullable=False),
        sa.UniqueConstraint("event_id", name="uq_device_events_event_id"),
    )
    op.create_index("ix_device_events_device_id", "device_events", ["device_id"])
    op.create_index("ix_device_events_event_type", "device_events", ["event_type"])
    op.create_index("ix_device_events_event_id", "device_events", ["event_id"])


def downgrade():
    op.drop_index("ix_device_events_event_id", table_name="device_events")
    op.drop_index("ix_device_events_event_type", table_name="device_events")
    op.drop_index("ix_device_events_device_id", table_name="device_events")
    op.drop_table("device_events")
