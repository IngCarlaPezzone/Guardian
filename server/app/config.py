from pathlib import Path

from pydantic_settings import SettingsConfigDict
from pydantic_settings import BaseSettings


class Settings(BaseSettings):
    model_config = SettingsConfigDict(env_file=".env", extra="ignore")

    database_url: str = "sqlite:///./guardian-dev.db"
    guardian_admin_username: str = "admin"
    guardian_admin_initial_password: str = "change_me"
    guardian_session_secret: str = "change_me"
    device_bootstrap_token: str = "change_me"
    guardian_admin_host: str = "guardian.example.com"
    guardian_admin_timezone: str = ""
    releases_dir: Path = Path("/data/guardian/releases")
    online_threshold_seconds: int = 180
    update_command_timeout_seconds: int = 900


settings = Settings()
