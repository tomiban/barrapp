import { Box, type BoxProps } from './Box';
import { cn } from '../utils/cn';

/** Escala de espaciado del design system (dp). */
export type SpacerSize = 'xs' | 'sm' | 'md' | 'lg' | 'xl';

/**
 * Props de `Spacer`: las de `Box` más un tamaño opcional de la escala.
 * Si se omite `size`, el spacer es flexible (`flex-1`).
 */
export type SpacerProps = BoxProps & {
  /**
   * Hueco fijo a reservar. Se aplica como `flex-basis` del token, así que
   * funciona igual en un `Stack` en fila (ancho) o en columna (alto). Si se
   * omite, el spacer ocupa el espacio libre.
   */
  size?: SpacerSize;
};

/*
 * `basis-<token>` se resuelve contra `--spacing-*` y controla el eje principal
 * del contenedor flex: ancho en fila, alto en columna.
 */
const SIZE_CLASS: Record<SpacerSize, string> = {
  xs: 'basis-xs',
  sm: 'basis-sm',
  md: 'basis-md',
  lg: 'basis-lg',
  xl: 'basis-xl',
};

/**
 * Reserva espacio entre dos elementos de un contenedor flex: crece con el
 * espacio libre por defecto, o mantiene un tamaño fijo de la escala con `size`.
 *
 * El tamaño sale siempre de los tokens; no se usan márgenes manuales.
 */
export function Spacer({ size, className, ...rest }: SpacerProps) {
  return (
    <Box
      className={cn(size ? cn(SIZE_CLASS[size], 'grow-0 shrink-0') : 'flex-1', className)}
      {...rest}
    />
  );
}
