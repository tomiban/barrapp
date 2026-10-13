# EAS — builds e instalación en dispositivo

Guía para compilar e instalar la app en un iPhone y un Android usando **Expo Application Services (EAS)** (US-37). Cubre la configuración ya incluida en el repo ([`eas.json`](./eas.json) y [`app.json`](./app.json)), cómo apuntar la app al API en cada entorno y la receta manual para generar e instalar builds.

> El build en la nube **requiere credenciales** (cuenta Expo + Apple Developer / Google Play) y es un **paso de humano**. Este documento deja el proyecto configurado: a una persona solo le faltaría `login` + `eas build`.

**Estado de la configuración** (verificado 13/10/2026, eas-cli 24.13.0): `eas.json`/`app.json` válidos, `expo-doctor` 21/21, `expo install --check` limpio, `EXPO_PUBLIC_API_URL` de `development`/`preview` verificada inline en el bundle Hermes con `expo export`, cleartext habilitado en el manifest de release (verificado con `expo prebuild`), keystore local de Android generado (`android-keystore/barrapp.keystore`, git-ignored). **Ningún comando EAS corre sin sesión Expo** — ni siquiera `eas config` ni `eas build --local` — por lo que generar los binarios es el paso humano que falta (ver [receta](#receta-paso-a-paso-humano)).

## Estado del proyecto

- **Expo SDK 57**. EAS CLI siempre como `npx eas-cli@latest <comando>` (ver [`mobile/AGENTS.md`](./AGENTS.md)).
- El bucle de desarrollo actual es **Expo Go**: `npx expo start` y escanear el QR (ver `docs/dev-setup.md`). La app no usa `expo-dev-client`.
- No hay `expo-updates`: EAS se usa para **builds**, no para actualizaciones OTA (EAS Update queda fuera del alcance).
- El build y firma corren en la nube de EAS; no hace falta Xcode ni Android Studio local.

## Identificadores (`app.json`)

| Plataforma | Campo                       | Valor             |
| ---------- | --------------------------- | ----------------- |
| iOS        | `expo.ios.bundleIdentifier` | `com.barrapp.app` |
| Android    | `expo.android.package`      | `com.barrapp.app` |

- Se eligió `com.barrapp.app` (notación reverse-DNS, igual en ambas plataformas) como identificador del MVP. Debe ser **único** en la App Store (iOS) y en Play Store (Android).
- ⚠️ **No cambiar estos valores después de distribuir**: un identificador distinto se comporta como una app nueva (el usuario tendría que desinstalar/reinstalar y pierde datos locales).
- El `expo.version` (`1.0.0`) es la versión visible en las tiendas; la versión de build (`versionCode`/`buildNumber`) la gestiona EAS en remoto (ver [`cli.appVersionSource`](#perfiles-de-build-easjson)).

## Perfiles de build (`eas.json`)

```json
{
  "cli": { "appVersionSource": "remote" },
  "build": {
    "development": {
      "environment": "development",
      "distribution": "internal",
      "android": { "buildType": "apk" },
      "env": { "EXPO_PUBLIC_API_URL": "http://192.168.0.249:5213" }
    },
    "preview": {
      "environment": "preview",
      "distribution": "internal",
      "android": { "buildType": "apk" },
      "env": { "EXPO_PUBLIC_API_URL": "http://192.168.0.249:5213" }
    },
    "production": { "environment": "production", "autoIncrement": true }
  }
}
```

| Perfil        | Uso                                                                                                                                     | Artefacto                      | Instalación                                  |
| ------------- | --------------------------------------------------------------------------------------------------------------------------------------- | ------------------------------ | -------------------------------------------- |
| `development` | Build instalable para iterar en tu propio dispositivo durante el desarrollo (sustituto rápido de Expo Go cuando hace falta un binario). | Android **.apk**, iOS **.ipa** | Directa por enlace/QR (distribución interna) |
| `preview`     | Build casi-producción para compartir con el equipo/stakeholders (sin herramientas de desarrollo).                                       | Android **.apk**, iOS **.ipa** | Directa por enlace/QR (distribución interna) |
| `production`  | Build listo para tiendas (App Store / Play).                                                                                            | Android **.aab**, iOS **.ipa** | Store (TestFlight / Play), o `eas submit`    |

Notas sobre la configuración (verificado contra la documentación de SDK 57 / EAS):

- **`distribution: "internal"`** (development y preview): el build se puede instalar en dispositivos desde un enlace/QR de expo.dev, sin pasar por las tiendas. Debe producir **.apk**/.ipa (por eso `android.buildType: "apk"`).
- **`developmentClient` NO está activo**: el perfil `development` no usa `expo-dev-client` porque la app hoy corre en Expo Go y no lo necesita. Si en el futuro algún módulo nativo no esté en Expo Go, instalá `npx expo install expo-dev-client` y añadí `"developmentClient": true` al perfil `development`.
- **`autoIncrement: true` + `cli.appVersionSource: "remote"`** (producción): EAS gestiona y autoincrementa `versionCode`/`buildNumber` en sus servidores, lo que evita rechazos por versión de build duplicada. No hace falta tocar `app.json`.
- **`environment`**: enlaza cada perfil con las variables de entorno de EAS del mismo nombre (`eas env:set --environment <env>`). Sin ellas el build funciona igual (ver siguiente sección).

## URL del API por entorno (`EXPO_PUBLIC_API_URL`)

`mobile/src/api/client.ts#getApiBaseUrl` ya lee `process.env.EXPO_PUBLIC_API_URL` (referencia **estática**, que Metro inline en el bundle al compilar).

- En desarrollo con **Expo Go** no hace falta definirla: la app deduce el host del dev server y usa `http://<host>:5213`.
- En un **build EAS instalado en dispositivo no hay dev server**: sin la variable la app cae a `http://localhost:5213`, que no es alcanzable desde el dispositivo.
- Por lo tanto **todo build EAS instalable que deba hablar con el API necesita `EXPO_PUBLIC_API_URL` definida en tiempo de build**. La URL no es un secreto (viaja en el bundle).

**Estado actual**: los perfiles `development` y `preview` de `eas.json` ya definen `env.EXPO_PUBLIC_API_URL` con la IP de la red local del portátil (`http://192.168.0.249:5213`, verificado inline en el bundle con `expo export`). ⚠️ Si la IP LAN del portátil cambia, actualizar **los dos perfiles (`development` y `preview`) y el snippet de esta guía** antes de construir.

**HTTP plano (cleartext)**: la URL de LAN es `http://`, y los builds de `preview`/`production` son de tipo _release_, donde **Android bloquea por defecto el tráfico cleartext** (verificado con `expo prebuild`: el manifest de debug lo permite, el de release no). Por eso `app.json` incluye el plugin `expo-build-properties` con `"android": { "usesCleartextTraffic": true }` (verificado: el manifest de release lo lleva). Cuando el API de producción esté en `https://`, conviene quitar ese flag.

En iOS, `NSAllowsLocalNetworking` (ya en `app.json`) exime hosts sin calificar y `.local`, **no así una IP cruda**; si el build de iOS no llega al API de LAN por ATS, añadir `NSAppTransportSecurity > NSAllowsArbitraryLoads` (solo aceptable en builds de desarrollo/preview, Apple lo revisa en producción).

Para `production` (o si preferís no versionar la URL), las opciones (la primera es la recomendada):

1. **Variables de entorno de EAS por entorno** (no se versionan; el perfil ya referencia el `environment`):

   ```sh
   npx eas-cli@latest env:set --name EXPO_PUBLIC_API_URL --value https://api.barrapp.example --environment production --visibility plaintext
   ```

   Para usarlas en local: `npx eas-cli@latest env:pull --environment development`.

2. **Campo `env` en el perfil de `eas.json`** (valores públicos, versionados): es lo usado hoy en `development`/`preview`.

> Un archivo `.env.production` local **no llega al build**: `.env.*` está en `.gitignore` (solo se versionan los `.example`), y EAS sube al build los archivos no ignorados por Git. Para builds desde CI podés añadirlo con `!` vía un `.easignore`, pero para este proyecto bastan las opciones 1 y 2. Ver [`.env.production.example`](./.env.production.example) como referencia del valor a definir.

> Verifica antes de construir que la URL sea alcanzable desde el dispositivo (en una red LAN, la IP del portátil donde corre el API, no `localhost`). El API debe estar corriendo: `dotnet run` en `src/Api` (escucha en `0.0.0.0:5213`, ver `docs/dev-setup.md`).

## Receta paso a paso (humano)

### 0. Prerrequisitos

- Cuenta de Expo (gratis) en [expo.dev](https://expo.dev/signup).
- Para iOS: cuenta **Apple Developer Program** de pago (99 USD/año) — necesaria también para instalación ad hoc en dispositivo.
- Para Android en Play Store: cuenta **Google Play Developer** (25 USD, una sola vez). Para instalar un **.apk** directo no hace falta.
- Node 20+ (`.nvmrc`).

### 1. Login y vinculación del proyecto

```sh
# desde mobile/
npx eas-cli@latest login        # te pide usuario/token de expo.dev
npx eas-cli@latest whoami       # confirma sesión
```

El primer `npx eas-cli@latest build ...` propone **crear el proyecto EAS** en tu cuenta y escribe `expo.extra.eas.projectId` en `mobile/app.json`. Aceptar (es el paso normal de vinculación).

### 2. Lanzar el build

```sh
# Android sin subir a la nube (el más rápido; ver «Build local de Android»)
npx eas-cli@latest build --local --platform android --profile preview

# O en la nube: una plataforma o ambas
npx eas-cli@latest build --profile development --platform all
npx eas-cli@latest build --profile preview     --platform all
npx eas-cli@latest build --profile production  --platform all
```

EAS te pedirá gestionar credenciales de firma (Android keystore / iOS provisioning) la primera vez: podés dejar que EAS las genere automáticamente y las guarda en sus servidores. El comando espera a que termine y muestra un enlace a la página del build. Para iOS con `distribution: internal` te pedirá registrar el dispositivo por UDID (paso 3).

> El árbol de trabajo debe estar commiteado (por defecto EAS no sube cambios sin commitear).

### 3. Instalación en el dispositivo

- **Android (`development` / `preview`)**: desde la página del build en [expo.dev/builds](https://expo.dev/builds), botón _Install_: se descarga el **.apk** (o se escanea un QR) y se instala directamente. Android pedirá confirmar la instalación de apps fuera de Play Store.
- **iOS (`development` / `preview`)**: distribución **ad hoc**. Necesita cuenta de pago; EAS registra el UDID del dispositivo (un enlace/QR en la página del build guía el registro). Limitado a **100 dispositivos/año**; añadir un dispositivo nuevo requiere reconstruir (o refirmar) el build.
- **Producción**: el **.aab** no se instala directo. Subilo con `npx eas-cli@latest submit --platform all` (o `--profile production`) a **App Store Connect** (TestFlight) y **Play Console**.
- **Bucle rápido de desarrollo**: mientras tanto, Expo Go sigue siendo la vía habitual (`npx expo start` + QR). Los builds de EAS son para instalar el binario real.

### 4. Apuntar el API en producción

1. Definí `EXPO_PUBLIC_API_URL` para el entorno `production` (ver [sección API](#url-del-api-por-entorno-expopublic_api_url), opción 1).
2. Reconstruí (`eas build --profile production --platform all`).
3. Verificá en la app que el plan/perfil cargan datos (la URL quedó inline en el bundle).

## Build local de Android (sin subir a la nube)

`eas build --local` corre el mismo proceso de build que la nube en esta máquina (Linux + Android SDK/NDK, ya instalados ver `docs/dev-setup.md`). Produce el **.apk** en el directorio actual sin publicar nada en [expo.dev/builds](https://expo.dev/builds).

```sh
# desde mobile/ — sí o sí requiere sesión Expo (verificado con eas-cli 24.13.0)
CI=1 npx eas-cli@latest build --local --platform android --profile preview
```

- **Requisito de sesión**: incluso el build local pide autenticación (`eas login` o `EXPO_TOKEN`); sin ella falla con _"An Expo user account is required to proceed"_.
- **iOS no puede construirse en local** en Linux: requiere macOS + Xcode + fastlane. iOS siempre va por la nube.
- **Firma**: por defecto los perfiles usan `credentialsSource: "remote"` (credenciales gestionadas por EAS). Esta máquina tiene además preparada una **firma local** como alternativa: keystore propio en `android-keystore/barrapp.keystore` (git-ignored, regenerable con `keytool -genkeypair -v -keystore android-keystore/barrapp.keystore -alias barrapp -keyalg RSA -keysize 2048 -validity 10000 -storetype JKS -storepass <pass> -keypass <pass> -dname "CN=com.barrapp.app"`) y `credentials.json` (git-ignored) apuntando a él. Para usarla hay que añadir `"credentialsSource": "local"` al perfil de `eas.json` (ver [local credentials](https://docs.expo.dev/app-signing/local-credentials.md)).
- **`CI=1`** evita prompts interactivos de EAS CLI; sin stdin legible los comandos fallan igual que los `config`/`build` de arriba.
- El perfil `preview` ya lleva `EXPO_PUBLIC_API_URL` con la IP LAN: el APK resultante habla con el API del portátil sin tocar nada (verificado con `expo export`).

## Notas y límites

- **Identificadores**: no cambiar `com.barrapp.app` una vez haya builds instalados/distribuidos.
- **Dev client**: hoy no se usa. Si hace falta (módulo nativo fuera de Expo Go), `npx expo install expo-dev-client` + `"developmentClient": true` en el perfil `development`.
- **EAS Update / OTA**: no configurado. Las correcciones de JS requieren un build nuevo.
- **Monorepo/CI**: fuera de alcance de esta guía; ver [build with monorepos](https://docs.expo.dev/build-reference/build-with-monorepos.md) y [build on CI](https://docs.expo.dev/build/building-on-ci.md) cuando toque.

## Referencias (documentación de Expo)

- [Configurar EAS Build con `eas.json`](https://docs.expo.dev/build/eas-json.md)
- [Referencia del esquema de `eas.json`](https://docs.expo.dev/eas/json.md)
- [Ejecutar EAS Build en local (`--local`)](https://docs.expo.dev/build-reference/local-builds.md)
- [Firma local con `credentials.json`](https://docs.expo.dev/app-signing/local-credentials.md)
- [Variables de entorno en EAS](https://docs.expo.dev/eas/environment-variables.md)
- [Distribución interna](https://docs.expo.dev/build/internal-distribution.md)
- [Gestión de versiones de la app](https://docs.expo.dev/build-reference/app-versions.md)
- [Variables de entorno en Expo (`EXPO_PUBLIC_`)](https://docs.expo.dev/guides/environment-variables.md)
