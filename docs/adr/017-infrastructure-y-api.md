# ADR 017 — Infrastructure y Api: migraciones al arrancar, serialización y simulación de falla

- **Estado:** Aceptado
- **Fecha:** 2026-09-30

## Contexto
Con el Tenant terminado (ADR 015 y 016), quedaba definir detalles de implementación de Infrastructure y de la Api que no estaban en ningún ADR: cómo se aplican las migraciones, cómo se serializan las fechas y el documento, cómo se activa la falla de firma simulada, y cómo se configura la conexión.

## Decisión
1. **Migraciones de EF Core, aplicadas al arrancar la API** (`Database.MigrateAsync()`), salvo que `Database__MigrateOnStartup=false`. Así `docker compose up` y `dotnet run` funcionan sin un paso manual, y los tests de integración prueban las migraciones reales. La herramienta `dotnet-ef` se fija como *local tool* (`backend/dotnet-tools.json`) en la misma versión que EF Core.
2. **Nombres de tablas, columnas e índices fijados a mano** en snake_case (`WalletDbContext`), tal como los define el ADR 002. La migración generada se marca como código generado en `.editorconfig` para que los analizadores no la evalúen.
3. **Unicidad de DNI:** el índice `ux_socios_dni` es el que produce la violación que el repositorio traduce a `DuplicateDniException` (ADR 015). Ante cualquier fallo de guardado se limpia el change tracker, para que el reintento no reinserte las entidades del primer intento (ADR 016).
4. **Configuración por variables de entorno:** `ConnectionStrings__Wallet` (con `Timeout=5` para el timeout de conexión; el de comando es de 10 s en el código), `Issuer__*`, `Tenant__Id`, `Database__MigrateOnStartup`. El `.env.example` las documenta y el `.env` real no se versiona.
5. **Serialización HTTP:**
   - Encoder `UnsafeRelaxedJsonEscaping`: la VC devuelta conserva las tildes y la ñ, igual que el documento firmado.
   - Un converter propio escribe **todas** las fechas como `yyyy-MM-ddTHH:mm:ssZ` (ADR 005), también las del sobre; sin él, System.Text.Json las devolvía como `+00:00`.
6. **Validación en un `RequestValidator` propio** (y no con DataAnnotations): devuelve los errores agrupados por campo con los nombres del JSON (`nombre`, `dni`…) dentro de un `ValidationProblemDetails`. Reutiliza `Dni.TryNormalize`, `CategoriaText.TryParse` y `NumeroSocio.TryParse` del Tenant, para que la regla exista en un solo lugar.
7. **Errores:** un `IExceptionHandler` traduce `IssuerSigningException` (500 `issuer_signing_failed`), `DuplicateDniException` (409 `duplicate_dni`) y las fallas transitorias de la base o timeouts (503 `database_unavailable`); todo lo demás es un 500 sin detalles internos.
8. **Falla de firma simulada:** `SimulatedFailureCredentialIssuer` **reemplaza** al Issuer real (no lo envuelve como decía el ADR 012, porque nunca delega). Se registra solo en Development y con `Issuer__SimulateFailure=true`. Los tests de integración lo registran directamente.
9. **`/health`** chequea la conexión a la base y se usa como healthcheck en Docker.
10. **Tests de integración:** `WebApplicationFactory` + Testcontainers (PostgreSQL 17). Cada test parte de tablas vacías y de la secuencia en 1. Cubren el alta (con verificación del HMAC sobre el documento devuelto), el socio existente, la validación, la falla de firma sin filas persistidas, la carrera de altas del mismo DNI, el listado y sus filtros.

## Alternativas consideradas
- **Aplicar las migraciones con un paso aparte** (`dotnet ef database update` o un contenedor de migración): más prolijo para varias réplicas, pero agrega un paso manual para quien evalúa. Con una sola instancia, migrar al arrancar alcanza; con varias habría que moverlo a un paso de despliegue.
- **DataAnnotations para validar:** los nombres de los campos en el error dependen de la configuración de MVC y las reglas del DNI y de la URL igual requerían validadores propios.
- **Convención snake_case con un paquete externo:** una dependencia más para dos tablas.

## Consecuencias
- Levantar la API contra una base vacía deja el esquema listo. Si dos instancias arrancan a la vez, ambas intentan migrar (Npgsql toma un lock, pero no es una estrategia para producción).
- La VC devuelta se puede verificar recalculando el HMAC sobre el documento sin `proof`, sin ningún paso de re-serialización propio de la API.
- La simulación de falla solo se demuestra a mano en Development; en un despliegue productivo el flag no tiene efecto.
