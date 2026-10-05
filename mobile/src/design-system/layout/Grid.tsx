import { Box, type BoxProps } from './Box';
import { cn } from '../utils/cn';

/** Número de columnas de la retícula móvil (spec: 4). */
export const GRID_COLUMNS = 4;

/** Columnas que ocupa una celda (1..4). */
export type GridSpan = 1 | 2 | 3 | 4;

/**
 * Props de `Grid`: las de `Box` más `margin`, que aplica el margen de página
 * del spec (20 dp) en horizontal.
 */
export type GridProps = BoxProps & {
  /**
   * Aplica el margen de página (`margin`, 20 dp) a los lados. Default: `true`.
   * Ponlo a `false` si la retícula va dentro de un contenedor que ya tiene su
   * propio padding horizontal.
   */
  margin?: boolean;
};

/** Props de `GridItem`: las de `Box` más las columnas que ocupa (`span`). */
export type GridItemProps = BoxProps & {
  /** Columnas que ocupa la celda, de 1 a 4. Default: 1. */
  span?: GridSpan;
};

/*
 * El gutter se reparte como padding de media separación (`sm` = gutter / 2) en
 * cada celda, y la retícula se saca hacia fuera con `-mx-sm` para que el
 * contenido de la primera/última columna quede a ras del margen de página.
 *
 * Por qué no `gap`: con `flex-wrap` y `gap`, cuatro columnas al 25 % suman
 * 100 % más los gutters y la cuarta salta de línea. Repartir el gutter como
 * padding de media separación mantiene exactamente 4 columnas por fila con
 * gap real de 16 dp, sin medir píxeles a mano.
 *
 * Requiere que el contenedor no aplique fondo: la caja se ensancha 8 dp por
 * lado; sólo se dibuja el contenido.
 */
const SPAN_CLASS: Record<GridSpan, string> = {
  1: 'basis-1/4',
  2: 'basis-1/2',
  3: 'basis-3/4',
  4: 'basis-full',
};

/**
 * Retícula de 4 columnas del spec: `gutter` 16 dp entre columnas y `margin`
 * 20 dp en los bordes. Los hijos se declaran como `GridItem`.
 *
 * ```tsx
 * <Grid>
 *   <GridItem span={2}>…</GridItem>
 *   <GridItem>…</GridItem>
 *   <GridItem>…</GridItem>
 * </Grid>
 * ```
 */
export function Grid({ margin = true, className, ...rest }: GridProps) {
  return (
    <Box className={cn('flex-row flex-wrap -mx-sm', margin && 'px-margin', className)} {...rest} />
  );
}

/**
 * Celda de la retícula. `span` reparte el ancho en cuartos (1 = 25 %,
 * 2 = 50 %, 4 = 100 %). El padding lateral de media separación construye el
 * `gutter` entre celdas vecinas.
 */
export function GridItem({ span = 1, className, ...rest }: GridItemProps) {
  return <Box className={cn(SPAN_CLASS[span], 'grow-0 shrink-0 px-sm', className)} {...rest} />;
}
