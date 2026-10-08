# Spec 0003 — Pantallas (rediseño Stitch)

**Estado**: lista para agente · **Tickets**: UI #82–#90 (M9) · cimientos #91–#96 (M10) · **Fuente de verdad**: este archivo. Deriva del mockup «Calistenia Progresiva» (Stitch `projects/15402477787954800448`) y de `0001-planificador-calistenia.md`.

## Problem Statement

La app tiene las pantallas mínimas (perfil, plan, biblioteca) pero no la experiencia del mockup: falta Inicio, la ejecución guiada set a set, el historial y el generador de sesión suelta; además, el plan y el perfil no siguen el lenguaje del rediseño. Y no es solo interfaz: el motor **no implementa todavía el deload ni la regresión** y **no existe el registro set a set**, así que varias pantallas no tienen de dónde leer. Antes de implementarlas hace falta una fuente de verdad de las pantallas y de sus cimientos.

## Solution

Un rediseño de la app en cinco pestañas —**Inicio · Plan · Entreno · Skills · Historial**— con **Perfil** en el avatar de la cabecera, sobre el lenguaje visual del design system (spec 0002), más los **cimientos de backend y motor** que las pantallas necesitan: registro set a set, sesión suelta, deload, regresión, calendario y métricas.

## Regla de resolución

**Spec 0001 + `GLOSSARY.md` mandan en dominio y comportamiento; el mockup manda en lenguaje visual y flujos.** Todo lo que sigue ya está reconciliado: donde el mockup y la spec discrepaban, se decidió caso a caso y quedó registrado aquí, en el glosario o en un ADR. El diseño nunca redefine un término de dominio.

## Navegación

- Cinco pestañas: `Inicio`, `Plan`, `Entreno`, `Skills`, `Historial`. `Entreno` es la central/destacada.
- **Perfil** deja de ser pestaña y se abre desde el **avatar** de la cabecera (slot `trailing` del `Header`).
- Cabecera con el patrón `BARRAS / <SECCIÓN>` + indicador de sincronización + avatar.
- **Skills es un hub** con tres segmentos: *Escaleras* (el principal, el del mockup), *Ejercicios* y *Rutinas* (el catálogo que hoy vive en Biblioteca).

## Pantallas

### 1. Onboarding

Flujo de primer arranque, en pasos, que crea el perfil y fija el objetivo. Si no hay perfil u objetivo, la app abre aquí; si los hay, abre en las pestañas.

- **Paso 1 · Antropometría y fuerza**: peso, altura, envergadura y entrepierna con **steppers**; y test de fuerza básica (máximos en reps estrictas) con steppers. Rangos de validación de 0001 (peso 30–200 kg; altura 120–220 cm; envergadura 100–250 cm; entrepierna 50–130 cm; máximos enteros ≥ 0).
- **Paso 2 · Skill objetivo**: elección del skill del catálogo.
- **Paso 3 · Días de entrenamiento**: elección de los **días de la semana** que entrena (3–5); el número de días es cuántos elige.
- Barra de progreso y acción **Omitir por ahora** (no bloquea).
- El onboarding y Perfil **comparten las mismas secciones y validaciones**; son dos entradas al mismo formulario.

_Fuera_: el «nivel inicial asignado (INTERMEDIO)» y el checkbox «¿entrenas con lastre?» del mockup.

### 2. Inicio (dashboard del mesociclo)

- Cabecera `BARRAS / INICIO` + avatar.
- **Mesociclo activo**: semana X/4, intensidad (**RIR**), **Adherencia** (%) y barra de progreso S01–S04.
- **Objetivo**: etapa actual, nombre del skill, **Criterio de etapa** y marcas de la sesión previa.
- **Sesión de hoy**: el día de la semana y la sesión que le corresponde, con sus bloques (calentamiento → skill → fuerza → core) y el botón **Comenzar sesión**.
- Atajo **¿poco tiempo hoy?** → Sesión suelta.

El «objetivo: 3 × 12s» del mockup es el **Criterio de etapa**, no el *Objetivo* del glosario.

### 3. Plan (periodización 4S)

- **Selector de semanas S1–S4** con estados: *completada*, *en curso*, *RIR 1* (semana 3) y **descarga** (semana 4, deload).
- **Volumen semanal** por patrón (empuje/tirón/pierna), **prescrito** por el motor.
- **Sesiones de la semana**: día de la semana, estado (*hecho* / *pendiente*), número de series y duración estimada; la de hoy ofrece **Comenzar sesión**.
- Acción **Ajustar sobrecarga o volumen**.

### 4. Entreno (sesión en curso)

Ejecución **guiada**, un ejercicio y una serie a la vez, con registro **set a set**.

- Cabecera `BARRAS / ENTRENO`, modo (p. ej. `isométrico`) e indicador **offline/guardado local**.
- **Cronómetro** del hold activo (pausar / reiniciar) y **descanso** programado entre series, ambos alimentados por el **descanso que expone el `SessionItem`**.
- **Registro de serie**: valor real con la unidad derivada del ejercicio (**reps** o **segundos**), **RIR real** opcional y **lastre** opcional. El RIR real es **informativo**: no mueve el avance de etapa.
- **Progreso de series**: estructura (p. ej. `4 × 10s`) y estado por serie (*superado* / *en curso* / *pendiente*), anunciado con etiqueta, no solo por color.
- Navegación anterior/siguiente entre ejercicios y **cierre** con resumen.
- Al **terminar y guardar**, la sesión queda **completada** y cuenta para la adherencia; el marcado manual (US23) se mantiene como vía alternativa.
- Se conserva el comportamiento **offline-first** (outbox).

