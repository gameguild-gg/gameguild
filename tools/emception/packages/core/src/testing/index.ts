// Phase 5 — testing subsystem barrel.
export { runTests, type TestKindHandler } from './engine.js';
export {
    compileMatcher,
    queryClangAst,
    runMatcher,
    type ClangAstNode,
    type CompiledMatcher,
    type MatchResult,
} from './clang-query/matcher.js';
