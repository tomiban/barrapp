import { useCallback, useEffect, useState } from 'react';

import { messageOf } from '@/api/messageOf';
import { fetchPlan, type Plan } from '@/api/plan';
import { fetchSessionLogs, registerSessionLog, type SessionLog } from '@/api/sessionLogs';
import { Button } from '@/design-system/Button';
import { Banner, Loading } from '@/design-system/Feedback';
import { Stack } from '@/design-system/layout';
import { SectionHeader } from '@/design-system/ListRow';
import { Header, Screen } from '@/design-system/Navigation';
import {
  SessionLoggingView,
  type ExerciseSetsPayload,
  type SaveFeedback,
} from '@/features/sessionLog/SessionLoggingView';
import { SoloSessionView } from '@/features/suelta/SoloSessionView';
import { SoloSessionHistory } from '@/features/suelta/SoloSessionHistory';

type LoadState =
  | { status: 'loading' }
  | { status: 'ready'; plan: Plan; logs: SessionLog[] }
  | { status: 'error'; message: string };

/**
 * Pantalla Entrenar: lee `GET /plan` (el motor lo genera a partir del perfil y el objetivo) y
 * `GET /session-logs`, y delega el registro set a set en `SessionLoggingView`. Al guardar envía
 * un `POST /session-logs` por ejercicio y relee los registros del servidor, sin fiarse del
 * estado local (spec 0001, US-34). Debajo queda el generador de sesión suelta (#28), que no
 * depende del plan.
 */
export default function TrainScreen() {
  const [state, setState] = useState<LoadState>({ status: 'loading' });
  const [saving, setSaving] = useState(false);
  const [feedback, setFeedback] = useState<SaveFeedback | null>(null);

  const load = useCallback((signal?: AbortSignal) => {
    Promise.all([fetchPlan(signal), fetchSessionLogs(signal)])
      .then(([plan, logs]) => setState({ status: 'ready', plan, logs }))
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
    setFeedback(null);
    load();
  }, [load]);

  const handleSave = useCallback(async (day: number, exercises: ExerciseSetsPayload[]) => {
    setSaving(true);
    setFeedback(null);
    try {
      for (const exercise of exercises) {
        await registerSessionLog({
          exerciseId: exercise.exerciseId,
          sessionDay: day,
          sets: exercise.sets,
        });
      }

      const logs = await fetchSessionLogs();
      setState((current) => (current.status === 'ready' ? { ...current, logs } : current));
      setFeedback({ role: 'confirmed', message: 'Registro de la sesión guardado.' });
    } catch (error) {
      setFeedback({ role: 'error', message: messageOf(error) });
    } finally {
      setSaving(false);
    }
  }, []);

  return (
    <Screen header={<Header title="Entrenar" />} testID="train-screen">
      {state.status === 'loading' ? (
        <Loading label="Preparando la sesión…" testID="train-loading" />
      ) : null}

      {state.status === 'error' ? (
        <Stack gap="sm">
          <Banner role="error" message={state.message} testID="train-error" />
          <Button onPress={retry} testID="train-retry">
            Reintentar
          </Button>
        </Stack>
      ) : null}

      {state.status === 'ready' ? (
        <SessionLoggingView
          plan={state.plan}
          logs={state.logs}
          saving={saving}
          feedback={feedback}
          onSave={handleSave}
        />
      ) : null}

      <Stack gap="sm">
        <SectionHeader label="Sesión suelta" testID="train-suelta-header" />
        <SoloSessionView />
        <SoloSessionHistory />
      </Stack>
    </Screen>
  );
}
