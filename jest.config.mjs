/** @type {import('jest').Config} */
const config = {
  projects: [],
  collectCoverageFrom: [
    'apps/*/src/**/*.{js,jsx,ts,tsx}',
    'packages/*/src/**/*.{js,jsx,ts,tsx}',
    '!**/*.d.ts',
    '!**/*.stories.{js,jsx,ts,tsx}',
  ],
  coverageDirectory: 'coverage',
  coverageReporters: ['text', 'lcov', 'html', 'json-summary'],
  maxWorkers: '50%',
  cache: true,
};

export default config;
