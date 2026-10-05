import { View } from 'react-native';

import { cn } from '../utils/cn';
import { SelectionBase, type SelectionBaseProps } from './SelectionBase';

/** Props públicas de `Radio`: las compartidas por los controles de selección. */
export type RadioProps = SelectionBaseProps;

/** Lado del bloque interior del radio cuando está activo (token `--spacing-radio-dot`). */
const RADIO_INNER_SIZE_CLASS = 'h-radio-dot w-radio-dot';

/**
 * `Radio` del design system (spec 0002, ticket #38).
 *
 * Es un **cuadrado**, no un círculo: misma caja de 20×20 dp con radios 0 y borde
 * activo (1.5 px) que el `Checkbox`, pero al activarse muestra un **bloque
 * interior sólido** en `primary` en lugar de una marca.
 */
export function Radio({ checked, testID, ...rest }: RadioProps) {
  return (
    <SelectionBase
      checked={checked}
      testID={testID}
      role="radio"
      controlClassName={checked ? 'border-primary' : 'border-border'}
      {...rest}
    >
      {checked ? (
        <View
          testID={testID ? `${testID}-inner` : undefined}
          className={cn(RADIO_INNER_SIZE_CLASS, 'rounded-none bg-primary')}
        />
      ) : null}
    </SelectionBase>
  );
}
