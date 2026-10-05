import { render } from '@testing-library/react-native';

import { Skeleton } from './Skeleton';

describe('Skeleton', () => {
  it('renders a tonal placeholder block', async () => {
    const { getByTestId } = await render(<Skeleton testID="skeleton" />);

    const skeleton = getByTestId('skeleton', { includeHiddenElements: true });

    expect(skeleton.props.className).toContain('bg-surface-muted');
    expect(skeleton.props.className).toContain('rounded-base');
  });

  it('is hidden from accessibility', async () => {
    const { getByTestId } = await render(<Skeleton testID="skeleton" />);

    expect(
      getByTestId('skeleton', { includeHiddenElements: true }).props.accessibilityElementsHidden,
    ).toBe(true);
  });

  it('lets consumer classes override its size', async () => {
    const { getByTestId } = await render(<Skeleton className="h-xl w-1/2" testID="skeleton" />);

    const className = getByTestId('skeleton', { includeHiddenElements: true }).props.className;

    expect(className).toContain('h-xl');
    expect(className).toContain('w-1/2');
    expect(className).not.toContain('h-md');
    expect(className).not.toContain('w-full');
  });

  it('renders when the optional pulse is enabled', async () => {
    const { getByTestId, unmount } = await render(<Skeleton animated testID="skeleton" />);

    expect(getByTestId('skeleton', { includeHiddenElements: true })).toBeOnTheScreen();

    await unmount();
  });
});
