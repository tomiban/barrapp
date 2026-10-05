import type { ReactNode } from 'react';
import { View } from 'react-native';
import { useCSSVariable } from 'uniwind';

import { Icon, type LucideIcon } from '@/design-system/Icon';
import { Text } from '@/design-system/Text';
import { cn } from '@/design-system/utils/cn';

/** Props públicas de `EmptyState`. */
export type EmptyStateProps = {
  /** Título del estado vacío; obligatorio y textual. */
  title: string;
  /** Texto de apoyo opcional. */
  description?: string;
  /** Icono opcional; por defecto no se muestra ninguno. */
  icon?: LucideIcon;
  /** Acción opcional (p. ej. un botón) que se apila bajo el texto. */
  action?: ReactNode;
  /** Clases del contenedor; se combinan con `cn()` para permitir sobrescritura. */
  className?: string;
  /** `testID` del contenedor; el icono deriva `<testID>-icon`. */
  testID?: string;
};

/**
 * Estado vacío reutilizable (spec 0002, ticket #47).
 *
 * Icono, título y descripción opcionales, más un hueco para una acción. Es un
 * bloque centrado de capas tonales, **sin sombras**. El estado se comunica con
 * texto, nunca solo con el icono.
 */
export function EmptyState({
  title,
  description,
  icon,
  action,
  className,
  testID,
}: EmptyStateProps) {
  const tokenColor = useCSSVariable('--color-text-muted');
  const iconColor = typeof tokenColor === 'string' ? tokenColor : undefined;

  return (
    <View className={cn('items-center justify-center gap-sm p-lg', className)} testID={testID}>
      {icon ? (
        <Icon
          color={iconColor}
          icon={icon}
          size={32}
          testID={testID ? `${testID}-icon` : undefined}
        />
      ) : null}
      <Text className="text-center text-text" variant="headlineSm">
        {title}
      </Text>
      {description ? (
        <Text className="text-center text-text-muted" variant="bodyMd">
          {description}
        </Text>
      ) : null}
      {action ? <View className="mt-sm">{action}</View> : null}
    </View>
  );
}
