import { render } from '@testing-library/react-native';

import { Loading } from './Loading';

describe('Loading', () => {
  it('renders an activity indicator', async () => {
    const { getByTestId } = await render(<Loading testID="loading" />);

    expect(getByTestId('loading-indicator', { includeHiddenElements: true })).toBeOnTheScreen();
  });

  it('colours the indicator with the primary accent token', async () => {
    const { getByTestId } = await render(<Loading testID="loading" />);

    expect(
      getByTestId('loading-indicator', { includeHiddenElements: true }).props.colorClassName,
    ).toBe('accent-primary');
  });

  it('announces a default loading label', async () => {
    const { getByLabelText } = await render(<Loading testID="loading" />);

    expect(getByLabelText('Cargando')).toBeOnTheScreen();
  });

  it('announces the optional label', async () => {
    const { getByLabelText, getByText } = await render(
      <Loading label="Generando plan" testID="loading" />,
    );

    expect(getByText('Generando plan')).toBeOnTheScreen();
    expect(getByLabelText('Generando plan')).toBeOnTheScreen();
  });

  it('merges consumer classes over its own', async () => {
    const { getByTestId } = await render(<Loading className="mt-lg" testID="loading" />);

    expect(getByTestId('loading').props.className).toContain('mt-lg');
  });
});
