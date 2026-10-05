import { View, type ViewProps } from 'react-native';

import {
  DEFAULT_ROLE_LABEL,
  ROLE_BORDER_CLASS,
  ROLE_DOT_CLASS,
  type SemanticRole,
} from '@/design-system/semantic';
import { Text } from '@/design-system/Text';
import { cn } from '@/design-system/utils/cn';

/**
 * Acabado del badge, pensado para leerse tanto sobre `canvas` (nivel 0) como
 * sobre `surface` (nivel 1):
 *
 * - `solid` (default): chip relleno con el token de fondo del rol y texto
 *   legible encima; el acento del rol queda en el borde y el punto.
 * - `outline`: chip sin relleno (`bg-transparent`), texto neutro de alto
 *   contraste y el rol se lee por el borde y el punto.
 */
export type StatusVariant = 'solid' | 'outline';

/**
 * Token de fondo de la variante `solid`. `error` es un color de texto en la
 * spec, así que su relleno es `errorContainer` (con `onErrorContainer` encima),
 * y el token `error` puro queda como acento del rol. Es específico del badge
 * (inversión de relleno), no del mapa genérico de `semantic.ts`.
 */
const SOLID_FILL_CLASS: Record<SemanticRole, string> = {
  active: 'bg-primary',
  confirmed: 'bg-secondary',
  error: 'bg-error-container',
  inactive: 'bg-surface-muted',
};

/** Texto legible sobre el relleno de cada rol. */
const SOLID_LABEL_CLASS: Record<SemanticRole, string> = {
  active: 'text-on-primary',
  confirmed: 'text-on-secondary',
  error: 'text-on-error-container',
  inactive: 'text-text-muted',
};

/** Props públicas de `StatusBadge`. */
export type StatusBadgeProps = Omit<ViewProps, 'children' | 'role'> & {
  /** Rol semántico de estado. Default: `active`. */
  role?: SemanticRole;
  /**
   * Etiqueta textual. Si se omite (o va vacía) se usa la etiqueta por defecto
   * del rol; el badge nunca comunica el estado solo por color.
   */
  label?: string;
  /** Acabado del badge. Default: `solid`. */
  variant?: StatusVariant;
  /** Muestra el punto de estado, decorativo. Default: `true`. */
  showDot?: boolean;
  /** Clases del contenedor; se combinan con `cn()` para permitir sobrescritura. */
  className?: string;
  /** `testID` del contenedor; el punto deriva `<testID>-dot`. */
  testID?: string;
};

/**
 * Badge compacto de estado del design system (spec 0002, ticket #43).
 *
 * Comunica el estado con **rol semántico explícito + etiqueta textual**: el rol
 * elige los tokens de color (vía `semantic.ts`) y la etiqueta siempre está
 * presente (nunca el color solo). La profundidad sale del borde y las capas
 * tonales, **sin sombras**.
 *
 * ```tsx
 * <StatusBadge role="confirmed" />
 * <StatusBadge role="error" label="Sobrecarga" variant="outline" />
 * ```
 */
export function StatusBadge({
  role = 'active',
  label,
  variant = 'solid',
  showDot = true,
  className,
  testID,
  ...rest
}: StatusBadgeProps) {
  const resolvedLabel = label && label.trim().length > 0 ? label : DEFAULT_ROLE_LABEL[role];

  const isSolid = variant === 'solid';

  return (
    <View
      accessible
      accessibilityRole="text"
      accessibilityLabel={resolvedLabel}
      className={cn(
        'flex-row items-center gap-xs self-start rounded-sm border px-sm py-xs',
        isSolid
          ? cn(SOLID_FILL_CLASS[role], ROLE_BORDER_CLASS[role])
          : cn('bg-transparent', ROLE_BORDER_CLASS[role]),
        className,
      )}
      testID={testID}
      {...rest}
    >
      {showDot ? (
        <View
          accessible={false}
          importantForAccessibility="no"
          accessibilityElementsHidden
          className={cn('h-xs w-xs rounded-sm', ROLE_DOT_CLASS[role])}
          testID={testID ? `${testID}-dot` : undefined}
        />
      ) : null}
      <Text variant="labelTechnical" className={isSolid ? SOLID_LABEL_CLASS[role] : 'text-text'}>
        {resolvedLabel}
      </Text>
    </View>
  );
}
