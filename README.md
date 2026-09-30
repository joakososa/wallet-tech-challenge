# E-Cred Wallet

Sistema de emisión y gestión de **credenciales digitales verificables** para los socios del Club de Fútbol de Córdoba. Cada credencial se firma criptográficamente (HMAC-SHA256) para garantizar su autenticidad.

La arquitectura separa, a nivel de servicio, el rol de negocio (**Tenant**, el club) del rol de emisión (**Issuer**).

> 🚧 Proyecto en construcción. Las secciones marcadas como *pendiente* se completan a medida que avanza la implementación.

## Funcionalidades

- **UC01 — Alta de credencial:** carga de los datos del socio (nombre, apellido, DNI, categoría, foto), emisión y firma por el Issuer, y persistencia de la credencial completa. Si la firma falla, no se persiste nada.
- **UC02 — Listado de credenciales:** foto, nombre, apellido, categoría, número de socio, vigencia y estado, con filtro por socio y estado vacío.

## Stack

| Componente | Tecnología |
|---|---|
| Backend | .NET 10 (ASP.NET Core, EF Core) |
| Persistencia | PostgreSQL |
| Frontend | Angular |
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

*Pendiente.*

### Local

**Requisitos:** .NET SDK 10, Node 24 LTS (por ejemplo con [nvm-windows](https://github.com/coreybutler/nvm-windows)), Docker (para PostgreSQL y los tests de integración).

*Pendiente.*

### Configuración

Toda la configuración se maneja por variables de entorno. Ver `.env.example` (*pendiente*).

| Variable | Descripción |
|---|---|
| `ConnectionStrings__Wallet` | Cadena de conexión a PostgreSQL |
| `Issuer__Did` | DID del emisor (`did:example:futbol`) |
| `Issuer__SigningKey` | Secreto HMAC (mínimo 32 bytes). **Nunca versionar.** |
| `Issuer__KeyId` | Identificador de la clave vigente (`key-1`). El Issuer arma `verificationMethod` = `{Did}#{KeyId}` |
| `Issuer__CredentialBaseUri` | Base del `id` de la credencial (`https://credenciales.futbol.com.ar/`) |
| `Tenant__Id` | Identificador del tenant (`club-futbol`) |
| `Issuer__SimulateFailure` | Solo en Development: fuerza una falla de firma para demostrar el manejo de errores |

## Tests

*Pendiente.*

## Documentación

- [Arquitectura](docs/architecture.md)
- [Decisiones de arquitectura (ADRs)](docs/adr/)
