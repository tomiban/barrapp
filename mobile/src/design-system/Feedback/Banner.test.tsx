import { render } from '@testing-library/react-native';

import { Banner } from './Banner';

describe('Banner', () => {
  it('renders the message', async () => {
    const { getByText } = await render(<Banner message="Mesociclo guardado" />);

    expect(getByText('Mesociclo guardado')).toBeOnTheScreen();
  });

  it('announces the message through an accessible container', async () => {
    const { getByTestId } = await render(<Banner message="Sin conexión" testID="banner" />);

    const banner = getByTestId('banner');
    expect(banner.props.accessible).toBe(true);
    expect(banner.props.accessibilityRole).toBe('text');
  });

  it('maps each role to its border token', async () => {
    const cases = [
      ['active', 'border-primary'],
      ['confirmed', 'border-secondary'],
      ['error', 'border-error'],
      ['inactive', 'border-border'],
    ] as const;

    for (const [role, expectedClass] of cases) {
      const { getByTestId, unmount } = await render(
        <Banner message="aviso" role={role} testID="banner" />,
      );

      expect(getByTestId('banner').props.className).toContain(expectedClass);

      await unmount();
    }
  });

  it('usa el rol active por defecto', async () => {
    const { getByTestId } = await render(<Banner message="aviso" testID="banner" />);

    expect(getByTestId('banner').props.className).toContain('border-primary');
  });

  it('renders a tone icon', async () => {
    const { getByTestId } = await render(<Banner message="En curso" testID="banner" />);

    expect(getByTestId('banner-icon', { includeHiddenElements: true })).toBeOnTheScreen();
  });

  it('merges consumer classes over its own', async () => {
    const { getByTestId } = await render(
      <Banner message="aviso" className="mt-lg" testID="banner" />,
    );

    expect(getByTestId('banner').props.className).toContain('mt-lg');
  });
});
