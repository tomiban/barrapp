import { useMemo, useState } from 'react';

import type { Plan, PlanItemRole, PlanSessionItem } from '@/api/plan';
import type { SessionLog } from '@/api/sessionLogs';
import { Banner, EmptyState } from '@/design-system/Feedback';
import { Button } from '@/design-system/Button';
import { SegmentedControl, type SegmentedOption } from '@/design-system/Chip';
import { Stack } from '@/design-system/layout';
import { ListRow, SectionHeader } from '@/design-system/ListRow';
import { StatusBadge } from '@/design-system/StatusBadge';
import { Text } from '@/design-system/Text';
import { TextField } from '@/design-system/TextField';

/** Papeles de la sesión que se registran serie a serie: skill, fuerza y core (anatomía de sesión). */
const LOGGED_ROLES: readonly PlanItemRole[] = ['skill', 'strength', 'core'];

/** Una respuesta de guardado (confirmación del servidor o error). */
export type SaveFeedback = { role: 'confirmed' | 'error'; message: string };

/** Una serie lista para el API, con el esfuerzo (RIR/RPE) resuelto (null si no se anota). */
export type SessionLogSetPayload = { setNumber: number; value: number; effort: number | null };

/** Series parseadas de un ejercicio, listas para el API. */
export type ExerciseSetsPayload = {
  exerciseId: string;
  item: PlanSessionItem;
  sets: SessionLogSetPayload[];
};

/** Props públicas de la vista de registro. */
export type SessionLoggingViewProps = {
  plan: Plan;
  logs: SessionLog[];
  saving: boolean;
  feedback: SaveFeedback | null;
  onSave: (
    session: { day: number; microcycleNumber: number; date: string | null },
    exercises: ExerciseSetsPayload[],
  ) => void;
  /** Edita un registro confirmado sustituyendo sus series (`PUT /session-logs/{id}`). */
  onUpdate: (log: SessionLog, sets: SessionLogSetPayload[]) => void;
  /** Elimina un registro confirmado (`DELETE /session-logs/{id}`). */
  onDelete: (log: SessionLog) => void;
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
  const unit = log.metric === 'seconds' ? 's' : 'reps';
  return `${values} ${unit}${effortOf(log)}`;
}

/**
 * El esfuerzo (RIR/RPE) anotado en el registro, p. ej. ` · RIR 2`; vacío si ninguna serie lo
 * trae. Si las series anotadas difieren, se listan en orden de aparición (`RIR 2/3`).
 */
function effortOf(log: SessionLog): string {
  const efforts = [
    ...new Set(log.sets.map((set) => set.effort).filter((effort) => effort !== null)),
  ];
  if (efforts.length === 0) {
    return '';
  }
  return ` · RIR ${efforts.join('/')}`;
}

/**
 * Registro de la sesión del día, serie a serie (spec 0001, US-34): elige microciclo y día,
 * introduce el valor real de cada serie —reps en fuerza, segundos en holds/skill— y guarda. La
 * unidad sale de la prescripción del plan, que el motor deriva del tipo de ejercicio (D5/D6);
 * el cliente solo introduce el número. Los ejercicios ya registrados se muestran como confirmados
 * y quedan fuera del siguiente guardado (editarlos es el ticket #22).
 */
