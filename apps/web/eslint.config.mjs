import baseConfig from '@game-guild/eslint-config';
import { FlatCompat } from '@eslint/eslintrc';

import nextConfig from 'eslint-config-next';

const compat = new FlatCompat({
  baseDirectory: import.meta.dirname,
});

/** @type {import('eslint').Linter.Config[]} */
const config = [
  ...baseConfig,
  ...compat.config({
    extends: ['next', 'next/core-web-vitals'],
    settings: {
      next: {
        rootDir: './',
      },
    },
  }),
  ...nextConfig,
  // Override rules for generated API code
  {
    files: ['src/lib/api/generated/**/*.{ts,js}'],
    rules: {
      '@typescript-eslint/no-explicit-any': 'off',
      '@typescript-eslint/ban-ts-comment': 'off',
    },
  },
];

export default config;
