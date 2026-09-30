# ADR 007 — Validación y formato de errores

- **Estado:** Aceptado
- **Fecha:** 2026-09-29

## Contexto
El formulario de alta especifica controles de UI (input numérico para el DNI, select para la categoría, etc.), pero la validación de la UI se puede saltear con cualquier cliente HTTP. En este sistema, lo que entra **queda firmado y persistido para siempre**: una credencial emitida con datos inválidos no se puede corregir sin reemitirla. Además, el frontend necesita un formato de error uniforme para mostrar los problemas campo por campo.

## Decisión
1. **El backend valida todo**, aunque la UI también valide para dar feedback inmediato:
   - `nombre`, `apellido`: obligatorios, sin espacios sobrantes (*trim*), con longitud máxima.
   - `dni`: normalizado; solo dígitos, entre 7 y 9 (ver ADR 006).
   - `categoria`: uno de `adulto`, `juvenil`, `niño`.
   - `foto`: URL absoluta `http` o `https`.
2. **Formato de error: ProblemDetails (RFC 9457).** Es el estándar para errores de APIs HTTP y el comportamiento por defecto de ASP.NET Core:
   - `[ApiController]` devuelve `ValidationProblemDetails` con los errores agrupados por campo.
   - `AddProblemDetails()` y un manejador de excepciones unifican el resto de los errores.

| Situación | Código | Detalle |
|---|---|---|
| Datos inválidos | 400 | `ValidationProblemDetails` con errores por campo |
| Falla de firma en el Issuer (UC01 5a) | 500 | `code: issuer_signing_failed`; no se persiste nada |
| Base de datos no disponible / timeout | 503 | `code: database_unavailable` |
| Error inesperado | 500 | Sin detalles internos en la respuesta; se registra en el log |

## Alternativas consideradas
- **Validar solo en la UI:** inseguro; cualquier cliente HTTP la saltea.
- **Formato de error propio:** no aporta nada frente a un estándar que el framework ya soporta.

## Consecuencias
- El frontend mapea los errores por campo del backend a los campos del formulario de manera uniforme.
- Las reglas de validación se duplican entre la UI y el backend. Se acepta: la del backend manda y la de la UI es solo una comodidad.
