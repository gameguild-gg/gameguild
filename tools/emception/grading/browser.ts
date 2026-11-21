import { createEmception } from '@gameguild/emception-browser';
import { runTests, withWorkspaceOverlay } from 'emception/testing';
import { resolveBuild, ToolchainPreset, type TestPlan } from 'emception';
import { buildAssessmentExecutionPlan } from '../../../packages/infrastructure/ui-emception/src/assessment/plan';
import { createAssessmentWorkspaceConfig, type CodingLanguage } from '../../../packages/infrastructure/ui-emception/src/assessment/presets';
import type { CodingAssessmentDefinition } from '../../../packages/infrastructure/ui-emception/src/assessment/types';
import { executeBudgetedCode, type CodeBudgetedOperation } from './execution-budget.js';

const EXECUTION_BUDGET = Object.freeze({ timeLimitMs: 30_000, outputLimitBytes: 2_000_000 });

interface CodeWorkerRequest {
  definition: CodingAssessmentDefinition;
  files: Record<string, { content: string; encoding: 'text' }>;
}

/** Runs only in a fresh, server-owned Chromium context. This is never a learner API. */
export async function executeCodeAssessment(request: CodeWorkerRequest): Promise<boolean[]> {
  const definition = request.definition;
  const canonical = (path: string) => '/home/user/' + path.replace(/^\/(?:home\/user|user)\//, '');
  const files = Object.fromEntries(Object.entries(definition.Data.Files).map(([path, file]) => [
    canonical(path), { encoding: file.Encoding ?? 'text', content: file.Content },
  ]));
  for (const [path, file] of Object.entries(request.files)) {
    files[canonical(path)] = file;
  }
  const workspace = createAssessmentWorkspaceConfig(definition.Environment.Language as CodingLanguage, files);
  const sources = Object.keys(files).filter((path) => /\.(?:c|cc|cpp|cxx)$/i.test(path));
  const build = resolveBuild({ preset: workspace.compile.toolchain,
    callsite: { sources, output: workspace.compile.output } });
  if (build.toolchain === ToolchainPreset.CMake || build.toolchain === ToolchainPreset.Python) {
    throw new Error('This Code adapter requires a native compilation preset.');
  }
  const api = await createEmception({ tty: 'none', manifestUrl: '/cdn/manifest.json' });
  let disposed = false;
  const dispose = () => {
    if (disposed) return;
    disposed = true;
    api.dispose();
  };
  try {
    let failed = false;
    let infrastructureFailure: unknown;
    let rejectFailure: (error: unknown) => void = () => undefined;
    const failure = new Promise<never>((_resolve, reject) => { rejectFailure = reject; });
    const protect = async (execute: CodeBudgetedOperation['execute']) => {
      if (failed) throw infrastructureFailure;
      try {
        return await executeBudgetedCode(EXECUTION_BUDGET, { execute, dispose });
      } catch (error) {
        failed = true;
        infrastructureFailure = error;
        rejectFailure(error);
        throw error;
      }
    };
    const compile = api.compileAndRun.bind(api);
    api.compileAndRun = (source, options) => protect(onOutput => compile(source, {
      ...options, stdout: onOutput, stderr: onOutput,
    }));
    const run = api.run.bind(api);
    api.run = (command, argv, options) => protect(onOutput => run(command, argv, {
      ...options, stdout: onOutput, stderr: onOutput,
    }));
    await api.workspace.setBuild(build);
    for (const [path, file] of Object.entries(files)) {
      const data = file.encoding === 'base64'
        ? Uint8Array.from(atob(file.content), (character) => character.charCodeAt(0))
        : file.content;
      await api.workspace.writeFile(path, data);
    }
    const execution = buildAssessmentExecutionPlan(definition, 'full');
    // Build paths must refer to exactly the persisted workspace files. Header files
    // are available to includes, but are not compiled as translation units.
    const plan: TestPlan = {
      ...execution.plan,
      build,
      timeoutMsPerCase: EXECUTION_BUDGET.timeLimitMs,
    };
    const overlay = execution.overlay.map((file) => ({ ...file, path: canonical(file.path) }));
    // A terminated runtime cannot restore its scratch VFS. Failure must settle
    // independently of pending overlay cleanup; the host then closes the context.
    const report = await Promise.race([failure, withWorkspaceOverlay(api.workspace, overlay,
      () => runTests(api, plan))]);
    // The reusable preview engine represents errors as failed cases. Official
    // grading must distinguish a failed student test from unavailable execution.
    if (failed) throw infrastructureFailure;
    if (report.cases.length !== plan.cases.length) throw new Error('Incomplete Code test report.');
    return report.cases.map((test) => test.passed);
  } finally {
    dispose();
  }
}
