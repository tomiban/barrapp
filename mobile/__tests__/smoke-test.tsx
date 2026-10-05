import { render, screen } from '@testing-library/react-native';
import { Text } from 'react-native';

/**
 * Humo: comprueba que el runner (Jest + jest-expo + Testing Library) monta
 * un componente de React Native. Es la base que usan los tickets del design system.
 */
test('renderiza un componente de React Native', async () => {
  await render(<Text>ok</Text>);

  expect(screen.getByText('ok')).toBeOnTheScreen();
});
