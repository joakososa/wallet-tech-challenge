# ADR 012 — Contrato del Issuer y canonicalización de claims

- **Estado:** Aceptado
- **Fecha:** 2026-09-30
- **Reemplaza a:** ADR 004, solo en la escritura de `credentialSubject` campo por campo (sección "Decisión", punto 1).

## Contexto
El ADR 001 fija que el Issuer no conoce socios, bases de datos ni HTTP. Hasta ahora el diseño lo contradecía en dos puntos:
- El canonicalizador del ADR 004 escribía uno por uno los campos de `credentialSubject` (`dni`, `categoria`, `numeroSocio`…), es decir, el Issuer conocía el negocio del club.
- El tipo de la credencial (`SocioCredential`) y el estado inicial (`credentialStatus`) quedaban fijos dentro del Issuer.

Se revisó también si el Issuer in-process puede fallar o volverse lento en runtime: firmar con HMAC-SHA256 unos cientos de bytes en memoria tarda microsegundos, y las causas plausibles de falla (clave ausente o corta, `KeyId` inválido) se detectan al arrancar. Una falla de firma real solo existiría con un Issuer remoto (KMS, HSM), que hoy no existe. Aun así, el enunciado exige que si falla la firma no se persista nada (UC01 5a), y esa garantía tiene que poder demostrarse con un test bien escrito y con una demo manual.

## Decisión
1. **Contrato:**
   ```csharp
   Task<VerifiableCredential> IssueAsync(
       IReadOnlyDictionary<string, string> credentialSubject,
       IReadOnlyList<string> types,
       CredentialStatus status = CredentialStatus.Active,
       CancellationToken ct = default);
   ```
   La interfaz es asíncrona para no cambiar de firma si el Issuer pasa a ser remoto (ADR 003). La implementación es `internal` (ADR 001).
2. **`credentialSubject` es un diccionario `string → string`.** Los nombres de los claims (`id`, `nombre`, `dni`, `numeroSocio`…) son responsabilidad del Tenant; el Issuer no los conoce ni les da un tratamiento especial, ni siquiera a `id`. Los valores son solo `string`: todos los claims actuales lo son (incluido `numeroSocio`) y es lo más simple de canonicalizar. Si hicieran falta números o estructuras anidadas, se pasaría a `JsonElement` con un ADR nuevo.
3. **`types` es un parámetro** y no se valida su contenido. El Tenant envía `["VerifiableCredential","SocioCredential"]`.
4. **`credentialStatus`:** enum pública `CredentialStatus { Active = 0, Revoked = 1, Suspended = 2 }`, con `Active` por defecto. El Tenant no lo elige, así que el alta sale siempre `Active`. Se escribe como número (`0`), no como texto. El estado actual vive en `credentials.status` y el del documento firmado no se muta (ADR 002).
5. **Canonicalización:**
   - Los campos de primer nivel se siguen escribiendo a mano en el orden fijo del enunciado.
   - Las claves de `credentialSubject` se ordenan con `StringComparer.Ordinal`. Para los claims actuales produce exactamente el orden que exige el enunciado.
   - La lógica vive en `CredentialPayload.ToCanonicalBytes()`: la forma canónica es una propiedad del documento a firmar. La firma queda en el Issuer, que es quien conoce la clave.
   - Se mantienen del ADR 004 el encoder `UnsafeRelaxedJsonEscaping`, el formato de fechas, el HMAC-SHA256 en base64 y el test golden.
