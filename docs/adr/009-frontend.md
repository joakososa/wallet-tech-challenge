# ADR 009 — Frontend

- **Estado:** Aceptado
- **Fecha:** 2026-09-29

## Contexto
La tecnología de UI queda a criterio del candidato, con Angular (última versión estable) como opción deseable. Tiene que implementar las pantallas de Alta y Listado; se sugiere una pantalla de resultado después del alta. El estilo puede ser simple y minimalista.

## Decisión
1. **Angular, última versión estable**, con **standalone components** y **signals** para el estado, que es el enfoque idiomático actual.
2. **Reactive forms** con validaciones que replican las del backend (ver ADR 007), y los errores por campo del backend (ProblemDetails) mapeados a los campos del formulario.
3. **CSS propio minimalista**, sin librería de componentes.
4. Pantallas:
   - **Listado:** foto (con imagen de reemplazo si la URL no carga), nombre, apellido, categoría, número de socio, vigencia y estado (con marca de "vencida" si corresponde). Filtro por DNI o número de socio. Estado vacío. Detalle expandible con los campos técnicos (`id`, `type`, `issuer`, `proof`).
   - **Alta:** formulario con los campos de la sección 4.1.3 del enunciado; el botón de envío se deshabilita mientras se procesa, para evitar altas duplicadas.
   - **Resultado:** número de socio, vigencia e indicación de si el socio era nuevo o existente.
5. **Comunicación con la API:**
   - En desarrollo, el proxy de `ng serve` reenvía `/api` al backend.
   - En Docker, nginx sirve la aplicación y reenvía `/api` al contenedor del backend.

   En ambos casos el frontend llama a una ruta relativa (`/api/...`) desde el mismo origen: no hace falta configurar CORS ni incrustar la URL de la API al compilar.

## Alternativas consideradas
- **Angular Material u otra librería de UI:** agrega peso y tiempo de configuración para dos o tres pantallas simples.
- **Otro framework (React, Blazor):** Angular es la opción que el enunciado marca como deseable.
- **URL de la API configurada al compilar** (`environment.ts`): obliga a recompilar por entorno.

## Consecuencias
- El frontend no depende de dónde está desplegado el backend.
- Las validaciones se duplican entre el frontend y el backend (aceptado en ADR 007).
