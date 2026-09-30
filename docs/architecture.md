# Arquitectura

> Esqueleto inicial. Los diagramas definitivos (componentes y secuencia del alta) se completan en el paso final, cuando el código esté estable.

## Visión general

- **Tenant** (negocio del club): arma el `credentialSubject`, gestiona socios y persiste.
- **Issuer** (emisión): agrega `id`, `type`, `issuer`, `validFrom`, `validUntil` y `proof`. No conoce la persistencia ni el negocio.

Detalle de la estructura y de las dependencias entre proyectos: [ADR 001](adr/001-estructura-de-la-solucion.md).

## Flujo de alta (UC01)

1. La UI envía `POST /api/credentials` con nombre, apellido, DNI, categoría y foto.
2. La API valida (ADR 007).
3. Tenant busca el socio por DNI:
   - si no existe, toma `numeroSocio` de la secuencia y genera `credentialSubject.id`;
   - si existe, reutiliza ambos (ADR 006).
4. Tenant arma el `credentialSubject` y llama al Issuer, **sin conexión abierta a la base**.
5. Issuer canonicaliza (ADR 004), firma y devuelve la VC.
   - Si falla, se responde un error y **no se persiste nada**.
6. Transacción corta: se guarda o actualiza el socio, se inserta la credencial y se hace commit (ADR 003).
7. La API responde 201 con la VC, el número de socio, la vigencia y si el socio es nuevo.

*Diagrama de secuencia: pendiente.*

## Listado (UC02)

`GET /api/credentials?dni=&numeroSocio=`: lee solo `credentials` (snapshot desnormalizado), filtrado por tenant y, opcionalmente, por socio, de lo más nuevo a lo más viejo.

## Modelo de datos

Ver [ADR 002](adr/002-persistencia.md).

## Modelo de la credencial (VC)

*Pendiente: ejemplo completo de una VC emitida por el sistema.*