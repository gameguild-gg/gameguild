// import baseConfig from '@game-guild/eslint-config';
import nextConfig from 'eslint-config-next';

/** @type {import('eslint').Linter.Config[]} */
const config = [
  // ...baseConfig, // Temporarily disabled due to build issues
  ...nextConfig
];

export default config;
