import { View } from 'react-native';
import { useCSSVariable } from 'uniwind';

import {
  Activity,
  CircleAlert,
  CircleCheck,
  Icon,
  Info,
  type LucideIcon,
} from '@/design-system/Icon';
import { Text } from '@/design-system/Text';
import { cn } from '@/design-system/utils/cn';

/**
 * Rol semántico de estado del aviso (spec 0002): define el color, nunca al
 * revés. `info` y `confirmed` usan `secondary` (referencia/confirmación),
 * `active` usa `primary` (activo/en curso) y `error` usa `error`
 * (sobrecarga/fallo).
 */
export type BannerTone = 'info' | 'active' | 'confirmed' | 'error';

/** Props públicas de `Banner`. */
export type BannerProps = {
  /** Texto del aviso; siempre hay etiqueta, nunca solo color. */
  message: string;
  /** Rol semántico de estado. Default `info`. */
  tone?: BannerTone;
  /** Icono a mostrar; por defecto el que corresponde al `tone`. */
  icon?: LucideIcon;
  /** Clases del contenedor; se combinan con `cn()` para permitir sobrescritura. */
  className?: string;
  /** `testID` del contenedor; el icono deriva `<testID>-icon`. */
  testID?: string;
};

/**
 * Clases del borde por rol semántico. El token también aparece aquí para que
 * el build lo incluya: `useCSSVariable` solo resuelve variables usadas en algún
 * `className`.
 */
const toneBorderClass: Record<BannerTone, string> = {
  info: 'border-secondary',
  active: 'border-primary',
  confirmed: 'border-secondary',
  error: 'border-error',
};

/** Variable CSS del color del icono por rol. */
const toneVariable: Record<BannerTone, string> = {
  info: '--color-secondary',
  active: '--color-primary',
  confirmed: '--color-secondary',
  error: '--color-error',
};

/** Icono por defecto de cada rol semántico. */
const toneIcon: Record<BannerTone, LucideIcon> = {
  info: Info,
  active: Activity,
  confirmed: CircleCheck,
  error: CircleAlert,
};

/**
 * Aviso inline persistente (spec 0002, ticket #47).
 *
 * Capa tonal (`surface`) delimitada por un borde del color del rol, con icono
 * y mensaje textual. **Sin sombras**: la profundidad sale del borde y de la
 * capa. El estado nunca se comunica solo por color: el mensaje siempre está
 * presente.
 */
export function Banner({ message, tone = 'info', icon, className, testID }: BannerProps) {
  const IconComponent = icon ?? toneIcon[tone];
  const tokenColor = useCSSVariable(toneVariable[tone]);
  const color = typeof tokenColor === 'string' ? tokenColor : undefined;

  return (
    <View
      accessible
      accessibilityRole="text"
      className={cn(
        'flex-row items-start gap-sm rounded-md border bg-surface p-md',
        toneBorderClass[tone],
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
