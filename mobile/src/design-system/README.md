# Design system

Lenguaje visual de Barrapp (spec `docs/specs/0002-design-system.md`): tokens en
`@theme` (Uniwind), tipografía y componentes reutilizables.

## Tokens

La **fuente única de verdad** es `mobile/global.css`, en un único bloque
`@theme` plano (theme-independent) → la app es **dark-only** por construcción.
Ningún componente escribe colores, espaciado, radios ni tipografía a mano; todo
sale de un utility.

| Categoría      | Variables                                                                                                                                                                                                                                                                                             | Utilities                                                           |
| -------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------- |
| Color          | `--color-canvas`, `--color-surface`, `--color-surface-muted`, `--color-border`, `--color-text`, `--color-text-muted`, `--color-primary`, `--color-on-primary`, `--color-secondary`, `--color-on-secondary`, `--color-error`, `--color-error-container`, `--color-on-error-container`, `--color-scrim` | `bg-canvas`, `text-text-muted`, `border-border`, `text-on-primary`… |
| Espaciado (dp) | `--spacing-xs/sm/md/lg/xl`, `--spacing-gutter`, `--spacing-margin`                                                                                                                                                                                                                                    | `p-md`, `gap-sm`, `px-lg`, `mt-md`…                                 |
| Radios (dp)    | `--radius-sm/base/md/lg/xl`                                                                                                                                                                                                                                                                           | `rounded-sm`, `rounded-base`, `rounded-md`…                         |
| Bordes         | `border` (hairline 1px), `@utility border-active` (1.5px), `border-2` (marco de overlay)                                                                                                                                                                                                              | color con `border-<color>`                                          |
| Familias       | `--font-display`, `--font-display-semibold`, `--font-body`, `--font-mono-medium`, `--font-mono-semibold`, `--font-mono-bold`                                                                                                                                                                          | `font-display`, `font-mono-bold`…                                   |
| Tipografía     | `--text-display-hero`, `--text-headline-metric/lg/md/sm`, `--text-body-lg/md/sm`, `--text-label-technical`, `--text-label-code` (+ `--line-height` / `--letter-spacing` / `--font-weight`)                                                                                                            | `text-headline-md`, `text-label-technical`…                         |
| Motion         | `--transition-duration-fast/base`, `--ease-standard`                                                                                                                                                                                                                                                  | `duration-fast`, `duration-base`, `ease-standard`                   |

