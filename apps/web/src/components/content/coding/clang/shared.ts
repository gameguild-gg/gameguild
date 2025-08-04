import { AssertError } from './errors';

export function readStr(u8: Uint8Array, o: number, len: number = -1): string {
  let str = '';
  let end = u8.length;
  if (len !== -1) end = o + len;
  for (let i = o; i < end && u8[i] !== 0; ++i) str += String.fromCharCode(u8[i]);
  return str;
}

export function assert(cond: boolean): void {
  if (!cond) {
    throw new AssertError('assertion failed.');
  }
}

export function getInstance(module: WebAssembly.Module, imports: WebAssembly.Imports): Promise<WebAssembly.Instance> {
  return WebAssembly.instantiate(module, imports);
}

export function getImportObject(obj: Record<string, unknown>, names: string[] = []): { [key: string]: (...args: unknown[]) => unknown } {
  const result: { [key: string]: (...args: unknown[]) => unknown } = {};
  for (const name of names) {
    const fn = obj[name];
    if (typeof fn === 'function') {
      result[name] = fn.bind(obj);
    }
  }
  return result;
}

export function msToSec(start: number, end: number): string {
  return ((end - start) / 1000).toFixed(2);
}

export const ESUCCESS = 0;

export const RAF_PROC_EXIT_CODE = 0xc0c0a;
