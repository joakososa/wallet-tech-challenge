# ADR 008 — Repositorio y ejecución

- **Estado:** Aceptado
- **Fecha:** 2026-09-29

## Contexto
El entregable es **un** repositorio Git con historial de commits y un `README.md` con instrucciones para levantar el proyecto. La dockerización del backend y del frontend es opcional; se recomienda incluir en un `docker-compose.yml` el motor de persistencia, si requiere un servicio propio. Toda la configuración se maneja por variables de entorno. El entorno de desarrollo tenía .NET SDK 9 y Node 16, que no alcanzan para .NET 10 ni para el Angular actual.

## Decisión
1. **Monorepo:** `backend/`, `frontend/`, `docs/` y `docker-compose.yml` en la raíz.
2. **Desarrollo local** con .NET SDK 10 (fijado con `global.json`) y Node 22 (gestionado con nvm-windows). Los SDKs de .NET se instalan en paralelo, así que no hace falta un gestor de versiones.
3. **`docker compose up` levanta todo** (PostgreSQL, API y UI) para quien evalúa, sin instalar toolchains. El README documenta también cómo levantarlo en local.
4. **Configuración por variables de entorno.** El repo incluye un `.env.example` con valores de ejemplo; el `.env` real no se versiona.

## Alternativas consideradas
- **Dos repositorios (frontend y backend):**
  - quien evalúa tiene que clonar ambos en rutas y versiones compatibles;
  - el `docker-compose.yml` necesitaría acceder a los dos, o que se publiquen imágenes;
  - un cambio de contrato de la API y su adaptación en el front quedarían en historiales separados.

  Tiene sentido con equipos o ciclos de deploy independientes, que acá no existen.
- **Solo local, sin Docker:** obliga a quien evalúa a instalar .NET 10, Node y PostgreSQL.
- **Solo Docker:** el ciclo de desarrollo es más lento (hay que reconstruir imágenes).

## Consecuencias
- Un solo `git clone` más `docker compose up` alcanza para ver el sistema funcionando.
- Hay que mantener dos Dockerfiles multi-stage y el compose; el costo es bajo.
