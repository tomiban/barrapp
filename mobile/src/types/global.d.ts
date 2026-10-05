/*
 * Tipos ambientales de Expo (incluyen `declare module '*.css'`, necesario para
 * importar `global.css`).
 *
 * `expo-env.d.ts` de la raíz está en `.gitignore` y solo lo genera `expo start`,
 * así que referenciamos aquí para que `npx tsc --noEmit` funcione en un checkout
 * limpio (CI) sin arrancar el bundler.
 */
/// <reference types="expo/types" />
