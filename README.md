# Barrapp

Planificador de calistenia: app nativa (Expo / React Native) más un API .NET que genera un
mesociclo a partir del perfil del atleta. La spec es `docs/specs/0001-planificador-calistenia.md`
y las reglas de trabajo están en `docs/working-rules.md`.

## Estructura

- `src/Barrapp.*` — backend .NET 10 (Clean Architecture + CQRS): Domain, Application,
  Infrastructure, Persistence y Api.
- `tests/Barrapp.*` — xUnit: dominio, persistencia, arquitectura y funcional del API.
- `mobile/` — app Expo / React Native (Expo Router, TypeScript).
- `docs/` — specs, ADRs y reglas de trabajo.

## Requisitos

- .NET SDK 10.
- Node.js 20+ y la app **Expo Go** en el móvil.

La guía completa de entorno (variables, emulador, puertos y checks) está en
[`docs/dev-setup.md`](docs/dev-setup.md).

## Arrancar el API

```sh
dotnet run --project src/Barrapp.Api
```

Escucha en `http://0.0.0.0:5213`. Endpoints actuales:

- `GET /ping` — comprueba API y pipeline de MediatR.
- `GET /profile` / `PUT /profile` — lee y guarda el perfil del atleta (peso y altura).
- `GET /health` — comprueba la conexión a la base SQLite.
- `GET /openapi/v1.json` — OpenAPI nativo (solo en desarrollo).

Al arrancar se aplican las migraciones de EF Core pendientes; el esquema vive en la base
SQLite (`Data Source=barrapp.db`, ver `appsettings.json`).

### Con Docker

La API también corre dockerizada con su SQLite en un volumen:

```sh
docker compose up --build
```

Queda en `http://localhost:5213` y la base persiste en el volumen `barrapp-data`.

## Arrancar la app

```sh
cd mobile
npm install
npm start
```

Abre el QR con Expo Go. La pestaña **Perfil** guarda el peso y la altura en el API (`PUT /profile`)
y los vuelve a leer (`GET /profile`) para mostrarlos; la pestaña **Inicio** llama a `GET /ping`.
Si no defines `EXPO_PUBLIC_API_URL` (ver `mobile/.env.example`), la app deduce la IP del portátil
a partir del dev server de Expo y usa el puerto `5213`.

## Checks

Lo mismo que corre la CI en cada PR:

```sh
dotnet format barrapp.slnx --verify-no-changes
dotnet test barrapp.slnx

cd mobile
npm run typecheck
npm run lint
npm run format:check
npm test
```
