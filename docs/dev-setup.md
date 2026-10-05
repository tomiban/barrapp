# Setup del entorno de desarrollo

Guía para montar el toolchain y verificar la app en un emulador. Los valores
concretos (rutas del SDK, versión exacta del JDK) dependen de tu máquina; aquí
van los requisitos y las variables que el proyecto espera.

## Requisitos

- **.NET SDK 10** — el backend apunta a `net10.0` (`Directory.Build.props`).
- **JDK 21 o superior** — Android Studio incluye uno (JetBrains Runtime); basta
  con apuntar `JAVA_HOME` a él.
- **Android SDK** con:
  - **NDK `27.1.12297006`** y **CMake `3.22.1`** (necesarios si compilas código
    nativo).
  - Un *platform* reciente, `platform-tools` y `emulator`.
- **Node 20 o superior** — `engines.node` fija el mínimo soportado. Para
  desarrollo usa la versión de `mobile/.nvmrc` (24) con `nvm use`.
- **Expo Go** en el móvil, o un emulador Android con un AVD creado.

## Variables de entorno

Añade estas variables a tu `~/.zshenv` (zsh) y reabre la shell; ajusta las
rutas a tu instalación:

```sh
export DOTNET_ROOT="${DOTNET_ROOT:-$HOME/.dotnet}"
export JAVA_HOME="${JAVA_HOME:-/opt/android-studio/jbr}"
export ANDROID_HOME="${ANDROID_HOME:-$HOME/Android/Sdk}"
export ANDROID_SDK_ROOT="${ANDROID_SDK_ROOT:-$ANDROID_HOME}"

# CLI de .NET, JDK y herramientas del SDK al PATH:
export PATH="$DOTNET_ROOT:$DOTNET_ROOT/tools:$JAVA_HOME/bin:$ANDROID_HOME/cmdline-tools/latest/bin:$ANDROID_HOME/platform-tools:$ANDROID_HOME/emulator:$PATH"
```

> **zsh vs bash:** un `.zshenv` solo lo lee zsh. Si usas bash, exporta lo mismo
> en `~/.bashrc` (o `~/.profile`); si no, las tareas que invocan Gradle fallarán
> por no encontrar `JAVA_HOME`.

Comprueba que todo está en su sitio:

```sh
dotnet --version          # 10.x
java -version             # 21+
echo "$ANDROID_HOME"      # ruta del SDK
ls "$ANDROID_HOME/ndk"    # 27.1.12297006
node -v                   # 20+ (idealmente la de .nvmrc)
```

## Backend

```sh
dotnet restore barrapp.slnx
dotnet run --project src/Barrapp.Api
```

El API escucha en `http://0.0.0.0:5213`.

## App móvil

```sh
cd mobile
npm ci
npm start
```

Escanea el QR con Expo Go. Si el puerto por defecto de Metro (**8081**) está
ocupado, arranca en otro libre:

```sh
npx expo start --port 8085
```

Si el dispositivo no alcanza el API por la red local, reenvía el puerto por USB:

```sh
adb reverse tcp:5213 tcp:5213
```

### Verificar en emulador

```sh
emulator -list-avds            # lista los AVD disponibles
emulator -avd <NombreDelAvd>   # arranca uno
adb devices                    # debe aparecer "device"

# Inspeccionar la jerarquía de la UI:
adb shell uiautomator dump /sdcard/window.xml
adb pull /sdcard/window.xml

# Hacer una captura de pantalla:
adb exec-out screencap -p > screen.png
```

## Carpetas nativas

`mobile/ios/` y `mobile/android/` **no se versionan**: se generan con Expo
Continuous Native Generation. No las edites a mano; la configuración nativa vive
en `mobile/app.json` y en los *config plugins*.

## Checks (lo mismo que corre la CI)

```sh
dotnet format barrapp.slnx --verify-no-changes
dotnet test barrapp.slnx

cd mobile
npm run typecheck
npm run lint
npm run format:check
npm test
```
