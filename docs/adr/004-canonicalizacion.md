# ADR 004 — Canonicalización del JSON a firmar

- **Estado:** Aceptado. La configuración de `proof.verificationMethod` fue reemplazada por el [ADR 011](011-verification-method.md). La escritura de `credentialSubject` campo por campo fue reemplazada por el [ADR 012](012-contrato-del-issuer.md).
- **Fecha:** 2026-09-29

## Contexto

### Qué es una credencial verificable y cómo se verifica
Una credencial verificable es un **documento** (un JSON con los datos del socio) más una **firma** que prueba que nadie lo modificó desde su emisión. Verificarla consiste en tomar el documento, **recalcular la firma** con la clave y compararla con la firma adjunta.

### El problema
La firma (HMAC-SHA256) se calcula sobre **texto, byte a byte**, no sobre el significado del JSON. Estos tres textos representan exactamente los mismos datos:

```
{"nombre":"Juan","categoria":"niño"}
{"categoria":"niño","nombre":"Juan"}
{"nombre":"Juan","categoria":"niño"}
```

Para cualquier parser de JSON son idénticos. Para el HMAC son tres entradas distintas, con tres firmas distintas. Si el emisor firma una representación y el verificador reconstruye otra, la verificación falla aunque nadie haya alterado nada.

**Canonicalizar** es acordar una única forma de escribir el JSON, de modo que emisor y verificador produzcan exactamente los mismos bytes. El enunciado fija las reglas:
1. JSON compacto (sin espacios ni saltos de línea).
2. Claves de primer nivel en orden alfabético.
3. Subcampos de `credentialSubject` también en orden alfabético.
4. Fechas ISO-8601 UTC con precisión de segundos y sufijo `Z`.
5. Caracteres no ASCII en UTF-8, sin escapar a `\uXXXX`.

### Dónde está la trampa en .NET
El serializador estándar (`System.Text.Json.JsonSerializer`) toma decisiones por su cuenta:
- **Escapes:** el encoder por defecto (`JavaScriptEncoder.Default`) escapa lo no ASCII (`Pérez` → `Pérez`, `niño` → `niño`) y caracteres "sensibles para HTML" (`O'Connor` → `O'Connor`; en la URL de la foto, `&` → `&` y `+` → `+`).
- **Orden:** emite las propiedades en el orden de declaración de la clase. Es frágil: basta con reordenar la clase o agregar una propiedad.
- **Nombres:** dependen de la naming policy configurada.
- **Fechas:** emite fracciones de segundo si las hay.
- **Enums:** según la configuración, `credentialStatus` podría salir como `"Active"` en lugar de `0`.

Todo esto se puede configurar, pero son varias perillas independientes y es fácil que alguien las mueva sin darse cuenta.

## Decisión
1. **Escribir el JSON canónico "a mano" con `Utf8JsonWriter`**, la API de bajo nivel de `System.Text.Json` que escribe token por token, campo por campo y en el orden de la regla:
   ```csharp
   writer.WriteStartObject();
   writer.WriteNumber("credentialStatus", 0);
   writer.WriteStartObject("credentialSubject");
   writer.WriteString("apellido", subject.Apellido);
   writer.WriteString("categoria", subject.Categoria);
   // ...
   writer.WriteEndObject();
   writer.WriteString("validFrom", validFrom.ToString("yyyy-MM-ddTHH:mm:ssZ"));
   // ...
   writer.WriteEndObject();
   ```
   El writer se configura con `Indented = false` y un encoder que no escapa lo no ASCII (`JavaScriptEncoder.UnsafeRelaxedJsonEscaping`). El nombre "Unsafe" se refiere a incrustar el JSON dentro de HTML, un contexto que acá no aplica.
2. **Test golden:** un test cuya respuesta correcta viene de afuera. El enunciado incluye el JSON canónico exacto de una credencial de ejemplo. El test arma esa misma credencial, la pasa por el canonicalizador y compara el resultado **byte a byte** con el texto del enunciado. El ejemplo contiene `Pérez`, así que detecta la trampa del encoder por sí mismo. Se complementa con tests de `niño`, `'` y `&`.
3. Sobre esos bytes UTF-8 se calcula `HMAC-SHA256(json_canónico, clave)` y el resultado se codifica en **base64**, que es el `proofValue`.

### Clave y `verificationMethod`
- La clave se lee de la variable de entorno `Issuer__SigningKey` (nunca se hardcodea ni se versiona) y se valida al arrancar (mínimo 32 bytes).
- `proof.verificationMethod` se configura con `Issuer__VerificationMethod` (`did:example:futbol#key-1`) e identifica **con qué clave** se firmó. Se cambia junto con la clave.
- **Rotación de claves (fuera de alcance):** el Issuer solo firma, siempre con la clave vigente, así que no necesita las claves viejas. Las necesita **quien verifica**, para validar credenciales emitidas antes de una rotación:
  - Con **HMAC** (simétrico, este caso), el verificador necesita el mismo secreto: sería un componente del backend con un llavero `keyId → secreto` (en la configuración o en un gestor de secretos) que usa el fragmento `#key-N` de la credencial para elegir el secreto.
  - Con **firma asimétrica** (el esquema real, por ejemplo Ed25519), el issuer conserva solo su clave privada vigente, y las claves **públicas** (viejas y nuevas) se publican en el *DID document* del emisor. Cualquier verificador externo resuelve el DID, busca la clave indicada por `verificationMethod` y valida sin conocer ningún secreto.

## Alternativas consideradas
- **`JsonSerializer` sobre un DTO con `[JsonPropertyOrder]`, un converter de fechas y un encoder configurado:** funciona, pero el resultado depende de la suma de varias configuraciones implícitas.
- **`CanonicalJsonBuilder`** (patrón builder): una abstracción que acepta los campos en cualquier orden y los ordena de forma ordinal al construir, escribiendo con `Utf8JsonWriter`. El orden quedaría garantizado por la estructura del código y no por la disciplina de quien escribe, y encaja con el propósito del Issuer: construir la VC. **Se descarta por ahora** porque agrega complejidad innecesaria para una estructura fija y chica, cuyo riesgo ya cubre el test golden. **Queda como evolución recomendada** si el Issuer escala a varios tipos de credencial o a campos opcionales.
- **Canonicalización genérica** (ordenar recursivamente las claves de cualquier `JsonNode`, al estilo RFC 8785/JCS): es más general de lo necesario, porque la estructura es fija y conocida.

## Consecuencias
- El orden y el formato quedan explícitos en el código, visibles y sin configuraciones ocultas.
- Agregar un campo a la credencial exige tocar el canonicalizador a propósito, y el test golden avisa si se rompe la regla.
- **Inconsistencia del enunciado:** el ejemplo de `proof` muestra `proofValue` = `b8f3a1e9…d3f7a`, que tiene 64 caracteres hexadecimales. Un HMAC-SHA256 produce 32 bytes: en hex son 64 caracteres y en base64 son 44, terminando en `=`. Si esos 64 caracteres se leyeran como base64, decodificarían a 48 bytes. El ejemplo es ilustrativo; se sigue la regla escrita (base64).
