import { useCSSVariable } from 'uniwind';

import { Icon } from '../Icon';
import { Check } from '../Icon/icons';
import { SelectionBase, type SelectionBaseProps } from './SelectionBase';

/** Props públicas de `Checkbox`: las compartidas por los controles de selección. */
export type CheckboxProps = SelectionBaseProps;

/**
 * `Checkbox` del design system (spec 0002, ticket #38).
 *
 * Caja **cuadrada** de 20×20 dp con radios 0 y borde activo (1.5 px). Al marcar
 * se rellena en `primary` y muestra una marca de verificación con `Icon`; el
 * estado también se comunica por la etiqueta textual y por accesibilidad.
 */
export function Checkbox({ checked, testID, ...rest }: CheckboxProps) {
  // `on-primary` sobre `primary` para que la marca tenga contraste. La variable
  // se resuelve en JS (Lucide colorea por prop) y además aparece en la clase del
  // control marcado, para que el build la incluya.
  const onPrimary = useCSSVariable('--color-on-primary');
  const markColor = typeof onPrimary === 'string' ? onPrimary : undefined;

  return (
    <SelectionBase
      checked={checked}
      testID={testID}
      role="checkbox"
      controlClassName={checked ? 'border-primary bg-primary text-on-primary' : 'border-border'}
      {...rest}
    >
      {checked ? (
        <Icon
          icon={Check}
          size={14}
          strokeWidth={3}
          color={markColor}
          testID={testID ? `${testID}-mark` : undefined}
        />
      ) : null}
    </SelectionBase>
  );
}
