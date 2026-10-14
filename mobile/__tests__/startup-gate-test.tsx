import { Text } from 'react-native';
import { Slot } from 'expo-router';
import { renderRouter, screen } from 'expo-router/testing-library';

import RootLayout from '../src/app/_layout';

// El layout raíz importa la hoja de tokens; Jest no la entiende y no aporta nada a esta prueba.
jest.mock('../global.css', () => ({}));
jest.mock('expo-font', () => ({
  useFonts: () => [true, null],
}));
jest.mock('expo-splash-screen', () => ({
  preventAutoHideAsync: jest.fn(),
  hideAsync: jest.fn(),
}));

const Home = () => <Text testID="home-route">home</Text>;
const TabsLayout = () => <Slot />;
const Onboarding = () => <Text testID="onboarding-route">onboarding</Text>;
const Profile = () => <Text testID="profile-route">profile</Text>;
const Showcase = () => <Text testID="showcase-route">showcase</Text>;
const PlanDetail = () => <Text testID="plan-detail-route">plan detail</Text>;

function jsonResponse(status: number, body: unknown): Response {
  return {
    ok: status >= 200 && status < 300,
    status,
    statusText: status === 404 ? 'Not Found' : 'OK',
    json: async () => body,
  } as unknown as Response;
}

function renderApp(fixture: 'profile' | 'empty' | 'error') {
  global.fetch = jest.fn((input: RequestInfo | URL) => {
    if (fixture === 'error') {
      return Promise.reject(new Error('sin conexión'));
    }

    const url = String(input);
    const hasProfile = fixture === 'profile';
    if (url.endsWith('/profile')) {
      return Promise.resolve(hasProfile ? jsonResponse(200, {}) : jsonResponse(404, {}));
    }
    if (url.endsWith('/profile/objective')) {
      return Promise.resolve(
        hasProfile ? jsonResponse(200, { skillId: 'x' }) : jsonResponse(404, {}),
      );
    }
    return Promise.resolve(jsonResponse(404, {}));
  }) as unknown as typeof fetch;

  return renderRouter(
    {
      _layout: RootLayout,
      '(tabs)/_layout': TabsLayout,
      '(tabs)/index': Home,
      onboarding: Onboarding,
      profile: Profile,
      showcase: Showcase,
      'plan-historial/[id]': PlanDetail,
    },
    { initialUrl: '/' },
  );
}

/**
 * Guarda de arranque (spec 0003, ticket #90): el layout raíz monta el onboarding
 * sin perfil u objetivo y las pestañas cuando ya los hay, sin que el resto de
 * rutas (Perfil, Catálogo, historial) capturen el arranque.
 */
describe('startup gate', () => {
  it('opens onboarding without a profile', async () => {
    await renderApp('empty');

    expect(await screen.findByTestId('onboarding-route')).toBeOnTheScreen();
    expect(screen.queryByTestId('home-route')).toBeNull();
  });

  it('opens the tabs with profile and objective', async () => {
    await renderApp('profile');

    expect(await screen.findByTestId('home-route')).toBeOnTheScreen();
    expect(screen.queryByTestId('onboarding-route')).toBeNull();
  });

  it('falls back to the tabs when the startup requests fail', async () => {
    await renderApp('error');

    expect(await screen.findByTestId('home-route')).toBeOnTheScreen();
    expect(screen.queryByTestId('onboarding-route')).toBeNull();
  });
});
