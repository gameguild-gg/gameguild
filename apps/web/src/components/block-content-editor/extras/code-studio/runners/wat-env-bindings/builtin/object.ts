type ObjectInput = object | string | number | boolean | bigint | symbol;

/**
 * Object bindings (Object namespace)
 */
export function createObjectBindings() {
  return {
    is: (a: unknown, b: unknown) => Object.is(a, b),
    keys: (obj: ObjectInput) => Object.keys(obj),
    values: (obj: ObjectInput): unknown[] => Object.values(obj),
    entries: (obj: ObjectInput): [string, unknown][] => Object.entries(obj),
  }
}
