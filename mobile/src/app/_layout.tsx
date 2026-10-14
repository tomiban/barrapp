import '../../global.css';

import { useEffect } from 'react';
import { Chivo_400Regular } from '@expo-google-fonts/chivo';
import {
  JetBrainsMono_500Medium,
  JetBrainsMono_600SemiBold,
  JetBrainsMono_700Bold,
} from '@expo-google-fonts/jetbrains-mono';
import { SpaceGrotesk_600SemiBold, SpaceGrotesk_700Bold } from '@expo-google-fonts/space-grotesk';
import { useFonts } from 'expo-font';
import { Stack } from 'expo-router';
import * as SplashScreen from 'expo-splash-screen';
import { StatusBar } from 'expo-status-bar';

import { StartupProvider, useStartup } from '@/features/onboarding';

/**
 * Las fuentes se cargan en runtime con `useFonts` (no con el config plugin, que
 * no funciona en Expo Go). En SDK 57 cada peso es una familia propia, así que
 * estas claves deben coincidir EXACTAMENTE con los valores `--font-*` de
 * `global.css`. Ver `02-fonts-expo-font.md`.
 */
SplashScreen.preventAutoHideAsync();

export default function RootLayout() {
  const [loaded, error] = useFonts({
    SpaceGrotesk_700Bold,
    SpaceGrotesk_600SemiBold,
    Chivo_400Regular,
    JetBrainsMono_500Medium,
    JetBrainsMono_600SemiBold,
    JetBrainsMono_700Bold,
  });

  return (
    <StartupProvider>
      <StatusBar style="light" />
      <RootNavigator fontsReady={loaded || Boolean(error)} />
    </StartupProvider>
  );
}

/**
 * Navegador raíz (spec 0003, ticket #90). Mantiene el splash nativo hasta que
 * las fuentes están cargadas *y* el arranque sabe dónde abrir, y deja accesible
 * la pestaña o el onboarding con `Stack.Protected`: la guarda es una excepción
 * sobre las rutas, así que el resto de la app (`/profile`, `/showcase`,
 * `/plan-historial`) sigue disponible en ambos casos.
 */
function RootNavigator({ fontsReady }: { fontsReady: boolean }) {
  const { status, decision } = useStartup();
  const ready = fontsReady && status === 'ready';

  useEffect(() => {
    if (ready) {
      SplashScreen.hideAsync();
    }
  }, [ready]);

  if (!ready) {
    return null;
  }

  return (
    <Stack screenOptions={{ headerShown: false }}>
      <Stack.Protected guard={decision === 'tabs'}>
        <Stack.Screen name="(tabs)" />
      </Stack.Protected>

      <Stack.Protected guard={decision === 'onboarding'}>
        <Stack.Screen name="onboarding" />
      </Stack.Protected>
    </Stack>
  );
}
