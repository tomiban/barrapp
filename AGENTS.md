# Barrapp — planificador de calistenia

App nativa (iOS/Android) que genera una planificación mensual de calistenia a partir del perfil del atleta: elige un *skill* objetivo y recibe un mesociclo con acondicionamiento general, seguimiento y una sesión suelta para imprevistos. Front Expo/React Native, backend .NET Minimal API, motor determinista de reglas.

## Arquitectura

La forma del código —Engine puro, API adaptador, App nativa— y las reglas de dependencia entre ellos. **Léelo antes de crear proyectos, mover código entre capas o añadir lógica de dominio.** Ver `docs/architecture.md`.

## Reglas de trabajo

Flujo de tickets, ramas, commits, testing y estándares. **Léelo antes de empezar un ticket o abrir un PR.** Ver `docs/working-rules.md`.

## Estándares de ingeniería

Validación, seguridad, datos, rendimiento y resiliencia (reglas permanentes y las que se activan por feature). **Léelo antes de escribir código de API o tocar la base de datos.** Ver `docs/engineering-standards.md`.

## Dominio

El vocabulario del proyecto (*objetivo*, *skill*, *mesociclo*, *deload*, *registro*…) y los términos a evitar. Ver `GLOSSARY.md`. Las decisiones técnicas duraderas, en `docs/adr/`.

## Agent skills

### Issue tracker

Issues and specs live as GitHub issues; use the `gh` CLI. See `docs/agents/issue-tracker.md`.

### Triage labels

The five canonical labels: `needs-triage`, `needs-info`, `ready-for-agent`, `ready-for-human`, `wontfix`. See `docs/agents/triage-labels.md`.

### Domain docs

Single-context: `GLOSSARY.md` + `docs/adr/` at the repo root. See `docs/agents/domain.md`.
