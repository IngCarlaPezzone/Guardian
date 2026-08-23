import argparse
import hashlib
from pathlib import Path

from server.app.config import settings
from server.app.db import SessionLocal
from server.app.models import Release
from server.app.security import is_prerelease, valid_semver


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--version", required=True)
    parser.add_argument("--file", required=True)
    parser.add_argument("--notes", default="")
    parser.add_argument("--allow-prod-version", action="store_true", help="allow an unsuffixed PROD reproduction in STG")
    args = parser.parse_args()

    if not valid_semver(args.version):
        raise SystemExit("version must be SemVer, optionally with a prerelease suffix")
    environment = settings.guardian_environment.strip().upper()
    if environment == "PROD" and is_prerelease(args.version):
        raise SystemExit("prerelease versions must never be registered in PROD")
    if environment == "STG" and not is_prerelease(args.version) and not args.allow_prod_version:
        raise SystemExit("STG requires a suffixed version unless reproducing an existing PROD version explicitly")

    source = Path(args.file)
    if not source.exists():
        raise SystemExit("release file does not exist")

    settings.releases_dir.mkdir(parents=True, exist_ok=True)
    target = settings.releases_dir / source.name
    if source.resolve() != target.resolve():
        target.write_bytes(source.read_bytes())

    data = target.read_bytes()
    sha256 = hashlib.sha256(data).hexdigest()

    with SessionLocal() as db:
        release = db.query(Release).filter(Release.version == args.version).first()
        if release is None:
            release = Release(version=args.version, filename=target.name, sha256=sha256, file_size=len(data), release_notes=args.notes or None)
            db.add(release)
        else:
            release.filename = target.name
            release.sha256 = sha256
            release.file_size = len(data)
            release.release_notes = args.notes or None
            release.is_active = True
        db.commit()
        print(f"registered release {release.version} {release.sha256}")


if __name__ == "__main__":
    main()
