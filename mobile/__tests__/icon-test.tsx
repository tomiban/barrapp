import { render, screen } from '@testing-library/react-native';
import { Play, Timer } from 'lucide-react-native';

import { Icon } from '../src/design-system/Icon';
import { baseIcons } from '../src/design-system/Icon/icons';

/**
 * Lucide marca sus SVG como decorativos (`aria-hidden`), así que hay que
 * incluirlos explícitamente en las consultas de Testing Library.
 */
const getIcon = (testID: string) => screen.getByTestId(testID, { includeHiddenElements: true });

/**
 * Icono: capa del design system sobre Lucide. El test comprueba comportamiento
 * observable (el icono pedido llega a pantalla y recibe los defaults del DS),
 * no la estructura interna del wrapper.
 */
describe('Icon', () => {
  it('renderiza el icono indicado', async () => {
    await render(<Icon icon={Timer} color="#E2E4E9" testID="timer-icon" />);

    expect(getIcon('timer-icon')).toBeOnTheScreen();
  });

  it('aplica los defaults del DS: 24 dp y trazo 2', async () => {
    await render(<Icon icon={Timer} color="#E2E4E9" testID="timer-icon" />);

    expect(getIcon('timer-icon').props).toMatchObject({
      width: 24,
      height: 24,
      strokeWidth: 2,
    });
  });

  it('permite sobrescribir tamaño, trazo y color', async () => {
    await render(
      <Icon icon={Timer} color="#FFCC00" size={32} strokeWidth={1.5} testID="timer-icon" />,
    );

    expect(getIcon('timer-icon').props).toMatchObject({
      width: 32,
      height: 32,
      strokeWidth: 1.5,
      stroke: '#FFCC00',
    });
  });

  it('renderiza varios iconos a la vez', async () => {
    await render(
      <>
        <Icon icon={Timer} color="#E2E4E9" testID="timer-icon" />
        <Icon icon={Play} color="#FFCC00" testID="play-icon" />
      </>,
    );

    expect(getIcon('timer-icon')).toBeOnTheScreen();
    expect(getIcon('play-icon')).toBeOnTheScreen();
  });

  it('integra un icono del set base', async () => {
    await render(<Icon icon={baseIcons.timer} color="#E2E4E9" testID="base-timer" />);

    expect(getIcon('base-timer')).toBeOnTheScreen();
  });
});

describe('baseIcons', () => {
  it('expone un set base no vacío del dominio de entrenamiento', () => {
    const names = Object.keys(baseIcons);

    expect(names.length).toBeGreaterThan(0);
    expect(names).toEqual(expect.arrayContaining(['timer', 'play', 'pause', 'check', 'activity']));
  });
});
