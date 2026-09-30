# ADR 006 — Identidad del socio y DNI

- **Estado:** Aceptado
- **Fecha:** 2026-09-29

## Contexto
El enunciado distingue entre `credentialSubject.id` ("identificador técnico y estable del socio, se genera una vez y se persiste") y el `id` de la credencial ("el documento emitido, no el socio"). De ahí se desprende que **socio y credencial son entidades distintas** y que un socio puede tener varias credenciales a lo largo del tiempo. El enunciado no dice qué pasa cuando se da de alta una credencial para alguien que ya es socio, ni qué hacer si los datos difieren.

## Decisión
1. **El DNI es la identidad del socio y es inmutable.** Es la clave con la que se busca al socio, así que no existe forma de "cambiarlo": un DNI distinto es, por definición, otro socio. Un número de socio nunca cambia de DNI.
2. El alta tiene dos caminos:
   - **Socio nuevo** (el DNI no existe): se registra el socio, con `credentialSubject.id` (`did:example:{Guid}`) y `numeroSocio` nuevos.
   - **Socio existente** (el DNI existe): se **reutilizan** `credentialSubject.id` y `numeroSocio`, y se **actualizan** nombre, apellido, categoría y foto con los datos del alta (gana la última escritura).
3. **El historial no se pierde:** cada credencial guarda el snapshot firmado de los datos al momento de emitirse (ver ADR 002).
4. **Se permiten varias credenciales activas por socio.** La revocación y el cambio de estado están fuera de alcance, así que una emisión nueva no desactiva la anterior. A futuro, una emisión nueva debería reemplazar (revocar o suspender) a la anterior.
5. La respuesta del alta indica si el socio **era nuevo o ya existía**, y la pantalla de resultado lo muestra. Si un DNI mal tipeado terminó actualizando los datos de otro socio, el operador lo ve en ese momento.

### Formato del DNI
- Se guarda como **string**: preserva ceros a la izquierda y no tiene límites numéricos (el enunciado ya lo define como string).
- Se **normaliza** sacando puntos y espacios (`30.123.456` → `30123456`).
- Se exigen **solo dígitos, con una longitud de 7 a 9**. Hoy se emiten DNIs de 8 dígitos (y existen de 7 más antiguos); se admite 9 para no atar la validación a una suposición que podría cambiar.

### Formato de `numeroSocio`
String con relleno de ceros a 6 dígitos (`000123`), tomado del ejemplo del enunciado. Por encima de 999999 simplemente crece.

## Alternativas consideradas
- **Rechazar (409) si nombre o apellido difieren:** protegería contra DNIs mal tipeados, pero impide corregir o actualizar datos que legítimamente cambian (errores de carga, cambios registrales).
- **Tratar cada alta como un socio nuevo:** duplica socios y rompe la regla de que `credentialSubject.id` se genera una sola vez.
- **Rechazar si el socio ya tiene una credencial vigente:** sin revocación, obligaría a esperar el vencimiento para reemitir.

## Consecuencias
- `socios` siempre refleja el dato actual; `credentials` refleja lo emitido en cada momento.
- Un error de tipeo en el DNI puede actualizar los datos de otro socio. Se mitiga mostrando "socio existente" en el resultado; las credenciales anteriores de ese socio no se alteran.
