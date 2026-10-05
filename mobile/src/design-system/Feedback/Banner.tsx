import { View } from 'react-native';
import { useCSSVariable } from 'uniwind';

import { Icon, type LucideIcon } from '@/design-system/Icon';
import {
  ROLE_BORDER_CLASS,
  ROLE_COLOR_VARIABLE,
  type SemanticRole,
} from '@/design-system/semantic';
import { Text } from '@/design-system/Text';
import { cn } from '@/design-system/utils/cn';

import { ROLE_ICON } from './roleIcon';

/** Props públicas de `Banner`. */
export type BannerProps = {
  /** Texto del aviso; siempre hay etiqueta, nunca solo color. */
  message: string;
  /** Rol semántico de estado. Default `active`. */
  role?: SemanticRole;
  /** Icono a mostrar; por defecto el que corresponde al `role`. */
  icon?: LucideIcon;
  /** Clases del contenedor; se combinan con `cn()` para permitir sobrescritura. */
  className?: string;
  /** `testID` del contenedor; el icono deriva `<testID>-icon`. */
  testID?: string;
};

/**
 * Aviso inline persistente (spec 0002, ticket #47).
 *
 * Capa tonal (`surface`) delimitada por un borde del color del rol, con icono
 * y mensaje textual. **Sin sombras**: la profundidad sale del borde y de la
 * capa. El estado nunca se comunica solo por color: el mensaje siempre está
 * presente.
 */
export function Banner({ message, role = 'active', icon, className, testID }: BannerProps) {
  const IconComponent = icon ?? ROLE_ICON[role];
  const tokenColor = useCSSVariable(ROLE_COLOR_VARIABLE[role]);
  const color = typeof tokenColor === 'string' ? tokenColor : undefined;

  return (
    <View
      accessible
      accessibilityRole="text"
      className={cn(
        'flex-row items-start gap-sm rounded-md border bg-surface p-md',
        ROLE_BORDER_CLASS[role],
        className,
      )}
      testID={testID}
    >
      <Icon
        icon={IconComponent}
        color={color}
        size={20}
        testID={testID ? `${testID}-icon` : undefined}
      />
      <Text className="flex-1 text-text" variant="bodyMd">
        {message}
      </Text>
    </View>
  );
}
