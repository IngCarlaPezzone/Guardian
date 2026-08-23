from server.app.config import Settings
from server.app import seed_stg
from server.app.security import is_prerelease, valid_semver


def test_stg_title_is_explicit():
    assert Settings(guardian_environment="STG").admin_title == "Guardian Admin — STG"
    assert Settings(guardian_environment="PROD").admin_title == "Guardian Admin"


def test_seed_refuses_non_staging_environment(monkeypatch):
    monkeypatch.setattr(seed_stg.settings, "guardian_environment", "PROD")
    try:
        seed_stg.main()
    except SystemExit as error:
        assert "must be STG" in str(error)
    else:
        raise AssertionError("the STG seed must refuse PROD")


def test_versioning_accepts_stg_and_rc_prereleases():
    assert valid_semver("0.1.2-staging-environment")
    assert valid_semver("0.4.2-rc")
    assert is_prerelease("0.4.2-rc")
    assert not is_prerelease("0.4.2")
