import type { PlanSessionItem } from '@/api/plan';

import {
  holdSecondsOf,
  lastreButtonLabel,
  lastreRowLabel,
  modeOf,
  objectiveOf,
  phaseOf,
  presetsOf,
  rangeOf,
  restSecondsOf,
  setStateOf,
  setSummaryOf,
  targetValueOf,
  twoDigits,
  unitLabelOf,
} from './sessionSheet';

/** Hold isométrico: rango en segundos y descanso propio. */
const hold: PlanSessionItem = {
  exerciseId: 'handstand-wall-support',
  exerciseName: 'Pino en pared',
  role: 'skill',
  pattern: null,
  sets: 4,
  repsMin: null,
  repsMax: null,
  holdSecondsMin: 20,
  holdSecondsMax: 30,
  restSeconds: 90,
};

/** Ejercicio de fuerza medido en repeticiones. */
const reps: PlanSessionItem = {
  exerciseId: 'push_up',
  exerciseName: 'Flexiones',
  role: 'strength',
  pattern: 'push',
  sets: 3,
  repsMin: 8,
  repsMax: 12,
  holdSecondsMin: null,
  holdSecondsMax: null,
  restSeconds: 60,
};

describe('sessionSheet', () => {
  it('deriva la unidad del ejercicio: segundos en los holds, reps en lo demás', () => {
    expect(unitLabelOf(hold)).toBe('SEG');
    expect(unitLabelOf(reps)).toBe('REPS');
  });

  it('etiqueta el modo de ejecución y la fase del bloque', () => {
    expect(modeOf(hold)).toBe('ISOMÉTRICO');
    expect(modeOf(reps)).toBe('DINÁMICO');
    expect(phaseOf(hold)).toBe('FASE SKILL · ISOMETRÍA PURA');
    expect(phaseOf(reps)).toBe('FASE FUERZA · EMPUJE');
  });

  it('compone la estructura y el objetivo con la unidad del ejercicio', () => {
    expect(rangeOf(hold)).toBe('4 × 20–30s');
    expect(rangeOf(reps)).toBe('3 × 8–12 reps');
    expect(objectiveOf(hold)).toBe('OBJETIVO: 20–30s SOSTÉN');
    expect(objectiveOf(reps)).toBe('OBJETIVO: 8–12 REPS');
  });

  it('usa el techo del rango como valor de arranque y como cronómetro del hold', () => {
    expect(targetValueOf(hold)).toBe(30);
    expect(holdSecondsOf(hold)).toBe(30);
    expect(holdSecondsOf(reps)).toBe(0);
  });

  it('ofrece presets alrededor del rango, sin duplicados ni negativos', () => {
    expect(presetsOf(hold)).toEqual([18, 20, 30, 32]);
    expect(presetsOf(reps)).toEqual([6, 8, 12, 14]);
    expect(presetsOf({ ...hold, holdSecondsMin: 1, holdSecondsMax: 1 })).toEqual([1, 3]);
  });

  it('lee el descanso del SessionItem', () => {
    expect(restSecondsOf(hold)).toBe(90);
    expect(restSecondsOf(reps)).toBe(60);
  });

  it('resuelve el estado de una serie: superada, en curso o pendiente', () => {
    expect(setStateOf(true, false)).toBe('passed');
    expect(setStateOf(false, true)).toBe('current');
    expect(setStateOf(false, false)).toBe('pending');
  });

  it('resume una serie con su valor, su RIR y su lastre', () => {
    expect(setSummaryOf({ value: 12, effort: 1, loadKg: 0 }, hold)).toBe(
      '12s SOSTÉN · RIR 1 · +0kg',
    );
    expect(setSummaryOf({ value: 10, effort: null, loadKg: 2.5 }, reps)).toBe('10 reps · +2.5kg');
  });

  it('etiqueta el lastre del selector y de la fila', () => {
    expect(lastreButtonLabel(0)).toBe('+0.0 KG (CORPORAL)');
    expect(lastreButtonLabel(2.5)).toBe('+2.5 KG');
    expect(lastreRowLabel(0)).toBe('+0kg');
    expect(lastreRowLabel(10)).toBe('+10kg');
  });

  it('numera a dos dígitos para las etiquetas técnicas', () => {
    expect(twoDigits(2)).toBe('02');
    expect(twoDigits(12)).toBe('12');
  });
});
