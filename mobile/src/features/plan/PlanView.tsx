import { useMemo, useState } from 'react';
import { Pressable, View } from 'react-native';
import { useCSSVariable } from 'uniwind';

import { todayStartDate } from '@/api/athleteProfile';
import type { Plan, PlanSession } from '@/api/plan';
import type { SessionLog } from '@/api/sessionLogs';
import { Button } from '@/design-system/Button';
import { EmptyState } from '@/design-system/Feedback';
import { Check, ChevronRight, Icon, Lock, SlidersHorizontal } from '@/design-system/Icon';
import { Box, Stack } from '@/design-system/layout';
import { Text } from '@/design-system/Text';
import { cn } from '@/design-system/utils/cn';
import {
  currentWeekNumber,
  doneSessionKeys,
  isSessionToday,
  MESOCYCLE_CODE,
  PERIODIZATION_LABEL,
  SESSION_DURATION_MINUTES,
  sessionDayLabel,
  sessionExercises,
  sessionKey,
  sessionSets,
  sessionTitle,
  TARGET_RIR,
  twoDigits,
  weekState,
  weekStateLabel,
  weeklyVolume,
  type WeekState,
} from '@/features/plan/planSheet';

/** Colores de icono resueltos desde los tokens; Lucide colorea por prop, no por `className`. */
function useIconColors() {
  const tokens = useCSSVariable(['--color-primary', '--color-text-muted', '--color-on-primary']);
  const asColor = (value: unknown) => (typeof value === 'string' ? value : undefined);

  return {
    primary: asColor(tokens[0]),
    muted: asColor(tokens[1]),
    onPrimary: asColor(tokens[2]),
  };
}

type IconColors = ReturnType<typeof useIconColors>;

/** Fondo y borde de cada celda del selector de semana. */
const WEEK_CELL: Record<WeekState, string> = {
  done: 'bg-surface',
  current: 'bg-surface-muted',
  upcoming: 'bg-canvas opacity-50',
};

/** Indicador de cada estado de semana: check (hecha), punto (en curso) o candado (pendiente). */
function WeekIndicator({ state, colors }: { state: WeekState; colors: IconColors }) {
  if (state === 'done') {
    return <Icon icon={Check} size={13} color={colors.muted} />;
  }
  if (state === 'current') {
    return <View className="h-xs w-xs rounded-sm bg-primary" />;
  }

  return <Icon icon={Lock} size={12} color={colors.muted} />;
}

/** Celda de una semana (S1–S4) del selector: número, indicador y estado. */
function WeekCell({
  number,
  state,
  selected,
  onSelect,
  colors,
}: {
  number: number;
  state: WeekState;
  selected: boolean;
  onSelect: () => void;
  colors: IconColors;
}) {
  const label = weekStateLabel(number, state);
  const accent = selected ? 'text-primary' : 'text-text-muted';

  return (
    <Pressable
      accessible
      accessibilityRole="button"
      accessibilityState={{ selected }}
      accessibilityLabel={`Semana ${number}, ${label}`}
      onPress={onSelect}
      testID={`plan-week-${number}`}
      className={cn(
        'flex-1 items-center justify-center gap-xs rounded-sm py-sm',
        selected ? 'bg-surface-muted border-b-2 border-primary' : WEEK_CELL[state],
      )}
    >
      <Stack direction="row" gap="xs" className="items-center">
        <Text variant="labelCode" className={accent}>{`S${number}`}</Text>
        <WeekIndicator state={state} colors={colors} />
      </Stack>
      <Text variant="labelTechnical" className={accent}>
        {label}
      </Text>
    </Pressable>
  );
}

