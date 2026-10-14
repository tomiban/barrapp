import { fireEvent, render, screen, waitFor } from '@testing-library/react-native';
import { Text } from 'react-native';

import type { Skill } from '@/api/catalog/skills';

import { OnboardingScreen } from './OnboardingScreen';
import { StartupProvider, useStartup } from './StartupProvider';

/** Respuesta mínima con la forma que consume el módulo de API. */
function jsonResponse(status: number, body: unknown): Response {
  return {
    ok: status >= 200 && status < 300,
    status,
    statusText: status === 404 ? 'Not Found' : 'OK',
    json: async () => body,
  } as unknown as Response;
}

/** Catálogo mínimo: dos skills para poder comprobar la preselección y el cambio. */
const SKILLS: Skill[] = [
  {
    id: 'front_lever',
    name: 'Front Lever',
    group: 'pull',
    lever: true,
    stages: [],
    patternRoutines: [],
  },
  {
    id: 'handstand',
    name: 'Handstand',
    group: 'push',
    lever: false,
    stages: [],
    patternRoutines: [],
  },
];

/** Llamada capturada por el doble de `fetch`, para inspeccionarla después. */
type Call = {
  url: string;
  method: string;
  body: unknown;
};

/**
 * Monta el onboarding contra un `fetch` enrutado por URL. El perfil y el
 * objetivo arrancan en `404` (estado de primer arranque), el catálogo responde
 * con `SKILLS` y los `PUT`/`POST` quedan registrados.
 */
function setupApi(options: { skills?: Skill[]; failGenerate?: boolean } = {}) {
  const calls: Call[] = [];
  let savedProfile: unknown = null;
  let savedObjective: unknown = null;

  const fetchMock = jest.fn((input: RequestInfo | URL, init?: RequestInit) => {
    const url = String(input);
    const method = init?.method ?? 'GET';
    const body = init?.body ? JSON.parse(String(init.body)) : undefined;
    calls.push({ url, method, body });

    if (url.endsWith('/catalog/skills')) {
      return Promise.resolve(jsonResponse(200, options.skills ?? SKILLS));
    }
    if (url.endsWith('/profile/objective')) {
      if (method === 'PUT') {
        savedObjective = body;
        return Promise.resolve(jsonResponse(200, body));
      }
      return Promise.resolve(
        savedObjective ? jsonResponse(200, savedObjective) : jsonResponse(404, {}),
      );
    }
    if (url.endsWith('/plan')) {
      return options.failGenerate
        ? Promise.resolve(jsonResponse(400, { detail: 'No se pudo generar el mesociclo.' }))
        : Promise.resolve(jsonResponse(200, {}));
    }
    if (url.endsWith('/profile')) {
      if (method === 'PUT') {
        savedProfile = body;
        return Promise.resolve(jsonResponse(200, body));
      }
      return Promise.resolve(
        savedProfile ? jsonResponse(200, savedProfile) : jsonResponse(404, {}),
      );
    }

    return Promise.resolve(jsonResponse(404, {}));
  });

  global.fetch = fetchMock as unknown as typeof fetch;
  return {
    calls,
    profile: () => savedProfile,
    objective: () => savedObjective,
  };
}

/** Sonda de la decisión de arranque: `onboarding` hasta que el flujo la cierra. */
function DecisionProbe() {
  const { decision } = useStartup();
  return <Text testID="startup-decision">{decision}</Text>;
}

function renderOnboarding() {
  return render(
    <StartupProvider>
      <OnboardingScreen />
      <DecisionProbe />
    </StartupProvider>,
  );
}

/** Llega hasta el paso 2 con el skill preseleccionado listo. */
async function goToObjectiveStep() {
  await screen.findByTestId('onboarding-step-profile');
  await fireEvent.press(screen.getByTestId('onboarding-continue'));
  await screen.findByTestId('onboarding-skill-front_lever-selected');
}

/** Llega hasta el paso 3 desde el paso 2. */
async function goToConfirmStep() {
  await goToObjectiveStep();
  await fireEvent.press(screen.getByTestId('onboarding-continue'));
  await screen.findByTestId('onboarding-step-confirm');
}

/**
 * Onboarding (spec 0003, ticket #90): flujo de primer arranque en tres pasos.
 *
 * Es una prueba de integración: monta la pantalla dentro de su `StartupProvider`
 * real y con `fetch` enrutado por URL, así cubre el contrato con el API
 * (`GET /profile`, `PUT /profile`, `PUT /profile/objective`, `POST /plan`) y el
 * cierre del arranque, no sólo el pintado.
 */