### 5. Skills (hub)

- Segmento **Escaleras**: los **cuatro skills** del catálogo, con el objetivo activo destacado; para cada uno, etapa actual, **escalera** con estados (*superado* / *actual* / *bloqueado* / meta final) y **Criterio de etapa** (marca en segundos o reps, con sus series).
- Segmentos **Ejercicios** y **Rutinas**: el catálogo que hoy vive en Biblioteca.

_Fuera_: el diagrama biomecánico («ángulo cadera», «retract %»), «retención máx / volumen sem. / RPE promedio», el botón «testear criterio» y el **protocolo de regresión por estancamiento**.

### 6. Historial

- Lista **por fecha descendente**, agrupada por semana.
- **Filtros**: *Todas* · *Mesociclo* · *Suelta*.
- Cada entrada: fecha, duración, nombre de la sesión, número de series, RIR real y desglose de ejercicios; las sesiones sueltas van marcadas como *extra*.
- Acciones **editar** y **borrar** un registro (US36); borrar des-marca la sesión y recalcula la adherencia.

_Fuera_: **récords/PR automáticos** y **export CSV / backup local**.

### 7. Sesión suelta

- Aviso **entrenamiento extra**: no altera el mesociclo ni los máximos.
- Parámetros: **tiempo** (15/30/45/60 min), **energía** (baja/media/alta, con las etiquetas del mockup: *suave · recuperar*, *normal · mantener*, *a tope · fuerza*) y **foco** (*patrón* o *skill*), más **sorpréndeme**.
- **Propuesta** generada por el motor con la duración estimada y su volumen.
- **Iniciar sesión suelta**; se guarda en el historial y **no** cambia la progresión (US28).

### 8. Perfil

- Se abre desde el avatar (`BARRAS / PERFIL` + volver).
- Reutiliza las secciones del onboarding: objetivo/skill, antropometría, días de entrenamiento y máximos.

## Cimientos de backend y motor

Ninguna pantalla de arriba funciona contra el API actual sin estos trabajos:

1. **Motor — deload**: la semana 4 debe ser descarga (~50 % de volumen, RIR 4), hoy pendiente en `RirWave.cs` (US16).
2. **Motor — regresión**: máximo `0`/`1` debe producir una regresión, no un ejercicio neutral, hoy pendiente en `StrengthLoad.cs` (US20).
3. **Registro set a set**: persistencia + API del `SessionLog`, anclado al plan en lectura por clave de sesión determinista + foto por ítem (ADR-0014). Cabecera (fecha, mesociclo/microciclo/día, tipo *mesociclo*/*suelta*, completada) y una fila por serie (valor, unidad, RIR real, lastre), editable y borrable.
4. **Sesión suelta**: `GenerarSesionSuelta(perfil, objetivo, parámetros)` en el motor + persistencia de la sesión suelta.
5. **Calendario**: **Día de entrenamiento** elegido por el atleta y **fecha de inicio** del mesociclo, para que «hoy» sea real.
6. **Descanso**: exponer en el `SessionItem` el descanso de la rutina que lo generó (con valor por defecto por papel).

Métricas derivadas de los registros:

- **Adherencia** = sesiones de mesociclo completadas / sesiones de mesociclo **programadas hasta la fecha actual**. Las sesiones sueltas **no** cuentan.
- **Volumen semanal** = series por patrón en un microciclo; **prescrito** en Plan, **ejecutado** en Historial.

## Fuera de alcance (post-MVP)

- Récords/PR automáticos y export CSV / backup local.
- Protocolo de estancamiento automático (3 semanas) y descarga técnica.
- Lastre en onboarding; nivel inicial autoasignado.
- Atributos técnicos de escalera (ángulo, retract) y diagrama biomecánico.
- **RPE** como campo: la intensidad percibida se captura como **RIR** (RIR + RPE ≈ 10).
- Tema claro, web/desktop, PWA, LLM.

## Testing Decisions

- **Motor** (costura principal): el deload de la semana 4 baja el volumen y sube el RIR a 4; el máximo `0`/`1` produce una regresión; la sesión suelta respeta tiempo/energía/foco y no altera el plan.
- **Registro y métricas**: el registro set a set guarda reps o segundos según la unidad, admite RIR real y lastre opcionales, y sobrevive a cambios de la base de conocimiento (foto por ítem). Adherencia y volumen se calculan como arriba.
- **Cliente**: la outbox sigue encolando sin red y subiendo una sola vez al reconectar.
- **Pantallas**: tests de comportamiento de cada pantalla (Jest + Testing Library) sobre datos simulados, sin asomarse a helpers internos.

## Further Notes

- **Vocabulario**: este rediseño añadió a `GLOSSARY.md` **Lastre**, **Adherencia**, **Volumen semanal** y **Día de entrenamiento**, y corrigió **Registro** (RIR real y lastre, sin RPE).
- **ADR**: `docs/adr/0014-anclaje-de-registros-a-plan-en-lectura.md`.
- **Tickets**: UI en M9 #82–#90; cimientos en M10 (#91 deload y regresión · #92 registro set a set · #93 sesión suelta · #94 calendario · #95 descanso · #96 métricas). Los de UI quedan **bloqueados** por el cimiento que los alimenta y por el bug #80 (pantallas en blanco); las dependencias están como `blocked_by` nativo en GitHub.
