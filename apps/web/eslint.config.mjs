import typescriptEslint from '@typescript-eslint/eslint-plugin';
import typescriptParser from '@typescript-eslint/parser';
import unusedImports from 'eslint-plugin-unused-imports';
import path from 'path';
import { fileURLToPath } from 'url';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

/** @type {import('eslint').Linter.Config[]} */
const config = [
  {
    ignores: [
      'src/lib/api/generated/**/*',
      '**/__tests__/**/*',
      '**/*.test.ts',
      '**/*.test.tsx',
      '**/test/**/*',
      '**/*.d.ts',
      '.next/**',
      'node_modules/**',
      'dist/**',
      'build/**',
    ]
  },
  // Basic Next.js/React linting without FlatCompat to avoid circular refs
  {
    files: ['**/*.{js,jsx,ts,tsx}'],
    rules: {
      'react/no-unescaped-entities': 'off',
    },
  },
  // TypeScript configuration
  {
    files: ['**/*.{ts,tsx}'],
    languageOptions: {
      parser: typescriptParser,
      parserOptions: {
        ecmaVersion: 'latest',
        sourceType: 'module',
        project: './tsconfig.json',
        tsconfigRootDir: __dirname,
      },
    },
    plugins: {
      '@typescript-eslint': typescriptEslint,
      'unused-imports': unusedImports,
    },
    rules: {
      'react/no-unescaped-entities': 'off',
      'unused-imports/no-unused-imports': 'warn',
      'unused-imports/no-unused-vars': 'warn',
      '@typescript-eslint/no-explicit-any': 'error',
      '@typescript-eslint/no-explicit-any': 'warn',
      '@typescript-eslint/ban-ts-comment': 'warn',
      "@typescript-eslint/no-restricted-types": [
        "error",
        {
          "types": {
            "unknown": "Avoid using 'unknown'. Use a more specific type instead.",
            "Function": "Avoid using 'Function' type. Specify the function signature explicitly.",
          },
        },
      ],
      "@typescript-eslint/consistent-type-assertions": [
        "error",
        {
          "assertionStyle": "never",
        },
      ],
      "@typescript-eslint/no-non-null-assertion": "error",
      "@typescript-eslint/explicit-function-return-type": "error",
      "@typescript-eslint/member-ordering": [
        "error",
        {
          "default": {
            "memberTypes": ["signature", "field", "constructor", "method"],
          },
        },
      ],
      "@typescript-eslint/switch-exhaustiveness-check": "error",
      "@typescript-eslint/strict-boolean-expressions": "error",
      "@typescript-eslint/no-unused-vars": "error",
      "@typescript-eslint/consistent-type-definitions": "off",
      "camelcase": "off",
      "@typescript-eslint/naming-convention": [
        "error",
        {
          "selector": "variableLike",
          "format": ["camelCase", "PascalCase", "UPPER_CASE"],
        },
      ],
    },
  },
  // Override rules for generated API code
  {
    files: ['src/lib/api/generated/**/*.{ts,js}'],
    rules: {
      '@typescript-eslint/no-explicit-any': 'off',
      '@typescript-eslint/ban-ts-comment': 'off',
    },
  },
  // Targeted overrides for editor preview/plugins where raw HTML rendering is expected
  {
    files: [
      'src/components/content/editor/plugins/**/*.{ts,tsx}',
      'src/components/block-content-editor/plugins/**/*.{ts,tsx}',
    ],
    rules: {
      '@next/next/no-img-element': 'off',
      'react-hooks/exhaustive-deps': 'off',
    },
  },
];

export default config;