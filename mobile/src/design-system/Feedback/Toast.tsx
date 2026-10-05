import { Pressable, View } from 'react-native';
import { useCSSVariable } from 'uniwind';

import { Icon, X } from '@/design-system/Icon';
import {
  ROLE_BORDER_CLASS,
  ROLE_COLOR_VARIABLE,
  type SemanticRole,
} from '@/design-system/semantic';
import { Text } from '@/design-system/Text';
import { cn } from '@/design-system/utils/cn';

import { ROLE_ICON } from './roleIcon';

/** Props públicas de `Toast`. */
export type ToastProps = {
  /** Texto del aviso; siempre hay etiqueta, nunca solo color. */
  message: string;
  /** Controla la visibilidad. Si es `false`, no renderiza nada. Default `false`. */
  visible?: boolean;
  /** Se invoca al pulsar el botón de descarte. */
  onDismiss?: () => void;
  /** Rol semántico de estado. Default `active`. */
  role?: SemanticRole;
  /** Etiqueta accesible del botón de descarte. Default `Cerrar aviso`. */
  dismissLabel?: string;
  /** Clases del contenedor; se combinan con `cn()` para permitir sobrescritura. */
  className?: string;
  /** `testID` del contenedor; el descarte deriva `<testID>-dismiss`. */
  testID?: string;
};

/**
 * Aviso transitorio (spec 0002, ticket #47).
 *
 * Capa tonal (`surface`) con borde según rol y mensaje textual. El mensaje
 * lleva `accessibilityRole="alert"` para que el lector de pantalla lo anuncie;
 * el botón de descarte queda como elemento independiente, con etiqueta textual
 * y área táctil ampliada. **Sin sombras.**
 */
export function Toast({
  message,
  visible = false,
  onDismiss,
  role = 'active',
  dismissLabel = 'Cerrar aviso',
  className,
  testID,
}: ToastProps) {
  const tokenColor = useCSSVariable(ROLE_COLOR_VARIABLE[role]);
  const color = typeof tokenColor === 'string' ? tokenColor : undefined;
  const mutedColor = useCSSVariable('--color-text-muted');
  const dismissColor = typeof mutedColor === 'string' ? mutedColor : undefined;

  if (!visible) {
    return null;
  }

  const IconComponent = ROLE_ICON[role];

  return (
    <View
      className={cn(
        'flex-row items-start gap-sm rounded-md border bg-surface p-md',
        ROLE_BORDER_CLASS[role],
        className,
      )}
      testID={testID}
    >
      <Icon icon={IconComponent} color={color} size={20} />
      <Text accessibilityRole="alert" className="flex-1 text-text" variant="bodyMd">
        {message}
      </Text>
      <Pressable
        accessibilityRole="button"
        accessibilityLabel={dismissLabel}
        className="p-xs text-text-muted"
        hitSlop={12}
        onPress={onDismiss}
        testID={testID ? `${testID}-dismiss` : undefined}
      >
        <Icon icon={X} color={dismissColor} size={20} />
      </Pressable>
    </View>
  );
}
