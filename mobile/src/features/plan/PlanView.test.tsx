import { render, screen } from '@testing-library/react-native';

import type { Plan } from '@/api/plan';
import { PlanView } from './PlanView';

/** Microciclo mínimo para pintar la vista sin caer en el estado vacío. */
const microcycles: Plan['microcycles'] = [
  {
    number: 1,
    sessions: [
      {
        day: 1,
        items: [
          {
            exerciseId: 'handstand-back-to-wall',
            exerciseName: 'Pino de espaldas a la pared',
            role: 'skill',
            pattern: null,
            sets: 3,
            repsMin: null,
            repsMax: null,
            holdSecondsMin: 30,
            holdSecondsMax: 30,
          },
        ],
      },
    ],
  },
];

function makePlan(stage: Plan['skillStage']): Plan {
  return { skillId: 'handstand', trainingDays: 3, skillStage: stage, microcycles };
}

/**
 * PlanView (ticket #18): el plan muestra la etapa actual del skill objetivo y su criterio para
 * avanzar de etapa, junto al resto del mesociclo.
 */
describe('PlanView', () => {
  it('muestra la etapa actual del skill y su criterio de avance en segundos', async () => {
    await render(
      <PlanView
        plan={makePlan({
          order: 2,
          name: 'Pino de espaldas a la pared',
          exerciseId: 'handstand-back-to-wall',
          criterion: { metric: 'seconds', target: 30, sets: 3 },
          notes: 'Sin apoyo de pies; los talones rozan la pared.',
        })}
      />,
    );

    expect(screen.getByText('Etapa 2 · Pino de espaldas a la pared')).toBeOnTheScreen();
    expect(screen.getByText('Supera 30 s × 3 series')).toBeOnTheScreen();
    expect(screen.getByText('Etapa actual')).toBeOnTheScreen();
    expect(screen.getByTestId('plan-skill-stage')).toBeOnTheScreen();
  });

  it('muestra el criterio en repeticiones cuando la métrica es reps', async () => {
    await render(
      <PlanView
        plan={makePlan({
          order: 1,
          name: 'Sentadilla pistol a cajón',
          exerciseId: 'pistol-box',
          criterion: { metric: 'reps', target: 5, sets: 3 },
          notes: '',
        })}
      />,
    );

    expect(screen.getByText('Etapa 1 · Sentadilla pistol a cajón')).toBeOnTheScreen();
    expect(screen.getByText('Supera 5 reps × 3 series')).toBeOnTheScreen();
  });
});
