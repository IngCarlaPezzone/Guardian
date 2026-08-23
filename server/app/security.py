import base64
import hashlib
import hmac
import os
import re
from datetime import datetime, timezone

from fastapi import Depends, HTTPException, Request
from sqlalchemy.orm import Session

from server.app.config import settings
from server.app.db import get_db
from server.app.models import AdminUser, Device

SEMVER_RE = re.compile(r"^(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)(?:-([0-9A-Za-z][0-9A-Za-z.-]*))?$")
VALID_UPDATE_STATUSES = {"pending", "acknowledged", "downloading", "installing", "success", "failed", "rolled_back", "cancelled"}
VALID_DEVICE_COMMAND_STATUSES = {"pending", "acknowledged", "success", "failed"}
VALID_DEVICE_COMMAND_TYPES = {"pause_monitoring", "resume_monitoring", "trigger_mission_now"}


def hash_secret(value: str, salt: str | None = None) -> str:
    salt = salt or base64.urlsafe_b64encode(os.urandom(16)).decode("ascii")
    digest = hashlib.pbkdf2_hmac("sha256", value.encode("utf-8"), salt.encode("ascii"), 120_000)
    return "pbkdf2_sha256$%s$%s" % (salt, base64.urlsafe_b64encode(digest).decode("ascii"))


def verify_secret(value: str, stored: str) -> bool:
    try:
        _, salt, expected = stored.split("$", 2)
    except ValueError:
        return False
    return hmac.compare_digest(hash_secret(value, salt), stored)


def new_token() -> str:
    return base64.urlsafe_b64encode(os.urandom(32)).decode("ascii").rstrip("=")


def valid_interval(seconds: int) -> bool:
    return 60 <= seconds <= 14400


def valid_semver(version: str) -> bool:
    return bool(SEMVER_RE.match(version or ""))


def is_prerelease(version: str) -> bool:
    match = SEMVER_RE.match(version or "")
    return bool(match and match.group(4))


def utcnow():
    return datetime.now(timezone.utc)


def current_device(device_id: str, request: Request, db: Session = Depends(get_db)) -> Device:
    auth = request.headers.get("authorization", "")
    if not auth.lower().startswith("bearer "):
        raise HTTPException(status_code=401, detail="missing bearer token")
    token = auth.split(" ", 1)[1].strip()
    device = db.get(Device, device_id)
    if device is None or not device.is_active or not verify_secret(token, device.token_hash):
        raise HTTPException(status_code=401, detail="invalid device token")
    return device


def current_admin(request: Request, db: Session = Depends(get_db)) -> AdminUser:
    username = request.cookies.get("guardian_admin")
    signature = request.cookies.get("guardian_admin_sig")
    if not username or not signature:
        raise HTTPException(status_code=303, headers={"Location": "/admin/login"})
    expected = hmac.new(settings.guardian_session_secret.encode("utf-8"), username.encode("utf-8"), hashlib.sha256).hexdigest()
    if not hmac.compare_digest(signature, expected):
        raise HTTPException(status_code=303, headers={"Location": "/admin/login"})
    user = db.query(AdminUser).filter(AdminUser.username == username, AdminUser.is_active.is_(True)).first()
    if not user:
        raise HTTPException(status_code=303, headers={"Location": "/admin/login"})
    return user
