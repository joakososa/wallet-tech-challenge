# ADR 014 — `CredentialJsonBuilder` para el JSON canónico y el documento

- **Estado:** Aceptado
- **Fecha:** 2026-09-30
- **Reemplaza a:** ADR 013, solo en el punto 2 (dónde vive la escritura del cuerpo de la credencial).

## Contexto
El ADR 013 decidió que el JSON canónico (lo que se firma) y el documento completo (con `proof`) comparten una única escritura del cuerpo, y ubicó esa escritura en `CredentialPayload`. Para producir las dos salidas, `CredentialPayload` recibía `proof` como parámetro opcional: un `null` que cambia el comportamiento de un método, es decir, un flag disfrazado.

Además, `CredentialPayload` es la representación de "todo lo que se firma" (la credencial sin `proof`). Que también sepa escribir el documento con `proof` mezcla dos conceptos.

El ADR 004 ya había dejado un `CanonicalJsonBuilder` como evolución recomendada "si el Issuer escala a campos opcionales". `proof` es ese primer campo opcional.

## Decisión
1. **`CredentialJsonBuilder` (interno) escribe el JSON.** Recibe el `CredentialPayload` y expone `WithProof(proof)` (opcional) y `Build()`, que devuelve los bytes UTF-8.
   - Sin `WithProof`: JSON canónico, el texto que se firma (ADR 004 y 012).
   - Con `WithProof`: el mismo cuerpo más `proof` como último campo, el documento a persistir y devolver (ADR 013).
2. **El orden de las claves, el encoder y el formato de fechas viven solo en el builder.** Sigue siendo una única fuente de verdad para las dos salidas.
3. **`CredentialPayload` queda como un record de datos.** Conserva `ToCanonicalBytes()` como atajo que delega en el builder, porque es la operación que la firma necesita; no conoce `proof`.
4. **`VerifiableCredential.ToJson()` sigue siendo la API pública** (ADR 013, punto 1) y usa el builder con `WithProof`.
5. Se mantienen todas las decisiones del ADR 013 salvo el punto 2: `proof` al final, sin deserialización, y la respuesta HTTP se decide al implementar la API.

## Alternativas consideradas
- **Dejar `proof` como parámetro nullable de `CredentialPayload`:** es la solución que se reemplaza.
- **Dos métodos en `CredentialPayload` (`ToCanonicalBytes` y `ToDocumentJson(proof)`) que comparten un helper privado:** evita el `null`, pero `CredentialPayload` seguiría conociendo el formato del documento con `proof`.
- **Builder genérico, con campos en cualquier orden:** sigue siendo una evolución posible (ADR 004), pero la estructura es fija y el orden ya está garantizado por el código y el test golden.

## Consecuencias
- La forma de cada salida queda explícita en la llamada (`Build()` o `WithProof(...).Build()`), sin banderas.
- Agregar otro campo opcional al documento se resuelve con otro método del builder, sin tocar `CredentialPayload`.
- Es un refactor: los tests existentes (golden del canónico, golden del documento y el resto) no cambian y son la red de seguridad.
