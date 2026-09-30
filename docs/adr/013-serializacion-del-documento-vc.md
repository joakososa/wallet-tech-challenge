# ADR 013 — Serialización del documento VC

- **Estado:** Aceptado. La ubicación de la escritura del cuerpo (punto 2) fue reemplazada por el [ADR 014](014-credential-json-builder.md).
- **Fecha:** 2026-09-30

## Contexto
Los ADR 004 y 012 definen cómo se escribe el JSON **que se firma** (la credencial sin `proof`). Ninguno define cómo se escribe el **documento completo**, con `proof`, que el sistema persiste en `credentials.document` (ADR 002) y devuelve en la respuesta del alta. El principio del ADR 005 es que lo firmado, lo persistido y lo devuelto sean lo mismo.

El `JsonSerializer` por defecto rompe ese principio sobre `VerifiableCredential`:
- escribe `DateTimeOffset` con offset (`2026-08-09T14:32:10+00:00`) en lugar de `Z`;
- escapa lo no ASCII y los caracteres sensibles para HTML (`Pérez`, `&` en la URL de la foto);
- emite las propiedades en el orden de declaración del record.

El documento guardado dejaría de parecerse al que se firmó, y un verificador que lo lea tendría que adivinar el formato.

## Decisión
1. **El Issuer es el dueño del formato del documento**, igual que lo es del formato canónico. Expone `VerifiableCredential.ToJson()`, que devuelve el documento como `string`.
2. **Una única fuente de verdad para el cuerpo:** la escritura del cuerpo de la credencial (orden de claves, formato de fechas, encoder) vive en `CredentialPayload` y la usan tanto `ToCanonicalBytes()` como `ToJson()`. El documento serializado es el JSON canónico con `proof` agregado como último campo, por lo que sin ese campo es byte a byte el texto firmado.
3. **`proof`** se escribe con `type`, `created` (mismo formato de fecha, con `Z`), `verificationMethod` y `proofValue`, en ese orden.
4. **`proof` va al final y no en orden alfabético.** El orden alfabético solo es obligatorio para lo que se firma (enunciado, sección 4.1.2), y `proof` queda fuera de la firma. Un verificador recanonicaliza el documento sin `proof`, así que el orden del documento persistido no afecta la verificación.
5. **No se ofrece deserialización.** El listado lee columnas propias (ADR 002) y nada necesita reconstruir una `VerifiableCredential` desde el JSON. Se agrega cuando haga falta.
6. **La respuesta HTTP** devuelve este mismo documento. Cómo se inserta en el sobre de la respuesta (`numeroSocio`, vigencia, `isNew`) se decide al implementar la API.

## Alternativas consideradas
- **Serializar en el Tenant con `JsonSerializer` y opciones:** duplica el conocimiento del formato de fechas y del encoder, y separa dos representaciones del mismo documento que tienen que coincidir.
- **`JsonConverter` sobre `VerifiableCredential`:** haría que cualquier `JsonSerializer.Serialize(vc)` saliera bien, pero el encoder lo decide el writer que recibe, no el converter, así que el escape de lo no ASCII seguiría dependiendo de quien llama.
- **`proof` en orden alfabético con el resto:** más uniforme, pero obliga a partir el cuerpo en dos mitades al escribirlo y no aporta nada a la verificación.
- **Devolver el documento como objeto en la API y serializarlo con el serializador de ASP.NET:** reproduce el problema del contexto.

## Consecuencias
- Lo persistido es exactamente lo que el Issuer emitió, con el mismo formato que el texto firmado.
- Un test golden cubre el documento completo (ejemplo del enunciado más `proof`) y otro verifica que, sin `proof`, coincide con `ToCanonicalBytes()`.
- Al persistirlo en una columna `json` (ADR 002), Postgres conserva el texto exacto.
- Quien consuma el documento fuera del sistema recibe el JSON tal como se guardó; si lo reescribe con otro serializador, el resultado puede diferir en bytes (es semánticamente igual, y la verificación recanonicaliza).
