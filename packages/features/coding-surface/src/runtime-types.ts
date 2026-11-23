import type { CodingToolchainIdentity } from '@game-guild/coding';
import type { BrowserEmceptionAPI, CreateEmceptionOptions, ParseManifestOptions } from '@gameguild/emception-browser';
import type { FSManifest } from 'emception';

export type CodingRuntimeFailureReason = 'unsupported-toolchain' | 'incompatible-manifest' | 'runtime-unavailable';

export type CodingRuntimeSessionState =
  | { readonly status: 'idle' | 'loading' | 'ready' | 'disposed' }
  | { readonly status: 'error'; readonly reason: CodingRuntimeFailureReason };

/** Only public browser exports; importing this contract starts no runtime. */
export interface CodingRuntimeModule {
  parseManifest(value: unknown, options?: ParseManifestOptions): FSManifest;
  createEmception(options: CreateEmceptionOptions): Promise<BrowserEmceptionAPI>;
}

export interface CodingRuntimeSessionOptions {
  readonly toolchain: CodingToolchainIdentity;
  readonly manifestUrl: string;
  readonly loadModule?: () => Promise<CodingRuntimeModule>;
  readonly readManifest?: (url: string, signal: AbortSignal) => Promise<unknown>;
}

/** One surface/context owns the session; it is never a global runtime cache. */
export interface CodingRuntimeSession {
  getState(): CodingRuntimeSessionState;
  acquire(): Promise<BrowserEmceptionAPI>;
  dispose(): void;
}

export interface CodingRuntimeSessionFactory {
  (options: CodingRuntimeSessionOptions): CodingRuntimeSession;
}
