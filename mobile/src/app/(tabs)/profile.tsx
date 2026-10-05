import { useCallback, useEffect, useState } from 'react';

import { fetchAthleteProfile, saveAthleteProfile, type AthleteProfile } from '@/api/athleteProfile';
import { Button } from '@/design-system/Button';
import { Banner, Loading } from '@/design-system/Feedback';
import { Box, Stack } from '@/design-system/layout';
import { Header, Screen } from '@/design-system/Navigation';
import { StatusBadge } from '@/design-system/StatusBadge';
import { Text } from '@/design-system/Text';
import { TextField } from '@/design-system/TextField';

type LoadState = 'loading' | 'ready' | 'error';

type FieldErrors = {
  weight?: string;
  height?: string;
};

type Feedback = {
  role: 'confirmed' | 'error';
  message: string;
};

function messageOf(error: unknown): string {
  return error instanceof Error ? error.message : 'Error desconocido';
}

/** Convierte el texto de un campo a número positivo; `null` si no es válido. */
function parsePositive(value: string): number | null {
  const parsed = Number.parseFloat(value.replace(',', '.'));
  return Number.isFinite(parsed) && parsed > 0 ? parsed : null;
}

/**
 * Pantalla del perfil del atleta: guarda peso y altura en el API y los vuelve a leer
 * para comprobar que la persistencia los conserva. Es la costura de la app con
 * `GET`/`PUT /profile`.
 */
export default function ProfileScreen() {
  const [loadState, setLoadState] = useState<LoadState>('loading');
  const [loadError, setLoadError] = useState('');
  const [weight, setWeight] = useState('');
  const [height, setHeight] = useState('');
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({});
  const [saving, setSaving] = useState(false);
  const [feedback, setFeedback] = useState<Feedback | null>(null);
  const [persisted, setPersisted] = useState<AthleteProfile | null>(null);

  const applyProfile = useCallback((profile: AthleteProfile | null) => {
    if (profile) {
      setWeight(String(profile.weightKilograms));
      setHeight(String(profile.heightCentimeters));
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
    const weightKilograms = parsePositive(weight);
    const heightCentimeters = parsePositive(height);

    const errors: FieldErrors = {};
    if (weightKilograms === null) {
      errors.weight = 'Introduce el peso en kg (mayor que 0).';
    }
    if (heightCentimeters === null) {
      errors.height = 'Introduce la altura en cm (mayor que 0).';
    }

    setFieldErrors(errors);
    setFeedback(null);

    if (weightKilograms === null || heightCentimeters === null) {
      return;
    }

    setSaving(true);
    try {
      await saveAthleteProfile({ weightKilograms, heightCentimeters });

      // Relee del servidor: la app no se fía de su estado local.
      const reloaded = await fetchAthleteProfile();
      applyProfile(reloaded);
      setFeedback({ role: 'confirmed', message: 'Perfil guardado y releído del servidor.' });
    } catch (error) {
      setFeedback({ role: 'error', message: messageOf(error) });
    } finally {
      setSaving(false);
    }
  }, [applyProfile, height, weight]);

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
                  {persisted.weightKilograms} kg · {persisted.heightCentimeters} cm
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
