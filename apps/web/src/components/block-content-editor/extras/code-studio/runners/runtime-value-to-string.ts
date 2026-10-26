type Primitive = string | number | bigint | boolean | symbol | null | undefined

const isPrimitive = (value: unknown): value is Primitive =>
  value === null || value === undefined || typeof value === 'string' || typeof value === 'number'
  || typeof value === 'bigint' || typeof value === 'boolean' || typeof value === 'symbol'

const isRuntimeObject = (value: unknown): value is object => !isPrimitive(value)

const convertedPrimitiveText = (value: Primitive): string => {
  if (typeof value === 'symbol') throw new TypeError('Cannot convert a Symbol value to a string')
  return String(value)
}

/** Preserve String(value) output while checking unknown conversion hooks and their results.
 * https://tc39.es/ecma262/multipage/abstract-operations.html#sec-toprimitive
 */
export function runtimeValueToString(value: unknown): string {
  if (isPrimitive(value)) return String(value)
  if (isRuntimeObject(value)) {
    const exotic: unknown = Reflect.get(value, Symbol.toPrimitive)
    if (exotic !== undefined && exotic !== null) {
      if (typeof exotic !== 'function') throw new TypeError('Cannot convert object to primitive value')
      const result: unknown = Reflect.apply(exotic, value, ['string'])
      if (!isPrimitive(result)) throw new TypeError('Cannot convert object to primitive value')
      return convertedPrimitiveText(result)
    }

    for (const name of ['toString', 'valueOf']) {
      const method: unknown = Reflect.get(value, name)
      if (typeof method === 'function') {
        const result: unknown = Reflect.apply(method, value, [])
        if (isPrimitive(result)) return convertedPrimitiveText(result)
      }
    }
  }
  throw new TypeError('Cannot convert object to primitive value')
}
