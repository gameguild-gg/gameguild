import type { ProgrammingLanguage } from '@/components/ui/source-code/types';
import type { ExecutionContext, ExecutionResult, LanguageExecutor } from './types';

class PythonExecutor implements LanguageExecutor {
  public isCompiled = false; // Set the isCompiled flag to false
  private isExecutionCancelled = false;

  execute = async (fileId: string, context: ExecutionContext): Promise<ExecutionResult> => {
    const { addOutput, setIsExecuting } = context;

    this.isExecutionCancelled = false;
    setIsExecuting(true);

    // Placeholder for future implementation
    addOutput([
      `Python execution is not fully implemented yet.`,
      'In a production environment, this would use Pyodide or a backend service.',
      'Stay tuned for future updates!',
    ]);

    setIsExecuting(false);
    return {
      success: true,
      output: [`Python execution is not fully implemented yet.`],
    };
  };

  stop = () => {
    // Set the cancellation flag
    this.isExecutionCancelled = true;
  };

  getFileExtension = (): string => {
    return 'py';
  };

  getSupportedLanguages = (): ProgrammingLanguage[] => {
    return ['python'];
  };
}

export const pythonExecutor = new PythonExecutor();
