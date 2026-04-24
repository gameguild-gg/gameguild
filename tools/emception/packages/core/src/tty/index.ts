// TTY surface re-exports. Pure interface + line buffer.

export type { IOProvider } from './io-provider';
export { LineBuffer } from './line-buffer';
export { HeadlessIOProvider, type HeadlessIOProviderOptions } from './headless';
