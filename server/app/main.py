from pathlib import Path

from fastapi import Request
from fastapi import FastAPI
from fastapi.staticfiles import StaticFiles
from fastapi.responses import JSONResponse
from sqlalchemy import text

from server.app.admin import router as admin_router
from server.app.api import router as api_router
from server.app.config import settings
from server.app.db import SessionLocal

app = FastAPI(title="Guardian Server")
app.mount("/admin/static", StaticFiles(directory=str(Path(__file__).parent / "static")), name="admin-static")
app.include_router(api_router)
app.include_router(admin_router)


@app.middleware("http")
async def block_device_api_on_public_admin_host(request: Request, call_next):
    public_host = (settings.guardian_admin_host or "").split(":")[0].lower()
    request_host = (request.headers.get("host") or "").split(":")[0].lower()
    if public_host and request_host == public_host and request.url.path.startswith("/api/v1/"):
        return JSONResponse({"detail": "device api is only available on LAN"}, status_code=404)
    return await call_next(request)


@app.get("/health")
def health():
    with SessionLocal() as db:
        db.execute(text("select 1"))
    return {"status": "ok"}
