import { useCallback, useMemo, useState } from 'react';

import type { Plan, PlanItemRole, PlanSessionItem } from '@/api/plan';
import type { SessionLog } from '@/api/sessionLogs';
import { EmptyState } from '@/design-system/Feedback';
import { Stack } from '@/design-system/layout';
import { ListRow, SectionHeader } from '@/design-system/ListRow';
import { StatusBadge } from '@/design-system/StatusBadge';
import { Text } from '@/design-system/Text';
import { TextField } from '@/design-system/TextField';
import { Button } from '@/design-system/Button';
import { SegmentedControl, type SegmentedOption } from '@/design-system/Chip';
import { Banner } from '@/design-system/Feedback';

/** Papeles de la sesión que se registran serie a serie (hoy solo fuerza). */
const LOGGED_ROLES: readonly PlanItemRole[] = ['strength'];

/** Una respuesta de guardado (confirmación del servidor o error). */
export type SaveFeedback = { role: 'confirmed' | 'error'; message: string };

/** Series parseadas de un ejercicio, listas para el API. */
export type ExerciseSetsPayload = {
  exerciseId: string;
  sets: { setNumber: number; value: number }[];
};

/** Props públicas de la vista de registro. */
export type SessionLoggingViewProps = {
  plan: Plan;
  logs: SessionLog[];
  saving: boolean;
  feedback: SaveFeedback | null;
  onSave: (day: number, exercises: ExerciseSetsPayload[]) => void;
};

/** Texto de la prescripción de una fila, p. ej. `3 × 8–12 reps`. */
function prescriptionOf(item: PlanSessionItem): string {
  if (item.repsMin !== null && item.repsMax !== null) {
    return `${item.sets} × ${item.repsMin}–${item.repsMax} reps`;
  }
  if (item.holdSecondsMin !== null && item.holdSecondsMax !== null) {
    return `${item.sets} × ${item.holdSecondsMin}–${item.holdSecondsMax} s`;
  }
  return `${item.sets} series`;
}

/** Valores reales de un registro ya guardado, p. ej. `8 · 9 · 10 reps`. */
function valuesOf(log: SessionLog): string {
  const values = log.sets.map((set) => String(set.value)).join(' · ');
  return `${values} ${log.metric === 'seconds' ? 's' : 'reps'}`;
}

