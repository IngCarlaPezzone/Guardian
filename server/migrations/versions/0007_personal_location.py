"""add personal location fields to mission profile

Revision ID: 0007_personal_location
Revises: 0006_device_kind
Create Date: 2026-09-09
"""

from alembic import op
import sqlalchemy as sa


revision = "0007_personal_location"
down_revision = "0006_device_kind"
branch_labels = None
depends_on = None


def upgrade():
    op.add_column("device_mission_profiles", sa.Column("city", sa.String(length=255), nullable=True))
    op.add_column("device_mission_profiles", sa.Column("province", sa.String(length=255), nullable=True))
    op.add_column("device_mission_profiles", sa.Column("country", sa.String(length=255), nullable=True))


def downgrade():
    op.drop_column("device_mission_profiles", "country")
    op.drop_column("device_mission_profiles", "province")
    op.drop_column("device_mission_profiles", "city")
