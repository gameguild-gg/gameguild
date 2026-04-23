import { reactConfig } from '@game-guild/jest-config';

/** @type {import('jest').Config} */
const config = {
  ...reactConfig,
  displayName: '@game-guild/emception-ui',
  rootDir: '.',
  testMatch: ['<rootDir>/src/**/*.(test|spec).(ts|tsx|js|jsx)'],
  moduleNameMapper: {
    ...reactConfig.moduleNameMapper,
    '\\.(css|svg)$': 'identity-obj-proxy',
  },
};

export default config;
