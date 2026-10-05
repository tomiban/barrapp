# Backend .NET con SQLite para un MVP mono-usuario

La herramienta es de uso personal y podría vivir 100% en el navegador, pero decidimos construir un backend mínimo (**.NET 9 Minimal API + EF Core + SQLite**, dockerizado en local) porque el proyecto busca servir de **aprendizaje/portafolio con .NET** y allanar una **futura versión multi-usuario**. SQLite por ser una base de datos ligera sin infraestructura; PostgreSQL queda como ruta de escalado. Se descarta el enfoque local-first precisamente por su dificultad para crecer a multi-usuario.

## Considered Options

- **Local-first (IndexedDB), sin servidor**: más simple y suficiente para un solo usuario, pero no escala a multi-usuario y no cumple el objetivo de aprendizaje con .NET.
- **Backend .NET + PostgreSQL**: mejor para escalar, pero añade infraestructura innecesaria para el MVP.
