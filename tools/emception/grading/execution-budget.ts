import type { ToolResult } from 'emception';

export interface CodeExecutionBudget {
  readonly timeLimitMs: number;
  readonly outputLimitBytes: number;
}

export interface CodeBudgetedOperation {
  readonly execute: (onOutput: (chunk: Uint8Array) => void) => Promise<ToolResult>;
  readonly dispose: () => void;
}

export function validateCodeExecutionBudget(budget: CodeExecutionBudget): void {
  if (!Number.isSafeInteger(budget.timeLimitMs) || budget.timeLimitMs < 1 || budget.timeLimitMs > 30_000
    || !Number.isSafeInteger(budget.outputLimitBytes) || budget.outputLimitBytes < 1 || budget.outputLimitBytes > 2_000_000) {
    throw new TypeError('Invalid Code execution budget.');
  }
}

export async function executeBudgetedCode(
  budget: CodeExecutionBudget,
  operation: CodeBudgetedOperation,
): Promise<ToolResult> {
  validateCodeExecutionBudget(budget);
  let settled = false;
  let outputBytes = 0;
  let timer: ReturnType<typeof setTimeout> | undefined;
  let exhaust: (kind: 'runtime' | 'output') => void = () => undefined;
  const started = Date.now();
  const exceeded = new Promise<never>((_resolve, reject) => {
    exhaust = kind => {
      if (settled) return;
      settled = true;
      clearTimeout(timer);
      try { operation.dispose(); }
      catch { /* Cancellation cannot replace the sanitized budget failure. */ }
      reject(new Error(`Code execution exceeded its ${kind} budget.`));
    };
    timer = setTimeout(() => exhaust('runtime'), budget.timeLimitMs);
  });
  const observe = (chunk: Uint8Array) => {
    if (settled) return;
    outputBytes += chunk.byteLength;
    if (outputBytes > budget.outputLimitBytes) exhaust('output');
  };
  try {
    const running = Promise.resolve().then(() => operation.execute(observe));
    const result = await Promise.race([running, exceeded]);
    if (result.timedOut || Date.now() - started >= budget.timeLimitMs) {
      exhaust('runtime');
      return await exceeded;
    }
    // Some runtimes return captured output without invoking the stream sinks.
    const encoder = new TextEncoder();
    if (result.stdout.length + result.stderr.length > budget.outputLimitBytes
      || encoder.encode(result.stdout).byteLength + encoder.encode(result.stderr).byteLength > budget.outputLimitBytes) {
      exhaust('output');
      return await exceeded;
    }
    return result;
  } finally {
    settled = true;
    clearTimeout(timer);
  }
}
