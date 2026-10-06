# El plan se genera en el lado de lectura sobre el agregado del atleta

`GET /plan` devuelve el mesociclo que produce el **motor puro de dominio** `PlanGenerator.Generate(perfil, objetivo, etapaActual, catálogo)`. Como la entrada del motor es el **modelo de dominio** —el agregado `AthleteProfile` (con sus máximos), el `Objective` y la **etapa actual** del atleta en ese skill—, el handler de la query los **hidrata** vía `IApplicationDbContext` y luego proyecta el `Plan` resultante a DTO. Es una **excepción documentada** a la regla «las queries saltan el dominio y proyectan directo a DTO» (`docs/engineering-standards.md`, `docs/architecture.md`): aquí no existe una proyección que baste, porque la propia generación es una **regla de negocio pura** y su costura pública es el motor. La query sigue leyendo como una tabla de contenidos (cargar → generar → mapear) y no persiste nada. Complementa a `docs/adr/0005-clean-architecture-cqrs.md` y `docs/adr/0008-efcore-en-application.md`.

## Considered Options

- **Un command** que genere y persista el plan: prematuro mientras el plan no se guarde; la generación es determinista y sin efectos, así que un `GET` idempotente es el contrato natural. Descartado.
- **Proyectar a DTO los datos que necesita el motor** y reconstruir el plan sin el agregado: obliga a duplicar la forma del perfil y a reimplementar invariantes fuera del dominio. Descartado.
- **Construir entidades de dominio a mano desde una proyección**: mismo problema, con un mapeo extra por cada campo nuevo del perfil. Descartado.
