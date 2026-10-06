import { render, screen } from '@testing-library/react-native';

import { Text } from './Text';

/** Clases del nodo como lista, para comprobar tokens exactos y no substrings. */
function classesOf(testID: string): string[] {
  return (screen.getByTestId(testID).props.className as string).split(/\s+/);
}

/**
 * Regresión (#80): `Text` no fijaba color, así que heredaba el negro por defecto
 * de RN y quedaba casi invisible sobre el fondo oscuro del tema dark-only.
 */
describe('Text', () => {
  it('usa el color base `text` por defecto', async () => {
    await render(<Text testID="text">hola</Text>);

    expect(classesOf('text')).toContain('text-text');
  });

  it('permite al consumidor sobrescribir el color', async () => {
    await render(
      <Text testID="text" className="text-primary">
        hola
      </Text>,
    );

    const classes = classesOf('text');
    expect(classes).toContain('text-primary');
    expect(classes).not.toContain('text-text');
  });
});
