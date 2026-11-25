import { parseCodingAuthoringDefinition, parseCodingLearnerDelivery } from '@game-guild/coding';
import { computeScore, runTests, withWorkspaceOverlay } from 'emception/testing';
import { buildCodingExecutionPlan } from './execution-plan';
import { withCodingLocalRuntime } from './local-execution';
import { resolveCodingRuntimeCapabilities } from './runtime-capabilities';
import type { CodingRuntimeSessionFactory } from './runtime-types';
import type { CodingPreviewResult, CodingSurfaceDefinition, CodingSurfaceMode } from './types';

export interface CodingPreviewRunOptions {
  readonly definition: CodingSurfaceDefinition;
  readonly mode: CodingSurfaceMode;
  readonly manifestUrl: string;
  readonly signal: AbortSignal;
  readonly createRuntime?: CodingRuntimeSessionFactory;
  readonly maxScore?: number;
  readonly passingScore?: number;
  /** Exercise preparation must not mistake a broken build for a caught wrong answer. */
  readonly requireSuccessfulBuild?: boolean;
}

export async function runCodingPreview(options: CodingPreviewRunOptions): Promise<CodingPreviewResult> {
  const definition = 'tests' in options.definition ? parseCodingAuthoringDefinition(options.definition) : parseCodingLearnerDelivery(options.definition);
  const environment = definition.environment;
  if (!environment || environment.runtime !== 'wasi' || definition.limits.memoryLimitBytes !== undefined) {
    throw new Error('Local tests are unavailable for this execution environment or RAM limit.');
  }
  const capability = resolveCodingRuntimeCapabilities({
    exerciseType: definition.exerciseType,
    execution: { environment, toolchain: definition.toolchain },
    mode: options.mode === 'author' ? 'author-test' : options.mode,
    readOnly: true,
    allowLearnerCreatedFiles: false,
    graphicsPanelVisible: false,
  }).tests;
  if (capability.status !== 'available') throw new Error(`Local tests are unavailable: ${capability.reason}.`);
  const scope = options.mode === 'learner' ? 'public' : 'full';
  const execution = buildCodingExecutionPlan(definition, scope);
  if (!execution.plan.cases.length) throw new Error('No tests are configured for this preview scope.');
  return withCodingLocalRuntime(options, async (api) => {
    const report = await withWorkspaceOverlay(api.workspace, execution.overlay, () => runTests(api, execution.plan, { signal: options.signal }));
    return { scope, report, score: computeScore(report, execution.plan, options.maxScore ?? 100, options.passingScore ?? 60), academic: false };
  });
}
