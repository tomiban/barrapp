import type { LucideIcon, LucideProps } from 'lucide-react-native';
import { useCSSVariable } from 'uniwind';

/**
 * Props del wrapper `Icon`: un icono de Lucide más los defaults del design system.
 */
export type IconProps = Omit<LucideProps, 'size' | 'color' | 'strokeWidth'> & {
  /** Icono de Lucide a renderizar (p. ej. `Timer`). */
  icon: LucideIcon;
  /** Tamaño en dp. Default del DS: 24. */
  size?: number;
  /** Ancho de trazo técnico. Default del DS: 2. */
  strokeWidth?: number;
  /**
   * Color del trazo. Si se omite, se resuelve el token `--color-text` en JS
   * con `useCSSVariable`; el color siempre sale de los tokens del DS.
   */
  color?: string;
};

/**
 * Envuelve un icono de Lucide con los defaults del design system.
 *
 * Lucide colorea por la prop `color` (no por `className`), así que el token se
 * lee en JS y se pasa como string.
 */
export function Icon({
  icon: IconComponent,
  size = 24,
  strokeWidth = 2,
  color,
  ...rest
}: IconProps) {
  const tokenColor = useCSSVariable('--color-text');
  const resolvedColor = color ?? (typeof tokenColor === 'string' ? tokenColor : undefined);

  return <IconComponent color={resolvedColor} size={size} strokeWidth={strokeWidth} {...rest} />;
}