/** Tarjeta de una sesión de la semana: hecha, de hoy (con CTA) o programada. */
function SessionCard({
  session,
  microcycleNumber,
  done,
  isToday,
  onStart,
  colors,
}: {
  session: PlanSession;
  microcycleNumber: number;
  done: boolean;
  isToday: boolean;
  onStart?: () => void;
  colors: IconColors;
}) {
  const testID = `plan-session-${microcycleNumber}-${session.day}`;
  const order = `S${twoDigits(session.day)}`;
  const sets = sessionSets(session);

  if (isToday) {
    return (
      <Box
        className="gap-sm rounded-base border-l-2 border-primary bg-surface p-md"
        testID={testID}
      >
        <Stack direction="row" className="items-center justify-between gap-sm">
          <Text variant="labelTechnical" className="text-primary">
            {`Hoy · ${sessionDayLabel(session)} · ${order}`}
          </Text>
          <Text variant="labelTechnical" className="text-text-muted">
            {`${sets} series · ~${SESSION_DURATION_MINUTES} min`}
          </Text>
        </Stack>
        <Stack gap="xs">
          <Text variant="headlineSm" className="text-text">
            {sessionTitle(session)}
          </Text>
          <Text variant="bodySm" className="text-text-muted">
            {sessionExercises(session)}
          </Text>
        </Stack>
        <Button onPress={onStart} testID={`${testID}-start`}>
          Comenzar sesión
        </Button>
      </Box>
    );
  }

  if (done) {
    return (
      <Pressable
        accessible
        accessibilityRole="button"
        accessibilityLabel={`${sessionTitle(session)}, ${sessionDayLabel(session)}, hecho`}
        onPress={onStart}
        testID={testID}
        className="flex-row items-center justify-between gap-sm rounded-base bg-surface p-md opacity-60"
      >
        <Stack gap="xs" className="min-w-0 flex-1">
          <Stack direction="row" gap="sm" className="items-center">
            <Text variant="labelTechnical" className="text-text-muted">
              {`${sessionDayLabel(session)} · ${order}`}
            </Text>
            <Stack direction="row" gap="xs" className="items-center">
              <Icon icon={Check} size={13} color={colors.primary} />
              <Text variant="labelTechnical" className="text-primary">
                Hecho
              </Text>
            </Stack>
          </Stack>
          <Text variant="headlineSm" className="text-text" numberOfLines={1}>
            {sessionTitle(session)}
          </Text>
          <Text variant="bodySm" className="text-text-muted">
            {`${sets} series finalizadas`}
          </Text>
        </Stack>
        <Icon icon={ChevronRight} size={20} color={colors.muted} />
      </Pressable>
    );
  }

  return (
    <Box
      className="flex-row items-center justify-between gap-sm rounded-base bg-surface p-md"
      testID={testID}
    >
      <Stack gap="xs" className="min-w-0 flex-1">
        <Text variant="labelTechnical" className="text-text-muted">
          {`${sessionDayLabel(session)} · ${order}`}
        </Text>
        <Text variant="headlineSm" className="text-text" numberOfLines={1}>
          {sessionTitle(session)}
        </Text>
        <Text variant="bodySm" className="text-text-muted" numberOfLines={1}>
          {`${sets} series · ${sessionExercises(session)}`}
        </Text>
      </Stack>
      <Text variant="labelTechnical" className="text-text-muted">
        Pendiente
      </Text>
    </Box>
  );
}

/**
 * Vista del plan (spec 0003, ticket #84): la hoja *Plan · Periodización 4S* del rediseño. Cabecera
 * con la semana y el reparto, selector de semanas S1–S4 con su estado, volumen semanal prescrito,
 * la lista de sesiones de la semana (la de hoy con **Comenzar sesión**) y el control de ajuste.
 *
 * Es presentacional: recibe el plan ya cargado, los registros (para marcar las sesiones hechas) y
 * la fecha de hoy. Solo lectura.
 */