export function SessionLoggingView({
  plan,
  logs,
  saving,
  feedback,
  onSave,
  onUpdate,
  onDelete,
}: SessionLoggingViewProps) {
  const [week, setWeek] = useState(() => String(plan.microcycles[0]?.number ?? 1));
  const [day, setDay] = useState(() => String(plan.microcycles[0]?.sessions[0]?.day ?? 1));
  const [drafts, setDrafts] = useState<Record<string, string>>({});
  const [effortDrafts, setEffortDrafts] = useState<Record<string, string>>({});
  const [validationError, setValidationError] = useState('');
  const [editingExerciseId, setEditingExerciseId] = useState<string | null>(null);

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
    logs.find(
      (log) =>
        log.exerciseId === exerciseId &&
        log.sessionDay === sessionDay &&
        (log.microcycleNumber === null ||
          log.microcycleNumber === undefined ||
          log.microcycleNumber === microcycle.number),
    );

  /**
   * Un registro confirmado por el servidor se puede editar y borrar (#22); el que sigue pendiente
   * en la cola local (#26) todavía no (la cola no soporta editar/borrar): la marca `pending` es la
   * costura que separa ambos y queda documentada en `SessionLog.pending`.
   */
  const editableLogFor = (exerciseId: string) => {
    const logged = logFor(exerciseId);
    return logged !== undefined && !logged.pending ? logged : undefined;
  };

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

  const effortDraftFor = (item: PlanSessionItem, setNumber: number) => {
    const typed = effortDrafts[draftKey(item.exerciseId, setNumber)];
    if (typed !== undefined) {
      return typed;
    }
    // Al editar, el campo de esfuerzo se re-habilita precargado con el esfuerzo anotado.
    if (editingExerciseId === item.exerciseId) {
      const effort = logFor(item.exerciseId)?.sets[setNumber - 1]?.effort;
      return effort !== undefined && effort !== null ? String(effort) : '';
    }
    return '';
  };

  const setEffortDraft = (exerciseId: string, setNumber: number, value: string) => {
    setValidationError('');
    setEffortDrafts((current) => ({ ...current, [draftKey(exerciseId, setNumber)]: value }));
  };

  const handleWeekChange = (value: string) => {
    setWeek(value);
    setEditingExerciseId(null);
    const next = plan.microcycles.find((candidate) => String(candidate.number) === value);
    const firstDay = next?.sessions[0]?.day;
    setDay(firstDay !== undefined ? String(firstDay) : '1');
  };

  const handleDayChange = (value: string) => {
    setDay(value);
    setEditingExerciseId(null);
  };

  /**
   * Valida y construye las series de un ejercicio a partir de los campos. Aplica el mismo
   * criterio que el registro nuevo (serie completada, entero ≥ 0 y RIR/RPE 0–10 opcional); si algo
   * falla, deja el aviso y devuelve `null` para que el flujo se detenga.
   */
  const buildSetsFor = (item: PlanSessionItem): SessionLogSetPayload[] | null => {
    const sets: SessionLogSetPayload[] = [];
    for (let setNumber = 1; setNumber <= item.sets; setNumber += 1) {
      const raw = draftFor(item, setNumber).trim();
      const value = Number(raw);
      if (raw === '' || !Number.isInteger(value) || value < 0) {
        setValidationError('Completa los valores de todas las series antes de guardar.');
        return null;
      }

      const rawEffort = effortDraftFor(item, setNumber).trim();
      let effort: number | null = null;
      if (rawEffort !== '') {
        const parsedEffort = Number(rawEffort);
        if (!Number.isInteger(parsedEffort) || parsedEffort < 0 || parsedEffort > 10) {
          setValidationError('El RIR/RPE debe ser un número entre 0 y 10.');
          return null;
        }
        effort = parsedEffort;
      }

      sets.push({ setNumber, value, effort });
    }
    return sets;
  };

  const handleSave = () => {
    const unchecked = loggableItems.filter((item) => logFor(item.exerciseId) === undefined);

    const exercises: ExerciseSetsPayload[] = [];
    for (const item of unchecked) {
      const sets = buildSetsFor(item);
      if (sets === null) {
        return;
      }
      exercises.push({ exerciseId: item.exerciseId, item, sets });
    }

    if (exercises.length === 0) {
      setValidationError('No hay series nuevas que guardar en esta sesión.');
      return;
    }

    onSave({ day: sessionDay, microcycleNumber: microcycle.number, date: session.date }, exercises);
  };

  const startEdit = (exerciseId: string) => {
    setValidationError('');
    setEditingExerciseId(exerciseId);
  };

  const cancelEdit = (item: PlanSessionItem) => {
    setEditingExerciseId(null);
    const discard = (current: Record<string, string>) => {
      const next = { ...current };
      for (let setNumber = 1; setNumber <= item.sets; setNumber += 1) {
        delete next[draftKey(item.exerciseId, setNumber)];
      }
      return next;
    };
    setDrafts(discard);
    setEffortDrafts(discard);
  };

  const handleUpdate = (item: PlanSessionItem, logged: SessionLog) => {
    const sets = buildSetsFor(item);
    if (sets === null) {
      return;
    }
    setEditingExerciseId(null);
    onUpdate(logged, sets);
  };

  const handleDelete = (logged: SessionLog) => {
    onDelete(logged);
  };

  const savedLogs = logs.filter(
    (log) =>
      log.sessionDay === sessionDay &&
      (log.microcycleNumber === null ||
        log.microcycleNumber === undefined ||
        log.microcycleNumber === microcycle.number),
  );

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
          onChange={handleDayChange}
          label="Día de la sesión"
          testID="session-log-day"
        />
      </Stack>

      {loggableItems.length === 0 ? (
        <EmptyState
          title="Sin ejercicios"
          description="Esta sesión no tiene ejercicios que registrar."
          testID="session-log-no-items"
        />
      ) : (
        <Stack gap="lg">
          {loggableItems.map((item) => {
            const logged = logFor(item.exerciseId);
            const editableLogged = editableLogFor(item.exerciseId);
            const editing = editingExerciseId === item.exerciseId;

            return (
              <Stack key={item.exerciseId} gap="sm" testID={`session-log-item-${item.exerciseId}`}>
                <SectionHeader
                  label={item.exerciseName}
                  count={item.sets}
                  trailing={
                    logged ? (
                      <StatusBadge
                        role={editing ? 'active' : 'confirmed'}
                        label={editing ? 'Editando' : 'Registrado'}
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
                  <Stack key={setNumber} gap="xs">
                    <TextField
                      label={`Serie ${setNumber}`}
                      value={draftFor(item, setNumber)}
                      onChangeText={(text) => setDraft(item.exerciseId, setNumber, text)}
                      editable={logged === undefined || editing}
                      keyboardType="number-pad"
                      testID={`session-log-set-${item.exerciseId}-${setNumber}`}
                    />
                    {logged === undefined || editing ? (
                      <TextField
                        label="RIR/RPE"
                        value={effortDraftFor(item, setNumber)}
                        onChangeText={(text) => setEffortDraft(item.exerciseId, setNumber, text)}
                        keyboardType="number-pad"
                        testID={`session-log-effort-${item.exerciseId}-${setNumber}`}
                      />
                    ) : null}
                  </Stack>
                ))}

                {editableLogged ? (
                  <Stack direction="row" gap="sm">
                    {editing ? (
                      <>
                        <Button
                          variant="secondary"
                          onPress={() => handleUpdate(item, editableLogged)}
                          disabled={saving}
                          testID={`session-log-update-${item.exerciseId}`}
                        >
                          Guardar cambios
                        </Button>
                        <Button
                          variant="tertiary"
                          onPress={() => cancelEdit(item)}
                          disabled={saving}
                          testID={`session-log-cancel-${item.exerciseId}`}
                        >
                          Cancelar
                        </Button>
                      </>
                    ) : (
                      <>
                        <Button
                          variant="secondary"
                          onPress={() => startEdit(item.exerciseId)}
                          disabled={saving}
                          testID={`session-log-edit-${item.exerciseId}`}
                        >
                          Editar
                        </Button>
                        <Button
                          variant="tertiary"
                          onPress={() => handleDelete(editableLogged)}
                          disabled={saving}
                          testID={`session-log-delete-${item.exerciseId}`}
                        >
                          Borrar
                        </Button>
                      </>
                    )}
                  </Stack>
                ) : null}
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
