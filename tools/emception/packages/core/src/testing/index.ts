// Testing subsystem barrel.
export {
    compileMatcher,
    queryClangAst,
    runMatcher,
    type ClangAstNode,
    type CompiledMatcher,
    type MatchResult
} from './clang-query/matcher.js';
export {
    parseDoctestConsole,
    type DoctestCounts,
    type DoctestFailure,
    type DoctestReport
} from './doctest/parse.js';
export { runTests, type TestKindHandler } from './engine.js';
export { computeScore, type ScoreResult } from './score.js';
export { withWorkspaceOverlay, type WorkspaceOverlayFile, type WorkspaceOverlayTarget } from './workspace-overlay.js';
export {
    escapeCppString,
    generateDoctestHarness,
    mapCppType,
    serializeCppLiteral,
    type FunctionParameter,
    type FunctionParameterType,
    type FunctionParameterWithName,
    type FunctionalTestCase,
    type FunctionalTestSignature,
} from './functional/harness.js';
export {
    buildTestPlan,
    type BuildTestPlanMode,
    type BuildTestPlanOptions,
    type BuildTestPlanResult,
    type BundleFileMeta,
    type CodingAssignmentContent,
    type FileEncoding,
    type FileVisibility,
    type FunctionalTestCase as WireFunctionalTestCase,
    type GeneratedFile,
    type Test,
    type TestFunctionData,
    type TestSuite,
    type WireFunctionParameterType,
} from './assignment-plan.js';

