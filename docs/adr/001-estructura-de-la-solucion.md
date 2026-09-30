# ADR 001 — Estructura de la solución

- **Estado:** Aceptado
- **Fecha:** 2026-09-29

## Contexto
El enunciado exige separar, **a nivel de servicio**, el rol de negocio (Tenant, el club) del rol de emisión de credenciales (Issuer). El Issuer es un servicio in-process, sin endpoint propio, y el backend expone un único controller. Clean Architecture es opcional y la estructura de capas queda a criterio del candidato. El dominio es chico (dos entidades) y el tiempo es acotado.

## Decisión
Una solución .NET con cuatro proyectos de producción y dos de tests:

```
Wallet.Api ──────────┬──────────────┬─────────────┐
                     ▼              ▼             ▼
          Wallet.Infrastructure ─▶ Wallet.Tenant ─▶ Wallet.Issuer
                                                   (no referencia a nadie)
```

| Proyecto | Responsabilidad |
|---|---|
| `Wallet.Issuer` | Canonicaliza, firma (HMAC-SHA256) y arma la VC. Expone la interfaz pública `ICredentialIssuer`; la implementación es `internal`. No conoce socios, bases de datos ni HTTP. |
| `Wallet.Tenant` | El rol de negocio del club: casos de uso (alta, listado), entidades `Socio` y `Credential`, e interfaces de persistencia. No sabe qué motor de base de datos hay atrás. |
| `Wallet.Infrastructure` | Implementa las interfaces de persistencia con EF Core + Npgsql (secuencia, transacción, migraciones). |
| `Wallet.Api` | Punto de entrada HTTP: `CredentialsController`, validación, ProblemDetails, configuración y composición de dependencias. |
| `Wallet.UnitTests` / `Wallet.IntegrationTests` | Tests unitarios y de integración (API + PostgreSQL real vía Testcontainers). |

La capa de aplicación se llama `Tenant` (y no `Application`) para usar el vocabulario del enunciado: Tenant e Issuer quedan como dos proyectos hermanos.

## Alternativas consideradas
- **Clean Architecture completa** (Domain, Application, Infrastructure y Api, con puertos y adaptadores estrictos): agrega un proyecto Domain para solo dos entidades, más mappers y capas de DTOs intermedios. Es mucha ceremonia para un dominio chico y con poco tiempo, sin un beneficio proporcional.
- **Un único proyecto con carpetas:** es más rápido, pero la separación entre carpetas es solo una convención. Nada impide que el Issuer termine accediendo a la base de datos, así que la separación a nivel de servicio no quedaría garantizada.

## Consecuencias
- **Desacople verificado por el compilador:** como `Wallet.Issuer` no referencia a ningún otro proyecto, no puede depender de la persistencia ni del negocio.
- **Reutilización:** el Issuer podría emitir credenciales desde otros flujos de negocio (otros tipos de credencial, otros tenants) sin arrastrar dependencias.
- **Testabilidad:** el caso de uso de alta se prueba con dobles (por ejemplo, un Issuer que falla para verificar que no se persiste nada).
- Hay algunos proyectos más que en la opción de un único proyecto; es un costo bajo.
