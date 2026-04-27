/** @type {import('jest').Config} */
const config = {
    testEnvironment: 'node',
    transform: {
        '^.+\\.(ts|tsx)$': ['babel-jest', { configFile: './babel.config.cjs' }],
    },
    testMatch: ['**/*.test.ts', '**/*.test.tsx'],
    moduleNameMapper: {
        '\\.css$': '<rootDir>/src/__mocks__/style.cjs',
    },
    extensionsToTreatAsEsm: [],
};

export default config;
