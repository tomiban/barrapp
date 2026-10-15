import { useMemo, useState } from 'react';

import type { Plan, PlanItemRole, PlanSessionItem } from '@/api/plan';
import type { SessionLog } from '@/api/sessionLogs';
import { Button } from '@/design-system/Button';
import { Chip, SegmentedControl, type SegmentedOption } from '@/design-system/Chip';
import { Banner, EmptyState } from '@/design-system/Feedback';
import { Box, Stack } from '@/design-system/layout';
import { ListRow, SectionHeader } from '@/design-system/ListRow';
import { Stepper } from '@/design-system/Stepper';
import { Text } from '@/design-system/Text';
import { Timer } from '@/design-system/Timer';

import {
  holdSecondsOf,
  lastreButtonLabel,
  LASTRE_OPTIONS,
  metricOf,
  modeOf,
  objectiveOf,
  phaseOf,
  presetsOf,
  rangeOf,
  restSecondsOf,
  RIR_OPTIONS,
  SET_STATE_LABEL,
  SET_STATE_ROLE,
  setStateOf,
  setSummaryOf,
  targetValueOf,
  twoDigits,
  unitLabelOf,
} from './sessionSheet';

/** Papeles de la sesión que se registran serie a serie: skill, fuerza y core (anatomía de sesión). */
const LOGGED_ROLES: readonly PlanItemRole[] = ['skill', 'strength', 'core'];

/** Una respuesta de guardado (confirmación del servidor o error). */
export type SaveFeedback = { role: 'confirmed' | 'error'; message: string };

/** Una serie lista para el API, con el esfuerzo (RIR) y el lastre resueltos (`null` si no se anotan). */
export type SessionLogSetPayload = {
  setNumber: number;
  value: number;
  effort: number | null;
  loadKg: number | null;
};

/** Series parseadas de un ejercicio, listas para el API. */
export type ExerciseSetsPayload = {
  exerciseId: string;
  item: PlanSessionItem;
  sets: SessionLogSetPayload[];
};

/** Props públicas de la vista de sesión en curso. */
export type SessionLoggingViewProps = {
  plan: Plan;
  logs: SessionLog[];
  saving: boolean;
  feedback: SaveFeedback | null;
  onSave: (
    session: { day: number; microcycleNumber: number; date: string | null },
    exercises: ExerciseSetsPayload[],
  ) => void;
};

/** Borrador de una serie: el valor tecleado, el RIR real y el lastre. */
type SetDraft = { value: string; effort: number | null; loadKg: number };

/**
 * Serie en curso (spec 0003 §4, ticket #85): ejecución guiada, un ejercicio y una serie a la vez,
 * reproduciendo la hoja *Sesión en curso · Registro set a set* del rediseño.
 *
 * La unidad de cada serie sale del ejercicio (segundos en los holds, reps en lo demás), el
 * cronómetro y el descanso se alimentan del `SessionItem` (#95) y el registro se guarda serie a
 * serie vía `onSave` (que la pantalla encola en la outbox offline, #26). Se elige el ejercicio con
 * la navegación anterior/siguiente y la sesión se cierra con «Finalizar y guardar el resumen».
 * La unidad nunca la fija el cliente: se deriva de la prescripción del motor (D5/D6).
 */
