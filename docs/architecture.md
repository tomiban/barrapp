# Arquitectura

Backend **.NET 10** con **Clean Architecture + CQRS (MediatR)**: el dominio (el motor de generación) es puro y el resto son adaptadores. La App nativa consume el API. Clean Architecture es un **medio, no un fin**: se aplica con pragmatismo (el dominio tiene reglas reales; nada de abstracción ni mapeo por ceremonia).

## Proyectos y referencias (regla de dependencia hacia dentro)

```
src/
  Barrapp.Domain          puro, sin paquetes externos
  Barrapp.Application     casos de uso, CQRS, validación, behaviors
  Barrapp.Infrastructure  servicios externos (tiempo, email, caché, auth)
  Barrapp.Persistence     EF Core + SQLite, repositorios, migraciones
  Barrapp.Api             Minimal API, composition root
tests/
  Barrapp.Domain.UnitTests
  Barrapp.Application.UnitTests
  Barrapp.Persistence.IntegrationTests
  Barrapp.Api.FunctionalTests
  Barrapp.ArchitectureTests
```

```
Api ─▶ Application ─▶ Domain
 │         ▲
 ├▶ Infrastructure ──┘
 └▶ Persistence ─────┘
```

- **Domain no depende de nada.** **Application** → Domain.
- **Infrastructure** y **Persistence** → Application.
- **Api** compone: referencia a Application, Infrastructure y Persistence.
- Cada capa expone **un** `AddX()` (`AddApplication`, `AddInfrastructure`, `AddPersistence`).

## Capas

- **Domain** (puro): entidades, *value objects*, *domain errors* y las **reglas de programación** — el **motor de generación**. Primitivas `Result`, `Error`, `IDomainEvent` (marcador puro, **sin MediatR**). **Organizado por concepto**, no por tipo. Sin paquetes externos; es la **costura de test principal**.
- **Application** (casos de uso, **CQRS con MediatR**): *commands* y *queries* con handlers (`IRequestHandler`), **DTOs**, **puertos** (repositorios, `IUnitOfWork`), **validadores** y **behaviors**. Devuelve **Result/Error**, no HTTP. Organizada **por feature**.
- **Persistence**: EF Core + SQLite, `ApplicationDbContext`, configuraciones, repositorios (implementan los puertos de Application) y migraciones.
- **Infrastructure**: servicios externos (tiempo, email, caché, auth).
- **Api** (Minimal API): **endpoints finos** que inyectan `ISender`, traducen HTTP → command/query y el resultado → respuesta; **Problem Details**, **OpenAPI nativo** y **exception handler global**.

## Handlers finos y CQRS

- Abstracciones propias: `ICommand`/`IQuery` con `ICommandHandler<,>`/`IQueryHandler<,>`, todas devolviendo `Result`.
- Un handler se lee como una **tabla de contenidos**: cargar → actuar → guardar. La regla de negocio vive en **Domain**.
- **Commands** pasan por el modelo de dominio (repositorios + `IUnitOfWork`). **Queries** saltan el dominio y proyectan **directamente a DTO** a través de un **puerto de lectura** (`I<Feature>ReadService`) que implementa Persistence; así EF Core no entra en Application.
- Los **puertos** se declaran en **Application**.

## Cross-cutting (pipeline de MediatR)

- Los cross-cutting van como **`IPipelineBehavior`** de MediatR (`AddOpenBehavior`), nunca dentro del handler.
- Orden de fuera hacia dentro: **logging → validación → (caché) → (transacción) → handler**. El logging es el más externo; la validación va antes de la caché.
- Validación con **FluentValidation**: cada command/query tiene su validador a su lado; un `ValidationBehavior` corta el pipeline si falla.
- Transacción y caché son **opt-in** por marcador (`ITransactional`, `ICacheable`) y usan **puertos de Application** (`IUnitOfWork`, caché), **nunca** `DbContext` directo.
- **Logging** por capa: Domain no loguea (lanza domain events); Application vía `LoggingBehavior`; Infrastructure directo; Api vía request logging + exception handler global. Ver `docs/engineering-standards.md`.

## Interfaz de dominio (la costura)

- `GenerarPlan(perfil, objetivo, frecuencia) → Plan`
- `GenerarSesionSuelta(perfil, objetivo, parámetros) → Sesión`

Todo lo demás del dominio es interno. Un LLM futuro sería **infraestructura** que traduce o aporta variedad, y el dominio la valida; el dominio nunca depende de él.

## App (cliente nativo)

Expo / React Native (iOS y Android), TypeScript y Expo Router.

- Pantallas, formularios, vista del plan y registro set a set.
- Estilos con **Uniwind** (Tailwind CSS v4) sobre los tokens del design system (`docs/specs/0002-design-system.md`).
- **Almacén local** (`expo-sqlite`) con el plan cacheado y una **outbox** para trabajar sin conexión; sincroniza contra el API (last-write-wins, mono-usuario).
- No duplica la lógica del dominio: la generación es server-side y requiere conexión.

## Base de conocimiento (datos, no código)

Catálogo de ejercicios, escaleras de progresión y reglas viven como **datos versionados** separados del código. Añadir un ejercicio o retocar una escalera no recompila el dominio.

## Reglas de dependencia (invariantes)

- **Domain no depende de nadie** (ni de paquetes externos).
- **Application** → Domain + paquetes de abstracciones (contratos de MediatR y FluentValidation); **nunca** EF Core ni ASP.NET Core.
- **Infrastructure** y **Persistence** → Application. **Api** compone.
- **Nada depende de Api.**
- La **lógica de programación solo vive en Domain.**
- **ArchitectureTests** bloquean el cruce de capas en CI.

## Costura de test

Principal: el **dominio** (motor puro, sin DB ni HTTP). Secundaria: los handlers de **Application** (con sus validadores) y, en el cliente, la **outbox/sync**. Los **architecture tests** vigilan los límites. Ver `docs/working-rules.md` y `docs/engineering-standards.md`.

## Decisiones

Las decisiones técnicas duraderas (backend, motor, front, offline, capas) están en `docs/adr/`.
