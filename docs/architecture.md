# Arquitectura

Tres piezas, con una sola dirección de dependencia: **Engine** → **API** → (App). El Engine es el corazón; el resto son adaptadores.

## Engine (dominio puro)

Librería .NET **pura y determinista**: sin E/S, sin HTTP, sin EF Core. Es el **único** lugar donde vive la lógica de programación.

- Tipos del dominio y la **base de conocimiento** (carga y validación de datos versionados).
- Reglas de generación: estructura del mesociclo, reparto por frecuencia, cargas desde los máximos, progresión por RIR, deload, regresiones, bloque de skill, sesión suelta y avance de etapa.

**Interfaz pública (la costura):**

- `GenerarPlan(perfil, objetivo, frecuencia) → Plan`
- `GenerarSesionSuelta(perfil, objetivo, parámetros) → Sesión`

Todo lo demás dentro del Engine es interno. Un LLM futuro sería una **capa** que traduce o aporta variedad y que el Engine valida; el Engine nunca depende de él.

## API (adaptador)

Minimal API .NET + EF Core + SQLite, dockerizada. **Adaptador fino**: valida la entrada, llama al Engine, persiste, responde. No contiene reglas de programación; si una regla aparece aquí, su sitio es el Engine.

## App (cliente nativo)

Expo / React Native (iOS y Android), TypeScript y Expo Router.

- Pantallas, formularios, vista del plan y registro set a set.
- **Almacén local** (`expo-sqlite`) con el plan cacheado y una **outbox** para trabajar sin conexión; sincroniza contra el API (last-write-wins, mono-usuario).
- No duplica la lógica del Engine: la generación es server-side y requiere conexión.

## Base de conocimiento (datos, no código)

Catálogo de ejercicios, escaleras de progresión y reglas viven como **datos versionados** separados del código. Añadir un ejercicio o retocar una escalera no recompila el Engine.

## Reglas de dependencia (invariantes)

- El **Engine no depende de nadie** (ni del API, ni de EF, ni de HTTP).
- El **API depende del Engine**; nunca al revés.
- La **App habla con el API**; no importa código del Engine.
- La lógica de programación **solo** vive en el Engine.

## Costura de test

Una costura de dominio: la **interfaz pública del Engine** (tests puros, rápidos, deterministas). El cliente añade una costura fina en su **outbox/sync**. Ver `docs/working-rules.md`.

## Decisiones

Las decisiones técnicas duraderas (backend, motor, front, offline) están en `docs/adr/`.
