import { useState, type ReactNode } from 'react';
import { ScrollView, View } from 'react-native';

import { BiomechanicalCard, CardSection } from '@/design-system/BiomechanicalCard';
import { Button } from '@/design-system/Button';
import { Chip, SegmentedControl } from '@/design-system/Chip';
import { Banner, EmptyState, Loading, Skeleton, Toast } from '@/design-system/Feedback';
import { baseIcons, Calendar, ChevronRight, Dumbbell, Icon } from '@/design-system/Icon';
import { Box, Grid, GridItem, Stack } from '@/design-system/layout';
import { ListRow, SectionHeader } from '@/design-system/ListRow';
import { MetricCounter } from '@/design-system/MetricCounter';
import { BottomSheet } from '@/design-system/Modal';
import { Header, Screen } from '@/design-system/Navigation';
import { ProgressIndicator } from '@/design-system/ProgressIndicator';
import { Checkbox, Radio } from '@/design-system/Selection';
import { StatusBadge } from '@/design-system/StatusBadge';
import { Text, type TextVariant } from '@/design-system/Text';
import { TextField } from '@/design-system/TextField';
import { Timer } from '@/design-system/Timer';

/**
 * Catálogo del design system (spec 0002, ticket #49).
 *
 * Pantalla de referencia: muestra cada componente con sus variantes y estados,
 * agrupados en las cuatro familias de la spec (Base, Entrenamiento, Navegación
 * y feedback, Iconos). Cada demo lleva un rótulo con el nombre del componente y
 * la variante/estado que ilustra, para que sirva de guía al construir pantallas.
 *
 * Es el mismo design system que consume la app: importa los componentes reales
 * desde sus barrels (`@/design-system/...`), nunca copias ni estilos ad hoc.
 */

/** Rótulo de una demo: nombre del componente y variante/estado. */
function Demo({ caption, children }: { caption: string; children: ReactNode }) {
  return (
    <View className="gap-sm">
      <Text variant="labelCode" className="text-text-muted">
        {caption}
      </Text>
      {children}
    </View>
  );
}

/** Sección del catálogo: `SectionHeader` + demos apiladas. */
function Section({
  label,
  testID,
  children,
}: {
  label: string;
  testID: string;
  children: ReactNode;
}) {
  return (
    <View className="gap-md" testID={testID}>
      <SectionHeader label={label} />
      {children}
    </View>
  );
}

/** Escala tipográfica de la spec, rotulada por variante. */
const TEXT_VARIANTS: readonly { variant: TextVariant; label: string; sample: string }[] = [
  { variant: 'displayHero', label: 'displayHero · 56/56', sample: 'Aa' },
  { variant: 'headlineMetric', label: 'headlineMetric · 44/44', sample: '120' },
  { variant: 'headlineLg', label: 'headlineLg · 28/32', sample: 'Mesociclo' },
  { variant: 'headlineMd', label: 'headlineMd · 24/28', sample: 'Semana 1' },
  { variant: 'headlineSm', label: 'headlineSm · 18/24', sample: 'Sentadilla' },
  { variant: 'bodyLg', label: 'bodyLg · 16/24', sample: 'Cuerpo grande' },
  { variant: 'bodyMd', label: 'bodyMd · 14/20', sample: 'Cuerpo medio' },
  { variant: 'bodySm', label: 'bodySm · 12/16', sample: 'Cuerpo pequeño' },
  { variant: 'labelTechnical', label: 'labelTechnical · 11/14', sample: 'Etiqueta técnica' },
  { variant: 'labelCode', label: 'labelCode · 13/18', sample: '01:30 / 80 kg' },
];

