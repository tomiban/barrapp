import { Box, type BoxProps } from './Box';
import { cn } from '../utils/cn';

/** Escala de espaciado del design system (dp). */
export type StackGap = 'xs' | 'sm' | 'md' | 'lg' | 'xl';

/**
 * Dirección del stack: `column` apila en vertical (por defecto), `row` en
 * horizontal.
 */
export type StackDirection = 'row' | 'column';

/**
 * Props de `Stack`: las de `Box` más la dirección y el `gap` de la escala.
 * `className` se fusiona al final, así que puede sobrescribir ambos.
 */
export type StackProps = BoxProps & {
  /** Eje de apilado. Default: `column`. */
  direction?: StackDirection;
  /** Separación entre hijos, de la escala de espaciado. Default: `md`. */
  gap?: StackGap;
};

/*
 * Clases completas y estáticas: Uniwind/Tailwind escanea el código en build
 * time, así que nunca se interpolan nombres de clase.
 */
const DIRECTION_CLASS: Record<StackDirection, string> = {
  row: 'flex-row',
  column: 'flex-col',
};

const GAP_CLASS: Record<StackGap, string> = {
  xs: 'gap-xs',
  sm: 'gap-sm',
  md: 'gap-md',
  lg: 'gap-lg',
  xl: 'gap-xl',
};

/**
 * Apila hijos en fila o columna con una separación tomada de los tokens.
 * El gap lo resuelve el motor de layout (`gap` de flexbox), no márgenes
 * manuales, así que los hijos no necesitan saber nada de la separación.
 */
export function Stack({ direction = 'column', gap = 'md', className, ...rest }: StackProps) {
  return <Box className={cn(DIRECTION_CLASS[direction], GAP_CLASS[gap], className)} {...rest} />;
}
