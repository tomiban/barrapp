import { useCallback, useEffect, useState } from 'react';
import { ScrollView, View } from 'react-native';
import Constants from 'expo-constants';
import { useCSSVariable } from 'uniwind';

import {
  fetchSkillCatalog,
  type Skill,
  type SkillStage,
  type StageCriterion,
} from '@/api/catalog/skills';
import { fetchSkillProgress } from '@/api/catalog/progress';
import { fetchObjective } from '@/api/objective';
import { messageOf } from '@/api/messageOf';
import { Button } from '@/design-system/Button';
import { Chip } from '@/design-system/Chip';
import { Banner, EmptyState, Loading } from '@/design-system/Feedback';
import {
  Check,
  Flag,
  Icon,
  Info,
  Lock,
  type LucideIcon,
  Play,
  TrendingUp,
  Trophy,
  X,
} from '@/design-system/Icon';
import { Box, Stack } from '@/design-system/layout';
import { ProgressIndicator } from '@/design-system/ProgressIndicator';
import { Text } from '@/design-system/Text';
import { cn } from '@/design-system/utils/cn';
import {
  formatGlobalProgress,
  formatStageCriterion,
  skillCode,
  skillMaterial,
  STAGE_STATE_LABEL,
  stageState,
  type StageState,
} from '@/features/library/skillLadder';

type LoadState =
  | { status: 'loading' }
  | {
      status: 'ready';
      skills: Skill[];
      currentStageBySkill: Map<string, number>;
      objectiveSkillId: string | null;
    }
  | { status: 'error'; message: string };

/** Revisión técnica de la hoja de skills, chrome del mockup (`SYS.CAL-v2.4`). */
const SHEET_REVISION = `SYS.CAL · v${Constants.expoConfig?.version ?? '1.0.0'}`;

/*
 * Marcadores del mockup «Calistenia Progresiva» que todavía no tienen dato real: la spec 0003
 * deja fuera de alcance el diagrama biomecánico, la telemetría, la validación por sesiones y el
 * protocolo de regresión. Se reproducen tal cual para clavar el diseño de la pantalla.
 * shortcut: contenido estático del mockup; reemplazar por las métricas reales (#96) cuando existan.
 */
const OBJECTIVE_PROGRESS_PERCENT = 85;
const OBJECTIVE_SESSIONS_MET = '1/2';
const QUICK_STATS: readonly { label: string; value: string }[] = [
  { label: 'Retención máx', value: '14.2s' },
  { label: 'Volumen sem', value: '84s tot' },
  { label: 'RPE promedio', value: '8.5 RPE' },
];

/** Orden a dos dígitos, p. ej. `02`. */
function twoDigits(value: number): string {
  return String(Math.max(Math.trunc(value), 0)).padStart(2, '0');
}

/** Umbral del criterio en su unidad, p. ej. `12s` o `5 reps`. */
function criterionThreshold(criterion: StageCriterion): string {
  return criterion.metric === 'seconds' ? `${criterion.target}s` : `${criterion.target} reps`;
}

/** Colores de icono resueltos desde los tokens; Lucide colorea por prop, no por `className`. */
function useIconColors() {
  const tokens = useCSSVariable([
    '--color-primary',
    '--color-text-muted',
    '--color-on-primary',
    '--color-error',
  ]);
  const asColor = (value: unknown) => (typeof value === 'string' ? value : undefined);

  return {
    primary: asColor(tokens[0]),
    muted: asColor(tokens[1]),
    onPrimary: asColor(tokens[2]),
    error: asColor(tokens[3]),
  };
}

/** Icono, caja y acento de cada estado de etapa, en el lenguaje del mockup. */
const STAGE_ICON: Record<StageState, LucideIcon> = {
  passed: Check,
  current: Play,
  locked: Lock,
  goal: Trophy,
};

const STAGE_ICON_BOX: Record<StageState, string> = {
  passed: 'bg-surface-muted',
  current: 'bg-primary',
  locked: 'bg-surface-muted',
  goal: 'bg-surface-muted',
};

const STAGE_CARD: Record<StageState, string> = {
  passed: 'bg-surface',
  current: 'bg-surface-muted',
  locked: 'bg-canvas opacity-75',
  goal: 'bg-canvas opacity-60',
};

const STAGE_ORDER_TEXT: Record<StageState, string> = {
  passed: 'text-text-muted',
  current: 'text-primary',
  locked: 'text-text-muted',
  goal: 'text-text-muted',
};

const STAGE_NAME_TEXT: Record<StageState, string> = {
  passed: 'text-text',
  current: 'text-text',
  locked: 'text-text-muted',
  goal: 'text-text-muted',
};

