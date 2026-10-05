import { useCallback, useEffect, useState } from 'react';

import { fetchObjective, OBJECTIVE_SKILLS, saveObjective, type Objective } from '@/api/objective';
import { Button } from '@/design-system/Button';
import { SegmentedControl, type SegmentedOption } from '@/design-system/Chip';
import { Banner, Loading } from '@/design-system/Feedback';
import { Stack } from '@/design-system/layout';
import { StatusBadge } from '@/design-system/StatusBadge';
import { Text } from '@/design-system/Text';

type LoadState = 'loading' | 'ready' | 'error';

type Feedback = {
  role: 'confirmed' | 'error';
  message: string;
};

/** Una opción por cada skill del catálogo. */
const SKILL_OPTIONS: readonly SegmentedOption[] = OBJECTIVE_SKILLS.map((skill) => ({
  value: skill.id,
  label: skill.name,
}));

function messageOf(error: unknown): string {
  return error instanceof Error ? error.message : 'Error desconocido';
}

/** Nombre para la UI del skill guardado. */
function nameOf(skillId: string): string {
  return OBJECTIVE_SKILLS.find((skill) => skill.id === skillId)?.name ?? skillId;
}

/**
 * Selector del skill objetivo del mesociclo: lee `GET /profile/objective`, deja elegir entre los
 * cuatro skills del catálogo y guarda con `PUT /profile/objective`. Es la costura del Perfil con
 * el objetivo que consumirán el motor de generación (#10) y la etapa por skill (#9).
 */
export function ObjectiveSection() {
  const [loadState, setLoadState] = useState<LoadState>('loading');
  const [loadError, setLoadError] = useState('');
  const [skillId, setSkillId] = useState('');
  const [persisted, setPersisted] = useState<Objective | null>(null);
  const [saving, setSaving] = useState(false);
  const [feedback, setFeedback] = useState<Feedback | null>(null);

  const applyObjective = useCallback((objective: Objective | null) => {
    setSkillId(objective?.skillId ?? '');
    setPersisted(objective);
  }, []);

  const load = useCallback(
    (signal?: AbortSignal) => {
      fetchObjective(signal)
        .then((objective) => {
          applyObjective(objective);
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
    [applyObjective],
  );

  useEffect(() => {
    const controller = new AbortController();
    load(controller.signal);
    return () => controller.abort();
  }, [load]);

  const handleRetry = useCallback(() => {
    setLoadState('loading');
    setFeedback(null);
    load();
  }, [load]);

  const handleSave = useCallback(async () => {
    setFeedback(null);

    if (!skillId) {
      setFeedback({ role: 'error', message: 'Elige un skill objetivo.' });
      return;
    }

    setSaving(true);
    try {
      const saved = await saveObjective(skillId);
      applyObjective(saved);
      setFeedback({ role: 'confirmed', message: 'Objetivo guardado y releído del servidor.' });
    } catch (error) {
      setFeedback({ role: 'error', message: messageOf(error) });
    } finally {
      setSaving(false);
    }
  }, [applyObjective, skillId]);

  return (
    <Stack gap="sm" testID="profile-objective-section">
      <Text variant="labelTechnical" className="text-text-muted">
        Objetivo del mesociclo
      </Text>

      {loadState === 'loading' ? (
        <Loading label="Leyendo el objetivo…" testID="profile-objective-loading" />
      ) : null}

      {loadState === 'error' ? (
        <Stack gap="sm">
          <Banner role="error" message={loadError} testID="profile-objective-load-error" />
          <Button variant="tertiary" onPress={handleRetry} testID="profile-objective-retry">
            Reintentar
          </Button>
        </Stack>
      ) : null}

      {loadState === 'ready' ? (
        <>
          <Text variant="bodySm" className="text-text-muted">
            Elige el skill que persigues este mes.
          </Text>

          <SegmentedControl
            options={SKILL_OPTIONS}
            value={skillId}
            onChange={setSkillId}
            label="Skill objetivo"
            testID="profile-objective"
          />

          {feedback ? (
            <Banner
              role={feedback.role}
              message={feedback.message}
              testID="profile-objective-feedback"
            />
          ) : null}

          <Stack direction="row" gap="sm" className="items-center">
            <Button onPress={handleSave} disabled={saving} testID="profile-objective-save">
              {saving ? 'Guardando…' : 'Guardar objetivo'}
            </Button>
            <StatusBadge
              role={persisted ? 'confirmed' : 'inactive'}
              label={persisted ? `Objetivo: ${nameOf(persisted.skillId)}` : 'Sin objetivo todavía'}
              testID="profile-objective-status"
            />
          </Stack>
        </>
      ) : null}
    </Stack>
  );
}
