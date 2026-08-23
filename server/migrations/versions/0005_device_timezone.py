"""add configurable device timezone

Revision ID: 0005_device_timezone
Revises: 0004_mission_system_v2
Create Date: 2026-08-23
"""

from alembic import op
import sqlalchemy as sa


revision = "0005_device_timezone"
down_revision = "0004_mission_system_v2"
branch_labels = None
depends_on = None


def upgrade():
    op.add_column(
        "device_configurations",
        sa.Column("timezone", sa.String(length=64), nullable=False, server_default="UTC"),
    )


def downgrade():
    op.drop_column("device_configurations", "timezone")
