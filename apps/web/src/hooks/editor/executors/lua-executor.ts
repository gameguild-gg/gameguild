import type { ProgrammingLanguage } from '@/components/ui/source-code/types';
import type { ExecutionResult, ExecutionContext, LanguageExecutor } from './types';

class LuaExecutor implements LanguageExecutor {
  private isExecutionCancelled = false;
  public isCompiled = false; // Set the isCompiled flag to false

  execute = async (fileId: string, context: ExecutionContext): Promise<ExecutionResult> => {
    const { addOutput, setIsExecuting } = context;

    this.isExecutionCancelled = false;
    setIsExecuting(true);

    // Placeholder for future implementation
    addOutput([
      'Lua execution is not fully implemented yet.',
      'In a production environment, this would use Fengari or a backend service.',
      'Stay tuned for future updates!',
    ]);

    setIsExecuting(false);
    return {
      success: true,
      output: ['Lua execution is not fully implemented yet.'],
    };
  };

  stop = () => {
    // Set the cancellation flag
    this.isExecutionCancelled = true;
  };

  getFileExtension = (): string => {
    return 'lua';
  };

  getSupportedLanguages = (): ProgrammingLanguage[] => {
    return ['lua'];
  };
}

export const luaExecutor = new LuaExecutor();