/** `Chip` controlado: el estado vive en el consumidor, como en una pantalla real. */
function ChipDemo() {
  const [selected, setSelected] = useState(true);

  return (
    <Stack direction="row" gap="sm" className="flex-wrap">
      <Chip label="Empuje" selected={selected} onChange={setSelected} testID="showcase-chip" />
      <Chip label="Tirón" testID="showcase-chip-inactive" />
      <Chip label="Pierna" disabled testID="showcase-chip-disabled" />
    </Stack>
  );
}

const SEGMENTED_OPTIONS = [
  { value: 'semana', label: 'Semana' },
  { value: 'mes', label: 'Mes' },
] as const;

/** `SegmentedControl` controlado sobre opciones tipadas. */
function SegmentedDemo() {
  const [value, setValue] = useState<(typeof SEGMENTED_OPTIONS)[number]['value']>('semana');

  return (
    <SegmentedControl
      label="Vista"
      options={SEGMENTED_OPTIONS}
      value={value}
      onChange={setValue}
      testID="showcase-segmented"
    />
  );
}

/**
 * `TextField` en estado normal, error y foco. El foco se dispara a demanda
 * (remontando el campo con `autoFocus`) para no abrir el teclado al entrar al
 * catálogo ni saltar el scroll.
 */
function TextFieldDemo() {
  const [normal, setNormal] = useState('80');
  const [error, setError] = useState('120');
  const [focus, setFocus] = useState({ requested: false, key: 0 });

  const requestFocus = () => {
    setFocus((current) => ({ requested: true, key: current.key + 1 }));
  };

  return (
    <Stack gap="md">
      <TextField
        label="Normal"
        value={normal}
        onChangeText={setNormal}
        placeholder="Peso"
        testID="showcase-field-normal"
      />
      <View className="gap-sm">
        <TextField
          key={focus.key}
          label="Foco"
          value="100"
          onChangeText={() => {}}
          autoFocus={focus.requested}
          testID="showcase-field-focus"
        />
        <Button variant="tertiary" onPress={requestFocus} testID="showcase-field-focus-trigger">
          Enfocar campo
        </Button>
      </View>
      <TextField
        label="Error"
        value={error}
        onChangeText={setError}
        error="Fuera de rango (0–100)"
        testID="showcase-field-error"
      />
    </Stack>
  );
}

/** `Checkbox` y `Radio` controlados, con un ejemplo deshabilitado. */
function SelectionDemo() {
  const [conditioning, setConditioning] = useState(true);
  const [skill, setSkill] = useState(false);
  const [pattern, setPattern] = useState<'push' | 'pull'>('push');

  return (
    <Stack gap="md">
      <Stack gap="sm">
        <Checkbox
          label="Acondicionamiento"
          checked={conditioning}
          onChange={setConditioning}
          testID="showcase-checkbox"
        />
        <Checkbox
          label="Skill"
          checked={skill}
          onChange={setSkill}
          testID="showcase-checkbox-off"
        />
        <Checkbox
          label="Deshabilitado"
          checked={false}
          disabled
          testID="showcase-checkbox-disabled"
        />
      </Stack>
      <Stack gap="sm">
        <Radio
          label="Empuje"
          checked={pattern === 'push'}
          onChange={() => setPattern('push')}
          testID="showcase-radio-empuje"
        />
        <Radio
          label="Tirón"
          checked={pattern === 'pull'}
          onChange={() => setPattern('pull')}
          testID="showcase-radio-tiron"
        />
      </Stack>
    </Stack>
  );
}

/** `Timer` con marcha/parada controlada desde un botón. */
function TimerDemo() {
  const [running, setRunning] = useState(false);

  return (
    <Stack gap="sm">
      <Timer label="Hold" durationSeconds={30} running={running} testID="showcase-timer" />
      <Button
        variant="secondary"
        onPress={() => setRunning((current) => !current)}
        testID="showcase-timer-toggle"
      >
        {running ? 'Pausar' : 'Iniciar'}
      </Button>
    </Stack>
  );
}

