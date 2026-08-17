FROM python:3.12-slim

ENV PYTHONDONTWRITEBYTECODE=1
ENV PYTHONUNBUFFERED=1

WORKDIR /app

COPY server/requirements.txt /app/requirements.txt
RUN pip install --no-cache-dir -r /app/requirements.txt

COPY server /app/server

WORKDIR /app
EXPOSE 8080

CMD ["sh", "-c", "alembic -c server/alembic.ini upgrade head && python -m server.app.bootstrap && uvicorn server.app.main:app --host 0.0.0.0 --port 8080"]
