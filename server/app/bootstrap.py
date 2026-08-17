from sqlalchemy.orm import Session

from server.app.db import SessionLocal
from server.app.models import AdminUser, Base
from server.app.db import engine
from server.app.config import settings
from server.app.security import hash_secret


def ensure_admin(db: Session) -> None:
    existing = db.query(AdminUser).filter(AdminUser.username == settings.guardian_admin_username).first()
    if existing:
        return
    db.add(AdminUser(
        username=settings.guardian_admin_username,
        password_hash=hash_secret(settings.guardian_admin_initial_password),
    ))
    db.commit()


def main() -> None:
    Base.metadata.create_all(bind=engine)
    with SessionLocal() as db:
        ensure_admin(db)


if __name__ == "__main__":
    main()