/** `BottomSheet` abierto por un botón, como en una pantalla real. */
function ModalDemo() {
  const [visible, setVisible] = useState(false);

  return (
    <>
      <Button variant="tertiary" onPress={() => setVisible(true)} testID="showcase-modal-trigger">
        Abrir BottomSheet
      </Button>
      <BottomSheet
        visible={visible}
        onClose={() => setVisible(false)}
        title="BottomSheet / Modal"
        testID="showcase-sheet"
      >
        <Text variant="bodyMd" className="text-text-muted">
          Nivel 3: superficie `surface` con marco de 2 px en `text` y scrim negro 80 %, sin blur.
        </Text>
      </BottomSheet>
    </>
  );
}

/** Una fila por icono del set base, con su nombre semántico. */
function IconGridDemo() {
  return (
    <Grid margin={false}>
      {Object.entries(baseIcons).map(([name, icon]) => (
        <GridItem key={name} className="mb-md items-center gap-xs">
          <Box className="rounded-base border border-border bg-surface p-sm">
            <Icon icon={icon} size={24} />
          </Box>
          <Text variant="labelCode" className="text-text-muted" numberOfLines={1}>
            {name}
          </Text>
        </GridItem>
      ))}
    </Grid>
  );
}

/** Pantalla del catálogo. */
export function ShowcaseScreen() {
  return (
    <Screen
      testID="showcase-screen"
      header={<Header title="Catálogo" />}
      contentClassName="px-0 py-0"
    >
      <ScrollView contentContainerClassName="gap-xl px-margin py-md pb-xl" testID="showcase-scroll">
        <Text variant="bodyMd" className="text-text-muted">
          Referencia viva del design system (spec 0002): cada componente con sus variantes y
          estados, agrupado por familias.
        </Text>

        <Section label="Base" testID="showcase-base">
          <Demo caption="Text · escala tipográfica">
            <Stack gap="sm">
              {TEXT_VARIANTS.map(({ variant, label, sample }) => (
                <View key={variant} className="gap-xs border-b border-border pb-sm">
                  <Text variant="labelTechnical" className="text-text-muted">
                    {label}
                  </Text>
                  <Text variant={variant}>{sample}</Text>
                </View>
              ))}
            </Stack>
          </Demo>

          <Demo caption="Button · primary / secondary / tertiary / disabled">
            <Stack gap="sm">
              <Button onPress={() => {}} testID="showcase-button-primary">
                Primario
              </Button>
              <Button variant="secondary" onPress={() => {}} testID="showcase-button-secondary">
                Secundario
              </Button>
              <Button variant="tertiary" onPress={() => {}} testID="showcase-button-tertiary">
                Terciario
              </Button>
              <Button disabled testID="showcase-button-disabled">
                Deshabilitado
              </Button>
            </Stack>
          </Demo>

          <Demo caption="TextField · normal / focus / error">
            <TextFieldDemo />
          </Demo>

          <Demo caption="Chip · seleccionado / inactivo / deshabilitado">
            <ChipDemo />
          </Demo>

          <Demo caption="SegmentedControl · selección única">
            <SegmentedDemo />
          </Demo>

          <Demo caption="Checkbox + Radio · controlados y deshabilitado">
            <SelectionDemo />
          </Demo>

          <Demo caption="Grid · retícula de 4 columnas">
            <Grid margin={false}>
              {[1, 2, 3, 4].map((column) => (
                <GridItem key={column}>
                  <Box className="items-center rounded-base border border-border bg-surface py-md">
                    <Text variant="labelCode" className="text-text-muted">
                      {column}
                    </Text>
                  </Box>
                </GridItem>
              ))}
            </Grid>
          </Demo>

          <Demo caption="MetricCounter · roles semánticos">
            <Grid margin={false}>
              <GridItem span={2} className="pb-md">
                <MetricCounter label="Peso" value={100} unit="KG" index="SEC.01" />
              </GridItem>
              <GridItem span={2} className="pb-md">
                <MetricCounter label="RIR" value={2} index={2} indexPrefix="SER" role="active" />
              </GridItem>
              <GridItem span={2} className="pb-md">
                <MetricCounter label="Máximo" value={12} unit="REPS" role="confirmed" />
              </GridItem>
              <GridItem span={2} className="pb-md">
                <MetricCounter label="Sobrecarga" value={120} unit="%" role="error" />
              </GridItem>
              <GridItem span={2} className="pb-md">
                <MetricCounter label="Pausa" value={0} unit="SEC" role="inactive" />
              </GridItem>
              <GridItem span={2} className="pb-md">
                <MetricCounter label="Volumen" value={3200} unit="KG" />
              </GridItem>
            </Grid>
          </Demo>

          <Demo caption="BiomechanicalCard · encabezado + compartimentos + notch">
            <Stack gap="md">
              <BiomechanicalCard
                title="Sentadilla búlgara"
                role="active"
                statusLabel="En curso"
                testID="showcase-card-active"
              >
                <CardSection>
                  <Stack gap="xs">
                    <Text variant="labelTechnical" className="text-text-muted">
                      Series
                    </Text>
                    <Text variant="headlineMd">4 × 8</Text>
                  </Stack>
                </CardSection>
                <CardSection>
                  <Text variant="bodySm" className="text-text-muted">
                    RIR 2 · descanso 90 s
                  </Text>
                </CardSection>
              </BiomechanicalCard>
              <BiomechanicalCard
                title="Fondos en anillas"
                role="error"
                statusLabel="Sobrecarga"
                testID="showcase-card-error"
              >
                <CardSection>
                  <Text variant="bodySm" className="text-text-muted">
                    Baja el volumen un 10 % esta semana.
                  </Text>
                </CardSection>
              </BiomechanicalCard>
            </Stack>
          </Demo>
        </Section>

        <Section label="Entrenamiento" testID="showcase-training">
          <Demo caption="Timer / Countdown · con marcha y parada">
            <TimerDemo />
          </Demo>

          <Demo caption="ProgressIndicator · bar + roles">
            <Stack gap="md">
              <ProgressIndicator
                value={3}
                max={4}
                role="active"
                label="Sets · 3/4"
                testID="showcase-progress-active"
              />
              <ProgressIndicator
                value={2}
                max={4}
                role="confirmed"
                label="Calibrado · 2/4"
                testID="showcase-progress-confirmed"
              />
              <ProgressIndicator
                value={4}
                max={4}
                role="error"
                label="Sobrecarga · 4/4"
                testID="showcase-progress-error"
              />
              <ProgressIndicator
                value={1}
                max={4}
                role="inactive"
                label="Inactivo · 1/4"
                testID="showcase-progress-inactive"
              />
            </Stack>
          </Demo>

          <Demo caption="ProgressIndicator · ring + roles">
            <Stack direction="row" gap="lg" className="items-center">
              <ProgressIndicator
                variant="ring"
                value={3}
                max={4}
                role="active"
                size={72}
                label="Activo"
                testID="showcase-ring-active"
              />
              <ProgressIndicator
                variant="ring"
                value={2}
                max={4}
                role="confirmed"
                size={72}
                label="Confirmado"
                testID="showcase-ring-confirmed"
              />
              <ProgressIndicator
                variant="ring"
                value={4}
                max={4}
                role="error"
                size={72}
                label="Fallo"
                testID="showcase-ring-error"
              />
            </Stack>
          </Demo>

          <Demo caption="StatusBadge · roles (solid)">
            <Stack direction="row" gap="sm" className="flex-wrap">
              <StatusBadge role="active" testID="showcase-badge-active" />
              <StatusBadge role="confirmed" testID="showcase-badge-confirmed" />
              <StatusBadge role="error" testID="showcase-badge-error" />
              <StatusBadge role="inactive" testID="showcase-badge-inactive" />
            </Stack>
          </Demo>

          <Demo caption="StatusBadge · outline">
            <Stack direction="row" gap="sm" className="flex-wrap">
              <StatusBadge role="active" variant="outline" />
              <StatusBadge role="confirmed" variant="outline" />
              <StatusBadge role="error" variant="outline" label="Sobrecarga" />
            </Stack>
          </Demo>

          <Demo caption="ListRow + SectionHeader · estado y slots">
            <View>
              <SectionHeader label="Microciclo 1" count={3} testID="showcase-list-header" />
              <ListRow
                title="Sesión A · Empuje"
                subtitle="Skill + fuerza"
                leading={<Icon icon={Dumbbell} size={20} />}
                trailing={<Icon icon={ChevronRight} size={20} />}
                role="confirmed"
                onPress={() => {}}
                testID="showcase-row-confirmed"
              />
              <ListRow
                title="Sesión B · Tirón"
                role="active"
                onPress={() => {}}
                testID="showcase-row-active"
              />
              <ListRow
                title="Deload"
                leading={<Icon icon={Calendar} size={20} />}
                role="inactive"
                stateLabel="Descarga"
                last
                testID="showcase-row-inactive"
              />
            </View>
          </Demo>
        </Section>

        <Section label="Navegación y feedback" testID="showcase-navigation">
          <Demo caption="Screen / Header / TabBar · chasis de la app">
            <Text variant="bodySm" className="text-text-muted">
              Screen y Header enmarcan esta pantalla; TabBar es la barra inferior del layout de
              pestañas. Se demuestran en vivo, no como pieza aislada.
            </Text>
          </Demo>

          <Demo caption="BottomSheet / Modal · disparador">
            <ModalDemo />
          </Demo>

          <Demo caption="Banner · active / confirmed / error / inactive">
            <Stack gap="sm">
              <Banner role="active" message="Sesión en curso: 2 de 4 bloques." />
              <Banner role="confirmed" message="Máximo calibrado a 12 repeticiones." />
              <Banner role="error" message="No se pudo guardar el registro." />
              <Banner role="inactive" message="Registro pausado hasta la próxima sesión." />
            </Stack>
          </Demo>

          <Demo caption="Toast · transitorio (los cuatro roles)">
            <Stack gap="sm">
              <Toast
                visible
                role="active"
                message="Sesión en curso."
                onDismiss={() => {}}
                testID="showcase-toast-active"
              />
              <Toast
                visible
                role="confirmed"
                message="Sesión guardada en el historial."
                onDismiss={() => {}}
                testID="showcase-toast-confirmed"
              />
              <Toast
                visible
                role="error"
                message="No se pudo guardar el registro."
                onDismiss={() => {}}
                testID="showcase-toast-error"
              />
              <Toast
                visible
                role="inactive"
                message="Sin cambios que guardar."
                onDismiss={() => {}}
                testID="showcase-toast-inactive"
              />
            </Stack>
          </Demo>

          <Demo caption="Loading · estado de carga">
            <Loading label="Generando plan…" testID="showcase-loading" />
          </Demo>

          <Demo caption="Skeleton · estático y animado">
            <Stack gap="sm">
              <Skeleton testID="showcase-skeleton-static" />
              <Skeleton animated className="w-2/3" testID="showcase-skeleton-animated" />
            </Stack>
          </Demo>

          <Demo caption="EmptyState · con icono y acción">
            <EmptyState
              title="Sin mesociclos"
              description="Genera un plan para empezar a registrar tus sesiones."
              icon={Calendar}
              action={<Button onPress={() => {}}>Generar plan</Button>}
            />
          </Demo>
        </Section>

        <Section label="Iconos" testID="showcase-icons">
          <Demo caption="Icon · set base de Lucide">
            <IconGridDemo />
          </Demo>
        </Section>
      </ScrollView>
    </Screen>
  );
}
