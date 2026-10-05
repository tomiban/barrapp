# EF Core en Application para el lado de lectura (`IApplicationDbContext`)

Las **queries** saltan el dominio y proyectan directamente a DTO, y necesitan ejecutarse de forma **asíncrona** con EF Core (`FirstOrDefaultAsync`, traducción de LINQ a SQL). Para no escribir una interfaz por grupo de queries, **Application referencia EF Core** y declara `IApplicationDbContext`, que expone `DbSet<T>`; **Persistence** implementa el contexto. Es el **compromiso pragmático** del lado de lectura: se acepta que EF Core entre en Application a cambio de una proyección directa, cómoda y asíncrona, en línea con el enfoque de Clean Architecture de Milan Jovanović. El lado de escritura no cambia: los **commands** pasan por el modelo de dominio (repositorios + `IUnitOfWork`), el **Domain sigue puro** (sin EF Core) y los architecture tests mantienen esa frontera. Consecuencia: el test `Application_does_not_depend_on_efcore_or_aspnetcore` se relaja a **solo ASP.NET Core**, y `docs/architecture.md` y `docs/engineering-standards.md` recogen la excepción. Complementa a `docs/adr/0005-clean-architecture-cqrs.md`.

## Considered Options

- **Puerto de lectura específico** (`I<Feature>ReadService`) implementado en Persistence: el más puro (cero EF Core en Application), pero obliga a una interfaz por grupo de queries y saca la proyección del handler. Descartado.
- **`IQueryable<T>` + materializador genérico** en `IApplicationDbContext`, sin EF Core en Application: mantiene la pureza, pero inventa una abstracción de ejecución que no es estándar. Descartado.
- **Dapper con `IDbConnectionFactory`**: SQL explícito y rápido, pero añade un segundo acceso a datos que mantener. Descartado para el MVP.
