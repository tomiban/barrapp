# Front styling: react-native-unistyles

El front estiliza con **react-native-unistyles** (MIT, v3): tokens, variantes y temas tipados sobre un núcleo nativo performante. Se elige sobre `@shopify/restyle` —buen encaje token-first, pero con un reporte creíble de **archivado a finales de 2026**— y sobre **NativeWind + Reusables** (Tailwind, sin módulo nativo). Coste asumido: Unistyles usa un **módulo nativo**, así que el desarrollo usa **development builds** en lugar de Expo Go (que la app acabará necesitando igual para EAS). El design system (`docs/specs/0002-design-system.md`) implementa sus tokens sobre Unistyles.

## Considered Options

- **`@shopify/restyle`**: JS puro y Expo Go-friendly, pero riesgo de archivado a corto plazo.
- **NativeWind + React Native Reusables**: Tailwind y componentes copy-paste, sin módulo nativo; se descarta para disponer de tokens/variantes tipados de primera clase.
- **StyleSheet + tokens propios**: cero dependencias, más trabajo manual de variantes.