const STAGE_PILL: Record<StageState, { box: string; text: string }> = {
  passed: { box: 'bg-surface-muted', text: 'text-text' },
  current: { box: 'bg-primary', text: 'text-on-primary' },
  locked: { box: 'bg-surface-muted', text: 'text-text-muted' },
  goal: { box: 'bg-surface-muted', text: 'text-primary' },
};

/** Etiqueta del badge de cada estado, sin `labelTechnical` (la variante ya va en mayúsculas). */
function stageBadgeLabel(state: StageState): string {
  switch (state) {
    case 'passed':
      return 'Superado 2/2 ✓';
    case 'current':
      return `Actual · ${OBJECTIVE_PROGRESS_PERCENT}%`;
    case 'locked':
      return 'Bloqueado';
    case 'goal':
      return 'Meta final';
  }
}

/** Chip de estado de la etapa; el color nunca comunica estado por sí solo (spec 0002). */
function StatePill({ state, label }: { state: StageState; label: string }) {
  return (
    <Box className={cn('shrink-0 rounded-sm px-sm py-xs', STAGE_PILL[state].box)}>
      <Text variant="labelTechnical" className={STAGE_PILL[state].text}>
        {label}
      </Text>
    </Box>
  );
}

/** Tarjeta de una etapa de la escalera con su estado, su criterio y su progreso. */
function StageCard({
  skillId,
  stage,
  state,
  currentStage,
  colors,
}: {
  skillId: string;
  stage: SkillStage;
  state: StageState;
  currentStage: number;
  colors: ReturnType<typeof useIconColors>;
}) {
  const isCurrent = state === 'current';
  const iconColor = isCurrent
    ? colors.onPrimary
    : state === 'passed'
      ? colors.primary
      : colors.muted;
  const isFirstLocked = state === 'locked' && stage.order === currentStage + 1;

  return (
    <Box
      accessible
      accessibilityRole="text"
      accessibilityLabel={`${stage.name}, ${formatStageCriterion(stage.criterion)}, ${STAGE_STATE_LABEL[state]}`}
      className={cn('gap-sm overflow-hidden rounded-base p-sm', STAGE_CARD[state])}
      testID={`library-skill-${skillId}-stage-${stage.order}`}
    >
      {isCurrent ? <View className="absolute bottom-0 left-0 top-0 w-xs bg-primary" /> : null}

      <Stack
        direction="row"
        className={cn('items-center justify-between gap-sm', isCurrent && 'pl-xs')}
      >
        <Stack direction="row" gap="sm" className="min-w-0 flex-1 items-center">
          <Box
            className={cn(
              'h-lg w-lg shrink-0 items-center justify-center rounded-base',
              STAGE_ICON_BOX[state],
            )}
          >
            <Icon icon={STAGE_ICON[state]} size={16} color={iconColor} />
          </Box>
          <Stack gap="xs" className="min-w-0 flex-1">
            <Text variant="labelTechnical" className={STAGE_ORDER_TEXT[state]}>
              {`Etapa ${twoDigits(stage.order)}`}
            </Text>
            <Text variant="labelCode" className={STAGE_NAME_TEXT[state]} numberOfLines={1}>
              {stage.name}
            </Text>
          </Stack>
        </Stack>

        <StatePill state={state} label={stageBadgeLabel(state)} />
      </Stack>

      {isCurrent ? (
        <Stack gap="xs" className="pl-lg">
          <ProgressIndicator value={OBJECTIVE_PROGRESS_PERCENT} max={100} role="active" />
          <Stack direction="row" className="items-center justify-between gap-sm">
            <Text variant="labelTechnical" className="text-text-muted">
              {`Criterio: 2 sesiones ≥ ${criterionThreshold(stage.criterion)}`}
            </Text>
            <Text variant="labelTechnical" className="text-primary">
              {`${OBJECTIVE_SESSIONS_MET} requeridas`}
            </Text>
          </Stack>
        </Stack>
      ) : null}

      {state === 'passed' ? (
        <Box className="pl-lg">
          <ProgressIndicator value={1} max={1} role="confirmed" />
        </Box>
      ) : null}

      {isFirstLocked ? (
        <Text variant="labelTechnical" className="pl-lg text-text-muted">
          {`Requiere validación completa de la etapa ${twoDigits(currentStage)}`}
        </Text>
      ) : null}
    </Box>
  );
}

