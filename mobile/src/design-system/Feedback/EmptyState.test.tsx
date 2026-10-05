import { render } from '@testing-library/react-native';

import { Info } from '@/design-system/Icon';
import { Text } from '@/design-system/Text';

import { EmptyState } from './EmptyState';

describe('EmptyState', () => {
  it('renders the title', async () => {
    const { getByText } = await render(<EmptyState title="Sin sesiones" />);

    expect(getByText('Sin sesiones')).toBeOnTheScreen();
  });

  it('renders the description when provided', async () => {
    const { getByText } = await render(
      <EmptyState title="Sin sesiones" description="Todavía no registraste ninguna sesión." />,
    );

    expect(getByText('Todavía no registraste ninguna sesión.')).toBeOnTheScreen();
  });

  it('renders the icon', async () => {
    const { getByTestId } = await render(
      <EmptyState icon={Info} title="Sin datos" testID="empty" />,
    );

    expect(getByTestId('empty-icon', { includeHiddenElements: true })).toBeOnTheScreen();
  });

  it('renders the action', async () => {
    const { getByText } = await render(
      <EmptyState action={<Text>Crear plan</Text>} title="Sin datos" />,
    );

    expect(getByText('Crear plan')).toBeOnTheScreen();
  });

  it('works without description, icon or action', async () => {
    const { getByText, queryByTestId } = await render(
      <EmptyState title="Sin datos" testID="empty" />,
    );

    expect(getByText('Sin datos')).toBeOnTheScreen();
    expect(queryByTestId('empty-icon')).toBeNull();
  });
});
