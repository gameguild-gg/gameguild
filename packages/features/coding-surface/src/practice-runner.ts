import { composeCodingNativeFunctionObservation, parseCodingLearnerDelivery, type CodingDraftScalar, type CodingScalarValue } from '@game-guild/coding';
import { withWorkspaceOverlay } from 'emception/testing';
import { codingLearnerPractice } from './learner-practice';
import type { CodingPracticeCase } from './learner-practice-types';
import { withCodingLocalRuntime } from './local-execution';
import type { CodingPracticeCheck, CodingPracticeRunner, CodingPracticeRunResult } from './practice-run-types';
import { resolveCodingRuntimeCapabilities } from './runtime-capabilities';

export const runCodingPractice: CodingPracticeRunner = async (options) => {
  if (options.request.contextKey !== options.contextKey) throw new TypeError('Practice execution context changed.');
  if (!Number.isSafeInteger(options.executionId) || options.executionId < 1) throw new TypeError('Invalid practice execution identity.');
  const delivery = parseCodingLearnerDelivery(options.delivery);
  const state = codingLearnerPractice.create(options.contextKey, delivery);
  const request = options.request;
  const selected =
    request.source === 'public'
      ? state.publicCases.find((value) => value.id === request.id)
      : codingLearnerPractice.reduce(codingLearnerPractice.reduce(state, { kind: 'add', id: request.value.id }), { kind: 'replace', value: request.value })
          .personalCases[0];
  if (!selected) throw new TypeError('The public case does not belong to this practice context.');
  const environment = delivery.environment;
  if (!environment || environment.runtime !== 'wasi' || delivery.limits.memoryLimitBytes !== undefined) {
    throw new Error('Practice execution is unavailable for this environment or RAM limit.');
  }
  const capability = resolveCodingRuntimeCapabilities({
    exerciseType: delivery.exerciseType,
    execution: { environment, toolchain: delivery.toolchain },
    mode: 'learner',
    readOnly: true,
    allowLearnerCreatedFiles: false,
    graphicsPanelVisible: false,
  }).tests;
  if (capability.status !== 'available') throw new Error(`Practice execution is unavailable: ${capability.reason}.`);
  const sources = delivery.files.filter((file) => file.encoding === 'text' && /\.(?:c|cc|cpp|cxx)$/i.test(file.path)).map((file) => file.path);
  if (!sources.length) throw new TypeError('Practice execution requires a native source file.');
  const expected = selected.kind === 'function' && selected.expected !== null ? scalar(selected.expected) : null;
  const expectedExit = selected.kind === 'stdio' && selected.expected.exitCode !== null ? integer(selected.expected.exitCode) : null;
  const support =
    selected.kind === 'function' && state.signature
      ? composeCodingNativeFunctionObservation({
          language: environment.language,
          signature: {
            name: state.signature.name,
            parameters: state.signature.parameters.map((parameter) => ({ ...parameter })),
            returnType: state.signature.returnType,
          },
          inputs: selected.inputs.map(scalar),
          outputLimitBytes: delivery.limits.outputLimitBytes,
          recordId: crypto.randomUUID().replaceAll('-', ''),
        })
      : null;
  if (selected.kind === 'function' && !support) throw new TypeError('A public function signature is required to execute personal inputs.');
  return withCodingLocalRuntime({ ...options, definition: delivery, requireSuccessfulBuild: true }, async (api) => {
    const execute = async (): Promise<CodingPracticeRunResult> => {
      const tool = await api.compileAndRun(undefined, {
        build: { sources: support ? [...sources, support.path] : sources },
        stdin: selected.kind === 'stdio' ? selected.stdin : 'none',
        stdout: 'capture',
        stderr: 'capture',
      });
      if (tool.timedOut || tool.signal) throw new Error(`Practice program did not complete${tool.signal ? `: ${tool.signal}` : ': timeout'}.`);
      let returnValue: CodingScalarValue | null = null;
      let stderr = tool.stderr;
      if (support && state.signature) {
        const begin = stderr.indexOf(support.recordPrefix);
        if (begin < 0) throw new Error('Practice function returned no observation.');
        const contentStart = begin + support.recordPrefix.length;
        const end = stderr.indexOf('\u001f', contentStart);
        if (end < 0 || stderr.indexOf(support.recordPrefix, contentStart) >= 0) throw new Error('Malformed or ambiguous practice return record.');
        const text = stderr.slice(contentStart, end);
        stderr = stderr.slice(0, begin) + stderr.slice(end + 1);
        if (state.signature.returnType === 'string') {
          if (!/^(?:[0-9a-f]{2})*$/.test(text)) throw new Error('Invalid native string observation.');
          const bytes = Uint8Array.from(text.match(/.{2}/g) ?? [], (byte) => Number.parseInt(byte, 16));
          returnValue = { type: 'string', value: new TextDecoder('utf-8', { fatal: true }).decode(bytes) };
        } else if (state.signature.returnType === 'boolean') {
          if (text !== '0' && text !== '1') throw new Error('Invalid native boolean observation.');
          returnValue = { type: 'boolean', value: text === '1' };
        } else returnValue = scalar({ type: state.signature.returnType, text });
      }
      const checks = comparisons(selected, { stdout: tool.stdout, stderr, exitCode: tool.exitCode }, expectedExit, expected, returnValue);
      return {
        contextKey: options.contextKey,
        executionId: options.executionId,
        selection: { source: request.source, id: selected.id },
        case: selected,
        observation: { stdout: tool.stdout, stderr, exitCode: tool.exitCode, durationMs: tool.durationMs, returnValue },
        comparison: checks.length ? { status: checks.every((check) => check.matches) ? 'passed' : 'failed', checks } : { status: 'observed' },
        academic: false,
      };
    };
    return support ? withWorkspaceOverlay(api.workspace, [support], execute) : execute();
  });
};

