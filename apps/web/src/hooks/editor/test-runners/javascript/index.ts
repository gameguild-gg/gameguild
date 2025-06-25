import { runSimpleTestsJavaScript } from "./simple"
import { runInOutTestsJavaScript } from "./inout"
import { runPredicateTestsJavaScript } from "./predicate"

export const javascriptTestRunners = {
  simple: runSimpleTestsJavaScript,
  inout: runInOutTestsJavaScript,
  predicate: runPredicateTestsJavaScript,
}