6. **Falla de firma (UC01 5a): excepción del contrato y simulación con un decorator.**
   - `IssuerSigningException` es pública y forma parte del contrato de `ICredentialIssuer`: es la forma en que un Issuer declara que no pudo firmar. El Issuer in-process no la lanza (no tiene una falla esperable, ver Contexto) y no envuelve sus errores en ella; la lanzan el decorator de simulación y, a futuro, un Issuer remoto.
   - `SimulatedFailureCredentialIssuer` (en `Wallet.Api`) implementa `ICredentialIssuer`, envuelve al Issuer real y, si `Issuer__SimulateFailure` está activo, lanza `IssuerSigningException` en lugar de delegar. La API lo registra solo en Development y con el flag activo. El Issuer no sabe que existe.
   - La garantía "si falla, no se persiste nada" es estructural: la firma ocurre antes de la transacción (ADR 003). La API traduce `IssuerSigningException` a un 500 con `code: issuer_signing_failed`, como definen los ADR 003 y 007, que siguen vigentes en este punto. Cualquier otra excepción sigue siendo un 500 genérico.
   - Tests que demuestran UC01 5a:
     - **Unitario (Tenant):** un doble de `ICredentialIssuer` que lanza `IssuerSigningException`; se verifica que no se abrió transacción ni se guardó socio ni credencial, y que la excepción se propaga.
     - **Integración (Api + PostgreSQL):** el alta con el decorator activo devuelve 500 con `code: issuer_signing_failed` y las tablas `socios` y `credentials` quedan vacías. Usa el decorator real, no un doble.
7. **Dependencias del Issuer:** `TimeProvider` inyectado (viene con el framework y permite fechas fijas en tests, ADR 005). El `Guid` de la credencial se genera con `Guid.NewGuid()` directamente; los tests verifican su formato.
8. **Validaciones que se mantienen**, porque cubren problemas reales y son baratas:
   - Al arrancar (`ValidateOnStart`): clave de al menos 32 bytes, `KeyId` sin `#` ni espacios (ADR 011), DID con prefijo `did:`, `CredentialBaseUri` absoluta.
   - Por llamada: claves o valores nulos y `status` definido en el enum.

## Alternativas consideradas
- **`SimulateFailure` dentro de `IssuerOptions`:** metería una rama de producción en el Issuer solo para una demo.
- **Solo dobles en los tests, sin simulación en la app:** cubre el enunciado en los tests, pero no permite demostrar el error a mano.
- **Sin excepción propia (500 genérico):** más simple, pero la API no podría devolver el código identificable `issuer_signing_failed` de los ADR 003 y 007, ni el Tenant distinguir una falla de firma de un bug.
- **Envolver todos los errores del Issuer en `IssuerSigningException`:** un bug propio (por ejemplo un `NullReferenceException`) se reportaría como falla de firma. Solo se lanza donde la falla de firma es real.
- **Claims como `JsonElement`:** más flexible, pero abre preguntas de canonicalización (números, anidados, orden recursivo) que hoy no hacen falta.
- **`id` del sujeto como propiedad separada y opcional:** el Issuer no lo valida ni lo procesa, así que no justifica un tratamiento especial.
- **La VC firma en su constructor:** el constructor tendría que recibir la clave, el DID, el `KeyId`, el reloj y el generador de Guid, y hacer criptografía. El flujo queda mejor en un único servicio (`CredentialIssuer`).
- **Interfaz `ISigner`:** hay una sola implementación y ninguna prevista. Se extrae cuando exista una segunda.
- **`CanonicalJsonBuilder` genérico:** sigue siendo una evolución posible (ADR 004), pero la estructura de primer nivel es fija y ordenar un diccionario alcanza.

## Consecuencias
- El Issuer queda libre de vocabulario del club: reutilizable para otros tipos de credencial y otros tenants.
- El orden del subject depende de un sort y no de escribirlo a mano. Lo cubren el test golden (mismo JSON del enunciado) y un test de que el orden de inserción de los claims no altera el resultado.
- El Tenant concentra el mapeo `Socio → claims` y la decisión de `types`.
- El contrato ya distingue la falla de firma. Si el Issuer pasa a ser remoto, esa implementación deberá lanzar `IssuerSigningException` (y sumar timeouts y *circuit breaker*, ver ADR 003).
- Un error inesperado del Issuer real (un bug) sigue siendo un 500 genérico, no una falla de firma.
- `Issuer__SimulateFailure` sigue existiendo en la configuración, pero la lee la API, no el Issuer.
