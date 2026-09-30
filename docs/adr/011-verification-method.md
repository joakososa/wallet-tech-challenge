# ADR 011 — Construcción del verificationMethod

- **Estado:** Aceptado
- **Fecha:** 2026-09-30
- **Reemplaza a:** ADR 004, solo en la configuración de `proof.verificationMethod` (sección "Clave y `verificationMethod`")

## Contexto
El ADR 004 definió que `proof.verificationMethod` se configura completo con la variable `Issuer__VerificationMethod` (por ejemplo `did:example:futbol#key-1`).

El `verificationMethod` identifica **qué clave del emisor** firmó la credencial. Su forma es un DID URL: el DID del emisor seguido de un fragmento con el identificador de la clave (`{did}#{keyId}`). Si se configura completo, nada garantiza que la parte del DID coincida con `Issuer__Did`: una configuración como `did:otro:emisor#key-1` sería aceptada, y la credencial diría que la firmó una clave de otra identidad. Validar que el valor "empiece con `did:`" no alcanza para detectarlo.

## Decisión
1. Se reemplaza `Issuer__VerificationMethod` por **`Issuer__KeyId`**, que contiene solo el identificador de la clave (por ejemplo `key-1`).
2. El Issuer **construye** el valor: `verificationMethod = {Issuer__Did}#{Issuer__KeyId}`. Así pertenece al DID del emisor **por construcción**, no por validación.
3. `Issuer__KeyId` se valida al arrancar: no puede estar vacío ni contener `#` ni espacios.
4. El `KeyId` se cambia junto con `Issuer__SigningKey` cuando se rota la clave. Lo explicado en el ADR 004 sobre la rotación de claves sigue vigente: el `KeyId` es lo que le permite al verificador elegir la clave correcta.

## Alternativas consideradas
- **Mantener `Issuer__VerificationMethod` y validar que empiece con `{Issuer__Did}#`:** cumple el mismo objetivo, pero duplica en la configuración un dato (el DID) que ya existe y obliga a mantener los dos valores sincronizados.

## Consecuencias
- Hay una variable de entorno menos propensa a errores. El valor resultante para la configuración de ejemplo sigue siendo `did:example:futbol#key-1`, así que la credencial emitida no cambia.
- Hay que actualizar el README y el `.env.example` para que usen `Issuer__KeyId`.
- El resto del ADR 004 (canonicalización, firma, rotación de claves) sigue vigente.
