/** @type {import('jest').Config} */
const config = {
  projects: [
    '<rootDir>/apps/api/jest.config.mjs',
    '<rootDir>/apps/website/jest.config.mjs',
    '<rootDir>/apps/academy/jest.config.mjs',
    '<rootDir>/apps/console/jest.config.mjs',
    '<rootDir>/packages/emception/jest.config.mjs',
  ],
  collectCoverageFrom: [
    'apps/*/src/**/*.{js,jsx,ts,tsx}',
    '!apps/*/src/**/*.d.ts',
    '!apps/*/src/**/*.stories.{js,jsx,ts,tsx}',
    '!apps/*/src/**/*.config.{js,mjs,ts}',
  ],
  coverageDirectory: 'coverage',
  coverageReporters: ['text', 'lcov', 'html', 'json-summary'],
  // Performance optimizations
  maxWorkers: '50%',
  cache: true,
};

export default config;
