import { useCallback, useEffect, useState } from 'react';

import {
  fetchAthleteProfile,
  saveAthleteProfile,
  validateAthleteProfileMeasurements,
  TRAINING_DAYS_LIMITS,
  type AthleteProfile,
  type AthleteProfileFieldErrors,
} from '@/api/athleteProfile';
import { Button } from '@/design-system/Button';
import { SegmentedControl, type SegmentedOption } from '@/design-system/Chip';
import { Banner, Loading } from '@/design-system/Feedback';
import { Box, Stack } from '@/design-system/layout';
import { Header, Screen } from '@/design-system/Navigation';
import { StatusBadge } from '@/design-system/StatusBadge';
import { Text } from '@/design-system/Text';
import { TextField } from '@/design-system/TextField';

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

function messageOf(error: unknown): string {
  return error instanceof Error ? error.message : 'Error desconocido';
}

/**
 * Pantalla del perfil del atleta: guarda peso, altura y días de entrenamiento por
 * semana en el API y los vuelve a leer para comprobar que la persistencia los
 * conserva. Es la costura de la app con `GET`/`PUT /profile`.
 */
export default function ProfileScreen() {
  const [loadState, setLoadState] = useState<LoadState>('loading');
  const [loadError, setLoadError] = useState('');
  const [weight, setWeight] = useState('');
  const [height, setHeight] = useState('');
  const [trainingDays, setTrainingDays] = useState(DEFAULT_TRAINING_DAYS);
  const [fieldErrors, setFieldErrors] = useState<AthleteProfileFieldErrors>({});
  const [saving, setSaving] = useState(false);
  const [feedback, setFeedback] = useState<Feedback | null>(null);
  const [persisted, setPersisted] = useState<AthleteProfile | null>(null);

  const applyProfile = useCallback((profile: AthleteProfile | null) => {
    if (profile) {
      setWeight(String(profile.weightKilograms));
      setHeight(String(profile.heightCentimeters));
      setTrainingDays(String(profile.trainingDays));
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
    } = validateAthleteProfileMeasurements(weight, height, Number(trainingDays));

    setFieldErrors(errors);
    setFeedback(null);

    if (weightKilograms === null || heightCentimeters === null || selectedTrainingDays === null) {
      return;
    }

    setSaving(true);
    try {
      await saveAthleteProfile({
        weightKilograms,
        heightCentimeters,
        trainingDays: selectedTrainingDays,
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
  }, [applyProfile, height, trainingDays, weight]);

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
                  testID="profile-training-days"
                />
                {fieldErrors.trainingDays ? (
                  <Text
                    variant="bodySm"
                    className="text-error"
                    accessibilityRole="alert"
                    accessibilityLiveRegion="polite"
                  >
                    {fieldErrors.trainingDays}
                  </Text>
                ) : null}
              </Stack>
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
                <Text variant="bodyMd" testID="profile-persisted-values">
                  {persisted.weightKilograms} kg · {persisted.heightCentimeters} cm ·{' '}
                  {persisted.trainingDays} días/semana
                </Text>
              ) : (
                <Text variant="bodySm" className="text-text-muted">
                  Todavía no hay ningún perfil guardado. Introduce tus datos y guarda.
                </Text>
              )}
            </Box>
          </>
        ) : null}
      </Stack>
    </Screen>
  );
}
