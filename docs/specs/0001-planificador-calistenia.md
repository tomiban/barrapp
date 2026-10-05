# Spec 0001 — Planificador de calistenia

**Estado**: lista para agente · **Tickets**: issues #2–#30 (milestones M1–M7) · **Fuente de verdad**: este archivo.

## Problem Statement

Entreno calistenia por mi cuenta y no sé estructurar un mes de entrenamiento que me haga progresar hacia un *skill* sin descuidar la fuerza general. Acabo improvisando sesiones, cargando unos patrones de más y otros de menos, y sin saber cuándo debería subir de nivel. Quiero introducir mis datos y recibir una **planificación mensual bien estructurada**, con criterios claros de progreso.

## Solution

Una **app nativa en español (kg/cm)** (iOS y Android, con Expo) donde introduzco mi perfil —peso, altura, **máximos** de ejercicios básicos, el *skill* que quiero conseguir y cuántos días puedo entrenar— y obtengo un **mesociclo de 4 semanas**: sesiones con bloque de *skill* + fuerza por **patrón** (empuje/tirón/pierna) + core, progresión ondulante por **RIR** con **deload**, y el criterio para avanzar mi *skill*. Incluye una **sesión suelta** guiada para días con poco tiempo o ganas. Funciona **offline-first**: puedo consultar el plan y **registrar set a set** mis sesiones (reps o segundos) sin conexión, y todo se sincroniza al volver la red. Un backend .NET con SQLite guarda perfiles, planes y registros.

## User Stories

1. Como atleta, quiero crear mi perfil con peso y altura, para que el plan use mis datos corporales reales.
2. Como atleta, quiero introducir mi **máximo** (reps estrictas) en cada ejercicio básico, para que las cargas se ajusten a mi nivel.
3. Como atleta, quiero poder introducir `0` cuando no puedo hacer ni una repetición, para que el plan me dé una **regresión** en lugar de un ejercicio imposible.
4. Como atleta, quiero elegir un **skill** objetivo del catálogo (pino, front lever, planche, pistol squat), para que el plan apunte a él.
5. Como atleta, quiero elegir cuántos días por semana entreno (3–5), para que el plan encaje en mi agenda.
6. Como atleta, quiero que el plan sea un **mesociclo de 4 semanas**, para tener un bloque definido con descarga.
7. Como atleta, quiero que cada sesión empiece con un calentamiento, para reducir el riesgo de lesión.
8. Como atleta, quiero un bloque de *skill* al inicio de la sesión, para practicarlo fresco.
9. Como atleta, quiero trabajo de fuerza para empuje, tirón y pierna, para mantener una fuerza general equilibrada.
10. Como atleta, quiero trabajo de core, para sostener mis *skills*.
11. Como atleta, quiero que con 3 días/semana el reparto sea **full-body**, para cubrirlo todo en cada sesión.
12. Como atleta, quiero que con 4 días/semana se alterne **tren superior / tren inferior**, para gestionar la fatiga.
13. Como atleta, quiero que con 5 días/semana el reparto sea **por patrón**, para repartir el volumen.
14. Como atleta, quiero que la progresión siga **RIR 3 → 2 → 1** y un **deload RIR 4**, para construir y recuperar dentro del mes.
15. Como atleta, quiero que las cargas se deriven como **porcentaje de mi máximo** y nunca al fallo, para entrenar con seguridad.
16. Como atleta, quiero que la semana de deload baje el volumen ~50 %, para recuperarme.
17. Como atleta, quiero ver mi etapa actual del *skill* y su **escalera de progresión**, para saber dónde estoy.
18. Como atleta, quiero un criterio claro para avanzar de etapa, para saber cuándo progresar.
19. Como atleta, quiero avanzar de etapa al cumplir el criterio en **dos sesiones consecutivas**, para que el progreso se gane.
20. Como atleta, quiero que el plan use **regresiones** cuando mi máximo es 0, para poder entrenar igualmente ese patrón.
21. Como atleta, quiero ver mi plan semana a semana, para poder seguirlo.
22. Como atleta, quiero ver en cada sesión sus ejercicios, series×reps e intensidad, para saber qué hacer.
23. Como atleta, quiero marcar una sesión como completada, para registrar mi adherencia.
24. Como atleta, quiero que mis máximos se ajusten para el siguiente mesociclo según las sesiones registradas, para que el plan siga mi progreso.
25. Como atleta, quiero generar una **sesión suelta** cuando tengo poco tiempo, para poder entrenar igualmente.
26. Como atleta, quiero fijar el tiempo disponible, la energía y el foco de la sesión suelta, para que encaje en mi día.
27. Como atleta, quiero una opción «sorpréndeme» en la sesión suelta, para escapar de la monotonía del plan.
28. Como atleta, quiero que la sesión suelta quede en mi historial pero **no** cambie mi progresión, para que siga siendo un extra.
29. Como atleta, quiero ver mi historial de mesociclos, para revisar lo que hice.
30. Como atleta, quiero la interfaz en español y unidades kg/cm, para que me resulte natural.
31. Como atleta, quiero consultar mi plan sin conexión, para seguirlo en el gimnasio aunque no haya señal.
32. Como atleta, quiero registrar mis sesiones sin conexión, para que la falta de red no me impida trackear.
33. Como atleta, quiero que mis registros se sincronicen solos al volver la conexión, para no tener que hacerlo a mano.
34. Como atleta, quiero registrar set a set las reps que hice en ejercicios de fuerza y los segundos que aguanté en holds, para trackear mi rendimiento real.
35. Como atleta, quiero anotar el RIR/RPE real de cada serie, para registrar mi esfuerzo.
36. Como atleta, quiero editar o borrar un registro pasado, para corregir errores.
37. Como atleta, quiero instalar la app en mi iPhone y mi Android, para entrenar desde el móvil.

