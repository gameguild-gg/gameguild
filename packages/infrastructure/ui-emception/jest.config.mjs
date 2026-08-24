import { reactConfig } from '@game-guild/jest-config';

/** @type {import('jest').Config} */
const config = {
  ...reactConfig,
  displayName: '@game-guild/emception-ui',
  rootDir: '.',
  moduleNameMapper: {
    ...reactConfig.moduleNameMapper,
    '^emception/testing$': '<rootDir>/../../../tools/emception/packages/core/src/testing/index.ts',
    '^emception$': '<rootDir>/../../../tools/emception/packages/core/src/index.ts',
    // Emception's source uses NodeNext `.js` specifiers for TypeScript files.
    // Jest executes the workspace sources through Babel, so resolve them to
    // their source extension just as the vanilla IDE test suite does.
    '^(\\.{1,2}/.*)\\.js$': '$1',
    '\\.(css|svg)$': 'identity-obj-proxy',
  },
};

export default config;
