# ADR 002 — Persistencia

- **Estado:** Aceptado
- **Fecha:** 2026-09-29

## Contexto
El motor y el diseño de las entidades quedan a criterio del candidato; como mínimo tiene que existir una entidad `credentials` con la credencial completa (VC). Del análisis surgieron dos entidades con ciclos de vida distintos:

- **Socio:** identificado por su DNI. Su `credentialSubject.id` y su `numeroSocio` "se generan una vez y se persisten".
- **Credencial:** un documento emitido y firmado en un momento dado. Un socio puede tener varias credenciales a lo largo del tiempo (ver ADR 006).

Además, el alta y el listado trabajan sobre los mismos datos, así que las lecturas no deberían bloquear a las escrituras.

## Decisión
**PostgreSQL** con **EF Core** (proveedor Npgsql), con dos tablas:

- **`socios`**: el **estado actual** del socio.
  `id` (uuid, PK), `subject_did` (único), `numero_socio` (bigint, único, desde una secuencia — ver ADR 003), `dni` (único), `nombre`, `apellido`, `categoria`, `foto`, `created_at`, `updated_at`.
- **`credentials`**: un **snapshot inmutable** de lo que se emitió.
  `id` (uuid, PK, el Guid de la VC), `vc_id` (URI, único), `socio_id` (FK), `tenant_id`, snapshot del sujeto (`nombre`, `apellido`, `dni`, `numero_socio`, `categoria`, `foto`), `valid_from`, `valid_until`, `status` (smallint, estado **actual**), `document` (**`json`**, la VC completa), `created_at`.
  Índices: `socio_id` y `valid_from desc`.

Criterios:
1. **`socios` refleja el dato actual; `credentials` refleja lo que se emitió en cada momento.** El listado muestra `credentials`, así que el historial nunca se pierde aunque cambien los datos del socio.
2. **El listado lee solo `credentials`** (columnas desnormalizadas), sin join.
3. **La VC se guarda en una columna `json`, no `jsonb`.** `jsonb` guarda una representación binaria ya parseada: reordena las claves, descarta espacios y, si hay claves duplicadas, se queda con la última. Para verificar la firma no importa (se recalcula sobre la forma canónica, ver ADR 004), pero el documento guardado dejaría de ser el que se emitió. `json` valida la sintaxis y conserva el texto exacto. La ventaja de `jsonb` (consultar e indexar dentro del documento) no se usa, porque todo lo que se consulta está en columnas propias.
4. **El estado actual vive en la columna `status`, separado de la VC firmada.** El `credentialStatus` dentro del documento queda tal como se emitió. La firma certifica que "en el momento T el issuer afirmó este contenido"; una revocación futura se consultaría en un registro aparte (una lista de revocadas o, según el estándar W3C, una *Bitstring Status List*) sin mutar el documento. Mutarlo invalidaría la firma.
5. **Concurrencia:** Postgres usa MVCC, así que las lecturas del listado no bloquean las altas y viceversa.

## Alternativas consideradas
- **SQLite:** no requiere infraestructura, pero admite **un solo escritor a la vez**; bajo altas concurrentes se vuelve un cuello de botella.
- **MongoDB:** es viable.
  - Soporta transacciones ACID entre varios documentos desde la versión 4.0, pero solo corriendo como replica set (aunque sea de un nodo), lo que complica el entorno.
  - El número secuencial se resuelve con un contador y `findOneAndUpdate` + `$inc`.
  - BSON preserva el orden de los campos.

  Se descarta porque el modelo es **relacional**: Socio 1—N Credencial, integridad referencial, unicidad de DNI y una secuencia. Postgres trae todo eso de fábrica; en Mongo habría que construirlo o resignarlo. La única parte documental (la VC) es un bloque opaco que no se consulta por dentro y entra en una columna `json`, así que Postgres cubre lo relacional y lo documental. Mongo convendría si hubiera muchos tipos de credenciales con esquemas muy distintos, o una escala de escritura que requiera sharding.
- **Una sola tabla `credentials`** sin entidad socio: no permite garantizar que `credentialSubject.id` y `numeroSocio` se generen una sola vez por socio.

## Consecuencias
- Requiere levantar PostgreSQL (se incluye en `docker-compose.yml`) y Docker para los tests de integración (Testcontainers).
- Hay datos duplicados a propósito entre `socios`, el snapshot y el documento. Es el precio de tener un historial fiel y un listado sin joins.
- El modelo queda preparado para la revocación (columna `status`) y para varios tenants (`tenant_id`) sin implementarlos.
