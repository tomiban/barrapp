# Arquitectura

Backend .NET con **Clean Architecture + CQRS**: el dominio (el motor de generación) es puro y el resto son adaptadores. La App nativa consume el API.

## Capas (regla de dependencia hacia dentro)

```
      Api  ──▶  Application  ──▶  Domain
                       ▲
                Infrastructure
```

- **Domain** (puro): entidades y las reglas de programación — el **motor de generación**. Sin E/S, sin EF, sin HTTP, sin dependencias externas. Es el único lugar con lógica de programación y la **costura de test principal**.
- **Application** (casos de uso, **CQRS**): *commands* y *queries* con sus handlers, **DTOs**, **puertos** (interfaces de repositorio/servicios) y **validadores**. Devuelve **Result/Error**, no HTTP. Organizada **por feature** (Screaming Architecture).
- **Infrastructure**: EF Core + SQLite, repositorios, migraciones y la **carga de la base de conocimiento** (datos versionados). Implementa los puertos que declara Application.
- **Api** (adaptador, Minimal API): **endpoints finos** que traducen HTTP → command/query y el resultado → respuesta; errores como **Problem Details** (400/404/409/429…).

## Handlers finos y CQRS

- Un handler se lee como una **tabla de contenidos**: cargar → actuar → guardar. La regla de negocio vive en **Domain**; el handler solo orquesta.
- **Commands** pasan por el modelo de dominio (repositorios + UnitOfWork). **Queries** saltan el dominio y proyectan **directamente a DTO** (vía `IApplicationDbContext`).
- Los **puertos** (interfaces de repositorio/servicios) se declaran en **Application**.

## Cross-cutting

- Validación, logging y caché van como **decoradores** sobre los handlers (Scrutor `Decorate`), nunca dentro del handler.
- Validación con **FluentValidation**: cada command/query tiene su validador a su lado.

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
- **Application** depende de Domain y de paquetes de **abstracciones** (p. ej. contratos de FluentValidation); **nunca** de EF Core ni ASP.NET Core.
- **Infrastructure** implementa los puertos de Application; **Api** es el composition root.
- **Nada depende de Api.**
- La **lógica de programación solo vive en Domain.**
- **Architecture tests** bloquean el cruce de capas.

## Costura de test

Principal: el **dominio** (motor puro, sin DB ni HTTP). Secundaria: los handlers de **Application** (con sus validadores) y, en el cliente, la **outbox/sync**. Los **architecture tests** vigilan los límites. Ver `docs/working-rules.md` y `docs/engineering-standards.md`.

## Decisiones

Las decisiones técnicas duraderas (backend, motor, front, offline, capas) están en `docs/adr/`.
