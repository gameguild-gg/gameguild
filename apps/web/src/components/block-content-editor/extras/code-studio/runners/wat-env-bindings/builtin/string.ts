/**
 * String bindings (String namespace)
 */
export function createStringBindings(): Record<string, any> {
  return {
    fromCharCode: (charCode: number) => String.fromCharCode(charCode),
    fromCodePoint: (codePoint: number) => String.fromCodePoint(codePoint),
    fromCodePoints: (...codePoints: number[]) => String.fromCodePoint(...codePoints),
  }
}
