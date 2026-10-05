import { fireEvent, render } from '@testing-library/react-native';

import { Toast } from './Toast';

describe('Toast', () => {
  it('renders nothing when not visible', async () => {
    const { queryByText } = await render(<Toast message="Guardado" visible={false} />);

    expect(queryByText('Guardado')).toBeNull();
  });

  it('renders the message when visible', async () => {
    const { getByText } = await render(<Toast message="Guardado" visible />);

    expect(getByText('Guardado')).toBeOnTheScreen();
  });

  it('announces the message with an alert role', async () => {
    const { getByRole } = await render(<Toast message="Error de red" visible testID="toast" />);

    expect(getByRole('alert')).toHaveTextContent('Error de red');
  });

  it('calls onDismiss when the dismiss affordance is pressed', async () => {
    const onDismiss = jest.fn();
    const { getByTestId } = await render(
      <Toast message="Guardado" visible onDismiss={onDismiss} testID="toast" />,
    );

    await fireEvent.press(getByTestId('toast-dismiss'));

    expect(onDismiss).toHaveBeenCalledTimes(1);
  });

  it('labels the dismiss affordance', async () => {
    const { getByLabelText } = await render(<Toast message="Guardado" visible testID="toast" />);

    expect(getByLabelText('Cerrar aviso')).toBeOnTheScreen();
  });

  it('maps each tone to its border token', async () => {
    const cases = [
      ['info', 'border-secondary'],
      ['active', 'border-primary'],
      ['confirmed', 'border-secondary'],
      ['error', 'border-error'],
    ] as const;

    for (const [tone, expectedClass] of cases) {
      const { getByTestId, unmount } = await render(
        <Toast message="aviso" visible tone={tone} testID="toast" />,
      );

      expect(getByTestId('toast').props.className).toContain(expectedClass);

      await unmount();
    }
  });
});
