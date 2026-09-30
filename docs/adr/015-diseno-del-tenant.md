# ADR 015 — Diseño del Tenant: casos de uso, puertos y reintento

- **Estado:** Aceptado
- **Fecha:** 2026-09-30

## Contexto
`Wallet.Tenant` es el rol de negocio del club (ADR 001): arma el `credentialSubject`, gestiona los socios y persiste. Referencia solo al Issuer y no conoce EF Core ni HTTP. Los ADR 002, 003, 006, 007 y 012 fijan el modelo, el flujo y el contrato del Issuer. Falta definir cómo se organiza el Tenant: entidades, puertos de persistencia, casos de uso y el manejo de la carrera por un DNI nuevo.

## Decisión

### Entidades
- **`Socio`**: `Id`, `SubjectDid`, `NumeroSocio` (`long`), `Dni`, `Nombre`, `Apellido`, `Categoria`, `Foto`, `CreatedAt`, `UpdatedAt`. `Update(...)` cambia solo los datos mutables (nombre, apellido, categoría y foto); el DNI, el DID y el número de socio no se pueden modificar (ADR 006).
- **`Credential`**: snapshot inmutable de lo emitido (ADR 002): `Id`, `VcId`, `SocioId`, `TenantId`, datos del sujeto, `ValidFrom`, `ValidUntil`, `Status` (estado actual), `Document` (el JSON del documento, de `VerifiableCredential.ToJson()`, ADR 013) y `CreatedAt`.
- **`Categoria`**: enum con mapeo explícito a `adulto`, `juvenil` y `niño`, para no llevar la ñ a un identificador de C#.

### Puertos de persistencia (los implementa `Wallet.Infrastructure`)
- **`ISocioRepository`**: `FindByDniAsync` y `NextNumeroSocioAsync` (`nextval` de la secuencia, ADR 003).
- **`ICredentialRepository`**:
  - `AddAsync(socio, credential)` guarda el socio (alta o actualización) y la credencial de forma **atómica**. La transacción corta queda dentro de Infrastructure; el Tenant no maneja transacciones.
  - `ListAsync(filtro)` devuelve un read model liviano, sin join.
- **`DuplicateDniException`**: la lanza el repositorio cuando el `UNIQUE(dni)` rechaza el alta.
- Los timeouts y la traducción a 503 (`database_unavailable`) pertenecen a Infrastructure y a la API; el Tenant no los captura.

### Casos de uso
Clases planas con `HandleAsync`, sin mediador: `IssueCredentialUseCase` y `ListCredentialsUseCase`.

**`IssueCredentialUseCase`** (ADR 003):
1. Normaliza el DNI y busca el socio.
2. Si no existe, toma el número de la secuencia y genera `did:example:{Guid}`. Si existe, reutiliza ambos.
3. Arma los claims (`id`, `nombre`, `apellido`, `dni`, `numeroSocio` con relleno a 6 dígitos, `categoria`, `foto`) y llama al Issuer con `types = ["VerifiableCredential","SocioCredential"]`, sin conexión abierta.
4. **Recién después de firmar** actualiza el socio existente. Si la firma falla, no queda nada modificado y `IssuerSigningException` se propaga.
5. Arma el snapshot desde la VC ya firmada y llama a `AddAsync`.
6. Devuelve la VC, el número de socio, la vigencia y si el socio era nuevo.

**Reintento por `DuplicateDniException`.** Cubre la carrera de dos altas del mismo DNI nuevo: entre el paso 1 y el commit, otra alta inserta el mismo DNI. El caso de uso **repite el flujo completo desde el paso 1, una sola vez**. No basta con reintentar el guardado: la VC del perdedor quedó firmada con un DID y un número de socio que no van a existir, así que hay que volver a firmar con los datos del socio ganador. Si el segundo intento también falla, la excepción se propaga.

**`ListCredentialsUseCase`**: filtra por tenant y, opcionalmente, por DNI y número de socio. Ordena por `validFrom` descendente. El read model incluye el `Document` para el detalle expandible opcional de la UI (sin paginación, ver enunciado 2.2).

### DNI
`Dni.TryNormalize` vive en el Tenant y es la única implementación de la regla del ADR 006 (sacar puntos y espacios; solo dígitos, entre 7 y 9).
- El **caso de uso normaliza siempre**, porque la búsqueda y el `UNIQUE` comparan strings exactos: sin normalizar, `30.123.456` y `30123456` serían dos socios.
- La **API llama a la misma función solo para validar** y devolver un 400 con el error en el campo (ADR 007); no transforma el valor.

### Configuración
`TenantOptions` con el identificador del tenant, leído de `Tenant__Id` (por defecto `club-futbol`). No hay autenticación (fuera de alcance); la columna `tenant_id` deja el modelo listo para varios tenants. `AddTenant()` registra los casos de uso y reutiliza el `TimeProvider` que ya registra `AddIssuer()`.

### Tests
Fakes escritos a mano para los puertos y para el Issuer, sin librería de mocking. Permiten simular comportamiento con estado (por ejemplo, "el primer `AddAsync` lanza `DuplicateDniException` y el segundo guarda") y el repositorio no suma una dependencia nueva.

## Alternativas consideradas
- **`IUnitOfWork` expuesto al Tenant:** obliga al caso de uso a abrir y cerrar la transacción, y a conocer su alcance. Un `AddAsync` atómico expresa la intención ("guardar esto junto") sin filtrar el mecanismo.
- **Reintentar solo el guardado:** dejaría firmada una VC con datos del socio equivocado (ver el reintento).
- **Normalizar el DNI solo en la API:** la unicidad dependería de que todo llamador lo haga; el Tenant es el dueño de la regla.
- **Mediador (MediatR o similar):** agrega una dependencia y un nivel de indirección para dos casos de uso.
- **Librería de mocking (Moq, NSubstitute):** viable y sin impacto en el diseño; se prefieren los fakes por lo dicho arriba.

## Consecuencias
- El Tenant se puede probar sin base de datos ni HTTP, incluida la garantía de UC01 5a y la carrera de DNI.
- La secuencia puede dejar huecos, también por el reintento (ADR 003).
- Infrastructure debe traducir la violación del `UNIQUE(dni)` a `DuplicateDniException` y no a un error genérico.
- El `TenantId` es hoy una constante de configuración.
