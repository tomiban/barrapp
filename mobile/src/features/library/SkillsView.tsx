import { useCallback, useEffect, useState } from 'react';
import { ScrollView } from 'react-native';
import Constants from 'expo-constants';

import { fetchExerciseCatalog } from '@/api/catalog/exercises';
import {
  fetchSkillCatalog,
  type PatternRoutine,
  type Skill,
  type StageCriterion,
} from '@/api/catalog/skills';
import { fetchSkillProgress } from '@/api/catalog/progress';
import { fetchObjective } from '@/api/objective';
import { messageOf } from '@/api/messageOf';
import { Button } from '@/design-system/Button';
import { Chip } from '@/design-system/Chip';
import { Banner, EmptyState, Loading } from '@/design-system/Feedback';
import { Box, Stack } from '@/design-system/layout';
import { ListRow, SectionHeader } from '@/design-system/ListRow';
import { Text } from '@/design-system/Text';
import { buildExerciseNameIndex } from '@/features/library/exerciseNames';
import { formatRoutineItem } from '@/features/library/routineItemText';
import {
  formatGlobalProgress,
  formatStageCriterion,
  skillCode,
  skillMaterial,
  STAGE_STATE_LABEL,
  STAGE_STATE_ROLE,
  stageState,
} from '@/features/library/skillLadder';

type LoadState =
  | { status: 'loading' }
  | {
      status: 'ready';
      skills: Skill[];
      exerciseNames: Map<string, string>;
      currentStageBySkill: Map<string, number>;
      objectiveSkillId: string | null;
    }
  | { status: 'error'; message: string };

/** Revisión técnica de la hoja de skills, chrome del mockup (`SYS.CAL-v2.4`). */
const SHEET_REVISION = `SYS.CAL · v${Constants.expoConfig?.version ?? '1.0.0'}`;

/** Orden a dos dígitos, p. ej. `02`. */
function twoDigits(value: number): string {
  return String(Math.max(Math.trunc(value), 0)).padStart(2, '0');
}

/** Bloques de una rutina de patrón: modelos con sus filas. */
function PatternRoutineBlock({
  skillId,
  routine,
  nameOf,
}: {
  skillId: string;
  routine: PatternRoutine;
  nameOf: (exerciseId: string) => string;
}) {
  return (
    <Stack gap="xs" testID={`library-skill-${skillId}-routine-${routine.id}`}>
      <Text variant="labelTechnical" className="text-text-muted">
        {routine.name}
      </Text>
      {routine.items.map((item, index) => (
        <ListRow
          key={`${routine.id}-${item.exerciseId}-${index}`}
          title={nameOf(item.exerciseId)}
          subtitle={formatRoutineItem(item)}
          last={index === routine.items.length - 1}
        />
      ))}
    </Stack>
  );
}

