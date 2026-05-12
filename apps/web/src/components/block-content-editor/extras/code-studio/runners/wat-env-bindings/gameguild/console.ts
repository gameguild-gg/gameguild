/**
 * GameGuild Console Interface (gameguild:runtime/console)
 * Custom component interface for console logging
 * 
 * WIT definition:
 * ```wit
 * package gameguild:runtime@0.1.0;
 * 
 * interface console {
 *   log: func(message: string);
 *   error: func(message: string);
 *   warn: func(message: string);
 *   info: func(message: string);
 *   debug: func(message: string);
 * }
 * ```
 */
export interface GameGuildConsole {
  log: (message: string) => void
  error: (message: string) => void
  warn: (message: string) => void
  info: (message: string) => void
  debug: (message: string) => void
}

export function createConsole(
  stdout: (text: string) => void,
  stderr: (text: string) => void
): GameGuildConsole {
  return {
    log: (message: string) => stdout(message + '\n'),
    error: (message: string) => stderr(message + '\n'),
    warn: (message: string) => stdout('[WARN] ' + message + '\n'),
    info: (message: string) => stdout('[INFO] ' + message + '\n'),
    debug: (message: string) => stdout('[DEBUG] ' + message + '\n'),
  }
}

