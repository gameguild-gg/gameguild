import type { ProgrammingLanguage } from '@/components/ui/source-code/types';
import type { Executor } from './types';
import { javascriptExecutor } from './javascript-executor';
import { typescriptExecutor } from './typescript-executor';
import { pythonExecutor } from './python-executor';
import { luaExecutor } from './lua-executor';
import { cExecutor } from './c-executor';
import { cppExecutor } from './cpp-executor';

/**
 * Get the appropriate executor for a language
 */
export function getExecutor(language: ProgrammingLanguage): Executor {
  switch (language) {
    case 'javascript':
      return javascriptExecutor;
    case 'typescript':
      return typescriptExecutor;
    case 'python':
      return pythonExecutor;
    case 'lua':
      return luaExecutor;
    case 'c':
      return cExecutor;
    case 'cpp':
      return cppExecutor;
    case 'h':
      return cExecutor; // Use C executor for .h files
    case 'hpp':
      return cppExecutor; // Use C++ executor for .hpp files
    default:
      throw new Error(`Unsupported language: ${language}`);
  }
}
