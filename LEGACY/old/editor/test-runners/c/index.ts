import type { TestRunnerOptions } from '../types';
import { runCInOutTests } from './inout';
import { runCPredicateTests } from './predicate';

/**
 * Run C tests based on the test type
 */
export async function runCTests(options: TestRunnerOptions): Promise<void> {
  const { fileCases } = options;

  // Get the test type from the first test case
  const testType = fileCases[0]?.type || 'inout';

  switch (testType) {
    case 'inout':
      return runCInOutTests(options);
    case 'predicate':
      return runCPredicateTests(options);
    default:
      options.addOutput(`Unsupported test type for C: ${testType}`);
      options.setIsExecuting(false);
  }
}

/**
 * C test runners for different test types
 */
export const cTestRunners = {
  inout: runCInOutTests,
  predicate: runCPredicateTests,
};
