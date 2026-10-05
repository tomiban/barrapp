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
- **Logs con request id.** Cada log lleva el identificador de la petición para poder correlacionar.
- **Health check que revise la base.** Un endpoint de salud que comprueba también la conexión a la base de datos.
- **429, no 500.** Al limitar la tasa o saturar, responde `429` (con `Retry-After`); nunca `500` por throttling.

## Límites de capas (Clean Architecture)

- La **validación** y demás cross-cutting van en **behaviors del pipeline de MediatR**, no dentro de los handlers.
- Orden de behaviors de fuera hacia dentro: **logging → validación → (caché) → (transacción)**; el logging es el más externo y la validación va antes de la caché.
- La transacción pasa por el puerto **`IUnitOfWork`**, no por `DbContext`.
- Los handlers son `IRequestHandler` de **MediatR**; los endpoints inyectan `ISender`.
- **Application** no referencia EF Core ni ASP.NET Core; solo Domain y paquetes de abstracciones.
- **Commands** pasan por el dominio; **queries** proyectan directo a DTO.
- Los handlers leen como **tabla de contenidos** (cargar → actuar → guardar); la regla vive en Domain.
- **Architecture tests** (NetArchTest) bloquean el cruce de capas y corren en CI.

## Cuando aplique

- **Cuando llegue el login (auth):** hashea las contraseñas con un algoritmo lento y sal; pon rate limit en el login; nunca compares en claro.
- **Cuando haya llamadas externas (p. ej. el LLM futuro):** timeout en cada llamada; reintentos **solo** en errores transitorios (con backoff), nunca en errores de negocio.
- **Cuando el esquema tenga datos en producción:** no borres ni renombres columnas en el mismo deploy; usa **expand–contract** (añade, migra, retira después).
- **Cuando haya despliegue:** backups automáticos y un **restore probado de verdad**.

## No aplica

- **Pagos idempotentes:** esta app no procesa pagos.
