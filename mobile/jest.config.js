// jest.config.js
const preset = require('jest-expo/jest-preset');

// Lucide se publica como ESM (`.mjs`) y el preset solo transforma `[jt]sx?`.
// Reutilizamos su transformador de Babel para que Jest entienda esos módulos.
const babelTransform = preset.transform['\\.[jt]sx?$'];

module.exports = {
  ...preset,
  transform: {
    ...preset.transform,
    '^.+\\.mjs$': babelTransform,
  },
  transformIgnorePatterns: [
    'node_modules/(?!(.pnpm|standard-navigation|react-native|@react-native|@react-native-community|expo|@expo|@expo-google-fonts|react-navigation|@react-navigation|@sentry/react-native|native-base|react-native-svg|uniwind|lucide-react-native))',
    '/node_modules/react-native-reanimated/plugin/',
    '/node_modules/@react-native/babel-preset/',
  ],
};
