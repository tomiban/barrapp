import type { ReactNode } from 'react';
import { Pressable, View } from 'react-native';

import {
  DEFAULT_ROLE_LABEL,
  ROLE_BG_CLASS,
  ROLE_TEXT_CLASS,
  type SemanticRole,
} from '@/design-system/semantic';
import { Text } from '../Text';
import { cn } from '../utils/cn';

/** Props públicas de `ListRow`. */
export type ListRowProps = {
  /** Texto principal de la fila. */
  title: string;
  /** Texto secundario (metadata de la fila). */
  subtitle?: string;
  /** Slot inicial (p. ej. el número de semana o un icono). */
  leading?: ReactNode;
  /** Slot final (p. ej. un `MetricCounter`, un `StatusBadge` o un chevron). */
  trailing?: ReactNode;
  /** Si se provee, la fila se vuelve pulsable y se anuncia como botón. */
  onPress?: () => void;
  /** Rol semántico de estado; dibuja el marcador y la etiqueta textual. */
  role?: SemanticRole;
  /** Etiqueta textual del estado; si se omite, se usa la del rol. */
  stateLabel?: string;
  /**
   * Marca la fila como última de la lista para **omitir el separador
   * inferior** y no dejar una hairline suelta al final.
   */
  last?: boolean;
  /** Clases del contenedor; se combinan con `cn()` para permitir sobrescritura. */
  className?: string;
  /** `testID` de la fila; el marcador de estado deriva `<testID>-state`. */
  testID?: string;
};

/**
 * Fila para listas de semanas, sesiones y ejercicios (spec 0002, ticket #44).
 *
 * Separadores **hairline** (`border-b border-border`) para evitar sombras: la
 * profundidad sale de bordes y capas tonales. Cada fila puede mostrar un estado
 * semántico con su **etiqueta textual** (nunca solo color) y anunciarlo al
 * lector de pantalla; el slot `trailing` queda libre para métricas, badges o
 * chevrons.
 *
 * Alcanza el **tamaño táctil secundario** de la spec (48 dp,
 * `min-h-control-secondary`) para que pulsar una fila sin subtítulo siga siendo
 * cómodo.
 */
export function ListRow({
  title,
  subtitle,
  leading,
  trailing,
  onPress,
  role,
  stateLabel,
  last = false,
  className,
  testID,
}: ListRowProps) {
  const resolvedStateLabel = role
    ? stateLabel && stateLabel.trim().length > 0
      ? stateLabel
      : DEFAULT_ROLE_LABEL[role]
    : undefined;

  /*
   * El estado se anuncia textualmente (nunca solo color): se compone el nombre
   * accesible de la fila con título, subtítulo y la etiqueta del estado para que
   * el lector de pantalla lo lea entero al agrupar la fila.
   */
  const accessibilityLabel = role
    ? [title, subtitle, resolvedStateLabel].filter(Boolean).join(', ')
    : undefined;

  const rowClassName = cn(
    'min-h-control-secondary flex-row items-center gap-md px-md py-sm',
    last ? undefined : 'border-b border-border',
    className,
  );

  const content = (
    <>
      {role ? (
        <View
          testID={testID ? `${testID}-state` : undefined}
          accessible={false}
          importantForAccessibility="no"
          accessibilityElementsHidden
          className={cn('w-xs self-stretch rounded-sm', ROLE_BG_CLASS[role])}
        />
      ) : null}
      {leading}
      <View className="flex-1 gap-xs">
        <Text variant="headlineSm">{title}</Text>
        {subtitle ? (
          <Text variant="bodySm" className="text-text-muted">
            {subtitle}
          </Text>
        ) : null}
      </View>
      {role ? (
        <Text variant="labelTechnical" className={ROLE_TEXT_CLASS[role]}>
          {resolvedStateLabel}
        </Text>
      ) : null}
      {trailing}
    </>
  );

  if (onPress) {
    return (
      <Pressable
        testID={testID}
        accessible
        accessibilityRole="button"
        accessibilityLabel={accessibilityLabel}
        onPress={onPress}
        className={rowClassName}
      >
        {content}
      </Pressable>
    );
  }

  return (
    <View
      testID={testID}
      accessible
      accessibilityRole="text"
      accessibilityLabel={accessibilityLabel}
      className={rowClassName}
    >
      {content}
    </View>
  );
}