## Implementation Decisions

- **Front**: **app nativa iOS y Android** con **Expo / React Native + TypeScript y Expo Router**, builds vía EAS. La base de código es React Native puro (no `react-native-web`); la PWA queda fuera del MVP (ADR-0004). En español, unidades kg/cm.
- **Backend**: .NET 10 Minimal API + EF Core + SQLite, dockerizado en local; PostgreSQL como ruta de escalado (ADR-0001).
- **Capas**: Clean Architecture + CQRS en cinco proyectos —**Domain** (motor puro), **Application** (commands/queries, puertos, validadores, behaviors), **Infrastructure** (servicios externos), **Persistence** (EF/SQLite, repositorios, migraciones), **Api** (Minimal API fina)— (ADR-0005). Reglas permanentes en `docs/engineering-standards.md`.
- **Patrones de la API**: endpoints finos por feature con extension methods `Map*Endpoints()` que inyectan `ISender`; **CQRS con MediatR** (`ICommand`/`IQuery` + handlers + `IPipelineBehavior`); **validación con FluentValidation** por command/query en Application, ejecutada por un `ValidationBehavior`; un `AddX()` por capa; **OpenAPI nativo**; **exception handler global**; **Result/Error** mapeado a códigos HTTP como Problem Details.
- **Cross-cutting**: behaviors de MediatR (`AddOpenBehavior`) en orden logging → validación → (caché) → (transacción); transacción y caché son opt-in por marcador y usan puertos de Application (`IUnitOfWork`), nunca `DbContext` directo.
- **Logging**: Serilog estructurado por capa —Domain no loguea (lanza domain events), Application vía `LoggingBehavior`, Infrastructure directo, Api vía request logging + exception handler global; correlation id; sin cuerpos por defecto.
- **Datos en Application**: puertos (interfaces de repositorio/servicios) en Application; **commands** por repositorios + UnitOfWork y **queries** que proyectan directo a DTO vía `IApplicationDbContext` (expone `DbSet<T>`).
- **Motor**: determinista, puro y server-side, sobre una **base de conocimiento curada**. Interfaz pública:
  - `GenerarPlan(perfil, objetivo, frecuencia) → Plan`
  - `GenerarSesionSuelta(perfil, objetivo, parámetros) → Sesión`
