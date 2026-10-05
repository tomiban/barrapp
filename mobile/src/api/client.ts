import Constants from 'expo-constants';

/** Puerto por defecto del API .NET en desarrollo (ver `launchSettings.json`). */
const DEFAULT_API_PORT = 5213;

/**
 * URL base del API. Prioriza `EXPO_PUBLIC_API_URL`; si no está definida, usa el host
 * del dev server de Expo (el mismo del QR) y el puerto del API, de modo que un
 * dispositivo físico en la red local alcance al portátil sin configuración manual.
 */
export function getApiBaseUrl(): string {
  const configuredUrl = process.env.EXPO_PUBLIC_API_URL;
  if (configuredUrl) {
    return configuredUrl.replace(/\/+$/, '');
  }

  const host = Constants.expoConfig?.hostUri?.split(':')[0];
  if (host) {
    return `http://${host}:${DEFAULT_API_PORT}`;
  }

  return `http://localhost:${DEFAULT_API_PORT}`;
}
