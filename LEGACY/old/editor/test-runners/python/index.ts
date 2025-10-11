import { runSimpleTestsPython } from './simple';
import { runInOutTestsPython } from './inout';
import { runPredicateTestsPython } from './predicate';

export const pythonTestRunners = {
  simple: runSimpleTestsPython,
  inout: runInOutTestsPython,
  predicate: runPredicateTestsPython,
};
