type JSObjectInput = object | string | number | boolean | bigint | symbol;

/**
 * JavaScript environment bindings
 */
export function createJSBindings(
  stdout: (text: string) => void,
  stderr: (text: string) => void
) {
  return {
    // Memory operations
    mem: {
      grow: (pages: number) => pages,
    },

    // String operations
    str: {
      fromCharCode: (code: number) => String.fromCharCode(code),
      charCodeAt: (str: string, index: number) => str.charCodeAt(index),
      length: (str: string) => str.length,
    },

    // Array operations
    arr: {
      length: (arr: readonly unknown[]) => arr.length,
      push: <T>(arr: T[], item: T) => arr.push(item),
      pop: <T>(arr: T[]) => arr.pop(),
    },

    // Object operations
    obj: {
      keys: (obj: JSObjectInput) => Object.keys(obj),
      values: (obj: JSObjectInput): unknown[] => Object.values(obj),
      entries: (obj: JSObjectInput): [string, unknown][] => Object.entries(obj),
    },
  }
}
