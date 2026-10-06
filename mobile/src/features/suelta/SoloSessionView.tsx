import { useCallback, useState } from 'react';

import { EXERCISE_GROUP_LABELS } from '@/api/catalog/exercises';
import {
  generateSoloSession,
  type SoloSession,
  type SoloSessionEnergy,
  type SoloSessionFocus,
  type SoloSessionItem,
  type SoloSessionPattern,
  type SoloSessionTime,
} from '@/api/soloSession';
import { messageOf } from '@/api/messageOf';
import { Button } from '@/design-system/Button';
import { SegmentedControl, type SegmentedOption } from '@/design-system/Chip';
import { Banner, Loading } from '@/design-system/Feedback';
import { Box, Stack } from '@/design-system/layout';
import { ListRow, SectionHeader } from '@/design-system/ListRow';
import { StatusBadge } from '@/design-system/StatusBadge';
import { Text } from '@/design-system/Text';

/** Nombre para la UI de cada papel de la sesión suelta. */
const ROLE_LABELS: Record<SoloSessionItem['role'], string> = {
  skill: 'Skill',
  strength: 'Fuerza',
  core: 'Core',
};

/** Clave del tiempo disponible en el segmentado; al API viaja el número de minutos. */
type TimeKey = '15' | '30' | '45' | '60';

const TIME_OPTIONS: readonly SegmentedOption<TimeKey>[] = [
  { value: '15', label: '15 min' },
  { value: '30', label: '30 min' },
  { value: '45', label: '45 min' },
  { value: '60', label: '60 min' },
];

const ENERGY_OPTIONS: readonly SegmentedOption<SoloSessionEnergy>[] = [
  { value: 'baja', label: 'Baja' },
  { value: 'media', label: 'Media' },
  { value: 'alta', label: 'Alta' },
];

const FOCUS_OPTIONS: readonly SegmentedOption<SoloSessionFocus>[] = [
  { value: 'patron', label: 'Patrón' },
  { value: 'skill', label: 'Skill' },
  { value: 'sorprendeme', label: 'Sorpréndeme' },
];

const PATTERN_OPTIONS: readonly SegmentedOption<SoloSessionPattern>[] = [
  { value: 'push', label: EXERCISE_GROUP_LABELS.push },
  { value: 'pull', label: EXERCISE_GROUP_LABELS.pull },
  { value: 'leg', label: EXERCISE_GROUP_LABELS.leg },
];

const ENERGY_LABELS: Record<SoloSessionEnergy, string> = {
  baja: 'Baja',
  media: 'Media',
  alta: 'Alta',
};

/** Texto del rango de una fila, p. ej. `3 × 20–30 s` o `3 × 8–12 reps`. */
function formatRange(item: SoloSessionItem): string {
  if (item.holdSecondsMin !== null && item.holdSecondsMax !== null) {
    return `${item.sets} × ${item.holdSecondsMin}–${item.holdSecondsMax} s`;
  }

  if (item.repsMin !== null && item.repsMax !== null) {
    return `${item.sets} × ${item.repsMin}–${item.repsMax} reps`;
  }

  return `${item.sets} series`;
}

/** Subtítulo de una fila: papel, patrón (en fuerza) y prescripción. */
function describeItem(item: SoloSessionItem): string {
  const parts = [ROLE_LABELS[item.role]];

  if (item.pattern) {
    parts.push(EXERCISE_GROUP_LABELS[item.pattern]);
  }

  parts.push(formatRange(item));

  return parts.join(' · ');
}

/** Línea resumen de la sesión generada, según el foco resuelto por el motor. */
function summarize(session: SoloSession): string {
  const focus =
    session.skillName !== null
      ? `Skill · ${session.skillName}`
      : EXERCISE_GROUP_LABELS[session.pattern ?? 'push'];
  return `${focus} · ${session.timeMinutes} min · energía ${ENERGY_LABELS[session.energy]}`;
}

type GenerateState =
  | { status: 'idle' }
  | { status: 'loading' }
  | { status: 'success'; session: SoloSession }
  | { status: 'error'; message: string };