function comparisons(
  selected: CodingPracticeCase,
  actual: { stdout: string; stderr: string; exitCode: number },
  expectedExit: number | null,
  expected: CodingScalarValue | null,
  returned: CodingScalarValue | null,
): CodingPracticeCheck[] {
  if (selected.kind === 'function') {
    if (expected === null) return [];
    if (returned === null || expected.type !== returned.type) throw new Error('Practice return type changed.');
    const matches =
      expected.type === 'float' && returned.type === 'float' ? Number(expected.value) === Number(returned.value) : expected.value === returned.value;
    return [{ field: 'returnValue', expected: String(expected.value), actual: String(returned.value), matches }];
  }
  const checks: CodingPracticeCheck[] = [];
  for (const field of ['stdout', 'stderr'] as const) {
    const value = selected.expected[field];
    if (value !== null) checks.push({ field, expected: value, actual: actual[field], matches: value === actual[field] });
  }
  if (expectedExit !== null)
    checks.push({ field: 'exitCode', expected: String(expectedExit), actual: String(actual.exitCode), matches: actual.exitCode === expectedExit });
  return checks;
}

function scalar(buffer: Readonly<CodingDraftScalar>): CodingScalarValue {
  switch (buffer.type) {
    case 'integer':
      return { type: 'integer', value: integer(buffer.text) };
    case 'float':
      if (!/^-?(?:0|[1-9]\d*)(?:\.\d+)?(?:[eE][+-]?\d+)?$/.test(buffer.text) || /\s/.test(buffer.text) || !Number.isFinite(Number(buffer.text))) {
        throw new TypeError('Enter a complete finite decimal.');
      }
      return { type: 'float', value: buffer.text };
    case 'boolean':
      if (buffer.text !== 'true' && buffer.text !== 'false') throw new TypeError('Enter true or false.');
      return { type: 'boolean', value: buffer.text === 'true' };
    case 'string':
      return { type: 'string', value: buffer.text };
  }
}

function integer(text: string): number {
  const value = Number(text);
  if (!/^-?(?:0|[1-9]\d*)$/.test(text) || /\s/.test(text) || !Number.isInteger(value) || value < -2_147_483_648 || value > 2_147_483_647) {
    throw new TypeError('Enter a complete 32-bit integer.');
  }
  return value;
}
