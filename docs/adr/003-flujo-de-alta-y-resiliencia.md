# ADR 003 — Flujo de alta, secuencia de numeroSocio y resiliencia

- **Estado:** Aceptado
- **Fecha:** 2026-09-29

## Contexto
En el alta (UC01), el `numeroSocio` forma parte del contenido firmado, así que tiene que existir **antes** de firmar. Además:

- Si la firma falla, **no se persiste nada** (extensión 5a).
- `numeroSocio` tiene que ser secuencial, persistido y consistente entre reinicios.
- Dos altas simultáneas no pueden recibir el mismo número.
- Ante una caída o lentitud de un componente, las requests no deberían quedar colgadas sin un error claro.

El primer diseño que se evaluó mantenía una transacción abierta durante todo el flujo: abrir transacción → reservar número → firmar → persistir → commit. Garantiza atomicidad, pero **mantiene abiertos una conexión y un lock mientras se firma**. Si la firma se demora o falla (hoy es in-process, pero mañana podría ser un KMS o un HSM remoto), se agota el pool de conexiones y todas las altas quedan en espera detrás del lock.

## Decisión
**La firma ocurre fuera de la transacción; la transacción es corta y solo escribe.**

1. Buscar el socio por DNI, sin lock.
2. Si no existe, obtener `numeroSocio` con `nextval` de una **SEQUENCE** de PostgreSQL y generar `credentialSubject.id`.
3. Armar el `credentialSubject` y pedir la emisión al Issuer, **sin conexión abierta a la base**.
4. Si la firma falla, se corta acá y no se escribe nada.
5. Transacción corta: `INSERT` o `UPDATE` del socio + `INSERT` de la credencial, y commit.

**Qué es una SEQUENCE:** un objeto de la base de datos que entrega números crecientes (`nextval('numero_socio_seq')` → 1, 2, 3…).
- Nunca repite un número, incluso bajo concurrencia, y lo logra **sin bloquear**.
- Persiste en la base, así que es consistente entre reinicios.
- `nextval` **no participa de la transacción**: si hay rollback, el número no se devuelve.

**Consecuencia aceptada: pueden quedar huecos.** Cada falla de firma, rollback o reinicio de la base puede "quemar" un número. El enunciado pide *secuencial*, no *sin huecos*. Un número de socio no es una factura, que sí exige correlatividad. El número se expone con relleno de ceros a 6 dígitos (`000123`), a partir de 999999 simplemente crece.

**Carrera por un DNI nuevo:** si dos altas del mismo DNI inexistente llegan a la vez, el `UNIQUE(dni)` hace fallar a una. Esa alta se reintenta una sola vez por el camino "socio existente".

**Resiliencia (fallar rápido y con un error claro):**
- Timeouts explícitos de conexión y de comando contra la base. Si se exceden, la respuesta es **503** con ProblemDetails, no una espera indefinida.
- Si falla la firma, **500** con un código identificable (`issuer_signing_failed`), sin persistir nada.
- Endpoint `GET /health` que chequea la base, usado también como healthcheck en Docker.
- `CancellationToken` propagado: si el cliente corta, se deja de trabajar.
- Si el proceso muere antes del commit, PostgreSQL descarta la transacción al perder la conexión, así que no quedan estados a medias.
- `ICredentialIssuer` es asíncrona y recibe un `CancellationToken`. Si el Issuer pasara a ser remoto, se le agregarían timeout y *circuit breaker* (tras fallas repetidas, las requests fallan al instante con 503 en lugar de acumularse). Hoy no se implementa.

## Alternativas consideradas
- **Transacción abierta durante la firma:** descartada por lo explicado en el contexto.
- **`SELECT MAX(numero_socio) + 1`:** dos requests simultáneas leen el mismo máximo y obtienen el mismo número. Es una condición de carrera.
- **Tabla contador con `UPDATE ... SET valor = valor + 1`:** no deja huecos, pero bloquea esa fila hasta el commit, y todas las altas se ejecutan en fila india (y con la firma dentro de la transacción, esperando también a la firma).
- **Emisión asíncrona** (cola, `202 Accepted`, patrón outbox): permitiría absorber picos y reintentar firmas fallidas. Se descarta como solución actual porque 1) **incumple el requerimiento**: el UC exige confirmar el alta y mostrar el número de socio en el momento, y 2) **complejiza** la solución (broker, workers, consulta de estado). Queda documentada como camino de escalado.

## Consecuencias
- No se mantiene ninguna conexión ni lock durante la firma.
- La secuencia puede tener huecos (queda documentado y es aceptable).
- Caso borde abierto: si el proceso muere **después** del commit y antes de responder, el cliente no sabe si el alta existe; si reintenta, emitiría una segunda credencial para el mismo socio. Se resolvería con una *idempotency key* (GUID por envío, guardado con restricción única). Queda como extra opcional.
