# CLAUDE.md

Contexto para retomar el trabajo en este repositorio.

## Qué es
Prueba técnica: sistema que emite (UC01) y lista (UC02) credenciales verificables (VC) firmadas con HMAC-SHA256 para socios de un club de fútbol. El enunciado original está en `docs/enunciado.md` (si no está, pedírselo al usuario). Se evalúa: funcionalidad de punta a punta, diseño documentado, calidad de código y documentación de decisiones.

## Modo de trabajo (importante)
- **Iterativo:** Claude propone (una o varias alternativas, con recomendación), el usuario revisa y decide. No se avanza de paso ni se commitea sin su revisión.
- Las preguntas abiertas se dejan **en texto** con una recomendación, no en popups de opciones.
- Explicar los conceptos desde cero cuando son de un dominio nuevo (credenciales verificables, canonicalización, etc.).
- **Toda decisión relevante va a un ADR** en `docs/adr/` (usar `000-template.md`). **Los ADRs aceptados no se editan:** si una decisión cambia, se emite un ADR nuevo que la reemplaza, y en el anterior solo se actualiza el campo *Estado* ("Reemplazado por ADR XXX").
- El usuario maneja el repositorio git. Commits frecuentes y chicos, en español, con conventional commits: `tipo(scope): descripción`. El tipo describe la naturaleza del cambio, no si la feature está terminada (`feat`, `test`, `refactor`, `fix`, `docs`, `build`, `chore`). Scopes: `issuer`, `tenant`, `infra`, `api`, `ui`, `docker`, `docs`. Cada commit debería compilar y dejar los tests en verde.

## Arquitectura (resumen; detalle en `docs/architecture.md`)
- Monorepo: `backend/` (.NET 10), `frontend/` (Angular), `docs/`, `docker-compose.yml` en la raíz.
- Proyectos backend y referencias permitidas:
  - `Wallet.Issuer` → **no referencia a nadie**. Canonicaliza, firma y arma la VC. Expone `ICredentialIssuer`; la implementación es interna.
  - `Wallet.Tenant` → Issuer. Casos de uso (alta, listado), entidades `Socio` y `Credential`, interfaces de persistencia.
  - `Wallet.Infrastructure` → Tenant. EF Core + Npgsql, secuencia, migraciones.
  - `Wallet.Api` → todos. Único `CredentialsController`, validación, ProblemDetails, DI.
- PostgreSQL: `socios` (estado actual) y `credentials` (snapshot inmutable + VC en una columna `json`).

## Invariantes que no se pueden romper
- **Canonicalización** (ADR 004 y 012): `Utf8JsonWriter`, primer nivel en orden fijo y claves del `credentialSubject` ordenadas con `StringComparer.Ordinal`, compacto, sin escapar lo no ASCII. El test golden contra el ejemplo del enunciado tiene que pasar siempre.
- **Fechas** (ADR 005): se truncan a segundos al generarlas; formato `yyyy-MM-ddTHH:mm:ssZ`. Lo firmado = lo persistido = lo devuelto.
- **Si la firma falla, no se persiste nada** (UC01 5a). La firma ocurre fuera de la transacción; la transacción es corta (ADR 003).
- **El documento VC persistido no se muta.** El estado actual vive en una columna aparte (ADR 002).
- **El DNI es la identidad inmutable del socio** (ADR 006).
- La clave HMAC nunca se hardcodea ni se versiona.

## ADRs
| # | Tema |
|---|---|
| 001 | Estructura de la solución |
| 002 | Persistencia (PostgreSQL, modelo, VC como `json`, estado separado) |
| 003 | Flujo de alta, secuencia de numeroSocio y resiliencia |
| 004 | Canonicalización del JSON a firmar |
| 005 | Manejo de fechas |
| 006 | Identidad del socio y DNI |
| 007 | Validación y formato de errores |
| 008 | Repositorio y ejecución |
| 009 | Frontend |
| 010 | Versión de Node (reemplaza al ADR 008 en ese punto) |
| 011 | `verificationMethod` = `{Did}#{KeyId}` (reemplaza al ADR 004 en ese punto) |
| 012 | Contrato del Issuer: claims genéricos, `credentialStatus` enum, `IssuerSigningException` y decorator de simulación de falla (reemplaza al ADR 004 en la canonicalización del subject) |
| 013 | Serialización del documento VC (`VerifiableCredential.ToJson()`, `proof` al final; el punto 2 fue reemplazado por el ADR 014) |
| 014 | `CredentialJsonBuilder`: escribe el JSON canónico (sin `proof`) y el documento (`WithProof`); reemplaza al ADR 013 en el punto 2 |
| 015 | Diseño del Tenant: entidades, puertos, casos de uso, reintento por carrera de DNI, `Dni.TryNormalize`, `TenantOptions`, tests con fakes |

## Estado actual
Plan aprobado. Pasos:
- [x] 0. Docs iniciales (README, CLAUDE.md, ADRs, esqueleto de arquitectura, enunciado)
- [x] 1. Bootstrap del backend
- [x] 2. Issuer + tests
- [ ] 3. Tenant + tests (en curso, ver "Paso 3: qué falta")
- [ ] 4. Infrastructure
- [ ] 5. Api + tests de integración
- [ ] 6. Frontend
- [ ] 7. Docker
- [ ] 8. Docs finales (extras si sobra tiempo: upload de foto, idempotency key)

