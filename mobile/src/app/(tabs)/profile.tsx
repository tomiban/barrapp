import { useCallback, useEffect, useState } from 'react';

import {
  BASIC_EXERCISES,
  EXERCISE_PATTERN_LABELS,
  EXERCISE_PATTERNS,
  fetchAthleteProfile,
  saveAthleteProfile,
  validateAthleteProfileDraft,
  validateMaximumDrafts,
  TRAINING_DAYS_LIMITS,
  type AthleteProfile,
  type AthleteProfileFieldErrors,
  type MaximumDraft,
  type MaximumFieldErrors,
} from '@/api/athleteProfile';
import { messageOf } from '@/api/messageOf';
import { Button } from '@/design-system/Button';
import { SegmentedControl, type SegmentedOption } from '@/design-system/Chip';
import { Banner, Loading } from '@/design-system/Feedback';
import { Box, Stack } from '@/design-system/layout';
import { Header, Screen } from '@/design-system/Navigation';
import { StatusBadge } from '@/design-system/StatusBadge';
import { Text } from '@/design-system/Text';
import { TextField } from '@/design-system/TextField';
import { ObjectiveSection } from '@/features/profile/ObjectiveSection';

type LoadState = 'loading' | 'ready' | 'error';

type Feedback = {
  role: 'confirmed' | 'error';
  message: string;
};

/** Una opción por cada valor del rango de días de entrenamiento (3–5). */
const TRAINING_DAYS_OPTIONS: readonly SegmentedOption[] = Array.from(
  { length: TRAINING_DAYS_LIMITS.max - TRAINING_DAYS_LIMITS.min + 1 },
  (_, index) => {
    const value = String(TRAINING_DAYS_LIMITS.min + index);
    return { value, label: value };
  },
);

const DEFAULT_TRAINING_DAYS = String(TRAINING_DAYS_LIMITS.min);

/** Un borrador vacío por cada ejercicio básico. */
function emptyMaximumDrafts(): Record<string, string> {
  return Object.fromEntries(BASIC_EXERCISES.map((exercise) => [exercise.code, '']));
}

/**
 * Pantalla del perfil del atleta: guarda peso, altura, días de entrenamiento por semana y
 * sus máximos por ejercicio básico en el API, y los vuelve a leer para comprobar que la
 * persistencia los conserva. Es la costura de la app con `GET`/`PUT /profile`.
 */
