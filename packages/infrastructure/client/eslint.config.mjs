import baseConfig from '@game-guild/eslint-config';
import prettierConfig from '@game-guild/prettier-config';
import { defineConfig, globalIgnores } from 'eslint/config';

export default defineConfig([
  ...baseConfig,
  globalIgnores(['src/generated/**']),
  {
    files: ['src/**/*.{ts,tsx}'],
    rules: {
      '@next/next/no-html-link-for-pages': 'off',
      '@next/next/no-location-assign-relative-destination': 'off',
      'prettier/prettier': ['error', { ...prettierConfig, endOfLine: 'auto' }],
    },
  },
]);