/**
 * Generador de sesión suelta: elige tiempo, energía y foco (patrón, skill o «sorpréndeme») y una
 * vez resuelto por el motor server-side pinta la sesión con el design system. «Sorpréndeme» no
 * muestra el patrón en el formulario: la respuesta revela qué ha tocado.
 */
export function SoloSessionView() {
  const [time, setTime] = useState<TimeKey>('30');
  const [energy, setEnergy] = useState<SoloSessionEnergy>('media');
  const [focus, setFocus] = useState<SoloSessionFocus>('patron');
  const [pattern, setPattern] = useState<SoloSessionPattern>('push');
  const [state, setState] = useState<GenerateState>({ status: 'idle' });

  const generate = useCallback(() => {
    setState({ status: 'loading' });

    generateSoloSession({
      timeMinutes: Number(time) as SoloSessionTime,
      energy,
      focus,
      ...(focus === 'patron' ? { pattern } : {}),
    })
      .then((session) => setState({ status: 'success', session }))
      .catch((error: unknown) => setState({ status: 'error', message: messageOf(error) }));
  }, [time, energy, focus, pattern]);

  return (
    <Stack gap="lg" testID="suelta-screen">
      <Stack gap="sm">
        <Text variant="labelTechnical" className="text-text-muted">
          BARRAPP · SESIÓN SUELTA
        </Text>
        <Text variant="bodySm" className="text-text-muted">
          Entrena hoy en tu tiempo, con tu energía y tu foco.
        </Text>
      </Stack>

      <Stack gap="md">
        <SegmentedControl
          options={TIME_OPTIONS}
          value={time}
          onChange={setTime}
          label="Tiempo disponible"
          testID="suelta-time"
        />
        <SegmentedControl
          options={ENERGY_OPTIONS}
          value={energy}
          onChange={setEnergy}
          label="Energía hoy"
          testID="suelta-energy"
        />
        <SegmentedControl
          options={FOCUS_OPTIONS}
          value={focus}
          onChange={setFocus}
          label="Foco de la sesión"
          testID="suelta-focus"
        />

        {focus === 'patron' ? (
          <SegmentedControl
            options={PATTERN_OPTIONS}
            value={pattern}
            onChange={setPattern}
            label="Patrón"
            testID="suelta-pattern"
          />
        ) : null}

        <Button onPress={generate} testID="suelta-generate">
          Generar sesión
        </Button>
      </Stack>

      {state.status === 'loading' ? (
        <Loading label="Componiendo tu sesión…" testID="suelta-loading" />
      ) : null}

      {state.status === 'error' ? (
        <Stack gap="sm">
          <StatusBadge role="error" label="No se pudo generar" testID="suelta-status" />
          <Banner role="error" message={state.message} testID="suelta-error" />
        </Stack>
      ) : null}

      {state.status === 'success' ? (
        <Stack gap="md" testID="suelta-result">
          <Box className="gap-xs rounded-md border border-border bg-surface p-md">
            <StatusBadge role="confirmed" label="Sesión lista" testID="suelta-status" />
            <Text variant="headlineMd" testID="suelta-summary">
              {summarize(state.session)}
            </Text>
          </Box>

          <Stack gap="sm">
            <SectionHeader
              label="Tu sesión"
              count={state.session.items.length}
              testID="suelta-items-header"
            />
            {state.session.items.map((item, index) => (
              <ListRow
                key={`${index}-${item.exerciseId}`}
                title={item.exerciseName}
                subtitle={describeItem(item)}
                last={index === state.session.items.length - 1}
                testID={`suelta-item-${item.exerciseId}`}
              />
            ))}
          </Stack>

          {state.session.focus === 'sorprendeme' ? (
            <Text variant="bodySm" className="text-text-muted">
              Sorpréndeme ha elegido por ti: toca el foco que ha tocado.
            </Text>
          ) : null}
        </Stack>
      ) : null}
    </Stack>
  );
}
