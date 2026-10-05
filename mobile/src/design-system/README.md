# Design system

Lenguaje visual de Barrapp (spec `docs/specs/0002-design-system.md`): tokens en
`@theme` (Uniwind), tipografía y componentes reutilizables.

Layout previsto:

- `utils/` — infraestructura compartida (`cn()`).
- (próximos tickets) componentes base y de entrenamiento, cada uno en su propio
  archivo/directorio; **sin barrel `index.ts`** para no colisionar entre tickets.

Reglas: nada de colores, espaciado ni tipografía hardcodeados; todo sale de los
tokens de `global.css` en la raíz de `mobile/`.
