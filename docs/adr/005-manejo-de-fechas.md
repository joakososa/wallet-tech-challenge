# ADR 005 — Manejo de fechas

- **Estado:** Aceptado
- **Fecha:** 2026-09-29

## Contexto
La misma fecha existe en dos formas:
- **En memoria**, como `DateTime`/`DateTimeOffset`. `DateTime.UtcNow` tiene precisión de 100 nanosegundos, por ejemplo `14:32:10.4567891`.
- **En el documento firmado**, como texto. La regla de canonicalización (ADR 004) exige precisión de segundos: `2026-08-09T14:32:10Z`.

El formato canónico **descarta información**. Si el valor en memoria conserva las fracciones, el sistema tiene dos versiones del mismo dato que no coinciden:
1. Se firma `"validFrom":"2026-08-09T14:32:10Z"`.
2. En la base se guarda el valor completo; PostgreSQL guarda microsegundos, así que queda `14:32:10.456789`, un tercer valor distinto.
3. La API devuelve la credencial con el serializador estándar, que escribe `14:32:10.4567891Z`.
4. Quien recibe esa respuesta ve un documento que **no es el que se firmó**. Además, comparaciones como `Assert.Equal(emitida.ValidFrom, guardada.ValidFrom)` fallan por el último dígito.

## Decisión
1. **Truncar a segundos en el momento de generar la fecha** (en el Issuer, al emitir). La pérdida de precisión ocurre una sola vez, al principio y a propósito. Desde ahí, memoria, base, respuesta y documento firmado tienen el mismo valor: **lo que se firmó es lo que existe**.
2. Siempre en **UTC**, con formato `yyyy-MM-ddTHH:mm:ssZ` al serializar.
3. `validUntil = validFrom.AddYears(1)`.
4. `proof.created = validFrom` (el enunciado indica que coinciden en la práctica).
5. La hora se obtiene de un **`TimeProvider`** inyectado, no de `DateTime.UtcNow`, para poder testear con fechas fijas.

## Caso borde: 29 de febrero
`AddYears(1)` sobre el 29/02 de un año bisiesto devuelve el **28/02** del año siguiente, porque el 29/02 no existe. Una credencial emitida el 2028-02-29 vence el 2029-02-28. Se considera correcto: la vigencia nunca supera un año calendario. Hay un test que cubre este caso.

## Alternativas consideradas
- **No truncar y formatear siempre con el canonicalizador:** funciona solo si *todos* los caminos (respuesta de la API, persistencia, logs) respetan el formato. Es frágil.
- **Vencimiento el 01/03 para emisiones del 29/02:** alargaría la vigencia un día; se descarta.

## Consecuencias
- No hay precisión por debajo del segundo en las fechas de la credencial; no hace falta para este dominio.
- Los tests de fechas son deterministas.
