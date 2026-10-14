import {
  buildProfilePayload,
  ONBOARDING_DEFAULTS,
  stepKicker,
  type OnboardingDraft,
} from './draft';
import { ONBOARDING_STEPS } from './draft';

const DRAFT: OnboardingDraft = {
  ...ONBOARDING_DEFAULTS,
  weightKilograms: 74,
  heightCentimeters: 178,
  armSpanCentimeters: 182,
  inseamCentimeters: 84,
  maximums: { push_up: 25, pull_up: 12, squat: 20 },
  skillId: 'front_lever',
  weekdays: ['monday', 'tuesday', 'thursday', 'friday'],
};

/**
 * Borrador del onboarding (spec 0003, ticket #90): los valores de partida de
 * los steppers, la etiqueta de cabecera de cada paso y la traducción del
 * borrador al cuerpo de `PUT /profile`. La lógica vive fuera de la pantalla
 * para poder cubrirla sin montar el árbol de navegación.
 */
describe('onboarding draft', () => {
  it('starts within the valid ranges', async () => {
    expect(ONBOARDING_DEFAULTS.weightKilograms).toBeGreaterThanOrEqual(30);
    expect(ONBOARDING_DEFAULTS.weightKilograms).toBeLessThanOrEqual(200);
    expect(ONBOARDING_DEFAULTS.heightCentimeters).toBeGreaterThanOrEqual(120);
    expect(ONBOARDING_DEFAULTS.heightCentimeters).toBeLessThanOrEqual(220);
    expect(ONBOARDING_DEFAULTS.armSpanCentimeters).toBeGreaterThanOrEqual(100);
    expect(ONBOARDING_DEFAULTS.armSpanCentimeters).toBeLessThanOrEqual(250);
    expect(ONBOARDING_DEFAULTS.inseamCentimeters).toBeGreaterThanOrEqual(50);
    expect(ONBOARDING_DEFAULTS.inseamCentimeters).toBeLessThanOrEqual(130);
  });

  it('starts with maximums at zero and three training days', async () => {
    expect(ONBOARDING_DEFAULTS.maximums).toEqual({ push_up: 0, pull_up: 0, squat: 0 });
    expect(ONBOARDING_DEFAULTS.weekdays).toHaveLength(3);
  });

  it('labels each step header', async () => {
    expect(stepKicker(ONBOARDING_STEPS[0])).toBe('PASO 01 / 03 · PERFIL');
    expect(stepKicker(ONBOARDING_STEPS[1])).toBe('PASO 02 / 03 · OBJETIVO');
    expect(stepKicker(ONBOARDING_STEPS[2])).toBe('PASO 03 / 03 · PLAN');
  });

  it('builds the profile with the measurements, chosen days and maximums', async () => {
    const profile = buildProfilePayload(DRAFT);

    expect(profile).toEqual({
      weightKilograms: 74,
      heightCentimeters: 178,
      armSpanCentimeters: 182,
      inseamCentimeters: 84,
      trainingDays: 4,
      trainingWeekdays: ['monday', 'tuesday', 'thursday', 'friday'],
      maximums: [
        { exerciseCode: 'push_up', repetitions: 25 },
        { exerciseCode: 'pull_up', repetitions: 12 },
        { exerciseCode: 'squat', repetitions: 20 },
      ],
    });
  });

  it('fills a missing maximum in the draft with 0', async () => {
    const profile = buildProfilePayload({ ...DRAFT, maximums: {} });

    expect(profile.maximums).toEqual([
      { exerciseCode: 'push_up', repetitions: 0 },
      { exerciseCode: 'pull_up', repetitions: 0 },
      { exerciseCode: 'squat', repetitions: 0 },
    ]);
  });
});
