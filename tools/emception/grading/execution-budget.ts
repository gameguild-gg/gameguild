import type { ToolResult } from 'emception';

export interface CodeExecutionBudget {
  readonly timeLimitMs: number;
  readonly outputLimitBytes: number;
}

export interface CodeBudgetedOperation {
  readonly execute: (
    onOutput: (chunk: Uint8Array) => void,
    onPhase: (phase: 'compile' | 'link' | 'run') => void,
  ) => Promise<ToolResult>;
  readonly dispose: () => void;
}

export interface CodePreparationBudget {
  readonly timeLimitMs: number;
}

// Infrastructure policy, never a teacher-controlled or persisted exercise limit.
export const CODE_PREPARATION_BUDGET: CodePreparationBudget = Object.freeze({ timeLimitMs: 120_000 });

export function validateCodeExecutionBudget(budget: CodeExecutionBudget): void {
  if (!Number.isSafeInteger(budget.timeLimitMs) || budget.timeLimitMs < 1 || budget.timeLimitMs > 30_000
    || !Number.isSafeInteger(budget.outputLimitBytes) || budget.outputLimitBytes < 1 || budget.outputLimitBytes > 2_000_000) {
    throw new TypeError('Invalid Code execution budget.');
  }
}

export async function executeBudgetedCode(
  budget: CodeExecutionBudget,
  operation: CodeBudgetedOperation,
  preparation?: CodePreparationBudget,
): Promise<ToolResult> {
  validateCodeExecutionBudget(budget);
  if (preparation && (!Number.isSafeInteger(preparation.timeLimitMs)
    || preparation.timeLimitMs < 1 || preparation.timeLimitMs > CODE_PREPARATION_BUDGET.timeLimitMs)) {
    throw new TypeError('Invalid Code preparation budget.');
  }
  let settled = false;
  let outputBytes = 0;
  let timer: ReturnType<typeof setTimeout> | undefined;
  let phase: 'compile' | 'link' | 'run' | undefined;
  let deadline = Date.now() + (preparation?.timeLimitMs ?? budget.timeLimitMs);
  let budgetKind: 'preparation' | 'runtime' = preparation ? 'preparation' : 'runtime';
  let stop: (error: Error) => Error = error => error;
  const budgetError = (kind: 'preparation' | 'runtime' | 'output') => new Error(`Code execution exceeded its ${kind} budget.`);
  const exceeded = new Promise<never>((_resolve, reject) => {
    stop = error => {
      if (settled) return error;
      settled = true;
      clearTimeout(timer);
      try { operation.dispose(); }
      catch { /* Cancellation cannot replace the sanitized budget failure. */ }
      reject(error);
      return error;
    };
    timer = setTimeout(() => stop(budgetError(budgetKind)), deadline - Date.now());
  });
  const observePhase: Parameters<CodeBudgetedOperation['execute']>[1] = next => {
    if (settled || !preparation) return;
    if (Date.now() >= deadline) throw stop(budgetError(budgetKind));
    const expected = phase === undefined ? 'compile' : phase === 'compile' ? 'link' : phase === 'link' ? 'run' : undefined;
    if (next !== expected) throw stop(new Error('Code runtime did not report a valid execution phase sequence.'));
    phase = next;
    if (next === 'run') {
      clearTimeout(timer);
      budgetKind = 'runtime';
      deadline = Date.now() + budget.timeLimitMs;
      timer = setTimeout(() => stop(budgetError('runtime')), budget.timeLimitMs);
    }
  };
  const observe = (chunk: Uint8Array) => {
    if (settled) return;
    outputBytes += chunk.byteLength;
    if (outputBytes > budget.outputLimitBytes) stop(budgetError('output'));
  };
  try {
    const running = Promise.resolve().then(() => operation.execute(observe, observePhase));
    const result = await Promise.race([running, exceeded]);
    if (result.timedOut || Date.now() >= deadline) {
      stop(budgetError(budgetKind));
      return await exceeded;
    }
    if (preparation && (phase === undefined || result.phase !== phase)) {
      stop(new Error('Code runtime did not report a valid terminal execution phase.'));
      return await exceeded;
    }
    // Some runtimes return captured output without invoking the stream sinks.
    const encoder = new TextEncoder();
    if (result.stdout.length + result.stderr.length > budget.outputLimitBytes
      || encoder.encode(result.stdout).byteLength + encoder.encode(result.stderr).byteLength > budget.outputLimitBytes) {
      stop(budgetError('output'));
      return await exceeded;
    }
    return result;
  } finally {
    settled = true;
    clearTimeout(timer);
  }
}
