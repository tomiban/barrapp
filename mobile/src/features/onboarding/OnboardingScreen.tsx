import { useCallback, useEffect, useState } from 'react';

import { fetchSkillCatalog, type Skill } from '@/api/catalog/skills';
import { messageOf } from '@/api/messageOf';
import { saveObjective } from '@/api/objective';
import { generatePlan } from '@/api/plan';
import { saveAthleteProfile, validateTrainingWeekdaySelection } from '@/api/athleteProfile';
import { Button } from '@/design-system/Button';
import { Banner } from '@/design-system/Feedback';
import { Stack } from '@/design-system/layout';
import { Header, Screen } from '@/design-system/Navigation';
import { ProgressIndicator } from '@/design-system/ProgressIndicator';
import { Text } from '@/design-system/Text';

import { ConfirmStep } from './ConfirmStep';
import {
  buildProfilePayload,
  ONBOARDING_DEFAULTS,
  ONBOARDING_STEPS,
  stepKicker,
  type MeasurementField,
  type OnboardingDraft,
} from './draft';
import { ObjectiveStep } from './ObjectiveStep';
import { ProfileStep } from './ProfileStep';
import { useStartup } from './StartupProvider';

type LoadState = 'loading' | 'ready' | 'error';

/** Etiquetas de la acción principal de cada paso, en el orden del mockup. */
const PRIMARY_LABELS = ['CONTINUAR AL PLAN', 'CONTINUAR', 'GENERAR MI PLAN'] as const;

/**
 * Pantalla de onboarding (spec 0003, ticket #90): flujo de primer arranque en
 * tres pasos —perfil, objetivo y frecuencia, y confirmación— que crea el perfil
 * y fija el objetivo antes de generar el mesociclo. Reutiliza las secciones y
 * validaciones del perfil (`athleteProfile`) y el patrón `stepper` del DS.
 *
 * No bloquea: «Omitir por ahora» deja entrar a las pestañas sin guardar nada.
 */
