export type WebRequestLogLevel = 'info' | 'warn' | 'error';

export interface WebRequestLogInput {
  event: string;
  method: string;
  path: string;
  status: number;
  durationMs: number;
  requestId: string;
  level?: WebRequestLogLevel;
  action?: string;
  error?: unknown;
}

function newRequestId(): string {
  try {
    return crypto.randomUUID();
  } catch {
    return `req_${Date.now()}_${Math.random().toString(36).slice(2, 10)}`;
  }
}

export function getRequestId(headers?: Headers): string {
  return headers?.get('x-request-id') || headers?.get('x-correlation-id') || newRequestId();
}

export function elapsedMs(startedAt: number): number {
  return Math.round(performance.now() - startedAt);
}

export function getErrorMessage(error: unknown): string | undefined {
  if (!error) return undefined;
  if (error instanceof Error) return error.message;
  return String(error);
}

export function logWebRequest(input: WebRequestLogInput): void {
  const { level = input.status >= 500 ? 'error' : input.status >= 400 ? 'warn' : 'info', error, ...rest } = input;
  const payload = {
    service: 'gameguild-web',
    timestamp: new Date().toISOString(),
    ...rest,
    ...(error ? { error: getErrorMessage(error) } : {}),
  };

  const line = JSON.stringify(payload);
  if (level === 'error') {
    console.error(line);
    return;
  }
  if (level === 'warn') {
    console.warn(line);
    return;
  }
  console.info(line);
}
