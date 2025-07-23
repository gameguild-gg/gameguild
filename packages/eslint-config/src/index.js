import globals from 'globals';
import eslint from '@eslint/js';
import eslintConfigPrettier from 'eslint-config-prettier';
import eslintPluginPrettierRecommended from 'eslint-plugin-prettier/recommended';
import prettierPlugin from 'eslint-plugin-prettier';
import prettierConfig from '@game-guild/prettier-config';
import reactPlugin from 'eslint-plugin-react';
import reactHooksPlugin from 'eslint-plugin-react-hooks';
import typescriptEslint from 'typescript-eslint';

/** @type {import('eslint').Linter.Config[]} */
const config = [
  { files: ['**/*.{js,mjs,cjs,ts,jsx,tsx}'] },
  {
    languageOptions: {
      globals: {
        ...globals.browser,
        ...globals.node,
        ...globals.jest,
      },
      sourceType: 'module',
    },
  },
  eslint.configs.recommended, // eslint recommended rules
  eslintConfigPrettier,
  eslintPluginPrettierRecommended, // prettier recommended rules
  typescriptEslint.configs.recommended, // typescript-eslint recommended rules
  {
    ...reactPlugin.configs.flat.recommended,
    languageOptions: {
      ...reactPlugin.configs.flat.recommended.languageOptions,
      globals: {
        ...globals.serviceworker,
      },
    },
  },
  {
    plugins: { prettier: prettierPlugin },
    rules: { 'prettier/prettier': ['error', prettierConfig] },
  },
  // {
  //   plugins: {
  //     '@next/next': nextJsPlugin,
  //   },
  //   rules: {
  //     ...nextJsPlugin.configs.recommended.rules,
  //     ...nextJsPlugin.configs['core-web-vitals'].rules,
  //   },
  // },
  {
    plugins: {
      'react-hooks': reactHooksPlugin,
    },
    settings: { react: { version: 'detect' } },
    rules: {
      ...reactHooksPlugin.configs.recommended.rules,
      // React scope no longer necessary with the new JSX transform.
      'react/react-in-jsx-scope': 'off',
      'react/prop-types': 'off',
    },
  },
  {
    ignores: ['dist/**', 'build/**', 'coverage/**', 'node_modules/**'],
  },
];

export default config.flat();