export default function ProfileScreen() {
  const [loadState, setLoadState] = useState<LoadState>('loading');
  const [loadError, setLoadError] = useState('');
  const [weight, setWeight] = useState('');
  const [height, setHeight] = useState('');
  const [trainingDays, setTrainingDays] = useState(DEFAULT_TRAINING_DAYS);
  const [maximumDrafts, setMaximumDrafts] = useState<Record<string, string>>(emptyMaximumDrafts);
  const [fieldErrors, setFieldErrors] = useState<AthleteProfileFieldErrors>({});
  const [maximumErrors, setMaximumErrors] = useState<MaximumFieldErrors>({});
  const [saving, setSaving] = useState(false);
  const [feedback, setFeedback] = useState<Feedback | null>(null);
  const [persisted, setPersisted] = useState<AthleteProfile | null>(null);

  const applyProfile = useCallback((profile: AthleteProfile | null) => {
    if (profile) {
      setWeight(String(profile.weightKilograms));
      setHeight(String(profile.heightCentimeters));
      setTrainingDays(String(profile.trainingDays));
      setMaximumDrafts(
        Object.fromEntries(
          BASIC_EXERCISES.map((exercise) => [
            exercise.code,
            String(
              profile.maximums.find((maximum) => maximum.exerciseCode === exercise.code)
                ?.repetitions ?? '',
            ),
          ]),
        ),
      );
    }
    setPersisted(profile);
  }, []);

  const load = useCallback(
    (signal?: AbortSignal) => {
      fetchAthleteProfile(signal)
        .then((profile) => {
          applyProfile(profile);
          setLoadState('ready');
        })
        .catch((error: unknown) => {
          if (error instanceof Error && error.name === 'AbortError') {
            return;
          }
          setLoadError(messageOf(error));
          setLoadState('error');
        });
    },
    [applyProfile],
  );

  useEffect(() => {
    const controller = new AbortController();
    load(controller.signal);
    return () => controller.abort();
  }, [load]);

  const handleReload = useCallback(() => {
    setLoadState('loading');
    setFeedback(null);
    load();
  }, [load]);

  const handleSave = useCallback(async () => {
    const {
      weightKilograms,
      heightCentimeters,
      trainingDays: selectedTrainingDays,
      errors,
    } = validateAthleteProfileDraft(weight, height, Number(trainingDays));

    const drafts: MaximumDraft[] = BASIC_EXERCISES.map((exercise) => ({
      exerciseCode: exercise.code,
      value: maximumDrafts[exercise.code] ?? '',
    }));
    const maximumValidation = validateMaximumDrafts(drafts);

    setFieldErrors(errors);
    setMaximumErrors(maximumValidation.errors);
    setFeedback(null);

    if (
      weightKilograms === null ||
      heightCentimeters === null ||
      selectedTrainingDays === null ||
      maximumValidation.maximums === null
    ) {
      return;
    }

    setSaving(true);
    try {
      await saveAthleteProfile({
        weightKilograms,
        heightCentimeters,
        trainingDays: selectedTrainingDays,
        maximums: maximumValidation.maximums,
      });

      // Relee del servidor: la app no se fía de su estado local.
      const reloaded = await fetchAthleteProfile();
      applyProfile(reloaded);
      setFeedback({ role: 'confirmed', message: 'Perfil guardado y releído del servidor.' });
    } catch (error) {
      setFeedback({ role: 'error', message: messageOf(error) });
    } finally {
      setSaving(false);
    }
  }, [applyProfile, height, maximumDrafts, trainingDays, weight]);

  return (
    <Screen testID="profile-screen" header={<Header title="Perfil" />}>
      <Stack gap="md">
        <Text variant="labelTechnical" className="text-text-muted">
          BARRAPP · PERFIL DEL ATLETA
        </Text>

        {loadState === 'loading' ? (
          <Loading label="Leyendo el perfil…" testID="profile-loading" />
        ) : null}

        {loadState === 'error' ? (
          <Stack gap="sm">
            <StatusBadge role="error" label="Sin conexión" testID="profile-status" />
            <Banner role="error" message={loadError} testID="profile-load-error" />
            <Button onPress={handleReload} testID="profile-retry">
              Reintentar
            </Button>
          </Stack>
        ) : null}

        {loadState === 'ready' ? (
          <>
            <Stack gap="sm">
              <TextField
                label="Peso (kg)"
                value={weight}
                onChangeText={setWeight}
                keyboardType="decimal-pad"
                placeholder="78.5"
                error={fieldErrors.weight}
                testID="profile-weight"
              />
              <TextField
                label="Altura (cm)"
                value={height}
                onChangeText={setHeight}
                keyboardType="number-pad"
                placeholder="181"
                error={fieldErrors.height}
                testID="profile-height"
              />
              <Stack gap="xs">
                <Text variant="labelTechnical" className="text-text-muted">
                  Días de entrenamiento
                </Text>
                <SegmentedControl
                  options={TRAINING_DAYS_OPTIONS}
                  value={trainingDays}
                  onChange={setTrainingDays}
                  label="Días de entrenamiento por semana"
                  error={fieldErrors.trainingDays}
                  testID="profile-training-days"
                />
              </Stack>
            </Stack>

            <Stack gap="sm">
              <Text variant="labelTechnical" className="text-text-muted">
                Máximos (reps)
              </Text>
              {EXERCISE_PATTERNS.map((pattern) => (
                <Stack key={pattern} gap="sm">
                  <Text variant="bodySm" className="text-text-muted">
                    {EXERCISE_PATTERN_LABELS[pattern]}
                  </Text>
                  {BASIC_EXERCISES.filter((exercise) => exercise.pattern === pattern).map(
                    (exercise) => (
                      <TextField
                        key={exercise.code}
                        label={exercise.name}
                        value={maximumDrafts[exercise.code] ?? ''}
                        onChangeText={(text) =>
                          setMaximumDrafts((current) => ({ ...current, [exercise.code]: text }))
                        }
                        keyboardType="number-pad"
                        placeholder="0"
                        error={maximumErrors[exercise.code]}
                        testID={`profile-maximum-${exercise.code}`}
                      />
                    ),
                  )}
                </Stack>
              ))}
            </Stack>

            {feedback ? (
              <Banner role={feedback.role} message={feedback.message} testID="profile-feedback" />
            ) : null}

            <Stack direction="row" gap="sm">
              <Button onPress={handleSave} disabled={saving} testID="profile-save">
                {saving ? 'Guardando…' : 'Guardar'}
              </Button>
              <Button variant="tertiary" onPress={handleReload} testID="profile-reload">
                Recargar
              </Button>
            </Stack>

            <Box className="gap-sm rounded-md border border-border bg-surface p-md">
              <StatusBadge
                role={persisted ? 'confirmed' : 'inactive'}
                label={persisted ? 'Perfil guardado' : 'Sin perfil todavía'}
                testID="profile-persisted-status"
              />
              {persisted ? (
                <>
                  <Text variant="bodyMd" testID="profile-persisted-values">
                    {persisted.weightKilograms} kg · {persisted.heightCentimeters} cm ·{' '}
                    {persisted.trainingDays} días/semana
                  </Text>
                  <Text variant="bodySm" testID="profile-persisted-maximums">
                    {BASIC_EXERCISES.map((exercise) => {
                      const maximum = persisted.maximums.find(
                        (candidate) => candidate.exerciseCode === exercise.code,
                      );
                      return `${exercise.name} ${maximum?.repetitions ?? 0}`;
                    }).join(' · ')}
                  </Text>
                </>
              ) : (
                <Text variant="bodySm" className="text-text-muted">
                  Todavía no hay ningún perfil guardado. Introduce tus datos y guarda.
                </Text>
              )}
            </Box>

            {persisted ? <ObjectiveSection /> : null}
          </>
        ) : null}
      </Stack>
    </Screen>
  );
}
