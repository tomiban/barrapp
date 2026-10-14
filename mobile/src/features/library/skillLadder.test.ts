import type { Skill } from '@/api/catalog/skills';

import {
  formatGlobalProgress,
  formatStageCriterion,
  globalProgressPercent,
  skillCode,
  skillMaterial,
  stageState,
  STAGE_STATE_LABEL,
  STAGE_STATE_ROLE,
  type StageState,
} from './skillLadder';

/** Skill mínimo con las rutinas de patrón dadas, para probar el material sin montar la vista. */
function skillWithEquipment(equipmentValues: (string | null)[]): Skill {
  return {
    id: 'skill',
    name: 'Skill',
    group: 'push',
    lever: false,
    stages: [],
    patternRoutines: equipmentValues.map((equipment, index) => ({
      id: `r${index}`,
      name: `Modelo R${index}`,
      intensity: 1,
      equipment,
      items: [],
    })),
  };
}

/**
 * Ayudas de la escalera de skills (spec 0003, ticket #86): el estado de cada etapa, el texto del
 * criterio, el progreso global, el código técnico y el material. La lógica vive fuera de la
 * pantalla para poder cubrirla sin montar el árbol de navegación.
 */
describe('skillLadder', () => {
  describe('stageState', () => {
    it('marks the stages below the current one as passed', () => {
      expect(stageState(1, 3, 5)).toBe('passed');
      expect(stageState(2, 3, 5)).toBe('passed');
    });

    it('marks the current stage as current', () => {
      expect(stageState(3, 3, 5)).toBe('current');
    });

    it('marks the stages above the current one, except the last, as locked', () => {
      expect(stageState(4, 3, 5)).toBe('locked');
    });

    it('marks the last stage as the goal', () => {
      expect(stageState(5, 3, 5)).toBe('goal');
    });

    it('makes the current stage win over the goal when it is the last one', () => {
      expect(stageState(5, 5, 5)).toBe('current');
      expect(stageState(1, 5, 5)).toBe('passed');
    });

    it('falls back to the goal for the last stage when there is no progress yet', () => {
      expect(stageState(1, 1, 4)).toBe('current');
      expect(stageState(2, 1, 4)).toBe('locked');
      expect(stageState(4, 1, 4)).toBe('goal');
    });
  });

  describe('state presentation', () => {
    it('labels and roles every stage state so it is never conveyed by colour alone', () => {
      const states: StageState[] = ['passed', 'current', 'locked', 'goal'];

      for (const state of states) {
        expect(STAGE_STATE_LABEL[state]).toBeTruthy();
        expect(STAGE_STATE_ROLE[state]).toBeTruthy();
      }
    });
  });

  describe('formatStageCriterion', () => {
    it('formats a hold criterion in seconds', () => {
      expect(formatStageCriterion({ metric: 'seconds', target: 20, sets: 3 })).toBe('3 × 20 s');
    });

    it('formats a repetition criterion in reps', () => {
      expect(formatStageCriterion({ metric: 'reps', target: 5, sets: 3 })).toBe('3 × 5 reps');
    });
  });

  describe('global progress', () => {
    it('is the current stage over the total stages', () => {
      expect(globalProgressPercent(2, 5)).toBe(40);
      expect(globalProgressPercent(3, 4)).toBe(75);
    });

    it('formats the percentage with one decimal', () => {
      expect(formatGlobalProgress(2, 5)).toBe('40.0%');
      expect(formatGlobalProgress(5, 5)).toBe('100.0%');
    });

    it('is zero when there are no stages', () => {
      expect(formatGlobalProgress(2, 0)).toBe('0.0%');
    });
  });

  describe('skillCode', () => {
    it('pads the index to two digits and names the metric mode', () => {
      expect(skillCode(1, 'seconds')).toBe('SKL.01 - ISOMÉTRICO');
      expect(skillCode(2, 'reps')).toBe('SKL.02 - DINÁMICO');
      expect(skillCode(10, 'seconds')).toBe('SKL.10 - ISOMÉTRICO');
    });
  });

  describe('skillMaterial', () => {
    it('joins the distinct equipment of the pattern routines in order', () => {
      const skill = skillWithEquipment([
        'Barra alta y baja',
        'Solo barra baja',
        'Barra alta y baja',
      ]);

      expect(skillMaterial(skill)).toBe('Barra alta y baja / Solo barra baja');
    });

    it('ignores empty equipment and trims the values', () => {
      const skill = skillWithEquipment([null, '  ', '  Anillas  ']);

      expect(skillMaterial(skill)).toBe('Anillas');
    });

    it('is empty when no routine declares material', () => {
      expect(skillMaterial(skillWithEquipment([null, null]))).toBe('');
    });
  });
});
