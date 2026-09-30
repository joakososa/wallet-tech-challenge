# Prueba Técnica — Wallet de Credenciales Verificables

## Índice

- [1. Objetivo](#1-objetivo)
- [2. Criterios de evaluación](#2-criterios-de-evaluación)
  - [2.1 Qué se evalúa](#21-qué-se-evalúa)
  - [2.2 Qué no se evalúa](#22-qué-no-se-evalúa)
- [3. Entregables](#3-entregables)
- [4. Detalles del sistema a construir](#4-detalles-del-sistema-a-construir)
  - [4.1 Requerimientos funcionales](#41-requerimientos-funcionales)
    - [4.1.1 Casos de uso](#411-casos-de-uso)
    - [4.1.2 Modelo de la credencial](#412-modelo-de-la-credencial)
    - [4.1.3 Formulario de alta (UI)](#413-formulario-de-alta-ui)
  - [4.2 Requerimientos técnicos](#42-requerimientos-técnicos)
    - [4.2.1 Arquitectura y stack](#421-arquitectura-y-stack)

## 1. Objetivo

La presente prueba técnica consiste en desarrollar un sistema que emite y gestiona credenciales digitales verificables para los socios del Club de Fútbol de Córdoba, firmadas criptográficamente para garantizar su autenticidad. La arquitectura debe separar, a nivel de servicio, el rol de negocio (**Tenant**, representado por club-futbol) del rol de emisión de credenciales (**Issuer**).

## 2. Criterios de evaluación

### 2.1 Qué se evalúa

- **Funcionalidad**: el sistema debe funcionar correctamente de punta a punta, cubriendo:
  - Alta de credencial (UC01): carga de los datos del socio (nombre, apellido, DNI, categoría y foto — sección 4.1.3), armado del sujeto de la credencial, emisión y firma por parte del Issuer, y persistencia de la credencial completa (VC).
  - Generación correcta y consistente de los datos que el sistema asigna automáticamente: identificadores, vigencia y firma de la credencial (sección 4.1.2).
  - Listado de credenciales (UC02): recuperación y presentación de las credenciales persistidas, incluyendo el estado vacío cuando no hay ninguna.
  - Manejo de errores: si falla la firma en el Issuer, no debe persistirse ninguna credencial.
- **Interfaz de usuario**: el diseño visual/UX debe ser funcional; es aceptable un estilo simple y minimalista.
- **Diseño de solución**: el diseño de la arquitectura del sistema debe estar documentado.
- **Calidad de código**: legibilidad y buenas prácticas.
- **Documentación de decisiones tomadas**: cualquier decisión relevante ante los puntos no especificados en el enunciado debe estar documentada.

### 2.2 Qué no se evalúa
- La implementación de la verificación de credenciales.
- La revocación o el cambio de estado.
- La autenticación y la autorización.
- La fidelidad estricta a la spec W3C VC/DID.
- La paginación en el listado (los filtros son opcionales, no obligatorios).

## 3. Entregables

El entregable incluye un repositorio Git con historial de commits y un `README.md` con las instrucciones para levantar el proyecto (con Docker, de forma local, o combinando ambas formas si algún componente se levanta con Docker y el resto en local).

## 4. Detalles del sistema a construir

### 4.1 Requerimientos funcionales

#### 4.1.1 Casos de uso

El sistema deberá cubrir los siguientes casos de uso, que describen la interacción del administrador del tenant con las funcionalidades de alta y listado de credenciales.

Formato Cockburn (casual).

**UC01 — Alta de credencial de socio**
- Actor principal: administrador del tenant.
- Precondición: el administrador tiene acceso al formulario de alta.
- Postcondición: la credencial queda persistida y visible en el listado.

Escenario principal:
1. El administrador abre el formulario de alta.
2. Completa los datos del socio (sección 4.1.3) y selecciona la categoría.
3. Envía el formulario.
4. El sistema (backend) arma el `credentialSubject` y solicita la emisión al servicio Issuer, interno del backend.
5. El servicio Issuer agrega `id`, `type`, `issuer`, `validFrom`, `validUntil` y `proof`, y devuelve la VC firmada.
6. El sistema persiste la credencial y confirma el alta.

Extensiones:
- 5a. Si falla la firma en el servicio Issuer, no se persiste nada y se informa el error.

**UC02 — Listado de credenciales emitidas**
- Actor principal: administrador del tenant.
- Precondición: el administrador accede a la pantalla de listado.
- Postcondición: se visualizan las credenciales del tenant.

Escenario principal:
1. El administrador abre el listado.
2. El sistema recupera las credenciales del tenant.
3. Se muestran con foto, nombre, apellido, categoría, número de socio, vigencia y estado.

Extensiones:
- 2a. Si no hay credenciales emitidas, se muestra un estado vacío.

#### 4.1.2 Modelo de la credencial

Esta sección define la estructura de datos de la credencial verificable: cada campo que la compone, su tipo, quién lo genera y la regla que determina su valor.

| Campo | Descripción | Tipo | Generado por | Regla |
|---|---|---|---|---|
| `id` | Identificador único de esta credencial en particular (el documento emitido, no el socio) | string (URI) | Issuer | `Guid` al emitir, formateado como URI absoluta bajo el dominio del issuer: `https://credenciales.futbol.com.ar/{Guid}`. Ejemplo completo: `https://credenciales.futbol.com.ar/8f14e45f-ceea-467e-9de1-93f5a5f4bfae` |
| `type` | Indica qué tipo de documento es, según el estándar de credenciales verificables | array | Issuer | Constante: `["VerifiableCredential","SocioCredential"]` |
| `issuer` | Quién emite la credencial (el club, representado como identidad digital) | string (DID) | Config | Fijo: `did:example:futbol` (env var) |
| `credentialSubject` | Los datos concretos del socio a quien pertenece la credencial | object | Tenant | Ver subcampos abajo |
| `validFrom` | Fecha desde la que la credencial es válida (fecha de emisión) | datetime | Issuer | Timestamp de emisión (UTC) |
| `validUntil` | Fecha hasta la que la credencial es válida (vencimiento) | datetime | Issuer | `validFrom` + 1 año |
| `credentialStatus` | Estado actual de la credencial: activa, revocada o suspendida | integer | Tenant | `0`=active, `1`=revoked, `2`=suspended. Default `0` |
| `proof` | La firma que permite verificar que la credencial no fue alterada desde su emisión | object | Issuer | Firma mock (HMAC-SHA256), clave desde env var/secret. Ver subcampos y ejemplo abajo |

**Subcampos de `credentialSubject`**

| Subcampo | Descripción | Tipo | Generado por | Regla |
|---|---|---|---|---|
| `id` | Identificador técnico y estable del socio como sujeto de la credencial (distinto del número de socio) | string (DID) | Tenant | `did:example:{Guid}`, se genera una vez y se persiste |
| `nombre` | Nombre de pila del socio | string | Input usuario | — |
| `apellido` | Apellido del socio | string | Input usuario | — |
| `dni` | Documento Nacional de Identidad del socio | string | Input usuario | — |
| `numeroSocio` | Número de socio del club: el identificador que usan el socio y el club en el día a día | string | Tenant | Secuencial, persistido y consistente entre reinicios (estrategia a criterio, documentar la decisión) |
| `categoria` | Categoría de membresía del socio dentro del club | string | Input usuario | Uno de: `adulto` / `juvenil` / `niño` |
| `foto` | Foto del socio, para identificarlo visualmente en la credencial | string (URI) | Input usuario | Mínimo: URL. Opcional: upload real de archivo |

**Subcampos de `proof`**

| Subcampo | Descripción | Tipo | Regla |
|---|---|---|---|
| `type` | Algoritmo usado para firmar la credencial | string | Constante: `"HMAC-SHA256"` |
| `created` | Momento en que se generó la firma | datetime | Timestamp de la firma (UTC); en la práctica coincide con `validFrom` |
| `verificationMethod` | Referencia a la clave usada para firmar (relevante si en el futuro se implementa la verificación) | string (DID) | Identifica la clave usada para firmar, ej. `did:example:futbol#key-1` |
| `proofValue` | El valor de la firma en sí, resultado del cálculo criptográfico | string | Salida del HMAC-SHA256, codificada en base64 |

Este apartado describe el procedimiento que sigue el Issuer para calcular `proofValue`, el valor de la firma HMAC-SHA256 que garantiza la integridad de la credencial.

**Cómo se forma:** para evitar ambigüedad, la canonicalización queda fijada de antemano (no es una decisión a criterio del candidato). El Issuer arma la credencial completa (todos los campos de la tabla de la sección 4.1.2, excluyendo el propio `proof`) y la serializa como JSON siguiendo estas reglas:

1. JSON compacto: sin espacios ni saltos de línea.
2. Claves de primer nivel en orden alfabético: `credentialStatus`, `credentialSubject`, `id`, `issuer`, `type`, `validFrom`, `validUntil`.
3. Dentro de `credentialSubject`, sus subcampos también en orden alfabético: `apellido`, `categoria`, `dni`, `foto`, `id`, `nombre`, `numeroSocio`.
4. Fechas en formato ISO-8601 UTC con precisión de segundos y sufijo `Z` (ej. `2026-08-09T14:32:10Z`), sin milisegundos.
5. Caracteres no ASCII (tildes, ñ) serializados en UTF-8 sin escapar a `\uXXXX`. En .NET con `System.Text.Json`, esto requiere configurar el encoder (por defecto escapa unicode); es un detalle fácil de pasar por alto y que rompe la reproducibilidad del hash si no se fija.

Ejemplo de JSON canónico (entrada del HMAC) para una credencial de muestra:

```json
{"credentialStatus":0,"credentialSubject":{"apellido":"Pérez","categoria":"adulto","dni":"30123456","foto":"https://cdn.futbol.com.ar/socios/8f14e45f.jpg","id":"did:example:3fa85f64-5717-4562-b3fc-2c963f66afa6","nombre":"Juan","numeroSocio":"000123"},"id":"https://credenciales.futbol.com.ar/8f14e45f-ceea-467e-9de1-93f5a5f4bfae","issuer":"did:example:futbol","type":["VerifiableCredential","SocioCredential"],"validFrom":"2026-08-09T14:32:10Z","validUntil":"2027-08-09T14:32:10Z"}
```

Sobre ese JSON canónico se calcula `HMAC-SHA256(json_canónico, clave_secreta)`. La `clave_secreta` se lee de una variable de entorno o secret (nunca hardcodeada). El resultado del HMAC se codifica en base64 y se asigna a `proofValue`.

Al tratarse de una firma mock, no reemplaza un esquema real de firma digital (JWS o Linked Data Proofs); simula, a los fines de esta prueba, la existencia de una prueba criptográfica verificable. No se implementa la verificación (fuera de alcance, ver Alcance en la sección 2.1).

Ejemplo de `proof` resultante:

```json
"proof": {
  "type": "HMAC-SHA256",
  "created": "2026-08-09T14:32:10Z",
  "verificationMethod": "did:example:futbol#key-1",
  "proofValue": "b8f3a1e9c02d4f7a6e1b9c3d8f2a5e7b1c4d6f9a0e3b7c1d5f8a2e6b9c0d3f7a"
}
```

La tabla anterior describe la credencial completa (VC), que se persiste íntegra según el modelo de datos (sección 4.2.1, Arquitectura y stack). El listado (UC02) no necesita mostrar ese objeto completo: alcanza con presentar el `credentialSubject` junto con la vigencia y el estado. Los campos técnicos del protocolo (`id`, `type`, `issuer`, `proof`) son opcionales en la interfaz, por ejemplo dentro de un detalle expandible.

#### 4.1.3 Formulario de alta (UI)

Esta sección especifica los campos del formulario de alta y su control de entrada.

| Campo | Control |
|---|---|
| Nombre | Input texto |
| Apellido | Input texto |
| DNI | Input texto numérico |
| Categoría | Select (valores según la sección 4.1.2) |
| Foto | Input URL (mínimo) / file upload (opcional) |

El sistema genera automáticamente el resto de los campos de la credencial (`id` de la VC, `credentialSubject.id`, `numeroSocio`, `validFrom`, `validUntil`, `credentialStatus` y `proof`); estos no son editables desde el formulario y deberían mostrarse como resultado del alta, no como parte de la carga.

Se sugiere, al confirmar el alta, mostrar una pantalla de resultado con el número de socio asignado y la vigencia antes de volver al listado.

### 4.2 Requerimientos técnicos

#### 4.2.1 Arquitectura y stack

- **Backend**: se implementa en .NET (última versión estable) con un único controller —por ejemplo `CredentialsController`— que recibe el alta, arma el `credentialSubject`, invoca al Issuer, persiste la credencial y expone el listado.
- **Issuer**: servicio in-process sin endpoint propio; recibe el `credentialSubject` y le agrega `id`, `type`, `issuer`, `validFrom`, `validUntil` y `proof`.
- **UI**: la tecnología y el diseño visual quedan a criterio del candidato, siendo Angular (última versión estable) la opción deseable. Cualquiera sea la elección, debe implementar las pantallas de Alta y Listado.
- **Clean Architecture**: opcional; la estructura concreta de capas y proyectos queda a criterio del candidato.
- **Modelo de datos**:
  - El motor de persistencia queda a criterio del candidato (por ejemplo PostgreSQL, SQL Server, SQLite, Redis, sistema de archivos, u otro); la elección debe documentarse.
  - El diseño de la entidad (tablas, colecciones, claves, archivos, etc., según el motor elegido) también queda a criterio del candidato. Como mínimo debe existir una entidad `credentials` que persista las credenciales emitidas.
- **Docker y configuración**:
  - La dockerización del backend (.NET) y del frontend (UI) es opcional, no obligatoria. El candidato puede optar por documentar en el `README.md` cómo levantar cada uno de forma local (por ejemplo `dotnet run` y el comando de arranque correspondiente a la tecnología de UI elegida) en lugar de contenerizarlos.
  - Si el motor de persistencia elegido requiere un servicio propio (por ejemplo PostgreSQL, SQL Server o Redis), se recomienda incluirlo en un `docker-compose.yml` para facilitar el levantamiento del entorno completo; motores embebidos o basados en archivos (SQLite, sistema de archivos) no lo requieren.
  - Toda la configuración se maneja vía variables de entorno.