export function OnboardingScreen() {
  const { completeOnboarding, skipOnboarding } = useStartup();
  const [stepIndex, setStepIndex] = useState(0);
  const [draft, setDraft] = useState<OnboardingDraft>(ONBOARDING_DEFAULTS);
  const [skills, setSkills] = useState<Skill[]>([]);
  const [skillsState, setSkillsState] = useState<LoadState>('loading');
  const [skillsError, setSkillsError] = useState('');
  const [stepError, setStepError] = useState('');
  const [submitting, setSubmitting] = useState(false);
  const [submitError, setSubmitError] = useState('');

  const loadSkills = useCallback((signal?: AbortSignal) => {
    fetchSkillCatalog(signal)
      .then((catalog) => {
        setSkills(catalog);
        // Preselección del primer skill, como en el mockup; el atleta puede cambiarlo.
        setDraft((current) =>
          current.skillId === '' && catalog.length > 0
            ? { ...current, skillId: catalog[0].id }
            : current,
        );
        setSkillsState('ready');
      })
      .catch((error: unknown) => {
        if (error instanceof Error && error.name === 'AbortError') {
          return;
        }
        setSkillsError(messageOf(error));
        setSkillsState('error');
      });
  }, []);

  useEffect(() => {
    // El estado inicial ya es `loading`; así el efecto no toca el estado de forma
    // síncrona (regla react-hooks/set-state-in-effect). El reintento sí lo hace,
    // pero desde un manejador de evento, que es donde corresponde.
    const controller = new AbortController();
    loadSkills(controller.signal);
    return () => controller.abort();
  }, [loadSkills]);

  const retrySkills = useCallback(() => {
    setSkillsState('loading');
    setSkillsError('');
    loadSkills();
  }, [loadSkills]);

  const step = ONBOARDING_STEPS[stepIndex];

  const onChangeMeasurement = useCallback((field: MeasurementField, value: number) => {
    setDraft((current) => ({ ...current, [field]: value }));
  }, []);

  const onChangeMaximum = useCallback((exerciseCode: string, value: number) => {
    setDraft((current) => ({
      ...current,
      maximums: { ...current.maximums, [exerciseCode]: value },
    }));
  }, []);

  const onSelectSkill = useCallback((skillId: string) => {
    setStepError('');
    setDraft((current) => ({ ...current, skillId }));
  }, []);

  const onToggleWeekday = useCallback(
    (day: OnboardingDraft['weekdays'][number], selected: boolean) => {
      setStepError('');
      setDraft((current) => ({
        ...current,
        weekdays: selected
          ? [...current.weekdays, day]
          : current.weekdays.filter((candidate) => candidate !== day),
      }));
    },
    [],
  );

  /** Valida el paso 2 (skill + días) y, si pasa, avanza al 3. */
  const goNext = useCallback(() => {
    setStepError('');

    if (stepIndex === 1) {
      if (draft.skillId === '') {
        setStepError('Elige un skill objetivo.');
        return;
      }
      const weekdays = validateTrainingWeekdaySelection(draft.weekdays);
      if (weekdays.error) {
        setStepError(weekdays.error);
        return;
      }
    }

    setStepIndex((current) => Math.min(current + 1, ONBOARDING_STEPS.length - 1));
  }, [stepIndex, draft.skillId, draft.weekdays]);

  const goBack = useCallback(() => {
    setStepError('');
    setStepIndex((current) => Math.max(current - 1, 0));
  }, []);

  /** Guarda perfil y objetivo, genera el mesociclo y entra en Inicio. */
  const handleGenerate = useCallback(async () => {
    setSubmitError('');
    setSubmitting(true);
    try {
      await saveAthleteProfile(buildProfilePayload(draft));
      await saveObjective(draft.skillId);
      await generatePlan();
      completeOnboarding();
    } catch (error) {
      setSubmitError(messageOf(error));
    } finally {
      setSubmitting(false);
    }
  }, [draft, completeOnboarding]);

  const skillName = skills.find((skill) => skill.id === draft.skillId)?.name ?? draft.skillId;

  return (
    <Screen
      testID="onboarding-screen"
      header={<Header kicker="BARRAS" title={stepKicker(step)} />}
      scrollable
    >
      <Stack gap="lg">
        <ProgressIndicator
          value={stepIndex + 1}
          max={ONBOARDING_STEPS.length}
          role="active"
          label={`Paso ${stepIndex + 1} de ${ONBOARDING_STEPS.length}`}
          testID="onboarding-progress"
        />

        <Text variant="headlineLg" testID="onboarding-step-title">
          {step.title}
        </Text>

        {stepIndex === 0 ? (
          <ProfileStep
            draft={draft}
            onChangeMeasurement={onChangeMeasurement}
            onChangeMaximum={onChangeMaximum}
          />
        ) : null}

        {stepIndex === 1 ? (
          <ObjectiveStep
            skills={skills}
            loadState={skillsState}
            loadError={skillsError}
            onRetry={retrySkills}
            skillId={draft.skillId}
            onSelectSkill={onSelectSkill}
            weekdays={draft.weekdays}
            onToggleWeekday={onToggleWeekday}
            error={stepError}
          />
        ) : null}

        {stepIndex === 2 ? (
          <ConfirmStep
            skillName={skillName}
            weekdays={draft.weekdays}
            weightKilograms={draft.weightKilograms}
            heightCentimeters={draft.heightCentimeters}
            maximums={draft.maximums}
          />
        ) : null}

        {submitError ? (
          <Banner role="error" message={submitError} testID="onboarding-submit-error" />
        ) : null}

        <Stack gap="sm">
          {stepIndex === 2 ? (
            <Button onPress={handleGenerate} disabled={submitting} testID="onboarding-generate">
              {submitting ? 'Generando…' : `${PRIMARY_LABELS[2]} →`}
            </Button>
          ) : (
            <Button onPress={goNext} testID="onboarding-continue">
              {`${PRIMARY_LABELS[stepIndex]} →`}
            </Button>
          )}

          {stepIndex === 0 ? (
            <Button variant="tertiary" onPress={skipOnboarding} testID="onboarding-skip">
              Omitir por ahora
            </Button>
          ) : (
            <Button variant="secondary" onPress={goBack} testID="onboarding-back">
              ← Volver
            </Button>
          )}
        </Stack>
      </Stack>
    </Screen>
  );
}