/**
 * Registro de la sesión del día, serie a serie (spec 0001, US-34): elige microciclo y día,
 * introduce las reps reales de cada serie de fuerza y guarda. Los ejercicios ya registrados se
 * muestran como confirmados y quedan fuera del siguiente guardado (editarlos es el ticket #22).
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
  const [drafts, setDrafts] = useState<Record<string, string>>({});
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

  const strengthItems = useMemo(
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

  const weekOptions: readonly SegmentedOption[] = plan.microcycles.map((microcycle) => ({
    value: String(microcycle.number),
    label: `Semana ${microcycle.number}`,
  }));

  const dayOptions: readonly SegmentedOption[] = microcycle.sessions.map((session) => ({
    value: String(session.day),
    label: `Día ${session.day}`,
  }));

  const sessionDay = Number(session.day);

  const logFor = (exerciseId: string) =>
    logs.find((log) => log.exerciseId === exerciseId && log.sessionDay === sessionDay);

  const draftKey = (exerciseId: string, setNumber: number) => `${exerciseId}:${setNumber}`;

  const draftFor = (item: PlanSessionItem, setNumber: number) => {
    const typed = drafts[draftKey(item.exerciseId, setNumber)];
    if (typed !== undefined) {
      return typed;
    }
    const logged = logFor(item.exerciseId);
    const index = setNumber - 1;
    return logged ? String(logged.sets[index]?.value ?? '') : '';
  };

  const setDraft = (exerciseId: string, setNumber: number, value: string) => {
    setValidationError('');
    setDrafts((current) => ({ ...current, [draftKey(exerciseId, setNumber)]: value }));
  };

  const handleWeekChange = (value: string) => {
    setWeek(value);
    const next = plan.microcycles.find((candidate) => String(candidate.number) === value);
    const firstDay = next?.sessions[0]?.day;
    setDay(firstDay !== undefined ? String(firstDay) : '1');
  };

  const handleSave = () => {
    const unchecked = strengthItems.filter((item) => logFor(item.exerciseId) === undefined);

    const exercises: ExerciseSetsPayload[] = [];
    for (const item of unchecked) {
      const sets: { setNumber: number; value: number }[] = [];
      for (let setNumber = 1; setNumber <= item.sets; setNumber += 1) {
        const raw = draftFor(item, setNumber).trim();
        const value = Number(raw);
        if (raw === '' || !Number.isInteger(value) || value < 0) {
          setValidationError('Completa las reps de todas las series antes de guardar.');
          return;
        }
        sets.push({ setNumber, value });
      }
      exercises.push({ exerciseId: item.exerciseId, sets });
    }

    if (exercises.length === 0) {
      setValidationError('No hay series nuevas que guardar en esta sesión.');
      return;
    }

    onSave(sessionDay, exercises);
  };

  const savedLogs = logs.filter((log) => log.sessionDay === sessionDay);

  return (
    <Stack gap="lg" testID="session-log-view">
      <Stack gap="sm">
        <Text variant="labelTechnical" className="text-text-muted">
          BARRAPP · REGISTRO DE SESIÓN
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
          onChange={setDay}
          label="Día de la sesión"
          testID="session-log-day"
        />
      </Stack>

      {strengthItems.length === 0 ? (
        <EmptyState
          title="Sin fuerza"
          description="Esta sesión no tiene ejercicios de fuerza que registrar."
          testID="session-log-no-strength"
        />
      ) : (
        <Stack gap="lg">
          {strengthItems.map((item) => {
            const logged = logFor(item.exerciseId);

            return (
              <Stack key={item.exerciseId} gap="sm" testID={`session-log-item-${item.exerciseId}`}>
                <SectionHeader
                  label={item.exerciseName}
                  count={item.sets}
                  trailing={
                    logged ? (
                      <StatusBadge
                        role="confirmed"
                        label="Registrado"
                        testID={`session-log-item-${item.exerciseId}-status`}
                      />
                    ) : undefined
                  }
                  testID={`session-log-item-${item.exerciseId}-header`}
                />
                <Text variant="bodySm" className="text-text-muted">
                  {prescriptionOf(item)}
                </Text>

                {Array.from({ length: item.sets }, (_, index) => index + 1).map((setNumber) => (
                  <TextField
                    key={setNumber}
                    label={`Serie ${setNumber}`}
                    value={draftFor(item, setNumber)}
                    onChangeText={(text) => setDraft(item.exerciseId, setNumber, text)}
                    editable={logged === undefined}
                    keyboardType="number-pad"
                    testID={`session-log-set-${item.exerciseId}-${setNumber}`}
                  />
                ))}
              </Stack>
            );
          })}
        </Stack>
      )}

      {feedback ? (
        <Banner role={feedback.role} message={feedback.message} testID="session-log-feedback" />
      ) : null}

      {!feedback && validationError ? (
        <Banner role="error" message={validationError} testID="session-log-validation" />
      ) : null}

      <Button onPress={handleSave} disabled={saving} testID="session-log-save">
        {saving ? 'Guardando…' : 'Guardar sesión'}
      </Button>

      <Stack gap="sm" testID="session-log-saved">
        <SectionHeader
          label="Registro de la sesión"
          count={savedLogs.length}
          testID="session-log-saved-header"
        />
        {savedLogs.length === 0 ? (
          <Text variant="bodySm" className="text-text-muted" testID="session-log-saved-empty">
            Todavía no hay registros en esta sesión.
          </Text>
        ) : (
          savedLogs.map((log, index) => (
            <ListRow
              key={log.id}
              title={log.exerciseName}
              subtitle={valuesOf(log)}
              role="confirmed"
              stateLabel="Registrado"
              last={index === savedLogs.length - 1}
              testID={`session-log-saved-${log.exerciseId}`}
            />
          ))
        )}
      </Stack>
    </Stack>
  );
}
