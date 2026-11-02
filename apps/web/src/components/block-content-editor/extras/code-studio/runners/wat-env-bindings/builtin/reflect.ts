/**
 * Reflect bindings (Reflect namespace)
 */
export function createReflectBindings() {
  return {
    get: (target: object, propertyKey: string): unknown => Reflect.get(target, propertyKey),
    has: (target: object, propertyKey: string) => Reflect.has(target, propertyKey),
    set: (target: object, propertyKey: string, value: unknown) =>
      Reflect.set(target, propertyKey, value),
  }
}