/** Secuencia de validación de las 2 sesiones consecutivas (marcador del mockup). */
function ValidationSequence({
  skillId,
  colors,
}: {
  skillId: string;
  colors: ReturnType<typeof useIconColors>;
}) {
  return (
    <Stack gap="xs" testID={`skills-validation-${skillId}`}>
      <Stack
        direction="row"
        className="items-center justify-between gap-sm rounded-base bg-surface p-sm"
      >
        <Stack direction="row" gap="sm" className="min-w-0 flex-1 items-center">
          <Box className="h-lg w-lg shrink-0 items-center justify-center rounded-base bg-error-container">
            <Icon icon={X} size={14} color={colors.error} />
          </Box>
          <Stack gap="xs" className="min-w-0 flex-1">
            <Text variant="labelTechnical" className="text-text-muted">
              Sesión 1 · previa (48h)
            </Text>
            <Text variant="labelCode" className="text-text">
              12s, 12s, 11s
            </Text>
          </Stack>
        </Stack>
        <Box className="shrink-0 rounded-sm bg-error-container px-sm py-xs">
          <Text variant="labelTechnical" className="text-error">
            Falló por 1s
          </Text>
        </Box>
      </Stack>

      <Stack
        direction="row"
        className="items-center justify-between gap-sm rounded-base bg-surface-muted p-sm"
      >
        <Stack direction="row" gap="sm" className="min-w-0 flex-1 items-center">
          <Box className="h-lg w-lg shrink-0 items-center justify-center rounded-base bg-primary">
            <Text variant="labelTechnical" className="text-on-primary">
              2
            </Text>
          </Box>
          <Stack gap="xs" className="min-w-0 flex-1">
            <Text variant="labelTechnical" className="text-primary">
              Sesión 2 · hoy
            </Text>
            <Text variant="labelCode" className="text-text-muted">
              Registro pendiente
            </Text>
          </Stack>
        </Stack>
        <Box className="shrink-0 rounded-sm bg-primary px-sm py-xs">
          <Text variant="labelTechnical" className="text-on-primary">
            Programada
          </Text>
        </Box>
      </Stack>
    </Stack>
  );
}

/** Hoja del skill: código, material, etapa, progreso global, criterio y telemetría. */
function SkillHero({
  skill,
  index,
  currentStage,
  colors,
}: {
  skill: Skill;
  index: number;
  currentStage: number;
  colors: ReturnType<typeof useIconColors>;
}) {
  const totalStages = skill.stages.length;
  // El modo (isométrico/dinámico) sale de la métrica del criterio de la etapa actual.
  const metric =
    skill.stages.find((stage) => stage.order === currentStage)?.criterion.metric ??
    skill.stages[0]?.criterion.metric ??
    'seconds';
  const material = skillMaterial(skill);
  const current = skill.stages.find((stage) => stage.order === currentStage);

  return (
    <Box
      className="gap-md overflow-hidden rounded-base bg-surface p-md"
      testID={`skills-detail-${skill.id}`}
    >
      <Stack direction="row" className="items-start justify-between gap-sm">
        <Stack gap="xs" className="flex-1">
          <Stack direction="row" gap="sm" className="items-center">
            <Text variant="labelTechnical" className="text-primary">
              {`[${skillCode(index, metric)}]`}
            </Text>
            {material ? (
              <>
                <View className="h-xs w-xs rounded-sm bg-text-muted" />
                <Text variant="labelTechnical" className="text-text-muted">
                  {material}
                </Text>
              </>
            ) : null}
          </Stack>
          <Text variant="headlineLg" className="uppercase">
            {skill.name}
          </Text>
          <Text
            variant="labelCode"
            className="text-primary"
            testID={`skills-detail-${skill.id}-stage`}
          >
            {`Etapa ${twoDigits(currentStage)} de ${twoDigits(totalStages)}`}
          </Text>
        </Stack>

        <Box className="shrink-0 items-end gap-xs rounded-base bg-surface-muted px-sm py-xs">
          <Text variant="labelTechnical" className="text-text-muted">
            Progreso global
          </Text>
          <Text
            variant="headlineSm"
            className="text-primary"
            testID={`skills-detail-${skill.id}-progress`}
          >
            {formatGlobalProgress(currentStage, totalStages)}
          </Text>
        </Box>
      </Stack>

      {/* Diagrama biomecánico (marcador del mockup). */}
      <Box
        className="relative h-36 items-center justify-center overflow-hidden rounded-base bg-surface-muted"
        testID={`skills-detail-${skill.id}-diagram`}
      >
        <Text variant="labelTechnical" className="text-text-muted">
          Diagrama biomecánico
        </Text>
        <Box className="absolute left-sm top-sm flex-row items-center gap-xs rounded-base bg-canvas px-sm py-xs">
          <View className="h-xs w-xs rounded-sm bg-secondary" />
          <Text variant="labelTechnical" className="text-secondary">
            Ángulo cadera: 90°
          </Text>
        </Box>
        <Box className="absolute bottom-sm right-sm rounded-base bg-canvas px-sm py-xs">
          <Text variant="labelTechnical" className="text-text-muted">
            Retract: 100% · escápulas
          </Text>
        </Box>
      </Box>

      {current ? (
        <Box
          className="gap-sm rounded-base bg-surface-muted p-sm"
          testID={`skills-criterion-${skill.id}`}
        >
          <Stack
            direction="row"
            className="items-center justify-between gap-sm rounded-base bg-surface px-sm py-xs"
          >
            <Stack direction="row" gap="xs" className="items-center">
              <Icon icon={Flag} size={14} color={colors.primary} />
              <Text variant="labelTechnical" className="text-primary">
                Criterio de etapa objetivo
              </Text>
            </Stack>
            <Text variant="labelCode" className="text-text-muted">
              {`[STG.${twoDigits(current.order)}-REQ]`}
            </Text>
          </Stack>

          <Text variant="bodyMd" className="px-xs text-text">
            {'Objetivo: '}
            <Text variant="bodyMd" className="text-primary">
              {formatStageCriterion(current.criterion)}
            </Text>
            {' en '}
            <Text variant="bodyMd" className="text-primary underline">
              2 sesiones consecutivas
            </Text>
            {'.'}
          </Text>

          <ValidationSequence skillId={skill.id} colors={colors} />
        </Box>
      ) : null}

      <Stack direction="row" gap="xs" testID={`skills-stats-${skill.id}`}>
        {QUICK_STATS.map((stat) => (
          <Box key={stat.label} className="flex-1 gap-xs rounded-base bg-surface-muted p-sm">
            <Text variant="labelTechnical" className="text-text-muted" numberOfLines={1}>
              {stat.label}
            </Text>
            <Text variant="labelCode" className="text-text">
              {stat.value}
            </Text>
          </Box>
        ))}
      </Stack>
    </Box>
  );
}

