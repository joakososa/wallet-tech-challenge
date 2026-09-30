# ADR 016 — Contrato HTTP y cierre de las decisiones abiertas del Tenant

- **Estado:** Aceptado
- **Fecha:** 2026-09-30

## Contexto
Antes de implementar Infrastructure, Api y el frontend quedaban tres consultas abiertas del ADR 015 (DID del socio, cómo `AddAsync` distingue un socio nuevo, y cómo va la VC dentro de la respuesta HTTP) y no estaba escrito el contrato de la API que el frontend consume. Se fijan acá para poder construir las capas sin idas y vueltas, y para dejar explícito qué queda fuera de esta entrega por tiempo.

## Decisión

### 1. Decisiones abiertas del ADR 015
- **DID del socio:** `did:example:{Id}`, con el mismo `Guid` que el `Id` del socio. Es una simplificación: el enunciado pide un identificador estable y no exige que sea distinto de la clave primaria.
- **`AddAsync` y el socio nuevo:** Infrastructure lo resuelve con el *change tracker* de EF Core. El socio que devolvió `FindByDniAsync` está rastreado y se actualiza; uno que no pasó por el repositorio se inserta. Si el guardado falla, Infrastructure limpia el change tracker, para que el reintento por `DuplicateDniException` no reinserte entidades del primer intento.
- **VC dentro de la respuesta:** el documento persistido (`Document`) se incrusta **como objeto JSON**, no como string escapado. La API lo parsea con `JsonDocument` y lo serializa tal cual, con el encoder `UnsafeRelaxedJsonEscaping` para no escapar tildes ni la ñ.

### 2. Contrato HTTP
| Endpoint | Descripción |
|---|---|
| `POST /api/credentials` | Alta (UC01). Cuerpo: `nombre`, `apellido`, `dni`, `categoria`, `foto`. |
| `GET /api/credentials?dni=&numeroSocio=` | Listado (UC02). Los filtros son opcionales. |
| `GET /health` | Chequea la base de datos. |

**`POST` → 201** (`Location` no se define: no hay endpoint de detalle):
```json
{
  "numeroSocio": "000123",
  "isNewSocio": true,
  "validFrom": "2026-08-09T14:32:10Z",
  "validUntil": "2027-08-09T14:32:10Z",
  "credential": { "...la VC completa, con proof..." }
}
```

**`GET` → 200**, un arreglo (vacío si no hay nada) de:
```json
{
  "id": "guid de la VC",
  "nombre": "Juan",
  "apellido": "Pérez",
  "dni": "30123456",
  "numeroSocio": "000123",
  "categoria": "adulto",
  "foto": "https://...",
  "validFrom": "...",
  "validUntil": "...",
  "status": 0,
  "credential": { "...la VC completa..." }
}
```
`status` es el número del enunciado (`0` activa, `1` revocada, `2` suspendida). "Vencida" no es un estado: la UI la deriva de `validUntil`.

**Errores** (ProblemDetails, ADR 007):
| Situación | Código |
|---|---|
| Datos inválidos (incluidos los filtros del listado) | 400, errores por campo |
| Falla de firma | 500, `code: issuer_signing_failed` |
| DNI duplicado que persiste tras el reintento | 409, `code: duplicate_dni` |
| Base no disponible o timeout | 503, `code: database_unavailable` |
| Inesperado | 500 genérico |

**Validación de longitudes** (ADR 007 dejó el máximo sin fijar): `nombre` y `apellido` hasta 100 caracteres; `foto` hasta 2048.

### 3. Alcance de esta entrega
Se implementa el diseño del ADR 015 completo, incluidos el reintento por carrera de DNI y los filtros del listado. **Quedan fuera**, por tiempo, y anotados como mejoras:
- Upload de foto (la foto es una URL).
- Idempotency key (ADR 003).
- Timeout específico y *circuit breaker* para un Issuer remoto (ADR 003): el Issuer es in-process.
- Paginación (el enunciado la excluye).

### 4. Pruebas
- **Unitarias del Tenant** con fakes escritos a mano (ADR 015).
- **De integración** de la API contra PostgreSQL real con Testcontainers (ADR 002): alta, listado, socio existente, validación y falla de firma sin filas persistidas (ADR 012).

## Alternativas consideradas
- **VC como string en la respuesta:** obliga al cliente a hacer `JSON.parse` de un valor escapado y complica la lectura del detalle en la UI.
- **`Guid` distinto para el DID:** separa la clave primaria del identificador público, a costa de una columna y de una generación más sin un requisito que lo pida.
- **`IUnitOfWork` o un flag `isNew` en `AddAsync`:** ya evaluados y descartados en el ADR 015.

## Consecuencias
- El frontend se puede construir contra un contrato fijo sin esperar a la API.
- Un socio existente y uno nuevo se distinguen por el change tracker: es implícito y depende de que el mismo `DbContext` (con alcance de request) atienda la búsqueda y el guardado. Queda documentado en `ICredentialRepository`.
- El documento se parsea al responder (un costo mínimo por credencial, sin paginación).
