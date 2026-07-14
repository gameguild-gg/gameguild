/** @type {import('jest').Config} */
const config = {
    testEnvironment: 'node',
    transform: {
        '^.+\\.(ts|tsx)$': ['babel-jest', { configFile: './babel.config.cjs' }],
    },
    testMatch: ['**/*.test.ts', '**/*.test.tsx'],
    moduleNameMapper: {
        '\\.css$': '<rootDir>/src/__mocks__/style.cjs',
        '^emception$': '<rootDir>/../core/src/index.ts',
        // Strip explicit .js extensions on relative imports so jest's CJS
        // resolver can find the corresponding .ts source files (NodeNext
        // ESM convention used by emception).
        '^(\\.{1,2}/.*)\\.js$': '$1',
    },
    extensionsToTreatAsEsm: [],
};

export default config;
