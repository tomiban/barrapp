import { Children, type ReactNode } from 'react';
import { View } from 'react-native';

import { Text } from '../Text';
import { cn } from '../utils/cn';

/**
 * Rol semántico de estado (spec 0002): define el color del notch, nunca al
 * revés.
 *
 * `active` → `primary` (activo/en curso) · `confirmed` → `secondary`
 * (calibrado/confirmado) · `error` → `error` (sobrecarga/fallo) ·
 * `inactive` → `textMuted` (inactivo).
 */
export type BiomechanicalCardStatus = 'active' | 'confirmed' | 'error' | 'inactive';

/** Clases del notch por rol; contrato que consume el build. */
const STATUS_NOTCH_CLASS: Record<BiomechanicalCardStatus, string> = {
  active: 'bg-primary',
  confirmed: 'bg-secondary',
  error: 'bg-error',
  inactive: 'bg-text-muted',
};

/** Props del encabezado modular de la tarjeta. */
export type CardHeaderProps = {
  /** Título textual; se renderiza como encabezado accesible. */
  title?: string;
  /** Etiqueta textual del estado; el color nunca comunica estado por sí solo. */
  statusLabel?: string;
  /** Contenido adicional bajo el título. */
  children?: ReactNode;
  /** Clases del encabezado; se combinan con `cn()`. */
  className?: string;
  /** `testID` del encabezado. */
  testID?: string;
};

/**
 * Encabezado modular de la tarjeta: título (encabezado accesible), contenido
 * libre y etiqueta textual de estado. El **status notch** (bloque de color de
 * la esquina) lo dibuja `BiomechanicalCard`, para que el encabezado pueda
 * sustituirse sin perderlo.
 */
export function CardHeader({ title, statusLabel, children, className, testID }: CardHeaderProps) {
  return (
    <View className={cn('gap-xs p-md', className)} testID={testID}>
      {title ? (
        <Text variant="headlineSm" accessibilityRole="header">
          {title}
        </Text>
      ) : null}
      {children}
      {statusLabel ? (
        <Text variant="labelTechnical" className="text-text-muted">
          {statusLabel}
        </Text>
      ) : null}
    </View>
  );
}

/** Props de un compartimento de la tarjeta. */
export type CardSectionProps = {
  /** Contenido del compartimento. */
  children?: ReactNode;
  /** Clases del compartimento; se combinan con `cn()`. */
  className?: string;
  /** `testID` del compartimento. */
  testID?: string;
};

/**
 * Compartimento de la tarjeta: contenido con el padding base del DS. La
 * **hairline** que lo separa del bloque anterior la dibuja `BiomechanicalCard`
 * envolviendo cada hijo directo.
 */
export function CardSection({ children, className, testID }: CardSectionProps) {
  return (
    <View className={cn('p-md', className)} testID={testID}>
      {children}
    </View>
  );
}

/** Props públicas de `BiomechanicalCard`. */
export type BiomechanicalCardProps = {
  /** Título del encabezado por defecto (ignorado si se provee `header`). */
  title?: string;
  /** Encabezado a medida; sustituye al encabezado por defecto. */
  header?: ReactNode;
  /** Rol semántico de estado; dibuja el notch de color en la esquina. */
  status?: BiomechanicalCardStatus;
  /** Etiqueta textual del estado; se renderiza en el encabezado por defecto. */
  statusLabel?: string;
  /** Compartimentos; cada hijo directo queda separado por una hairline. */
  children?: ReactNode;
  /** Clases de la superficie; se combinan con `cn()` para permitir sobrescritura. */
  className?: string;
  /** `testID` de la tarjeta. Deriva `<testID>-header`, `-compartment-<n>` y `-status`. */
  testID?: string;
};

/**
 * Tarjeta biomecánica del design system (spec 0002, ticket #39).
 *
 * Superficie de nivel 1 (`surface`) con borde hairline y radio `base`, **sin
 * sombras**: la profundidad sale de bordes y capas tonales. El encabezado es
 * modular y cada compartimento hijo queda separado por una hairline de 1 px.
 * El **status notch** es un bloque de color en la esquina superior derecha,
 * definido por un rol semántico explícito; el estado se acompaña siempre de una
 * etiqueta textual (`statusLabel`), nunca se comunica solo por color.
 */
export function BiomechanicalCard({
  title,
  header,
  status,
  statusLabel,
  children,
  className,
  testID,
}: BiomechanicalCardProps) {
  return (
    <View
      className={cn('overflow-hidden rounded-base border border-border bg-surface', className)}
      testID={testID}
    >
      {header ?? (
        <CardHeader
          title={title}
          statusLabel={statusLabel}
          testID={testID ? `${testID}-header` : undefined}
        />
      )}
      {Children.map(children, (child, index) => (
        <View
          key={index}
          className="border-t border-border"
          testID={testID ? `${testID}-compartment-${index}` : undefined}
        >
          {child}
        </View>
      ))}
      {status ? (
        <View
          testID={testID ? `${testID}-status` : undefined}
          accessible={false}
          accessibilityElementsHidden
          importantForAccessibility="no-hide-descendants"
          className={cn('absolute top-0 right-0 h-sm w-sm', STATUS_NOTCH_CLASS[status])}
        />
      ) : null}
    </View>
  );
}
