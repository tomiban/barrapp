This is an Expo/React Native mobile application. Prioritize mobile-first patterns, performance, and cross-platform compatibility.

## Expo has changed — do not trust your training data

Expo ships breaking changes every SDK release. APIs you remember are likely renamed, moved, or removed. Before writing any code that touches an Expo, EAS, or React Native API:

1. Read the major version of the `expo` package in `package.json`.
2. Fetch the matching versioned docs: `https://docs.expo.dev/versions/v<major>.0.0/`
3. For anything else, use https://docs.expo.dev/llms.txt as an **index**, then fetch the **specific `.md` page** you need (e.g. `https://docs.expo.dev/versions/v<major>.0.0/sdk/camera.md`). Never pull the whole CLI reference or all of `llms.txt`, and never answer from memory.

## Commands

Use `bunx` instead of `npx` if the project uses bun (`bun.lock` present).

```bash
npx expo install <package>  # ALWAYS use instead of npm/yarn/pnpm/bun add — resolves SDK-compatible versions
npx expo start              # start the dev server
npm run lint                # lint
npm run typecheck           # typecheck (tsc --noEmit)
npm test                    # tests (Jest + Testing Library)
npm run format:check        # format check (Prettier)
npx expo-doctor             # diagnose dependency and config issues
npx expo install --check    # verify dependency versions match the SDK
npx expo install --fix      # fix incompatible package versions
```

Run lint, format check, typecheck and tests before declaring any task done.

`typecheck` falla si `.expo/types/router.d.ts` (typed routes, generado y git-ignored) quedó obsoleto tras añadir o mover una ruta: regeneralo con `npx expo start` y reintentá.

## Navigation & Routing

- Use **Expo Router** for all navigation. Routes live in `src/app/` — every file there is a screen, `_layout.tsx` files define navigators. Keep non-route code (components, hooks, utils) outside `src/app/`.
- Import `Link`, `router`, and `useLocalSearchParams` from `expo-router`.
- Docs: https://docs.expo.dev/router/introduction.md

## Building with EAS

Use EAS to build, sign, and submit the app in the cloud (`eas build`, `eas submit`) and to ship over-the-air updates (`eas update`) — no local Xcode or Android Studio required. Run EAS CLI as `bunx eas-cli <command>` in Bun projects, or `npx eas-cli@latest <command>` otherwise; substitute that for bare `eas` in docs examples.
Docs: https://docs.expo.dev/eas/index.md

Build profiles, app identifiers and the manual build/install recipe live in `mobile/EAS.md`. The repo does not use `expo-dev-client` (dev loop is Expo Go) nor `expo-updates`.

## Rules

- If `ios/` and `android/` directories do not exist, they are generated (Continuous Native Generation). Never create or edit them by hand — configure native behavior in `app.json` and config plugins.
- Expo Go only includes its bundled native modules. After adding a library with native code, the app needs a development build: `npx expo run:ios|android` locally, or `eas build --profile development`.
- Prefer recommended Expo modules over third-party libraries, and check your available skills before adding dependencies. Docs: https://docs.expo.dev/versions/latest/index.md
