import { nextJsConfig } from '@gameguild/jest-config';

/** @type {import('jest').Config} */
const jestConfig = {
  ...nextJsConfig,
  displayName: '@gameguild/website',
  rootDir: '.',
  testMatch: ['<rootDir>/src/**/*.(test|spec).(js|jsx|ts|tsx)'],
  moduleNameMapper: {
    ...nextJsConfig.moduleNameMapper,
    '^@/(.*)$': '<rootDir>/src/$1',
  },
  collectCoverageFrom: [
    'src/**/*.{js,jsx,ts,tsx}',
    '!src/**/*.d.ts',
    '!src/**/*.stories.{js,jsx,ts,tsx}',
    '!src/**/index.{js,jsx,ts,tsx}',
    '!src/app/**/layout.tsx',
    '!src/app/**/loading.tsx',
    '!src/app/**/error.tsx',
    '!src/app/**/not-found.tsx',
    '!src/app/**/global-error.tsx',
  ],
  coverageThreshold: {
    global: {
      branches: 70,
      functions: 70,
      lines: 70,
      statements: 70,
    },
  },
};

export default jestConfig;
