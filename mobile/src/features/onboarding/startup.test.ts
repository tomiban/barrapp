import type { AthleteProfile } from '@/api/athleteProfile';
import type { Objective } from '@/api/objective';

import { resolveStartup } from './startup';

/** Perfil mínimo válido para la costura de arranque. */
const PROFILE: AthleteProfile = {
  weightKilograms: 74,
  heightCentimeters: 178,
  armSpanCentimeters: 182,
  inseamCentimeters: 84,
  trainingDays: 4,
  trainingWeekdays: ['monday', 'tuesday', 'thursday', 'friday'],
  maximums: [{ exerciseCode: 'pull_up', repetitions: 12 }],
};

/** Objetivo mínimo válido. */
const OBJECTIVE: Objective = { skillId: 'front_lever' };

/**
 * `resolveStartup` (spec 0003, ticket #90): regla pura de la costura de
 * arranque. Sin perfil u objetivo la app abre en el onboarding; con los dos —o
 * si el atleta omitió—, en las pestañas.
 */
describe('resolveStartup', () => {
  it('abre en el onboarding sin perfil', async () => {
    expect(resolveStartup({ profile: null, objective: OBJECTIVE, skipped: false })).toBe(
      'onboarding',
    );
  });

  it('abre en el onboarding sin objetivo', async () => {
    expect(resolveStartup({ profile: PROFILE, objective: null, skipped: false })).toBe(
      'onboarding',
    );
  });

  it('abre en el onboarding sin perfil ni objetivo', async () => {
    expect(resolveStartup({ profile: null, objective: null, skipped: false })).toBe('onboarding');
  });

  it('abre en las pestañas con perfil y objetivo', async () => {
    expect(resolveStartup({ profile: PROFILE, objective: OBJECTIVE, skipped: false })).toBe('tabs');
  });

  it('abre en las pestañas si el atleta omitió el onboarding', async () => {
    expect(resolveStartup({ profile: null, objective: null, skipped: true })).toBe('tabs');
  });
});
