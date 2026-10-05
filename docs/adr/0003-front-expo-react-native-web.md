# Front en Expo (React Native + react-native-web)

El front se construye con **Expo (React Native + `react-native-web`) y TypeScript**, con Expo Router, apuntando a **PWA** en el MVP. Elegimos un stack React Native aunque el MVP solo necesite una web app porque el **roadmap incluye apps nativas iOS/Android**: así un único código sirve para PWA y nativo sin reescribir. Se descarta React + Vite —más simple y con control fino del service worker/manifest— porque obligaría a rehacer el front al añadir el nativo, y se descarta una app nativa directa porque el MVP debe instalarse desde el navegador.

## Considered Options

- **React + Vite (SPA web)**: más simple y PWA "pura", pero no reutilizable para nativo.
- **App nativa directa**: mejor rendimiento nativo, pero pierde la instalación vía navegador que pide el MVP.
- **Expo / React Native Web** (elegida): un solo código para PWA y nativo, al precio de más tooling y dependencia de Expo/EAS.
