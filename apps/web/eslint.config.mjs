import baseConfig from '@game-guild/eslint-config';
import { FlatCompat } from '@eslint/eslintrc';

// import nextConfig from 'eslint-config-next';

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
];

export default config;
