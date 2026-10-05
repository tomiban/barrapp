# Clean Architecture + CQRS en el backend

El backend .NET se organiza en cuatro capas —**Domain, Application, Infrastructure, Api**— con la regla de dependencia hacia dentro y **CQRS** (*commands*/*queries* con handlers) en Application, siguiendo el enfoque de Minimal APIs como adaptador fino y errores como Problem Details. El dominio (el motor de generación) queda **puro y testeable**, separado de la infraestructura. Se elige sobre un esquema plano Engine+API porque el proyecto busca servir de aprendizaje/portafolio en .NET y porque aísla las reglas de programación. Se descarta un enfoque sin capas por acoplar dominio y persistencia; el dispatch de handlers (DI directa o MediatR) se decide en el primer ticket de backend.

## Considered Options

- **Engine + API plano**: más simple, pero mezcla responsabilidades y no separa dominio de infraestructura.
- **Clean Architecture sin CQRS**: misma separación con menos ceremonia; se descarta para ejercitar CQRS, objetivo del proyecto.