Convención: el nombre del token en kebab-case es el sufijo del utility
(`--color-surface-muted` → `bg-surface-muted`). Las familias `--font-*` deben
coincidir **exactamente** con las claves de `useFonts()` (ticket #33).

Motion: la spec lo dejó «a definir con la implementación». El set actual es
mínimo y no decorativo (duraciones + easing estándar, sin rebotes ni springs).

La paleta por defecto de Tailwind sigue disponible; el design system no la usa.
Para prohibirla, añadir `--color-*: initial;` al inicio del `@theme`.

## Layout

- Móvil: retícula de **4 columnas**, `gutter 16` (`gap-gutter`), `margin 20`
  (`p-margin` / `px-margin`).
- Split-screen de ejecución: top **60 %** telemetría, bottom **40 %** controles.
- Profundidad por bordes y capas tonales (`canvas` → `surface` →
  `surface-muted`), **sin sombras**.

### Primitivas

`src/design-system/layout/` expone primitivas finas sobre `View`; el espaciado
sale siempre de los tokens. Todas aceptan `className` y lo fusionan con `cn()`
resolviendo conflictos (gana la clase del consumidor).

| Primitiva  | API                                                                        | Clases que emite                                                              |
| ---------- | -------------------------------------------------------------------------- | ----------------------------------------------------------------------------- |
| `Box`      | `ViewProps` + `className`                                                  | sólo fusiona `className`                                                      |
| `Stack`    | `direction?: 'row' \| 'column'` (def. `column`), `gap?: xs…xl` (def. `md`) | `flex-row`/`flex-col` + `gap-<token>`                                         |
| `Spacer`   | `size?: xs…xl` (si se omite, ocupa el espacio libre)                       | `flex-1` o `basis-<token> grow-0 shrink-0`                                    |
| `Grid`     | `margin?: boolean` (def. `true`)                                           | `flex-row flex-wrap -mx-sm` + `px-margin`                                     |
| `GridItem` | `span?: 1 \| 2 \| 3 \| 4` (def. `1`)                                       | `basis-1/4 \| basis-1/2 \| basis-3/4 \| basis-full` + `px-sm grow-0 shrink-0` |

```tsx
<Grid>
  <GridItem span={2}>
    <MetricCounter />
  </GridItem>
  <GridItem>
    <StatusBadge />
  </GridItem>
  <GridItem>
    <StatusBadge />
  </GridItem>
</Grid>
```

`Grid` reparte el `gutter` (16 dp) como padding de media separación (`sm`) en
cada celda y se saca 8 dp por lado con `-mx-sm`, de modo que el `px-margin`
deja el contenido a 20 dp del borde sin medir píxeles a mano. La caja de la
retícula se ensancha 8 dp por lado: **no le pongas fondo**; el fondo va en las
celdas (`GridItem`).

`cn()` registra las escalas del design system (espaciado y tipografía) en
`tailwind-merge`, así que el `className` de un consumidor puede sobrescribir
`gap-*`, `p-*`, `text-*`… de una primitiva sin perder el token del componente.

## Componentes

Cada componente vive en su propio directorio `src/design-system/<Nombre>/` con
un `index.ts` que expone su API pública, sus props y sus tipos.

### ProgressIndicator

Barra o anillo de progreso sobre los tokens de color, para series y para el
avance del mesociclo. Sin sombras: la pista es una capa tonal (`surface-muted`)
y la profundidad sale de bordes y radios.

| Prop        | Tipo              | Default    | Uso                                                            |
| ----------- | ----------------- | ---------- | -------------------------------------------------------------- |
| `value`     | `number`          | —          | Progreso actual; se recorta a `[0, max]`.                      |
| `max`       | `number`          | `1`        | Total de la escala; el ratio es `value / max`.                 |
| `tone`      | `ProgressTone`    | `'active'` | Rol semántico de estado.                                       |
| `label`     | `string`          | —          | Etiqueta textual; el estado nunca se comunica solo por color.  |
| `variant`   | `'bar' \| 'ring'` | `'bar'`    | Forma del indicador.                                           |
| `size`      | `number`          | `64`       | Lado del anillo en dp (solo `ring`).                           |
| `className` | `string`          | —          | Clases del contenedor (se combinan con `cn()`).                |
| `testID`    | `string`          | —          | Identificador; barra/anillo derivan `<testID>-fill` / `-ring`. |

`ProgressTone` mapea los roles semánticos de la spec a tokens:

| Rol                    | `tone`      | Token       |
| ---------------------- | ----------- | ----------- |
| Activo / en curso      | `active`    | `primary`   |
| Calibrado / confirmado | `confirmed` | `secondary` |
| Sobrecarga / fallo     | `error`     | `error`     |
| Inactivo               | `inactive`  | `textMuted` |

Accesibilidad: `accessibilityRole="progressbar"` con
`accessibilityValue={{ min: 0, max, now }}` (con `max` ya saneado). La barra
colorea el relleno por `className`; el anillo colorea por la prop `stroke` de
`react-native-svg`, resolviendo el token con `useCSSVariable`.

## Estructura

- `utils/` — infraestructura compartida (`cn()`).
- `layout/` — primitivas de layout (`Box`, `Stack`, `Spacer`, `Grid`,
  `GridItem`) con su `index.ts` local.
- `<Nombre>/` — cada componente con su `index.ts` de API pública. Un barrel por
  directorio; **sin barrel de `design-system/`** para no colisionar entre
  tickets.

Reglas: nada de colores, espaciado ni tipografía hardcodeados; todo sale de los
tokens de `mobile/global.css`.
