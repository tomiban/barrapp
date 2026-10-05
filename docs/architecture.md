# Arquitectura

Backend .NET con **Clean Architecture + CQRS**: el dominio (el motor de generación) es puro y el resto son adaptadores. La App nativa consume el API.

## Capas (regla de dependencia hacia dentro)

```
      Api  ──▶  Application  ──▶  Domain
                       ▲
                Infrastructure
```

- **Domain** (puro): entidades y las reglas de programación — el **motor de generación**. Sin E/S, sin EF, sin HTTP, sin dependencias externas. Es el único lugar con lógica de programación y la **costura de test principal**.
- **Application** (casos de uso, **CQRS**): *commands* y *queries* con sus handlers. Orquestan el dominio y los repositorios y devuelven un **Result/Error**, no HTTP.
- **Infrastructure**: EF Core + SQLite, repositorios, migraciones y la **carga de la base de conocimiento** (datos versionados). Implementa las interfaces que declara Application.
- **Api** (adaptador, Minimal API): **endpoints finos** que traducen HTTP → command/query y el resultado → respuesta. Validación por *endpoint filters*; errores como **Problem Details** (400/404/409/429…).

## Interfaz de dominio (la costura)

- `GenerarPlan(perfil, objetivo, frecuencia) → Plan`
- `GenerarSesionSuelta(perfil, objetivo, parámetros) → Sesión`

Todo lo demás del dominio es interno. Un LLM futuro sería **infraestructura** que traduce o aporta variedad, y el dominio la valida; el dominio nunca depende de él.

## App (cliente nativo)

Expo / React Native (iOS y Android), TypeScript y Expo Router.

- Pantallas, formularios, vista del plan y registro set a set.
- **Almacén local** (`expo-sqlite`) con el plan cacheado y una **outbox** para trabajar sin conexión; sincroniza contra el API (last-write-wins, mono-usuario).
- No duplica la lógica del dominio: la generación es server-side y requiere conexión.

## Base de conocimiento (datos, no código)

Catálogo de ejercicios, escaleras de progresión y reglas viven como **datos versionados** separados del código. Añadir un ejercicio o retocar una escalera no recompila el dominio.

## Reglas de dependencia (invariantes)

- **Domain no depende de nadie.**
- **Application** depende de Domain; **Infrastructure** de Application/Domain; **Api** de Application/Infrastructure (composition root).
- **Nada depende de Api.**
- La **lógica de programación solo vive en Domain.**
- La base de conocimiento son **datos versionados**, no código.

## Costura de test

Principal: el **dominio** (motor puro, sin DB ni HTTP). Secundaria: los handlers de **Application** y, en el cliente, la **outbox/sync**. Ver `docs/working-rules.md` y `docs/engineering-standards.md`.

## Decisiones

Las decisiones técnicas duraderas (backend, motor, front, offline, capas) están en `docs/adr/`.