export function PlanView({
  plan,
  logs = [],
  today = todayStartDate(),
  onStartSession,
}: {
  plan: Plan;
  logs?: readonly SessionLog[];
  today?: string;
  onStartSession?: () => void;
}) {
  const colors = useIconColors();
  const currentWeek = useMemo(() => currentWeekNumber(plan, today), [plan, today]);
  const [selectedWeek, setSelectedWeek] = useState(() => String(currentWeek));

  const done = useMemo(
    () => doneSessionKeys(plan.mesocycleId ?? null, logs),
    [plan.mesocycleId, logs],
  );

  if (plan.microcycles.length === 0) {
    return (
      <EmptyState
        title="Sin plan"
        description="El mesociclo todavía no tiene semanas."
        testID="plan-empty"
      />
    );
  }

  const microcycle =
    plan.microcycles.find((candidate) => String(candidate.number) === selectedWeek) ??
    plan.microcycles[0];
  const volume = weeklyVolume(microcycle.sessions);
  const split = volume.map((entry) => entry.label).join(' / ');
  const completed = microcycle.sessions.filter((session) =>
    done.has(sessionKey(plan.mesocycleId ?? null, microcycle.number, session.day)),
  ).length;

  return (
    <Stack gap="lg">
      <Stack gap="xs">
        <Stack direction="row" className="items-center justify-between gap-sm">
          <Text variant="labelTechnical" className="text-text-muted">
            {`Semana ${microcycle.number} de ${plan.microcycles.length}`}
          </Text>
          {split ? (
            <Text variant="labelTechnical" className="text-text-muted">
              {split}
            </Text>
          ) : null}
        </Stack>
        <Text variant="headlineLg" className="uppercase">
          {`${MESOCYCLE_CODE} · ${PERIODIZATION_LABEL}`}
        </Text>
        <Text variant="bodySm" className="text-text-muted">
          {`${plan.trainingDays} sesiones semanales · Objetivo RIR ${TARGET_RIR}`}
        </Text>
      </Stack>

      <Stack direction="row" gap="sm" testID="plan-week">
        {plan.microcycles.map((candidate) => {
          const number = candidate.number;
          const isDone =
            candidate.sessions.length > 0 &&
            candidate.sessions.every((session) =>
              done.has(sessionKey(plan.mesocycleId ?? null, number, session.day)),
            );

          return (
            <WeekCell
              key={number}
              number={number}
              state={weekState(number, currentWeek, isDone)}
              selected={String(number) === selectedWeek}
              onSelect={() => setSelectedWeek(String(number))}
              colors={colors}
            />
          );
        })}
      </Stack>

      <Stack
        direction="row"
        className="items-center justify-between gap-sm rounded-sm border border-border bg-surface px-sm py-sm"
        testID="plan-volume"
      >
        <Text variant="labelTechnical" className="text-text-muted">
          Volumen semanal
        </Text>
        <Stack direction="row" gap="md">
          {volume.map((entry) => (
            <Text
              key={entry.pattern}
              variant="labelCode"
              className="text-text"
              testID={`plan-volume-${entry.pattern}`}
            >
              <Text variant="labelCode" className="font-mono-semibold text-primary">
                {String(entry.sets)}
              </Text>
              {` ${entry.label}`}
            </Text>
          ))}
        </Stack>
      </Stack>

      <Stack gap="sm">
        <Stack direction="row" className="items-center justify-between gap-sm">
          <Text variant="labelTechnical" className="text-text-muted">
            Sesiones de la semana
          </Text>
          <Text variant="labelTechnical" className="text-text-muted">
            {`${completed} / ${microcycle.sessions.length} completadas`}
          </Text>
        </Stack>

        <Stack gap="sm" testID={`plan-microcycle-${microcycle.number}`}>
          {microcycle.sessions.map((session) => (
            <SessionCard
              key={session.day}
              session={session}
              microcycleNumber={microcycle.number}
              done={done.has(sessionKey(plan.mesocycleId ?? null, microcycle.number, session.day))}
              isToday={isSessionToday(session, today)}
              onStart={onStartSession}
              colors={colors}
            />
          ))}
        </Stack>
      </Stack>

      <Stack
        direction="row"
        className="items-center justify-between gap-sm border-t border-border py-sm"
        testID="plan-adjust"
      >
        <Stack direction="row" gap="sm" className="items-center">
          <Icon icon={SlidersHorizontal} size={18} color={colors.muted} />
          <Text variant="labelTechnical" className="text-text-muted">
            Ajustar sobrecarga o volumen
          </Text>
        </Stack>
        {/* Stub del control de ajuste: la adaptación en vivo está fuera del MVP (#84), así que no
            se puede pulsar todavía. */}
        <Text variant="labelTechnical" className="text-primary" testID="plan-adjust-modify">
          Modificar
        </Text>
      </Stack>
    </Stack>
  );
}
