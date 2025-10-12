/**
 * CI-optimized Jest configuration
 * Use this configuration in CI/CD environments for better performance and reporting
 */

/** @type {import('jest').Config} */
export const ciConfig = {
  // Optimizations for CI
  maxWorkers: 2, // Limit workers in CI
  cache: false, // Disable cache in CI
  bail: 1, // Stop on first failure
  verbose: false, // Reduce output verbosity

  // Better error reporting
  reporters: [
    'default',
    [
      'jest-junit',
      {
        outputDirectory: 'test-results',
        outputName: 'junit.xml',
        suiteName: 'Jest Tests',
      },
    ],
  ],

  // Coverage settings for CI
  collectCoverage: true,
  coverageReporters: ['text', 'lcov', 'cobertura'],
  coverageDirectory: 'coverage',

  // Test timeouts
  testTimeout: 30000, // Longer timeout for CI

  // Fail fast settings
  errorOnDeprecated: true,

  // Memory management
  logHeapUsage: true,

  // Force exit to prevent hanging
  forceExit: true,
  detectOpenHandles: true,
};
