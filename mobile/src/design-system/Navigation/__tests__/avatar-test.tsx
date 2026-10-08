import { fireEvent, render, screen } from '@testing-library/react-native';

import { Avatar } from '../Avatar';

/**
 * `Avatar` del design system (ticket #82): control del atleta en la cabecera.
 * Se comprueba el rol/label accesibles y el contrato de clases, no el estilo
 * computado (Jest no corre Metro).
 */
describe('Avatar', () => {
  it('es un botón con etiqueta «Perfil» por defecto sobre primary', async () => {
    await render(<Avatar testID="avatar" />);

    const button = screen.getByRole('button', { name: 'Perfil' });
    expect(button).toBeOnTheScreen();
    expect(button).toHaveProp('className', expect.stringContaining('bg-primary'));
  });

  it('acepta una etiqueta accesible propia y reenvía onPress', async () => {
    const onPress = jest.fn();
    await render(<Avatar accessibilityLabel="Abrir ajustes" onPress={onPress} />);

    fireEvent.press(screen.getByRole('button', { name: 'Abrir ajustes' }));

    expect(onPress).toHaveBeenCalledTimes(1);
  });
});
