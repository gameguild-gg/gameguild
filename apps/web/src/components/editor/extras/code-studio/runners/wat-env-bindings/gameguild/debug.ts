/**
 * GameGuild Debug Interface (gameguild:runtime/debug)
 * Custom component interface for debugging utilities
 * 
 * WIT definition:
 * ```wit
 * package gameguild:runtime@0.1.0;
 * 
 * interface debug {
 *   trace: func(message: string);
 *   assert: func(condition: bool, message: string);
 *   breakpoint: func();
 *   inspect: func(value: string) -> string;
 * }
 * ```
 */
export interface GameGuildDebug {
  trace: (message: string) => void
  assert: (condition: boolean, message: string) => void
  breakpoint: () => void
  inspect: (value: string) => string
}

export function createDebug(
  stdout: (text: string) => void,
  stderr: (text: string) => void
): GameGuildDebug {
  return {
    trace: (message: string) => stdout('[TRACE] ' + message + '\n'),
    
    assert: (condition: boolean, message: string) => {
      if (!condition) {
        stderr('[ASSERT FAILED] ' + message + '\n')
        throw new Error('Assertion failed: ' + message)
      }
    },
    
    breakpoint: () => {
      stdout('[BREAKPOINT]\n')
    },
    
    inspect: (value: string) => {
      try {
        const parsed = JSON.parse(value)
        return JSON.stringify(parsed, null, 2)
      } catch {
        return value
      }
    },
  }
}