/** Tarjeta de detalle del skill elegido: código, material, etapa actual y progreso global. */
function SkillDetail({
  skill,
  index,
  currentStage,
}: {
  skill: Skill;
  index: number;
  currentStage: number;
}) {
  const totalStages = skill.stages.length;
  // El modo (isométrico/dinámico) sale de la métrica del criterio; si la etapa actual no está en
  // la escalera, se usa la métrica de la primera etapa para no etiquetar mal el skill.
  const criterionMetric =
    skill.stages.find((stage) => stage.order === currentStage)?.criterion.metric ??
    skill.stages[0]?.criterion.metric ??
    'seconds';
  const material = skillMaterial(skill);

  return (
    <Box
      className="gap-md rounded-base border border-border bg-surface p-md"
      testID={`skills-detail-${skill.id}`}
    >
      <Stack direction="row" gap="sm" className="items-start justify-between">
        <Stack gap="xs" className="flex-1">
          <Stack direction="row" gap="sm" className="items-center">
            <Text variant="labelTechnical" className="text-primary">
              {`[${skillCode(index, criterionMetric)}]`}
            </Text>
            {material ? (
              <Text variant="labelTechnical" className="text-text-muted">
                {material}
              </Text>
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

        <Box className="items-end gap-xs rounded-base bg-surface-muted px-sm py-xs">
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
    </Box>
  );
}

/** Tarjeta del criterio de la etapa actual: la marca con sus series y las 2 sesiones consecutivas. */
function StageCriterionPanel({
  skillId,
  stageOrder,
  criterion,
}: {
  skillId: string;
  stageOrder: number;
  criterion: StageCriterion;
}) {
  return (
    <Box
      className="gap-sm rounded-base border border-border bg-surface p-md"
      testID={`skills-criterion-${skillId}`}
    >
      <Stack direction="row" gap="sm" className="items-center justify-between">
        <Text variant="labelTechnical" className="text-primary">
          Criterio de etapa objetivo
        </Text>
        <Text
          variant="labelCode"
          className="text-text-muted"
        >{`[STG.${twoDigits(stageOrder)}-REQ]`}</Text>
      </Stack>
      <Text variant="bodyMd" className="text-text">
        {'Objetivo: '}
        <Text variant="bodyMd" className="text-primary">
          {formatStageCriterion(criterion)}
        </Text>
        {' en 2 sesiones consecutivas.'}
      </Text>
    </Box>
  );
}

/**
 * Vista «Escaleras» del hub Skills (spec 0003, ticket #86): el detalle del skill elegido y su
 * escalera de progresión, siguiendo la hoja *Skills · Escalera de Progresión* del rediseño.
 *
 * Lee `GET /catalog/skills` (escalera y material de cada skill), `GET /catalog/progress` (etapa
 * actual del atleta en cada uno) y `GET /profile/objective` (skill objetivo, para destacarlo y
 * abrirlo por defecto). El selector cambia de skill; el detalle muestra su código, su material, su
 * etapa actual, su progreso global y su criterio, y pinta cada etapa de la escalera con su estado.
 * Las rutinas de patrón del skill quedan debajo, como material de apoyo. Solo lectura.
 */
export function SkillsView() {
  const [state, setState] = useState<LoadState>({ status: 'loading' });
  const [selectedSkillId, setSelectedSkillId] = useState<string | null>(null);

  const load = useCallback((signal?: AbortSignal) => {
    Promise.all([
      fetchSkillCatalog(signal),
      fetchExerciseCatalog(signal),
      fetchSkillProgress(signal),
      fetchObjective(signal),
    ])
      .then(([skills, catalog, progress, objective]) =>
        setState({
          status: 'ready',
          skills,
          exerciseNames: buildExerciseNameIndex(catalog, skills),
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
  const currentStageDefinition = skill.stages.find((stage) => stage.order === currentStage);
  const nameOf = (exerciseId: string) => state.exerciseNames.get(exerciseId) ?? exerciseId;

  return (
    <Stack gap="xl">
      <Stack gap="sm" testID="skills-selector">
        <Stack direction="row" gap="sm" className="items-center justify-between">
          <Text variant="labelTechnical" className="text-text-muted">
            {`Índice de destrezas [${twoDigits(state.skills.length)}]`}
          </Text>
          <Text variant="labelCode" className="text-text-muted">
            {SHEET_REVISION}
          </Text>
        </Stack>

        <ScrollView
          horizontal
          showsHorizontalScrollIndicator={false}
          contentContainerClassName="gap-sm"
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

      <SkillDetail
        skill={skill}
        index={state.skills.indexOf(skill) + 1}
        currentStage={currentStage}
      />

      {currentStageDefinition ? (
        <StageCriterionPanel
          skillId={skill.id}
          stageOrder={currentStageDefinition.order}
          criterion={currentStageDefinition.criterion}
        />
      ) : null}

      <Stack gap="sm">
        <SectionHeader
          label="Escalera de progresión"
          count={totalStages}
          testID={`library-skill-${skill.id}-ladder`}
        />
        {skill.stages.map((stage, index) => {
          const stageStatus = stageState(stage.order, currentStage, totalStages);

          return (
            <ListRow
              key={stage.order}
              leading={
                <Text variant="labelCode" className="text-text-muted">
                  {twoDigits(stage.order)}
                </Text>
              }
              title={stage.name}
              subtitle={formatStageCriterion(stage.criterion)}
              role={STAGE_STATE_ROLE[stageStatus]}
              stateLabel={STAGE_STATE_LABEL[stageStatus]}
              last={index === skill.stages.length - 1}
              testID={`library-skill-${skill.id}-stage-${stage.order}`}
            />
          );
        })}
      </Stack>

      {skill.patternRoutines.length > 0 ? (
        <Stack gap="sm">
          <SectionHeader
            label="Rutinas de patrón"
            count={skill.patternRoutines.length}
            testID={`library-skill-${skill.id}-routines`}
          />
          {skill.patternRoutines.map((routine) => (
            <PatternRoutineBlock
              key={routine.id}
              skillId={skill.id}
              routine={routine}
              nameOf={nameOf}
            />
          ))}
        </Stack>
      ) : null}
    </Stack>
  );
}
