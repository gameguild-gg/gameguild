import { describe, expect, it } from 'vitest'
import { runtimeValueToString } from './runtime-value-to-string'

describe('unknown runtime value string conversion', () => {
  it.each<{ value: unknown; expected: string }>([
    { value: null, expected: 'null' },
    { value: undefined, expected: 'undefined' },
    { value: 'text', expected: 'text' },
    { value: 42, expected: '42' },
    { value: -0, expected: '0' },
    { value: true, expected: 'true' },
    { value: BigInt(12), expected: '12' },
    { value: Symbol('value'), expected: 'Symbol(value)' },
    { value: [1, null, 'text'], expected: '1,,text' },
    { value: {}, expected: '[object Object]' },
  ])('preserves $expected', ({ value, expected }) => {
    expect(runtimeValueToString(value)).toBe(expected)
  })

  it('uses the string hint and original receiver for a conversion hook', () => {
    const value = {
      text: 'converted',
      [Symbol.toPrimitive](hint: string) { return `${this.text}:${hint}` },
    }
    expect(runtimeValueToString(value)).toBe('converted:string')
  })

  it('falls back from a nonprimitive toString result to valueOf', () => {
    expect(runtimeValueToString({ toString: () => ({}), valueOf: () => 7 })).toBe('7')
  })

  it('rejects invalid conversion hooks and nonprimitive hook results', () => {
    expect(() => runtimeValueToString({ [Symbol.toPrimitive]: 7 })).toThrow(TypeError)
    expect(() => runtimeValueToString({ [Symbol.toPrimitive]: () => ({}) })).toThrow(TypeError)
  })

  it('retains the distinction between a primitive Symbol and a boxed Symbol', () => {
    const boxed: unknown = Object(Symbol('value'))
    expect(() => runtimeValueToString(boxed)).toThrow(TypeError)
  })

  it('propagates conversion errors rather than hiding them', () => {
    const failure = new Error('conversion failed')
    expect(() => runtimeValueToString({ toString() { throw failure } })).toThrow(failure)
  })

  it('rejects an object without a primitive conversion', () => {
    const value: unknown = Object.create(null)
    expect(() => runtimeValueToString(value)).toThrow(TypeError)
  })
})
