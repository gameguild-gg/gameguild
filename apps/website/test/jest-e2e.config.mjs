import { nextJsConfig } from '@gameguild/jest-config';

/** @type {import('jest').Config} */
const e2eConfig = {
  ...nextJsConfig,
  displayName: '@gameguild/website-e2e',
  rootDir: '.',
  testMatch: ['<rootDir>/**/*.e2e.{js,jsx,ts,tsx}'],
  testEnvironment: 'node',
  collectCoverageFrom: [
    '../src/**/*.(t|j)s?(x)',
    '!../src/**/*.d.ts',
    '!../src/**/*.config.*',
    '!../src/**/*.stories.*',
    '!../src/styles/**',
    '!../src/app/**/layout.tsx',
    '!../src/app/**/loading.tsx',
    '!../src/app/**/error.tsx',
    '!../src/app/**/not-found.tsx',
    '!../src/app/**/global-error.tsx',
  ],
  coverageDirectory: './coverage-e2e',
  moduleNameMapper: {
    '^@/(.*)$': '<rootDir>/../src/$1',
    '^src/(.*)$': '<rootDir>/../src/$1',
  },
  setupFilesAfterEnv: [],
};

export default e2eConfig;