/**
 * Vista «Escaleras» del hub Skills (spec 0003, ticket #86): el detalle del skill elegido y su
 * escalera de progresión, siguiendo la hoja *Skills · Escalera de Progresión* del rediseño.
 *
 * Lee `GET /catalog/skills` (escalera y material de cada skill), `GET /catalog/progress` (etapa
 * actual por skill) y `GET /profile/objective` (skill objetivo, para destacarlo y abrirlo por
 * defecto). El selector cambia de skill; el detalle muestra su código, su material, su etapa
 * actual, su progreso global y su criterio, y pinta cada etapa de la escalera con su estado.
 * Solo lectura.
 */
export function SkillsView() {
  const [state, setState] = useState<LoadState>({ status: 'loading' });
  const [selectedSkillId, setSelectedSkillId] = useState<string | null>(null);
  const [testNotice, setTestNotice] = useState(false);
  const colors = useIconColors();

  const load = useCallback((signal?: AbortSignal) => {
    Promise.all([fetchSkillCatalog(signal), fetchSkillProgress(signal), fetchObjective(signal)])
      .then(([skills, progress, objective]) =>
        setState({
          status: 'ready',
          skills,
          currentStageBySkill: new Map(progress.map((entry) => [entry.skillId, entry.stageOrder])),
          objectiveSkillId: objective?.skillId ?? null,
        }),
      )
      .catch((error: unknown) => {
        if (error instanceof Error && error.name === 'AbortError') {
          return;
        }
        setState({ status: 'error', message: messageOf(error) });
      });
  }, []);

  useEffect(() => {
    const controller = new AbortController();
    load(controller.signal);
    return () => controller.abort();
  }, [load]);

  const retry = useCallback(() => {
    setState({ status: 'loading' });
    load();
  }, [load]);

  if (state.status === 'loading') {
    return <Loading label="Leyendo las escaleras de skills…" testID="library-skills-loading" />;
  }

  if (state.status === 'error') {
    return (
      <Stack gap="sm">
        <Banner role="error" message={state.message} testID="library-skills-error" />
        <Button onPress={retry} testID="library-skills-retry">
          Reintentar
        </Button>
      </Stack>
    );
  }

  if (state.skills.length === 0) {
    return (
      <EmptyState
        title="Sin skills"
        description="El catálogo todavía no tiene skills."
        testID="library-skills-empty"
      />
    );
  }

  const objectiveSkillId = state.objectiveSkillId;
  const skill =
    state.skills.find((candidate) => candidate.id === selectedSkillId) ??
    state.skills.find((candidate) => candidate.id === objectiveSkillId) ??
    state.skills[0];

  const currentStage = state.currentStageBySkill.get(skill.id) ?? 1;
  const totalStages = skill.stages.length;

  return (
    <Stack gap="lg">
      <Stack gap="xs" testID="skills-selector">
        <Stack direction="row" className="items-center justify-between gap-sm">
          <Text variant="labelTechnical" className="text-text-muted">
            {`Índice de destrezas [${twoDigits(state.skills.length)}]`}
          </Text>
          <Text variant="labelCode" className="text-primary">
            {SHEET_REVISION}
          </Text>
        </Stack>
        <ScrollView
          horizontal
          showsHorizontalScrollIndicator={false}
          contentContainerClassName="gap-xs py-xs"
        >
          {state.skills.map((candidate) => {
            const isSelected = candidate.id === skill.id;
            const isObjective = candidate.id === objectiveSkillId;

            return (
              <Chip
                key={candidate.id}
                label={isObjective ? `${candidate.name} · objetivo` : candidate.name}
                selected={isSelected}
                onChange={() => setSelectedSkillId(candidate.id)}
                className={isObjective && !isSelected ? 'border-primary' : undefined}
                testID={`skills-selector-${candidate.id}`}
              />
            );
          })}
        </ScrollView>
      </Stack>

      <SkillHero
        skill={skill}
        index={state.skills.indexOf(skill) + 1}
        currentStage={currentStage}
        colors={colors}
      />

      <Stack gap="sm">
        <Stack direction="row" className="items-center justify-between gap-sm">
          <Stack direction="row" gap="sm" className="items-center">
            <Icon icon={TrendingUp} size={18} color={colors.primary} />
            <Text variant="headlineSm" className="uppercase" accessibilityRole="header">
              Escalera de progresión
            </Text>
          </Stack>
          <Text variant="labelTechnical" className="text-text-muted">
            V-hierarchy
          </Text>
        </Stack>
        <Stack gap="xs" testID={`library-skill-${skill.id}-ladder`}>
          {skill.stages.map((stage) => (
            <StageCard
              key={stage.order}
              skillId={skill.id}
              stage={stage}
              state={stageState(stage.order, currentStage, totalStages)}
              currentStage={currentStage}
              colors={colors}
            />
          ))}
        </Stack>
      </Stack>

      <Box className="gap-sm rounded-base bg-surface p-sm" testID="skills-regression-protocol">
        <Stack direction="row" gap="sm" className="items-center">
          <View className="h-xs w-xs rounded-sm bg-text-muted" />
          <Text variant="labelTechnical" className="text-text-muted">
            Protocolo de regresión técnica
          </Text>
        </Stack>
        <Stack direction="row" gap="sm" className="items-start rounded-base bg-surface-muted p-sm">
          <Icon icon={Info} size={20} color={colors.muted} />
          <Stack gap="xs" className="flex-1">
            <Text variant="bodySm" className="text-text-muted">
              <Text variant="bodySm" className="text-text">
                Criterio de estancamiento:{' '}
              </Text>
              {'si fallas el criterio durante '}
              <Text variant="bodySm" className="text-primary">
                3 semanas consecutivas
              </Text>
              {', el sistema activará una descarga técnica a '}
              <Text variant="bodySm" className="text-text">
                Tuck básico lastrado (+5 kg)
              </Text>
              {' para regenerar el tendón distal del bíceps.'}
            </Text>
            <Stack direction="row" gap="sm" className="items-center">
              <Text variant="labelTechnical" className="text-text-muted">
                Estado actual:
              </Text>
              <Text variant="labelTechnical" className="text-primary">
                Semana 1 de 3 [dentro de tolerancia]
              </Text>
            </Stack>
          </Stack>
        </Stack>
      </Box>

      <Stack gap="sm">
        <Button
          onPress={() => setTestNotice(true)}
          testID="skills-test-criterion"
        >{`Testear criterio en próxima sesión →`}</Button>
        <Stack direction="row" className="items-center justify-between px-xs">
          <Text variant="labelTechnical" className="text-text-muted">
            Modalidad: evaluación formal
          </Text>
          <Text variant="labelTechnical" className="text-text-muted">
            Calibración auto
          </Text>
        </Stack>
        {testNotice ? (
          <Banner
            role="inactive"
            message="El criterio se evalúa al registrar la sesión en Entreno."
            testID="skills-test-notice"
          />
        ) : null}
      </Stack>
    </Stack>
  );
}
