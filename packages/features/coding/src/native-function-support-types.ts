import type { CodingFunctionTest } from './contracts';

interface CodingNativeFunctionSupportContext {
  readonly index: number;
  /** Already authorized public or server-only cases; no projection happens here. */
  readonly test: Pick<CodingFunctionTest, 'function' | 'cases'>;
}

export type CodingNativeFunctionSupportRequest = CodingNativeFunctionSupportContext & (
  | { readonly language: 'c' }
  | {
    readonly language: 'cpp';
    /** The consumer supplies the unchanged public harness for this bridge symbol. */
    readonly createCppHarness: (bridgeSymbol: string) => string;
  }
);

export interface CodingNativeFunctionSupportFile {
  /** Outside the submitted workspace; use a restoring public workspace overlay. */
  readonly path: string;
  readonly content: string;
}

export interface CodingNativeFunctionSupportComposer {
  (request: CodingNativeFunctionSupportRequest): CodingNativeFunctionSupportFile;
}