export function SessionLoggingView({
  plan,
  logs,
  saving,
  feedback,
  onSave,
}: SessionLoggingViewProps) {
  const [week, setWeek] = useState(() => String(plan.microcycles[0]?.number ?? 1));
  const [day, setDay] = useState(() => String(plan.microcycles[0]?.sessions[0]?.day ?? 1));
  const [exerciseIndex, setExerciseIndex] = useState(0);
  const [drafts, setDrafts] = useState<Record<string, SetDraft[]>>({});
  const [progress, setProgress] = useState<Record<string, number>>({});
  const [chronoRunning, setChronoRunning] = useState(false);
  const [chronoKey, setChronoKey] = useState(0);
  const [resting, setResting] = useState(false);
  const [validationError, setValidationError] = useState('');

  const microcycle = useMemo(
    () =>
      plan.microcycles.find((candidate) => String(candidate.number) === week) ??
      plan.microcycles[0],
    [plan.microcycles, week],
  );

  const session =
    microcycle?.sessions.find((candidate) => String(candidate.day) === day) ??
    microcycle?.sessions[0];

  const loggableItems = useMemo(
    () => session?.items.filter((item) => LOGGED_ROLES.includes(item.role)) ?? [],
    [session],
  );

  if (!microcycle || !session || plan.microcycles.length === 0) {
    return (
      <EmptyState
        title="Sin plan"
        description="El mesociclo todavía no tiene sesiones para registrar."
        testID="session-log-empty"
      />
    );
  }

  const weekOptions: readonly SegmentedOption[] = plan.microcycles.map((entry) => ({
    value: String(entry.number),
    label: `Semana ${entry.number}`,
  }));

  const dayOptions: readonly SegmentedOption[] = microcycle.sessions.map((entry) => ({
    value: String(entry.day),
    label: `Día ${entry.day}`,
  }));

  const sessionDay = Number(session.day);

  /** El registro guardado del ejercicio en la sesión elegida, si existe. */
  const logFor = (exerciseId: string) =>
    logs.find(
      (log) =>
        log.exerciseId === exerciseId &&
        log.sessionDay === sessionDay &&
        (log.microcycleNumber === null ||
          log.microcycleNumber === undefined ||
          log.microcycleNumber === microcycle.number),
    );

  /** Series del ejercicio: el borrador en curso o, si no se ha tocado, el que dicta el registro. */
  const draftsFor = (item: PlanSessionItem): SetDraft[] => {
    const current = drafts[item.exerciseId];
    if (current) {
      return current;
    }
    const log = logFor(item.exerciseId);
    return Array.from({ length: item.sets }, (_, index) => {
      const logged = log?.sets.find((set) => set.setNumber === index + 1);
      return {
        value: logged ? String(logged.value) : String(targetValueOf(item)),
        effort: logged?.effort ?? null,
        loadKg: logged?.loadKg ?? 0,
      };
    });
  };

  /** Serie en curso (1-based): la primera sin registrar; una más allá de la última si terminó. */
  const progressFor = (item: PlanSessionItem): number => {
    const current = progress[item.exerciseId];
    if (current !== undefined) {
      return current;
    }
    const log = logFor(item.exerciseId);
    let next = 1;
    while (next <= item.sets && log?.sets.some((set) => set.setNumber === next)) {
      next += 1;
    }
    return next;
  };

  const updateDraft = (item: PlanSessionItem, setNumber: number, patch: Partial<SetDraft>) => {
    setValidationError('');
    const next = draftsFor(item).map((set, index) =>
      index === setNumber - 1 ? { ...set, ...patch } : set,
    );
    setDrafts((current) => ({ ...current, [item.exerciseId]: next }));
  };

  const goToExercise = (index: number) => {
    setExerciseIndex(index);
    setValidationError('');
    setChronoRunning(false);
    setChronoKey((key) => key + 1);
    setResting(false);
  };

  const handleWeekChange = (value: string) => {
    setWeek(value);
    setExerciseIndex(0);
    setResting(false);
    const firstDay = plan.microcycles.find((candidate) => String(candidate.number) === value)
      ?.sessions[0]?.day;
    setDay(firstDay !== undefined ? String(firstDay) : '1');
  };

  const handleDayChange = (value: string) => {
    setDay(value);
    setExerciseIndex(0);
    setResting(false);
  };

  const currentItem = loggableItems[Math.min(exerciseIndex, loggableItems.length - 1)];
  const isometric = currentItem ? metricOf(currentItem) === 'seconds' : false;
  const currentSet = currentItem ? Math.min(progressFor(currentItem), currentItem.sets) : 1;
  const finished = currentItem ? progressFor(currentItem) > currentItem.sets : false;
  const currentDraft = currentItem ? draftsFor(currentItem)[currentSet - 1] : undefined;

  const registerSet = () => {
    if (!currentItem || !currentDraft) {
      return;
    }

    const value = Number(currentDraft.value.trim());
    if (currentDraft.value.trim() === '' || !Number.isInteger(value) || value < 0) {
      setValidationError('Introduce el valor de la serie antes de registrarla.');
      return;
    }

    if (!finished) {
      setProgress((current) => ({
        ...current,
        [currentItem.exerciseId]: progressFor(currentItem) + 1,
      }));
    }
    setValidationError('');
    setResting(true);
  };

  const toggleEffort = (value: number) => {
    if (!currentItem || !currentDraft) {
      return;
    }
    updateDraft(currentItem, currentSet, {
      effort: currentDraft.effort === value ? null : value,
    });
  };

  const cycleLastre = () => {
    if (!currentItem || !currentDraft) {
      return;
    }
    const index = LASTRE_OPTIONS.indexOf(currentDraft.loadKg);
    const next = LASTRE_OPTIONS[(index + 1 + LASTRE_OPTIONS.length) % LASTRE_OPTIONS.length];
    updateDraft(currentItem, currentSet, { loadKg: next });
  };

  const finishSession = () => {
    const exercises: ExerciseSetsPayload[] = [];
    for (const item of loggableItems) {
      const registered = draftsFor(item).slice(0, progressFor(item) - 1);
      if (registered.length === 0) {
        continue;
      }
      exercises.push({
        exerciseId: item.exerciseId,
        item,
        sets: registered.map((set, index) => ({
          setNumber: index + 1,
          value: Number(set.value),
          effort: set.effort,
          loadKg: set.loadKg > 0 ? set.loadKg : null,
        })),
      });
    }

    if (exercises.length === 0) {
      setValidationError('No hay series registradas para guardar.');
      return;
    }

    setValidationError('');
    onSave({ day: sessionDay, microcycleNumber: microcycle.number, date: session.date }, exercises);
  };

  if (loggableItems.length === 0 || !currentItem || !currentDraft) {
    return (
      <EmptyState
        title="Sin ejercicios"
        description="Esta sesión no tiene ejercicios que registrar."
        testID="session-log-no-items"
      />
    );
  }

  const sets = draftsFor(currentItem);
  const previous = loggableItems[exerciseIndex - 1];
  const next = loggableItems[exerciseIndex + 1];

  return (
    <Stack gap="lg" testID="session-log-view">
      <Stack direction="row" gap="sm" className="items-center">
        <Box className="h-xs w-xs bg-primary" />
        <Text
          variant="labelTechnical"
          className="text-text-muted"
          testID="session-log-mode"
        >{`MODO EJECUCIÓN // ${modeOf(currentItem)}`}</Text>
      </Stack>

      <Box className="gap-sm rounded-base border border-border bg-surface p-md">
        <Text variant="labelTechnical" className="text-text-muted">
          Sesión
        </Text>
        <SegmentedControl
          options={weekOptions}
          value={week}
          onChange={handleWeekChange}
          label="Microciclo"
          testID="session-log-week"
        />
        <SegmentedControl
          options={dayOptions}
          value={String(session.day)}
          onChange={handleDayChange}
          label="Día de la sesión"
          testID="session-log-day"
        />
      </Box>

      <Box
        className="gap-md rounded-base border border-border bg-surface p-md"
        testID="session-log-current"
      >
        <Stack direction="row" gap="sm" className="items-start justify-between">
          <Stack gap="xs" className="flex-1">
            <Text variant="labelCode" className="text-primary">
              {phaseOf(currentItem)}
            </Text>
            <Text variant="headlineLg" className="uppercase">
              {`${twoDigits(exerciseIndex + 1)} ${currentItem.exerciseName}`}
            </Text>
          </Stack>
          <Box className="items-end gap-xs rounded-base bg-surface-muted px-sm py-xs">
            <Text variant="labelTechnical" className="text-text-muted">
              Serie actual
            </Text>
            <Text variant="labelCode" testID="session-log-current-set">
              {`SET ${twoDigits(currentSet)} / ${twoDigits(currentItem.sets)}`}
            </Text>
          </Box>
        </Stack>

        <Box className="gap-sm rounded-base bg-canvas p-md">
          <Stack direction="row" gap="sm" className="items-center justify-between">
            <Text variant="labelTechnical" className="text-text-muted">
              {objectiveOf(currentItem)}
            </Text>
            {isometric ? (
              <Text
                variant="labelTechnical"
                className={chronoRunning ? 'text-secondary' : 'text-text-muted'}
                testID="session-log-chrono-state"
              >
                {chronoRunning ? 'CRONO ACTIVO' : 'CRONO PAUSADO'}
              </Text>
            ) : null}
          </Stack>
          {isometric ? (
            <>
              <Timer
                key={chronoKey}
                durationSeconds={holdSecondsOf(currentItem)}
                running={chronoRunning}
                label="Sostén"
                testID="session-log-chrono"
              />
              <Stack direction="row" gap="sm">
                <Button
                  variant="tertiary"
                  className="flex-1"
                  onPress={() => setChronoRunning((running) => !running)}
                  disabled={saving}
                  testID="session-log-chrono-toggle"
                >
                  {chronoRunning ? 'Pausar tiempo' : 'Iniciar tiempo'}
                </Button>
                <Button
                  variant="tertiary"
                  className="flex-1"
                  onPress={() => {
                    setChronoRunning(false);
                    setChronoKey((key) => key + 1);
                  }}
                  disabled={saving}
                  testID="session-log-chrono-reset"
                >
                  Reiniciar
                </Button>
              </Stack>
            </>
          ) : null}
        </Box>
      </Box>

      <Box
        className="gap-md rounded-base border border-border bg-surface p-md"
        testID="session-log-logger"
      >
        <Stack direction="row" gap="sm" className="items-center justify-between">
          <Text variant="headlineSm" className="uppercase">
            {`Registro de serie ${twoDigits(currentSet)}`}
          </Text>
          <Text variant="labelTechnical" className="text-primary">
            [Parámetros]
          </Text>
        </Stack>

        <Stack gap="xs">
          <Stack direction="row" gap="sm" className="items-center justify-between">
            <Text variant="labelTechnical" className="text-text-muted">
              {isometric ? 'Tiempo ejecutado' : 'Reps ejecutadas'}
            </Text>
            <Text variant="labelTechnical" className="text-text-muted">
              {unitLabelOf(currentItem)}
            </Text>
          </Stack>
          <Stack direction="row" gap="xs">
            {presetsOf(currentItem).map((preset) => (
              <Chip
                key={preset}
                label={isometric ? `${preset}s` : String(preset)}
                selected={Number(currentDraft.value) === preset}
                onChange={() => updateDraft(currentItem, currentSet, { value: String(preset) })}
                disabled={saving || finished}
                className="flex-1"
                testID={`session-log-preset-${preset}`}
              />
            ))}
          </Stack>
          <Stepper
            label="Valor de la serie"
            value={Number(currentDraft.value) || 0}
            onChange={(value) => updateDraft(currentItem, currentSet, { value: String(value) })}
            min={0}
            unit={unitLabelOf(currentItem)}
            disabled={saving || finished}
            testID="session-log-value"
          />
        </Stack>

        <Stack gap="xs">
          <Text variant="labelTechnical" className="text-text-muted">
            RIR real (reps en recámara)
          </Text>
          <Stack direction="row" gap="xs">
            {RIR_OPTIONS.map((option) => (
              <Chip
                key={option}
                label={
                  option === RIR_OPTIONS[RIR_OPTIONS.length - 1]
                    ? `RIR ${option}+`
                    : `RIR ${option}`
                }
                selected={currentDraft.effort === option}
                onChange={() => toggleEffort(option)}
                disabled={saving || finished}
                className="flex-1"
                testID={`session-log-rir-${option}`}
              />
            ))}
          </Stack>
        </Stack>

        <Stack
          direction="row"
          gap="sm"
          className="items-center justify-between rounded-base bg-surface-muted px-sm py-sm"
        >
          <Text variant="labelTechnical" className="text-text-muted">
            Lastre / sobrecarga
          </Text>
          <Chip
            label={lastreButtonLabel(currentDraft.loadKg)}
            onChange={cycleLastre}
            disabled={saving || finished}
            testID="session-log-lastre"
          />
        </Stack>

        <Button onPress={registerSet} disabled={saving || finished} testID="session-log-register">
          {finished ? 'Todas las series registradas' : 'Registrar serie → entrar en descanso'}
        </Button>
      </Box>

      <Box
        className="gap-sm rounded-base border border-border bg-surface p-md"
        testID="session-log-progress"
      >
        <Stack direction="row" gap="sm" className="items-center justify-between">
          <Text variant="headlineSm" className="uppercase">
            Progreso de series
          </Text>
          <Text variant="labelTechnical" className="text-text-muted">
            {`Estructura ${rangeOf(currentItem)}`}
          </Text>
        </Stack>
        {sets.map((set, index) => {
          const setNumber = index + 1;
          const state = setStateOf(setNumber < progressFor(currentItem), setNumber === currentSet);
          return (
            <ListRow
              key={setNumber}
              leading={
                <Text variant="labelCode" className="text-text-muted">
                  {twoDigits(setNumber)}
                </Text>
              }
              title={setSummaryOf(
                { value: Number(set.value) || 0, effort: set.effort, loadKg: set.loadKg },
                currentItem,
              )}
              role={SET_STATE_ROLE[state]}
              stateLabel={SET_STATE_LABEL[state]}
              last={index === sets.length - 1}
              testID={`session-log-set-${currentItem.exerciseId}-${setNumber}`}
            />
          );
        })}
      </Box>

      <Stack direction="row" gap="sm">
        <Button
          variant="tertiary"
          className="flex-1"
          disabled={saving || exerciseIndex === 0}
          onPress={() => goToExercise(exerciseIndex - 1)}
          testID="session-log-previous"
        >
          {previous ? `< ${previous.exerciseName}` : 'Inicio'}
        </Button>
        <Button
          variant="secondary"
          className="flex-1"
          disabled={saving || exerciseIndex >= loggableItems.length - 1}
          onPress={() => goToExercise(exerciseIndex + 1)}
          testID="session-log-next"
        >
          {next ? `${next.exerciseName} >` : 'Fin'}
        </Button>
      </Stack>

      <Stack
        direction="row"
        gap="sm"
        className="items-center justify-between rounded-base bg-canvas p-md"
        testID="session-log-rest"
      >
        <Stack gap="xs" className="flex-1">
          <Text variant="labelTechnical" className="text-text-muted">
            Pausa programada entre series
          </Text>
          <Text variant="labelCode">{`DESCANSO ${restSecondsOf(currentItem)}s POST-SET`}</Text>
        </Stack>
        {resting ? (
          <Timer
            durationSeconds={restSecondsOf(currentItem)}
            running
            label="Descanso"
            onComplete={() => setResting(false)}
            onPress={() => setResting(false)}
            testID="session-log-rest-timer"
          />
        ) : null}
      </Stack>

      <SectionHeader
        label="Registro de la sesión"
        count={loggableItems.filter((item) => progressFor(item) > 1).length}
        testID="session-log-saved-header"
      />

      {feedback ? (
        <Banner role={feedback.role} message={feedback.message} testID="session-log-feedback" />
      ) : null}

      {!feedback && validationError ? (
        <Banner role="error" message={validationError} testID="session-log-validation" />
      ) : null}

      <Button
        variant="tertiary"
        onPress={finishSession}
        disabled={saving}
        testID="session-log-save"
      >
        {saving ? 'Guardando…' : 'Finalizar sesión y guardar resumen'}
      </Button>
    </Stack>
  );
}
