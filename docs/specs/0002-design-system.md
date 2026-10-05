# Spec 0002 — Design system (Functional Swiss Brutalism)

**Estado**: lista para agente · **Tickets**: por crear · **Fuente de verdad**: este archivo. Deriva de la propuesta «Functional Swiss Brutalism», normalizada para React Native y **dark-only**.

## Problem Statement

La app necesita un lenguaje visual y un set de componentes coherentes, legibles bajo esfuerzo físico y a distancia, para no improvisar estilos pantalla a pantalla.

## Solution

Un design system dark-only de estética industrial-suiza funcional: tokens, tipografía, layout, profundidad por bordes (sin sombras) y un set de componentes base más los específicos de entrenamiento (timers, métricas, listas).

## Principios

- **La forma sigue a la función biomecánica**; nada decorativo (sin skeuomorfismo, blurs ni gradientes).
- **Profundidad por bordes y capas tonales**, nunca por sombras o luz.
- **Métricas monolíticas** legibles a 3 m; números **monoespaciados tabulares** (sin jitter durante timers).
- El **amarillo señal** se reserva a acción primaria y estado activo; el **cobalto** a referencia/confirmación.
- **Etiquetas además de color** (nunca el color solo) para comunicar estado.
- **Dark-only**.

## Tokens

### Color

| Token | Valor | Rol |
|---|---|---|
| `canvas` | `#111214` | Fondo del aparato (nivel 0) |
| `surface` | `#1E2022` | Celdas y paneles (nivel 1) |
| `surfaceMuted` | `#26292C` | Inputs, chips inactivos (nivel 2) |
| `border` | `#33373B` | Hairline técnica (1 px) |
| `text` | `#E2E4E9` | Dato y texto principal |
| `textMuted` | `#8A9099` | Metadata |
| `primary` | `#FFCC00` | Acción primaria, set activo, countdown |
| `onPrimary` | `#111214` | Texto sobre primary |
| `secondary` | `#0055FF` | Referencia biomecánica, confirmación, calibración |
| `onSecondary` | `#FFFFFF` | Texto sobre secondary |
| `error` | `#FFB4AB` | Texto de error |
| `errorContainer` | `#93000A` | Fondo de error |
| `onErrorContainer` | `#FFDAD6` | Texto sobre error |
| `scrim` | `#000000` 80 % | Velos de modal, **sin blur** |

Estados: `focused/active` = borde `1.5 px` en `primary` o `secondary`; `selected` = inversión de fondo/texto. **Sin sombras.**

### Tipografía

Las tres fuentes son OFL (Google Fonts) → `expo-font`, sin coste.

| Estilo | Fuente | Tamaño/línea | `letterSpacing` |
|---|---|---|---|
| `displayHero` | Space Grotesk 700 | 56/56 | `-1.7` |
| `headlineMetric` | JetBrains Mono 700 | 44/44 | `-1.8` |
| `headlineLg` | Space Grotesk 700 | 28/32 | `-0.6` |
| `headlineMd` | Space Grotesk 600 | 24/28 | `-0.5` |
| `headlineSm` | Space Grotesk 600 | 18/24 | `-0.2` |
| `bodyLg` | Chivo 400 | 16/24 | `0` |
| `bodyMd` | Chivo 400 | 14/20 | `0` |
| `bodySm` | Chivo 400 | 12/16 | `0` |
| `labelTechnical` | JetBrains Mono 600 | 11/14 | `+0.9` |
| `labelCode` | JetBrains Mono 500 | 13/18 | `+0.3` |

Los tamaños mayores (`displayHero` 88, `headlineMetric` 64…) quedan para pantallas grandes; en teléfono manda la tabla.

### Espaciado (dp, base 8 pt)

`xs 4` · `sm 8` · `md 16` · `lg 24` · `xl 40` · `gutter 16` · `margin 20`.

### Radios (dp)

`sm 2` · `base 4` · `md 6` · `lg 8` · `xl 12`. **Pills prohibidas.** Se permiten esquinas achaflanadas (corte a 45°) en contadores y chips.

### Bordes

`hairline 1` · `active 1.5` · `overlay frame 2`.

### Niveles de elevación

| Nivel | Fondo | Uso |
|---|---|---|
| 0 | `canvas` | Backplate |
| 1 | `surface` | Celdas y paneles |
| 2 | `surfaceMuted` | Inputs, chips inactivos |
| 3 | `surface` + marco `2 px` `text` + scrim | Modales/alerts |

### Layout

- Móvil: retícula de **4 columnas**, `gutter 16`, `margin 20`.
- **Split-screen de ejecución**: top **60 %** telemetría, bottom **40 %** controles.

## Componentes

### Base

Button (primary/secondary/tertiary) · MetricCounter · TextField · Chip/SegmentedControl · Checkbox (20×20 dp, radio 0) · Radio (cuadrado, no círculo) · BiomechanicalCard.

### De entrenamiento

Timer/Countdown · ProgressIndicator · StatusBadge (roles semánticos explícitos) · ListRow/section header (semanas, sesiones, ejercicios) · SplitScreen de ejecución.

### Navegación y feedback

Screen/Header · TabBar · BottomSheet/Modal · Toast/Banner · Loading/Skeleton · EmptyState · Text.

**Roles semánticos de estado** (definidos, no al revés): activo/en curso → `primary`; calibrado/confirmado → `secondary`; sobrecarga/fallo → `error`; inactivo → `textMuted`.

## Accesibilidad

- Alto contraste; verificar cada par texto/fondo.
- **Estado nunca solo por color**: siempre etiqueta textual.
- Tamaños táctiles: 56 dp (primaria), 48 dp (secundaria).
- Foco/activo visible por borde, no por glow.

## Restricciones de plataforma

- Solo **iOS y Android** (Expo / React Native). Sin retícula desktop.
- Unidades **dp**; `letterSpacing` en **puntos** (no `em`); `mm` descartado.
- Fuentes OFL vía `expo-font`.

## Out of Scope

- Tema claro.
- Web/desktop.
- Iconografía definitiva.
- Motion tokens (duraciones/easing) — a definir junto con la implementación.
- Pantallas concretas de la app.

## Further Notes

- **Implementación**: **Uniwind** (MIT, free) —Tailwind CSS v4 sobre React Native (ADR-0007). Los tokens se definen en `@theme`; corre en Expo Go.
- **Tickets**: issues #31–#49 en el milestone M8; vive en `src/design-system/` de la app.
- Deriva de la propuesta «Functional Swiss Brutalism», con la paleta fría de su prosa como fuente de verdad.
