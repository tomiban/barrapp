import type { ReactNode } from 'react';
import { View } from 'react-native';

import { Text } from '../Text';
import { cn } from '../utils/cn';

/** Props públicas de `SectionHeader`. */
export type SectionHeaderProps = {
  /** Etiqueta visible y nombre accesible; se pinta en `labelTechnical`. */
  label: string;
  /** Contador opcional (p. ej. número de semanas o de ejercicios). */
  count?: number | string;
  /** Slot final para acciones o metadatos (p. ej. «ver todo»). */
  trailing?: ReactNode;
  /** Clases del contenedor; se combinan con `cn()` para permitir sobrescritura. */
  className?: string;
  /** `testID` del encabezado; el contador deriva `<testID>-count`. */
  testID?: string;
};

/**
 * Encabezado de sección del design system (spec 0002, ticket #44).
 *
 * Etiqueta técnica en mayúsculas (`labelTechnical`) con contador/slot
 * opcionales y **subrayado hairline** (`border-b border-border`): separa
 * secciones sin sombras, apoyándose en la retícula y los tokens. Reutilizable
 * para las listas de semanas, sesiones y ejercicios.
 */
export function SectionHeader({ label, count, trailing, className, testID }: SectionHeaderProps) {
  return (
    <View className={cn('border-b border-border pb-sm', className)} testID={testID}>
      <View className="flex-row items-center justify-between gap-sm">
        <View className="flex-1 flex-row items-center gap-sm">
          <Text variant="labelTechnical" className="text-text-muted" accessibilityRole="header">
            {label}
          </Text>
          {count !== undefined && count !== '' ? (
            <Text
              variant="labelTechnical"
              className="text-text-muted"
              testID={testID ? `${testID}-count` : undefined}
            >
              {count}
            </Text>
          ) : null}
        </View>
        {trailing}
      </View>
    </View>
  );
}
