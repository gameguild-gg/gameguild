/** @type {import('jest').Config} */
const baseConfig = {
  clearMocks: true,
  restoreMocks: true,
  moduleFileExtensions: ['ts', 'tsx', 'js', 'jsx', 'json'],
  testPathIgnorePatterns: ['<rootDir>/node_modules/', '<rootDir>/dist/', '<rootDir>/build/', '<rootDir>/.next/'],
  coverageDirectory: 'coverage',
  coveragePathIgnorePatterns: [
    '/node_modules/',
    '/dist/',
    '/build/',
    '/.next/',
    '/coverage/',
    '\\.d\\.ts$',
    '\\.stories\\.(js|jsx|ts|tsx)$',
    '\\.configs\\.(js|mjs|ts)$',
  ],
  collectCoverageFrom: [
    'src/**/*.{js,jsx,ts,tsx}',
    '!src/**/*.d.ts',
    '!src/**/*.stories.{js,jsx,ts,tsx}',
    '!src/**/*.configs.{js,mjs,ts}',
    '!src/**/index.{js,jsx,ts,tsx}',
  ],
  // Global test setup
  setupFilesAfterEnv: [],
  // Test environment optimization
  testEnvironmentOptions: {
    url: 'http://localhost',
  },
  // Performance optimizations
  maxWorkers: '50%',
  cache: true,
};

/** @type {import('jest').Config} */
const nodeConfig = {
  ...baseConfig,
  testEnvironment: 'node',
  moduleNameMapper: {
    '^@/(.*)$': '<rootDir>/src/$1',
  },
  transform: {
    '^.+\\.(t|j)s$': [
      'ts-jest',
      {
        tsconfig: {
          module: 'commonjs',
        },
      },
    ],
  },
  testRegex: ['.*\\.(test|spec)\\.(t|j)s$'],
  collectCoverageFrom: [...baseConfig.collectCoverageFrom, '!src/main.ts', '!src/**/*.module.ts'],
};

/** @type {import('jest').Config} */
const reactConfig = {
  ...baseConfig,
  testEnvironment: 'jsdom',
  setupFilesAfterEnv: ['@testing-library/jest-dom'],
  moduleNameMapper: {
    '^@/(.*)$': '<rootDir>/src/$1',
    '\\.(css|less|scss|sass)$': 'identity-obj-proxy',
    '\\.(jpg|jpeg|png|gif|eot|otf|webp|svg|ttf|woff|woff2|mp4|webm|wav|mp3|m4a|aac|oga)$': 'jest-transform-stub',
  },
  transform: {
    '^.+\\.(js|jsx|ts|tsx)$': [
      'babel-jest',
      {
        presets: [['@babel/preset-env', { targets: { node: 'current' } }], ['@babel/preset-react', { runtime: 'automatic' }], '@babel/preset-typescript'],
      },
    ],
  },
  testMatch: ['**/*.{test,spec}.{js,jsx,ts,tsx}'],
  // React-specific optimizations
  testEnvironmentOptions: {
    url: 'http://localhost:3000',
  },
};

/** @type {import('jest').Config} */
const nextJsConfig = {
  ...reactConfig,
  moduleNameMapper: {
    ...reactConfig.moduleNameMapper,
    '^@/components/(.*)$': '<rootDir>/src/components/$1',
    '^@/app/(.*)$': '<rootDir>/src/app/$1',
    '^@/lib/(.*)$': '<rootDir>/src/lib/$1',
    '^@/utils/(.*)$': '<rootDir>/src/utils/$1',
    '^@/hooks/(.*)$': '<rootDir>/src/hooks/$1',
    '^@/types/(.*)$': '<rootDir>/src/types/$1',
    '^@/config/(.*)$': '<rootDir>/src/configs/$1',
    '^@/styles/(.*)$': '<rootDir>/src/styles/$1',
  },
  transform: {
    '^.+\\.(js|jsx|ts|tsx)$': [
      'babel-jest',
      {
        presets: [['@babel/preset-env', { targets: { node: 'current' } }], ['@babel/preset-react', { runtime: 'automatic' }], '@babel/preset-typescript'],
      },
    ],
  },
};

/** @type {import('jest').Config} */
const nestConfig = {
  ...nodeConfig,
  rootDir: 'src',
  testRegex: '.*\\.spec\\.ts$',
  moduleNameMapper: {
    '^@/(.*)$': '<rootDir>/$1',
    '^src/(.*)$': '<rootDir>/$1',
  },
  collectCoverageFrom: ['**/*.(t|j)s', '!**/*.d.ts', '!**/*.module.ts', '!**/*.configs.ts', '!main.ts', '!**/__tests__/**', '!**/*.spec.ts'],
  coverageDirectory: '../coverage',
};

/** @type {import('jest').Config} */
const performanceConfig = {
  ...nodeConfig,
  displayName: 'performance',
  testMatch: ['<rootDir>/**/*.perf.(js|jsx|ts|tsx)'],
  testTimeout: 60000, // 60 seconds for performance tests
  setupFilesAfterEnv: [],
  coverageDirectory: './coverage-perf',
  verbose: true,
};

export default baseConfig;
export { nodeConfig, reactConfig, nextJsConfig, nestConfig, performanceConfig };
export { ciConfig } from './ci.js';