### Paso 3: qué falta
**Hecho y commiteado** (84 tests en verde):
- ADR 013, 014 y 015 (aceptados).
- Issuer: `VerifiableCredential.ToJson()` y `CredentialJsonBuilder`.
- Tenant: `Socio`, `Credential` (con `FromIssued`), `Categoria` + `CategoriaText`, `ISocioRepository`, `ICredentialRepository`, `DuplicateDniException`, `CredentialFilter`, `CredentialSummary`. Tests de `Categoria`, `Socio` y `Credential`.

**Pendiente, en este orden** (el diseño completo está en el ADR 015; se implementó una primera versión del punto 1 y se descartó sin commitear, hay que rehacerla):
1. **`IssueCredentialUseCase`** + `IssueCredentialCommand` (nombre, apellido, DNI, categoría, foto) + `IssueCredentialResult` (VC, `NumeroSocio` con formato, `IsNewSocio`) + `Dni.TryNormalize` + `NumeroSocio.Format` (`D6`, culture invariant) + `TenantOptions` (`Id`, por defecto `club-futbol`, lo necesita el caso de uso). Constructor: `ISocioRepository`, `ICredentialRepository`, `ICredentialIssuer`, `TimeProvider`, `TenantOptions`.
   - Flujo: normalizar DNI (si es inválido lanza `ArgumentException`; la API valida antes) → buscar socio → si no existe, `NextNumeroSocioAsync` y DID nuevo; si existe, reutilizar DID y número → claims (`id`, `nombre`, `apellido`, `dni`, `numeroSocio`, `categoria`, `foto`) y `types = ["VerifiableCredential","SocioCredential"]` → firmar → **recién después** `Socio.Update` (si es existente) o `new Socio` → `Credential.FromIssued` → `AddAsync`.
   - Ante `DuplicateDniException` se repite todo el flujo una sola vez (hay que volver a firmar con los datos del ganador); si falla de nuevo, se propaga.
   - Tests con fakes escritos a mano (sin librería de mocking) en `tests/Wallet.UnitTests/Tenant/Fakes`: `InMemoryStore` (implementa los dos puertos, con contador de llamadas y un gancho `OnAdd` para simular la carrera), `FakeCredentialIssuer` (arma una VC con lo recibido y tiene un gancho `OnIssue` para lanzar `IssuerSigningException`) y `FixedTimeProvider`. Casos: socio nuevo, socio existente, DNI con puntos, DNI inválido, falla de firma (con y sin socio existente: no se persiste ni se modifica nada), carrera de DNI con reintento, carrera repetida, snapshot igual a la VC, propagación del `CancellationToken`.
2. **`ListCredentialsUseCase`** + su query: filtra por tenant y, opcionalmente, por DNI (normalizado) y número de socio (parseo del string a `long`); el orden y el estado vacío lo resuelve el repositorio. Tests con el `InMemoryStore` (que también implementa `ListAsync`).
3. **`AddTenant()`** (extensión de DI; reutiliza el `TimeProvider` de `AddIssuer()`) y su test, al estilo de `IssuerServiceCollectionExtensions`.

**Consultas abiertas para el usuario** (no bloquean, decidir al retomar):
- El DID del socio nuevo usa el mismo `Guid` que su `Id` (`did:example:{Id}`). Es una simplificación; si se prefieren Guids distintos, es un cambio de una línea.
- `ICredentialRepository.AddAsync(socio, credential)` no dice si el socio es nuevo: Infrastructure lo resuelve con el change tracker de EF (el socio devuelto por `FindByDniAsync` está tracked y se actualiza; uno nuevo se inserta). Está documentado en la interfaz. Si en el paso 4 resulta incómodo, se cambia con un ADR nuevo.
- Cómo se inserta el documento VC (`ToJson()`) dentro del sobre de la respuesta HTTP se decide en el paso 5.

## Convenciones de tests
- Nombres `Metodo_Escenario_Resultado` en español (por ejemplo `HandleAsync_SiFallaLaFirma_PropagaLaExcepcionYNoPersisteNada`). Todo test lleva las tres partes.
- Estructura arrange / act / assert separada por líneas en blanco; los tests con valores de referencia externos (golden, RFC, OpenSSL) los comentan.
- Fakes a mano, sin Moq ni NSubstitute (ADR 015).

## Comandos
Desde `backend/`:
- `dotnet build`: compila la solución (`ECredWallet.slnx`). Los warnings son errores.
- `dotnet test`: corre todos los tests.
- `dotnet run --project src/Wallet.Api`: levanta la API en `http://localhost:5080`.

Convenciones del backend:
- Configuración común en `Directory.Build.props` (net10.0, nullable, analizadores).
- Versiones de NuGet centralizadas en `Directory.Packages.props`: los `.csproj` usan `<PackageReference Include="X" />` sin versión.
- Estilo en `.editorconfig` (file-scoped namespaces, `_camelCase` para campos privados, llaves obligatorias).
