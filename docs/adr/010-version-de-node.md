# ADR 010 — Versión de Node para el frontend

- **Estado:** Aceptado
- **Fecha:** 2026-09-29
- **Reemplaza a:** ADR 008, solo en la versión de Node (punto 2 de la decisión)

## Contexto
El ADR 008 fijó Node 22 para el desarrollo local del frontend. Al consultar el registro de npm, la última versión estable de Angular es la **22.2.0** y declara como requisito `node: ^22.22.3 || ^24.15.0 || >=26.0.0`.

Líneas de Node compatibles:
- **22:** en mantenimiento, con menos tiempo de soporte por delante; además exige 22.22.3 o superior.
- **24:** LTS activa.
- **26:** línea actual, todavía no es LTS.

## Decisión
Usar **Node 24 LTS** (24.15.0 o superior) para el desarrollo local del frontend, gestionado con nvm-windows. La imagen de build del frontend en Docker usa la misma línea (`node:24`).

## Alternativas consideradas
- **Node 22 (22.22.3 o superior):** compatible, pero en fase de mantenimiento, así que su soporte termina antes.
- **Node 26:** compatible, pero todavía no es LTS; se prioriza estabilidad.

## Consecuencias
- Quien desarrolle en local necesita Node 24.15.0 o superior (`nvm install 24`).
- El resto del ADR 008 sigue vigente.