- **Base de conocimiento**: datos versionados separados del código en **JSON embebido** (`knowledge/exercises.json` y `knowledge/skills.json`), cargados al arrancar a un **catálogo en memoria** y validados con **fail-fast**; el catálogo **no** vive en tablas. Incluye el catálogo de ejercicios agrupados (empuje/tirón/pierna más core) con su acondicionamiento de apoyo por skill, las escaleras de progresión de cada skill (4–6 etapas) con su criterio y su **rutina de patrón** (la prescripción, específica del skill, que entrena su patrón: series, reps o segundos y descanso), plantillas de sesión y reglas de progresión. Añadir un ejercicio o retocar una escalera no recompila el dominio ni genera una migración (ADR-0009).
- **LLM**: solo como **capa posterior** que traduce/variar y que el motor valida (ADR-0002). Nada de IA en la v1.
- **Offline-first**: la app mantiene **almacén local en el dispositivo** (SQLite vía `expo-sqlite`) con el plan cacheado y una **cola de sincronización** (*outbox*); los cambios suben al backend al recuperar la red. Sincronización **last-write-wins**, válida por ser mono-usuario. La **generación** de plan y de sesión suelta **requiere conexión**, porque el motor es server-side (ADR-0002, ADR-0003).
- **Modelo de registro**: `SessionLog` guarda, **por serie**, el valor real ejecutado —**reps** en fuerza o **segundos** en holds/skill— con la unidad derivada del tipo de ejercicio y **RIR/RPE real opcional**; editable y borrable. Alimenta el avance de etapa del skill y el ajuste de máximos del siguiente mesociclo.
- **Vocabulario de dominio**: el de `GLOSSARY.md`.
- **Modelo de datos** (todo con `UserId`; tabla `Users` con una fila única en el MVP, sin login): `User`, `AthleteProfile` (peso, altura), `Exercise`, `Maximum` (por ejercicio), `Skill`, `SkillStage` (escalera), `AthleteSkillProgress`, `Objective`, `Plan`/`Mesocycle`, `Microcycle` (semana), `Session`, `SessionItem` (ejercicio, series, reps, intensidad, patrón), `SessionLog` (por serie: reps o segundos reales, unidad, RIR/RPE real), `SessionSuelta` (parámetros + generada + registrada). El cliente replica en su almacén local el plan y los registros, más una cola de salida pendiente de sincronizar.
- **Auth**: sin login en el MVP; esquema preparado para multi-usuario desde el inicio.
- **Validación de entradas**: peso 30–200 kg; altura 120–220 cm; máximos enteros ≥ 0 y obligatorios (0 permitido → regresión).
- **Reparto por frecuencia**: 3 días → full-body; 4 → tren superior/inferior alterno (skill en días de tren superior); 5 → por patrón. En todos, cada patrón recibe trabajo ≈2×/semana.
- **Anatomía de sesión**: calentamiento → bloque de *skill* (fresco) → fuerza por patrón (1–2 ejercicios) → core.
- **Progresión**: S1 RIR 3 (base) → S2 RIR 2 (+volumen) → S3 RIR 1 (+volumen) → S4 deload RIR 4 (~50 % del volumen). Cargas como % del máximo, nunca al fallo.
- **Criterio de etapa de skill**: cada etapa mide una marca en **segundos mantenidos** (holds: pino, front lever, planche) o en **repeticiones** (pistol squat), con sus series. Es dato de la escalera, no regla.
- **Avance de skill**: se sube de etapa al cumplir el criterio de la etapa actual en dos sesiones consecutivas.
- **Sesión suelta**: parámetros tiempo (15/30/45/60 min), energía (baja/media/alta), foco (patrón o skill) y «sorpréndeme». El motor mapea tiempo+energía a ejercicios/series/RIR, filtra el catálogo por el foco y respeta la escalera del skill si aparece. Se guarda en historial y **no** altera el mesociclo ni los máximos.
- **API** (contractos): perfil (crear/leer) y **objetivo** (skill objetivo: leer/fijar), catálogo (ejercicios, skills y **progreso** por skill), generar plan, leer plan e historial, registrar sesión, generar sesión suelta. El prompt en lenguaje natural queda para después.
- **Pantallas**: onboarding (perfil y objetivo), sesión del día, vista del plan (semana/sesión), biblioteca (catálogo de ejercicios y escaleras de skill), registro de sesión, generador de sesión suelta, historial.

