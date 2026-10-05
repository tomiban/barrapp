import { useState } from 'react';
import { fireEvent, render, screen, userEvent } from '@testing-library/react-native';

import { TextField } from '@/design-system/TextField';

/** Renderiza un campo controlado real para probar el tipeo como en la app. */
function ControlledField({ onChangeText }: { onChangeText?: (text: string) => void }) {
  const [value, setValue] = useState('');

  return (
    <TextField
      label="Peso"
      value={value}
      onChangeText={(text) => {
        setValue(text);
        onChangeText?.(text);
      }}
    />
  );
}

describe('TextField', () => {
  it('muestra la label y la asocia al input', async () => {
    await render(<TextField label="Peso" value="80" onChangeText={jest.fn()} />);

    expect(screen.getByText('Peso')).toBeOnTheScreen();
    expect(screen.getByLabelText('Peso')).toBeOnTheScreen();
  });

  it('usa tipografía monoespaciada en el valor y label técnica en mayúsculas', async () => {
    await render(<TextField label="Peso" value="80" onChangeText={jest.fn()} />);

    const input = screen.getByLabelText('Peso');
    expect(input).toHaveProp('className', expect.stringContaining('font-mono-medium'));
    expect(input).toHaveProp('className', expect.stringContaining('text-label-code'));

    const label = screen.getByText('Peso');
    expect(label).toHaveProp('className', expect.stringContaining('uppercase'));
    expect(label).toHaveProp('className', expect.stringContaining('text-text-muted'));
  });

  it('emite onChangeText al tipear', async () => {
    const onChangeText = jest.fn();
    const user = userEvent.setup();

    await render(<ControlledField onChangeText={onChangeText} />);
    await user.type(screen.getByLabelText('Peso'), '80kg');

    expect(onChangeText).toHaveBeenLastCalledWith('80kg');
  });

  it('cambia el borde hairline a foco amarillo 1.5px y vuelve al perder foco', async () => {
    await render(<TextField label="Peso" value="" onChangeText={jest.fn()} />);

    expect(screen.getByLabelText('Peso')).toHaveProp(
      'className',
      expect.stringContaining('border-border'),
    );

    await fireEvent(screen.getByLabelText('Peso'), 'focus');
    expect(screen.getByLabelText('Peso')).toHaveProp(
      'className',
      expect.stringContaining('border-active'),
    );
    expect(screen.getByLabelText('Peso')).toHaveProp(
      'className',
      expect.stringContaining('border-primary'),
    );

    await fireEvent(screen.getByLabelText('Peso'), 'blur');
    expect(screen.getByLabelText('Peso')).toHaveProp(
      'className',
      expect.stringContaining('border-border'),
    );
  });

  it('muestra y anuncia el error como texto, con borde de error', async () => {
    await render(
      <TextField label="Peso" value="" onChangeText={jest.fn()} error="Ingresá un peso válido" />,
    );

    const message = screen.getByText('Ingresá un peso válido');
    expect(message).toBeOnTheScreen();
    expect(message).toHaveProp('accessibilityRole', 'alert');
    expect(message).toHaveProp('accessibilityLiveRegion', 'polite');

    const input = screen.getByLabelText('Peso');
    expect(input).toHaveProp('className', expect.stringContaining('border-error'));
  });

  it('prioriza el borde de error sobre el foco', async () => {
    await render(
      <TextField label="Peso" value="" onChangeText={jest.fn()} error="Ingresá un peso válido" />,
    );

    await fireEvent(screen.getByLabelText('Peso'), 'focus');

    expect(screen.getByLabelText('Peso')).toHaveProp(
      'className',
      expect.stringContaining('border-error'),
    );
    expect(screen.getByLabelText('Peso')).not.toHaveProp(
      'className',
      expect.stringContaining('border-primary'),
    );
  });
});
