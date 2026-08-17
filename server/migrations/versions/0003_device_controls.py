"""add remote device controls

Revision ID: 0003_device_controls
Revises: 0002_device_events
Create Date: 2026-08-17
"""

from alembic import op
import sqlalchemy as sa


revision = "0003_device_controls"
down_revision = "0002_device_events"
branch_labels = None
depends_on = None


def upgrade():
    op.add_column("devices", sa.Column("monitoring_enabled", sa.Boolean(), nullable=False, server_default=sa.true()))
    op.create_table(
        "device_commands",
        sa.Column("id", sa.String(length=36), primary_key=True),
        sa.Column("device_id", sa.String(length=36), sa.ForeignKey("devices.id"), nullable=False),
        sa.Column("command_type", sa.String(length=64), nullable=False),
        sa.Column("status", sa.String(length=32), nullable=False, server_default="pending"),
        sa.Column("requested_at", sa.DateTime(timezone=True), nullable=False),
        sa.Column("acknowledged_at", sa.DateTime(timezone=True), nullable=True),
        sa.Column("completed_at", sa.DateTime(timezone=True), nullable=True),
        sa.Column("error_message", sa.Text(), nullable=True),
    )
    op.create_index("ix_device_commands_device_id", "device_commands", ["device_id"])
    op.create_index("ix_device_commands_command_type", "device_commands", ["command_type"])
    op.create_index("ix_device_commands_status", "device_commands", ["status"])


def downgrade():
    op.drop_index("ix_device_commands_status", table_name="device_commands")
    op.drop_index("ix_device_commands_command_type", table_name="device_commands")
    op.drop_index("ix_device_commands_device_id", table_name="device_commands")
    op.drop_table("device_commands")
    op.drop_column("devices", "monitoring_enabled")
