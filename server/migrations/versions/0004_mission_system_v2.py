"""add Mission System v2 configuration and private device profile

Revision ID: 0004_mission_system_v2
Revises: 0003_device_controls
Create Date: 2026-08-22
"""

from alembic import op
import sqlalchemy as sa


revision = "0004_mission_system_v2"
down_revision = "0003_device_controls"
branch_labels = None
depends_on = None


def upgrade():
    op.add_column("device_configurations", sa.Column("mission_config", sa.JSON(), nullable=False, server_default="{}"))
    op.create_table(
        "device_mission_profiles",
        sa.Column("device_id", sa.String(length=36), sa.ForeignKey("devices.id"), primary_key=True),
        sa.Column("preferred_name", sa.String(length=255), nullable=True),
        sa.Column("first_name", sa.String(length=255), nullable=True),
        sa.Column("middle_name", sa.String(length=255), nullable=True),
        sa.Column("last_name", sa.String(length=255), nullable=True),
        sa.Column("birth_date", sa.String(length=10), nullable=True),
        sa.Column("created_at", sa.DateTime(timezone=True), nullable=False),
        sa.Column("updated_at", sa.DateTime(timezone=True), nullable=False),
    )


def downgrade():
    op.drop_table("device_mission_profiles")
    op.drop_column("device_configurations", "mission_config")
