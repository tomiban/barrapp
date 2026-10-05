# Estándares de ingeniería

Prácticas permanentes de validación, seguridad, datos, rendimiento y resiliencia. Las que aún no aplican están abajo con su **disparador**: se activan cuando aparece la feature que las necesita.

## Aplican siempre (API .NET)

- **Validá toda entrada.** Cada command/query tiene su **validador de FluentValidation** en Application; un **`ValidationBehavior`** del pipeline de MediatR lo ejecuta y corta antes del handler. El API traduce el fallo a Problem Details. Nunca confíes en el cliente.
- **No concatenes SQL.** Cero SQL armado con strings; EF Core parametriza. El SQL crudo solo como consulta parametrizada.
- **Sacá los secretos del repo.** Claves y cadenas de conexión por variables de entorno o *user-secrets*; `.env` en `.gitignore`; el repo solo lleva `.env.example`.
- **Transacciones donde haya más de una escritura.** Una operación que escribe en más de un sitio va dentro de una transacción.
- **Índices en lo que filtrás.** Indexa las columnas por las que se filtra u ordena.
- **Matá los N+1.** Usa `Include`/proyecciones; nunca consultes dentro de un bucle por elemento.
- **Pool de conexiones.** El pool es el de EF Core; no abras una conexión por request a mano.
- **Migraciones versionadas.** EF Core migrations, siempre versionadas y aplicadas en orden.
- **Logs con request id.** Cada log lleva el identificador de la petición para correlacionar (ver *Logging*).
- **Health check que revise la base.** Un endpoint de salud que comprueba también la conexión a la base de datos.
- **429, no 500.** Al limitar la tasa o saturar, responde `429` (con `Retry-After`); nunca `500` por throttling.

## Límites de capas (Clean Architecture)

- La **validación** y demás cross-cutting van en **behaviors del pipeline de MediatR**, no dentro de los handlers.
- Orden de behaviors de fuera hacia dentro: **logging → validación → (caché) → (transacción)**; el logging es el más externo y la validación va antes de la caché.
- La transacción pasa por el puerto **`IUnitOfWork`**, no por `DbContext`.
- Los handlers son `IRequestHandler` de **MediatR**; los endpoints inyectan `ISender`.
- **Application** no referencia EF Core ni ASP.NET Core; solo Domain y paquetes de abstracciones.
- Los **puertos** (repositorios, `IUnitOfWork`) se declaran en **Application**; sus implementaciones, en **Persistence**.
- **Commands** pasan por el dominio; **queries** proyectan directo a DTO.
- Los handlers leen como **tabla de contenidos** (cargar → actuar → guardar); la regla vive en Domain.
- **Architecture tests** (NetArchTest) bloquean el cruce de capas y corren en CI.

## Dominio y patrones (Clean Architecture)

- El **Domain no referencia paquetes externos**; `IDomainEvent` es un marcador puro (no hereda de MediatR).
- Entidades con **constructor privado + factory** que valida invariantes; setters privados; el estado solo cambia por métodos.
- **Value objects** para conceptos con reglas; **errores de dominio** en `DomainErrors`; usa `Result`/`Error`, no excepciones para el flujo.
- Organiza el dominio **por concepto/agregado**, no por tipo (`Entidades/`, `Eventos/`…).
- **Un `AddX()` por capa**; `Program.cs` legible.
- **OpenAPI nativo** (`AddOpenApi`/`MapOpenApi`), sin Swashbuckle.
- **Exception handler global** (`IExceptionHandler`) + `AddProblemDetails`; `UseExceptionHandler` arriba del pipeline.
- Los endpoints inyectan `ISender`.

## Logging (Serilog)

- **Domain no loguea.** No referencia `ILogger`; lanza **domain events** y se loguea en su handler.
- **Application**: un **`LoggingBehavior`** registra entrada, salida, duración y **resultado fallido** de cada command/query; los handlers no llevan `ILogger`.
- **Infrastructure**: `ILogger` directo en cada interacción externa (antes, después y el fallo con su excepción).
- **Presentation**: middleware de request/response (`UseSerilogRequestLogging`) + **exception handler global** que loguea lo no controlado.
- **Cada fallo se loguea una sola vez**: el `LoggingBehavior` no captura excepciones; suben al handler global.
- **Structured logging** con Serilog; enriquece con **correlation id** y contexto de usuario.
- **No loguees el body por defecto**: registra el **nombre** del request y opta explícitamente por propiedades; nunca secretos.
- **Niveles**: `Warning` para peticiones lentas (>500 ms); `Error` para fallos; `Critical` para arranque/corrupción.
- El handler global loguea la **excepción completa** (stack trace) y devuelve un mensaje genérico al cliente.

## Anti-patrones (y su antídoto)

Guardrails, en positivo:

- **Domain puro**: nada de EF Core, atributos de persistencia ni `ILogger` en Domain; la configuración EF va en Persistence (`IEntityTypeConfiguration`) y los invariantes se validan en el constructor. Lo vigilan los architecture tests.
- **Dominio rico, no anémico**: las entidades exponen comportamiento (métodos) con setters privados; la regla vive en la entidad o en el motor, no en un servicio que la manipula desde fuera.
- **Casos de uso que solo orquestan**: cargar → actuar → guardar; ninguna regla inline en el handler.
- **Abstrae solo en la frontera**: interfaces para base de datos, APIs externas, tiempo y sistema de ficheros; la lógica interna no necesita interfaz (`IDateTimeProvider` sí; un `IGuidGenerator` no).
- **Infraestructura dividida por preocupación**: cada proyecto de infraestructura referencia solo sus paquetes (hoy `Persistence`; cuando lleguen caché o mensajería, se separan).
- **Sin "mapping mania"**: los commands/queries **son** los DTO de entrada; las respuestas se proyectan directo en la query y el dominio se mapea a persistencia por configuración EF. No un mapper por frontera.
- **Lee por el camino corto**: las queries no cargan agregados; proyectan a DTO (ver CQRS).
- **Dependencias solo hacia dentro**: Application nunca referencia Infrastructure; los architecture tests lo bloquean en CI.
- **Pragmatismo**: Clean Architecture es un medio, no un fin; se aplica con criterio y se evita la ceremonia por la ceremonia.

## Front (app nativa)

- Estilos con **Unistyles**; colores, espaciado y tipografía salen de los tokens del design system (`docs/specs/0002-design-system.md`), nunca hardcodeados.
- Los componentes de UI se toman del design system, no de `View`/`Text` con estilos ad hoc.

## Cuando aplique

- **Cuando llegue el login (auth):** hashea las contraseñas con un algoritmo lento y sal; pon rate limit en el login; nunca compares en claro.
- **Cuando haya llamadas externas (p. ej. el LLM futuro):** timeout en cada llamada; reintentos **solo** en errores transitorios (con backoff), nunca en errores de negocio.
- **Cuando el esquema tenga datos en producción:** no borres ni renombres columnas en el mismo deploy; usa **expand–contract** (añade, migra, retira después).
- **Cuando haya despliegue:** backups automáticos y un **restore probado de verdad**.

## No aplica

- **Pagos idempotentes:** esta app no procesa pagos.
