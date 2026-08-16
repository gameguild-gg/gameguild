/**
 * Code Formatting Utilities
 *
 * Formats generated code with Prettier.
 */

import { format, resolveConfig } from 'prettier';

/**
 * Format TypeScript code with Prettier
 */
export async function formatOutput(code: string, filepath: string): Promise<string> {
  try {
    const config = await resolveConfig(filepath);

    return await format(code, {
      ...config,
      filepath,
      parser: 'typescript',
    });
  } catch {
    // If Prettier fails, return unformatted code
    console.warn('  ⚠️  Prettier formatting failed, using unformatted output');
    return code;
  }
}
