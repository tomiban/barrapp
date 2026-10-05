# Clean Architecture + CQRS en el backend

El backend .NET (**net10.0**) se organiza en cinco proyectos —**Domain, Application, Infrastructure, Persistence, Api**— con la regla de dependencia hacia dentro y **CQRS** (*commands*/*queries* con handlers) en Application, siguiendo el enfoque de Minimal APIs como adaptador fino y errores como Problem Details. El dominio (el motor de generación) queda **puro y testeable**, separado de la infraestructura. Se elige sobre un esquema plano Engine+API porque el proyecto busca servir de aprendizaje/portafolio en .NET y porque aísla las reglas de programación. Se descarta un enfoque sin capas por acoplar dominio y persistencia. El **dispatch es MediatR** (`ISender` + `IPipelineBehavior`); los **puertos** (repositorios, `IUnitOfWork`) viven en **Application** y se implementan en **Persistence**; los cross-cutting —logging y validación ahora, caché y transacción cuando su disparador los active— viven como **behaviors**, no dentro de los handlers; y **architecture tests** velan por los límites. Se acepta la dependencia de MediatR (de pago para empresas grandes) como coste asumible en un proyecto personal.

## Considered Options

- **Engine + API plano**: más simple, pero mezcla responsabilidades y no separa dominio de infraestructura.
- **Clean Architecture sin CQRS**: misma separación con menos ceremonia; se descarta para ejercitar CQRS, objetivo del proyecto.
- **Handlers por DI con decoradores (Scrutor), sin MediatR**: el mismo patrón de cross-cutting sin dependencia comercial, pero se descarta para ejercitar MediatR, muy reconocido en portafolios.
