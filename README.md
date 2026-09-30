# E-Cred Wallet

Sistema de emisión y gestión de **credenciales digitales verificables** para los socios del Club de Fútbol de Córdoba. Cada credencial se firma criptográficamente (HMAC-SHA256) para garantizar su autenticidad.

La arquitectura separa, a nivel de servicio, el rol de negocio (**Tenant**, el club) del rol de emisión (**Issuer**).

## Funcionalidades

- **UC01 — Alta de credencial:** carga de los datos del socio (nombre, apellido, DNI, categoría, foto), emisión y firma por el Issuer, y persistencia de la credencial completa. Si la firma falla, no se persiste nada. Al confirmar, una pantalla de resultado muestra el número de socio y la vigencia.
- **UC02 — Listado de credenciales:** foto, nombre, apellido, categoría, número de socio, vigencia y estado, con filtro por DNI y número de socio, estado vacío y un detalle expandible con la credencial completa (`id`, `type`, `issuer`, `proof`).

## Stack

| Componente | Tecnología |
|---|---|
| Backend | .NET 10 (ASP.NET Core, EF Core) |
| Persistencia | PostgreSQL 17 |
| Frontend | Angular 22 |
| Infraestructura | Docker Compose |

## Estructura del repositorio

```
/
├─ backend/     Solución .NET (Api, Tenant, Issuer, Infrastructure + tests)
├─ frontend/    Aplicación Angular
├─ docs/        Arquitectura y decisiones (ADRs)
└─ docker-compose.yml
```

## Cómo levantar el proyecto

### Con Docker (recomendado)

Requisito: Docker con Compose.

```bash
cp .env.example .env
# Editar .env y definir Issuer__SigningKey con un secreto propio de al menos 32 bytes.
docker compose up --build
```

- Aplicación: <http://localhost:4200>
- API: <http://localhost:5080> (por ejemplo `GET /api/credentials` y `GET /health`)

Las migraciones se aplican solas al arrancar la API (ADR 017). Para empezar de cero: `docker compose down -v`.

### Local (sin contenedores para la aplicación)

**Requisitos:** .NET SDK 10, Node 24 LTS y Docker (para PostgreSQL y los tests de integración).

1. Base de datos:

   ```bash
   cp .env.example .env
   docker compose up -d db
   ```

2. API. Las variables de entorno están en `.env.example`; en bash se pueden cargar con `set -a; source .env; set +a`. En PowerShell hay que definirlas una por una (`$env:Issuer__SigningKey = "..."`).

   ```bash
   cd backend
   dotnet run --project src/Wallet.Api   # http://localhost:5080
   ```

3. Frontend. `ng serve` reenvía `/api` a `http://localhost:5080` (proxy en `frontend/proxy.conf.json`).

   ```bash
   cd frontend
   npm install
   npm start                             # http://localhost:4200
   ```

### Configuración

Toda la configuración se maneja por variables de entorno (`.env.example` tiene un ejemplo completo; el `.env` real no se versiona).

| Variable | Descripción |
|---|---|
| `ConnectionStrings__Wallet` | Cadena de conexión a PostgreSQL (con `Timeout=5` para el timeout de conexión) |
| `Issuer__Did` | DID del emisor (`did:example:futbol`) |
| `Issuer__SigningKey` | Secreto HMAC (mínimo 32 bytes). **Nunca versionar.** |
| `Issuer__KeyId` | Identificador de la clave vigente (`key-1`). El Issuer arma `verificationMethod` = `{Did}#{KeyId}` |
| `Issuer__CredentialBaseUri` | Base del `id` de la credencial (`https://credenciales.futbol.com.ar/`) |
| `Tenant__Id` | Identificador del tenant (`club-futbol`) |
| `Database__MigrateOnStartup` | Aplica las migraciones al arrancar (por defecto `true`) |
| `Issuer__SimulateFailure` | Solo con `ASPNETCORE_ENVIRONMENT=Development`: toda alta falla al firmar (ADR 012 y 017) |

### Demostrar el error de firma (UC01 5a)

Con `ASPNETCORE_ENVIRONMENT=Development` y `Issuer__SimulateFailure=true` (en el `.env` con Docker, o exportadas en local), toda alta responde `500` con `code: issuer_signing_failed` y **no se persiste nada**. Se ve en la pantalla de alta y se puede comprobar en la base:

```bash
docker compose exec db psql -U wallet -d wallet -c "select (select count(*) from socios) as socios, (select count(*) from credentials) as credentials;"
```

El número de socio se toma de una secuencia antes de firmar (ADR 003), así que una falla deja un hueco en la numeración; es una consecuencia aceptada y documentada.

## API

| Método y ruta | Descripción |
|---|---|
| `POST /api/credentials` | Alta. Cuerpo: `nombre`, `apellido`, `dni`, `categoria` (`adulto`, `juvenil`, `niño`), `foto` (URL). Responde `201` con `numeroSocio`, `isNewSocio`, vigencia y la VC completa. |
| `GET /api/credentials?dni=&numeroSocio=` | Listado, de la más nueva a la más vieja. Los filtros son opcionales. |
| `GET /health` | Estado de la conexión a la base. |

Los errores siguen ProblemDetails (RFC 9457): `400` con los errores por campo, `409 duplicate_dni`, `500 issuer_signing_failed` y `503 database_unavailable`. El contrato completo está en el [ADR 016](docs/adr/016-contrato-http-y-decisiones-abiertas.md).

## Tests

Desde `backend/` (los de integración levantan PostgreSQL con Testcontainers, así que requieren Docker):

```bash
dotnet test
```

Desde `frontend/`:

```bash
npm test -- --watch=false
```

- **Unitarios (backend):** el Issuer (incluye el test *golden* contra el ejemplo del enunciado) y el Tenant (con fakes escritos a mano).
- **Integración (backend):** la API real contra PostgreSQL: alta, socio existente, validación, falla de firma sin filas persistidas, altas concurrentes del mismo DNI, listado y filtros.
- **Frontend:** validadores, cliente HTTP, formulario y listado.

## Decisiones y alcance

Las decisiones relevantes están documentadas en [`docs/adr/`](docs/adr/) y el diseño en [`docs/architecture.md`](docs/architecture.md).

Quedan fuera, por tiempo y documentado en el [ADR 016](docs/adr/016-contrato-http-y-decisiones-abiertas.md): upload de foto (la foto es una URL), *idempotency key*, *circuit breaker* para un Issuer remoto y paginación. Tampoco se implementan la verificación, la revocación ni la autenticación (fuera del alcance del enunciado).

## Documentación

- [Arquitectura](docs/architecture.md)
- [Decisiones de arquitectura (ADRs)](docs/adr/)
