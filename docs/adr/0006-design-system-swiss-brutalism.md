# Design system: Functional Swiss Brutalism, dark-only

Adoptamos como lenguaje visual el **Functional Swiss Brutalism**: estética industrial-suiza funcional, paleta fría de señalización (amarillo `#FFCC00` y cobalto `#0055FF`) sobre carbón, tipografía Space Grotesk / Chivo / JetBrains Mono, profundidad por **bordes sin sombras**, radios de 2–4 px y **dark-only**. Tomamos la **prosa** de la propuesta como fuente de verdad del color y descartamos su set de tokens Material 3 (neutros cálidos oliva/arena) por contradecir esa intención. Se descarta el tema claro por coste/beneficio: el uso es gimnasio a alto contraste. La **implementación** (cómo los componentes consumen los tokens) se decide en un ADR aparte. Normalizado para React Native (dp, `letterSpacing` en puntos) en `docs/specs/0002-design-system.md`.

## Considered Options

- **Paleta cálida Material 3** (los tokens del YAML): descartada; contradice la intención fría de la prosa.
- **Tema claro + oscuro**: descartado por doblar tokens y QA sin beneficio para el caso de uso.
