# Cloudflare Tunnel

Etapa 0 usa Cloudflare Tunnel solo para exponer Guardian Admin, no PostgreSQL.

Configurar valores reales solo en `.env`:

```env
GUARDIAN_ADMIN_HOST=guardian.example.com
CLOUDFLARE_TUNNEL_TOKEN=
```

Luego iniciar el perfil opcional:

```powershell
docker compose --env-file .env -f deploy/docker-compose.yml --profile cloudflare up -d
```
