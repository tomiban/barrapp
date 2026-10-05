import { useEffect, useRef, useState } from 'react';
import { Pressable } from 'react-native';

import { Box } from '@/design-system/layout';
import { MetricCounter } from '@/design-system/MetricCounter';
import { cn } from '@/design-system/utils/cn';

/** Cadencia del tick en milisegundos: la cuenta atrás avanza en segundos. */
const TICK_MS = 1000;

/** Unidad del readout: holds y TUT se miden en segundos (spec 0002). */
const UNIT = 'SEC';

/** Etiqueta por defecto cuando el consumidor no da una. */
const DEFAULT_LABEL = 'Tiempo';

/** Sanea la duración: entero de segundos >= 0. `Infinity`/`NaN`/negativos → 0. */
function normalizeDuration(durationSeconds: number): number {
  if (!Number.isFinite(durationSeconds) || durationSeconds <= 0) {
    return 0;
  }
  return Math.floor(durationSeconds);
}

/** Props públicas de `Timer`. */
export type TimerProps = {
  /** Duración total de la cuenta atrás, en segundos. */
  durationSeconds: number;
  /** Si la cuenta atrás está en marcha. Al pasar a `false` se pausa. Default `false`. */
  running?: boolean;
  /** Se invoca una sola vez cuando la cuenta atrás llega a 0. */
  onComplete?: () => void;
  /** Micro-label del readout. Default `Tiempo`. */
  label?: string;
  /** Si se provee, el contador se vuelve pulsable (p. ej. alternar marcha). */
  onPress?: () => void;
  /** Clases del contenedor; se combinan con `cn()` para permitir sobrescritura. */
  className?: string;
  /** `testID` del contenedor; el readout deriva `<testID>-metric-value`. */
  testID?: string;
};

/**
 * Cuenta atrás del design system (spec 0002, ticket #41), también conocida como
 * `Countdown`.
 *
 * Readout monolítico en segundos reutilizando `MetricCounter`: números
 * monoespaciados tabulares (`headlineMetric`) que no bailan durante la cuenta, y
 * color `primary` (rol activo). La cuenta avanza cada segundo mientras
 * `running`; se pausa al pasar a `false`, se reinicia al cambiar
 * `durationSeconds` (o al volver a arrancar tras completarse) y dispara
 * `onComplete` una sola vez al llegar a 0. **Sin sombras.**
 */
export function Timer({
  durationSeconds,
  running = false,
  onComplete,
  label,
  onPress,
  className,
  testID,
}: TimerProps) {
  const duration = normalizeDuration(durationSeconds);

  const [remaining, setRemaining] = useState(duration);
  const [prevDuration, setPrevDuration] = useState(duration);
  const [prevRunning, setPrevRunning] = useState(running);

  /*
   * Guarda de "ya completado": evita que `onComplete` se dispare más de una vez
   * por cuenta. Se marca de antemano si la duración arranca en 0 (no hay cuenta
   * que completar). Sólo se toca dentro de efectos, nunca en render.
   */
  const completedRef = useRef(duration <= 0);

  /*
   * Ajuste en render (patrón "storing information from previous renders"): al
   * cambiar la duración, el contador se reinicia antes de comprometer el render,
   * de modo que los efectos no vean un `remaining` obsoleto y disparen
   * `onComplete` durante un reinicio.
   */
  if (duration !== prevDuration) {
    setPrevDuration(duration);
    setRemaining(duration);
  }

  /*
   * Reanudar tras completarse: un flanco `false → true` de `running` con el
   * contador a 0 reinicia desde la duración, permitiendo repetir sin desmontar.
   */
  if (running !== prevRunning) {
    setPrevRunning(running);
    if (running && remaining === 0 && duration > 0) {
      setRemaining(duration);
    }
  }

  /** Sólo hay tick si está en marcha y queda tiempo por descontar. */
  const isTicking = running && remaining > 0;

  /*
   * El intervalo vive mientras corre y queda tiempo; se limpia al pausar,
   * completar o desmontar. El estado se actualiza por función para no depender
   * de `remaining` en las dependencias y no recrear el intervalo cada segundo.
   */
  useEffect(() => {
    if (!isTicking) {
      return undefined;
    }

    const id = setInterval(() => {
      setRemaining((current) => (current > 0 ? current - 1 : 0));
    }, TICK_MS);

    return () => clearInterval(id);
  }, [isTicking]);

  // Rearma la guarda al cambiar la duración; una duración 0 nunca completa.
  useEffect(() => {
    completedRef.current = duration <= 0;
  }, [duration]);

  // Vuelve a haber tiempo (reinicio o replay): se permite completar de nuevo.
  useEffect(() => {
    if (remaining > 0) {
      completedRef.current = false;
    }
  }, [remaining]);

  // `onComplete` se dispara exactamente una vez al llegar a 0 en marcha.
  useEffect(() => {
    if (remaining !== 0 || !running || completedRef.current) {
      return;
    }
    completedRef.current = true;
    onComplete?.();
  }, [remaining, running, onComplete]);

  const readout = (
    <MetricCounter
      label={label ?? DEFAULT_LABEL}
      value={remaining}
      unit={UNIT}
      tone="active"
      testID={testID ? `${testID}-metric` : undefined}
    />
  );

  if (onPress) {
    return (
      <Pressable
        accessibilityRole="button"
        accessibilityLabel={`${label ?? DEFAULT_LABEL}: ${remaining} ${UNIT}`}
        className={cn(className)}
        onPress={onPress}
        testID={testID}
      >
        {readout}
      </Pressable>
    );
  }

  return (
    <Box className={cn(className)} testID={testID}>
      {readout}
    </Box>
  );
}

/** Alias del nombre de la spec (`Timer/Countdown`): es el mismo componente. */
export const Countdown = Timer;
