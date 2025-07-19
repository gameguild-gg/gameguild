import { runSimpleTestsTypeScript } from './simple';
import { runInOutTestsTypeScript } from './inout';
import { runPredicateTestsTypeScript } from './predicate';

// Export individual test runners
export { runSimpleTestsTypeScript, runInOutTestsTypeScript, runPredicateTestsTypeScript };

// Export the test runners object
export const typescriptTestRunners = {
  simple: runSimpleTestsTypeScript,
  inout: runInOutTestsTypeScript,
  predicate: runPredicateTestsTypeScript,
};