describe('OnboardingScreen', () => {
  const originalFetch = global.fetch;

  afterEach(() => {
    global.fetch = originalFetch;
    jest.restoreAllMocks();
  });

  it('walks the three steps and preselects the first catalog skill', async () => {
    setupApi();

    await renderOnboarding();

    expect(await screen.findByTestId('onboarding-step-profile')).toBeOnTheScreen();
    expect(screen.getByText('CONFIGURA TU BASE')).toBeOnTheScreen();

    await fireEvent.press(screen.getByTestId('onboarding-continue'));
    expect(await screen.findByTestId('onboarding-step-objective')).toBeOnTheScreen();
    expect(await screen.findByTestId('onboarding-skill-front_lever-selected')).toBeOnTheScreen();
    expect(screen.getByText('Front Lever')).toBeOnTheScreen();

    await fireEvent.press(screen.getByTestId('onboarding-continue'));
    expect(await screen.findByTestId('onboarding-step-confirm')).toBeOnTheScreen();
    expect(await screen.findByTestId('onboarding-param-SKILL OBJETIVO')).toHaveTextContent(
      'Front Lever',
    );
  });

  it('adjusts measurements and maximums with the steppers', async () => {
    setupApi();

    await renderOnboarding();
    await screen.findByTestId('onboarding-step-profile');

    // Los defaults neutros arrancan en 70 kg y los máximos a 0.
    expect(screen.getByTestId('onboarding-weightKilograms-value')).toHaveTextContent('70');
    await fireEvent.press(screen.getByTestId('onboarding-weightKilograms-increase'));
    expect(screen.getByTestId('onboarding-weightKilograms-value')).toHaveTextContent('71');

    expect(screen.getByTestId('onboarding-maximum-push_up-value')).toHaveTextContent('0');
    await fireEvent.press(screen.getByTestId('onboarding-maximum-push_up-increase'));
    expect(screen.getByTestId('onboarding-maximum-push_up-value')).toHaveTextContent('1');
  });

  it('goes back to the previous step with VOLVER', async () => {
    setupApi();

    await renderOnboarding();
    await goToObjectiveStep();

    await fireEvent.press(screen.getByTestId('onboarding-back'));
    expect(await screen.findByTestId('onboarding-step-profile')).toBeOnTheScreen();
  });

  it('blocks step 2 when there is no target skill', async () => {
    setupApi({ skills: [] });

    await renderOnboarding();
    await screen.findByTestId('onboarding-step-profile');
    await fireEvent.press(screen.getByTestId('onboarding-continue'));
    await screen.findByTestId('onboarding-step-objective');

    await fireEvent.press(screen.getByTestId('onboarding-continue'));

    expect(await screen.findByTestId('onboarding-objective-error')).toHaveTextContent(
      'Elige un skill objetivo.',
    );
    expect(screen.queryByTestId('onboarding-step-confirm')).toBeNull();
  });

  it('blocks step 2 with fewer than three training days', async () => {
    setupApi();

    await renderOnboarding();
    await goToObjectiveStep();

    // La selección inicial es lunes, miércoles y viernes; al quitar lunes quedan 2.
    await fireEvent.press(screen.getByTestId('onboarding-weekday-monday'));
    await fireEvent.press(screen.getByTestId('onboarding-continue'));

    expect(await screen.findByTestId('onboarding-objective-error')).toHaveTextContent(
      'Elige entre 3 y 5 días de entrenamiento.',
    );
    expect(screen.queryByTestId('onboarding-step-confirm')).toBeNull();
  });

  it('generates the plan saving profile and objective, and completes onboarding', async () => {
    const api = setupApi();

    await renderOnboarding();
    await goToConfirmStep();

    await fireEvent.press(screen.getByTestId('onboarding-generate'));

    await waitFor(() => expect(screen.getByTestId('startup-decision')).toHaveTextContent('tabs'));
    expect(api.profile()).toEqual({
      weightKilograms: 70,
      heightCentimeters: 175,
      armSpanCentimeters: 175,
      inseamCentimeters: 80,
      trainingDays: 3,
      trainingWeekdays: ['monday', 'wednesday', 'friday'],
      maximums: [
        { exerciseCode: 'push_up', repetitions: 0 },
        { exerciseCode: 'pull_up', repetitions: 0 },
        { exerciseCode: 'squat', repetitions: 0 },
      ],
    });
    expect(api.objective()).toEqual({ skillId: 'front_lever' });
    expect(api.calls.some((call) => call.method === 'POST' && call.url.endsWith('/plan'))).toBe(
      true,
    );
  });

  it('shows the API problem detail when generation fails', async () => {
    setupApi({ failGenerate: true });

    await renderOnboarding();
    await goToConfirmStep();

    await fireEvent.press(screen.getByTestId('onboarding-generate'));

    expect(await screen.findByTestId('onboarding-submit-error')).toHaveTextContent(
      'No se pudo generar el mesociclo.',
    );
    // El onboarding no se cierra si el plan no se pudo generar.
    expect(screen.getByTestId('startup-decision')).toHaveTextContent('onboarding');
  });

  it('skips without saving anything and enters the tabs', async () => {
    const api = setupApi();

    await renderOnboarding();
    await screen.findByTestId('onboarding-step-profile');

    await fireEvent.press(screen.getByTestId('onboarding-skip'));

    await waitFor(() => expect(screen.getByTestId('startup-decision')).toHaveTextContent('tabs'));
    expect(api.calls.some((call) => call.method === 'PUT')).toBe(false);
  });
});
