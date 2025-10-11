import { runInOutTests } from './inout';
import { runPredicateTests } from './predicate';
import type { TestCase, TestResult, TestRunner } from '../types';

/**
 * Run tests for C header files
 */
export async function runHTests(code: string, tests: TestCase[], testType: string, executeC: (code: string) => Promise<string>): Promise<TestResult[]> {
  switch (testType) {
    case 'inout':
      return runInOutTests(code, tests, executeC);
    case 'predicate':
      return runPredicateTests(code, tests, executeC);
    default:
      throw new Error(`Unsupported test type: ${testType}`);
  }
}

/**
 * C header test runners
 */
export const hTestRunners: Record<string, TestRunner> = {
  inout: async (code, tests, options) => {
    if (!options.executeC) {
      throw new Error('C executor is required for H tests');
    }
    return runInOutTests(code, tests, options.executeC);
  },
  predicate: async (code, tests, options) => {
    if (!options.executeC) {
      throw new Error('C executor is required for H tests');
    }
    return runPredicateTests(code, tests, options.executeC);
  },
};