## Testing Decisions

- Un buen test aquí afirma el **comportamiento externo del motor**: dados un perfil y un objetivo, el plan/sesión resultante cumple los invariantes de programación (cobertura de patrones, volumen, calendario de RIR, deload, etapa de skill). Nunca se asoman a helpers internos.
- **Costura principal (dominio)**: la interfaz pública del **motor de generación**. Es pura (sin E/S), así que los tests son deterministas y rápidos; el API queda como adaptador fino.
- **Costura secundaria fina (cliente)**: la **cola de sincronización** (*outbox*), para verificar que encola cambios sin conexión y los sube una sola vez al reconectar. La lógica de sync no entra en el motor; se aísla en el almacén local del cliente.
- **Módulos a testear**: el motor de generación y su conjunto de reglas; la cola de sincronización del cliente; la carga/validación de la base de conocimiento puede llevar un test fino.
- **Architecture tests** (NetArchTest) que bloquean el cruce de capas: Domain puro, Application sin ASP.NET Core (EF Core solo para `IApplicationDbContext`).
- **Casos**: cada frecuencia (3/4/5) produce un reparto válido; máximo 0 → regresión; la semana 4 es deload (~50 % volumen, RIR 4); derivación de cargas desde el máximo; mapeo y avance de etapa de skill; la sesión suelta respeta tiempo/energía/foco; invariantes (cada patrón ≈2×/semana, nunca al fallo); registro de reps/segundos por serie y encolado offline sube sin duplicar.
- **Prior art**: no hay (greenfield). Se establece **xUnit** para el proyecto de tests, apuntando al motor como librería.

## Out of Scope

- Generación por LLM y prompt en lenguaje natural (post-MVP; ADR-0002).
- **Generación sin conexión** (motor en el cliente): la generación de plan y sesión suelta requiere red en el MVP (ADR-0003).
- Auth/login y multi-usuario real (esquema preparado, funcionalidad pospuesta).
- Nutrición, IMC y objetivos de pérdida de grasa.
- Cardio y acondicionamiento metabólico.
- Skills más allá de los cuatro iniciales y escaleras de más de 4–6 etapas.
- Adaptación del plan en vivo dentro del mes.
- PWA / versión web (la v1 es solo nativa; ADR-0004).
- PostgreSQL y despliegue en nube.
- Material multimedia de ejercicios (vídeos/imágenes).

## Further Notes

- **Costura de test**: principal, la interfaz pública del motor; secundaria, la outbox del cliente.
- **Tickets**: viven en el issue tracker (GitHub), issues #2–#30 en los milestones M1–M7.
- **Repositorio**: https://github.com/tomiban/barrapp (privado).
- **Decisiones registradas**: `docs/adr/0001-backend-dotnet-sqlite.md`, `docs/adr/0002-motor-determinista-llm-como-capa.md`, `docs/adr/0003-offline-first-sincronizacion.md`, `docs/adr/0004-nativo-ios-android-mvp.md`, `docs/adr/0005-clean-architecture-cqrs.md`.
