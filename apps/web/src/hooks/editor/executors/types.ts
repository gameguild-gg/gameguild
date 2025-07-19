import type { CodeFile, ProgrammingLanguage } from '@/components/editor/ui/source-code/types';
// Replace the above line with the correct import or define CodeFile here if missing:

// Add global type definitions for the prompt and confirm flags

// Add these declarations to the global Window interface:

// Add this near the top of the file, after any imports:

export interface Executor {
  isCompiled: any;
  stop: any;
  handleCommand: any;
  execute(fileId: string, context: ExecutionContext): Promise<ExecutionResult>;
}

declare global {
  interface Window {
    promptCallback: (value: string) => void;
    confirmCallback: (value: string) => void;
    alertCallback: () => void;
    __awaitingPromptInput: boolean;
    __awaitingConfirmInput: boolean;
    __awaitingAlertAck: boolean;
    __promptMessage: string | null;
    __confirmMessage: string | null;
    __alertMessage: string | null;
  }
}

export interface ExecutionContext {
  files: CodeFile[];
  selectedLanguage: ProgrammingLanguage;
  addOutput: (output: string | string[]) => void;
  setIsExecuting: (isExecuting: boolean) => void;
}

export interface ExecutionResult {
  success: boolean;
  output: string[];
}

export interface LanguageExecutor {
  isCompiled?: boolean;
  execute: (fileId: string, context: ExecutionContext) => Promise<ExecutionResult>;
  stop: () => void;
  getFileExtension?: () => string;
  getSupportedLanguages: () => ProgrammingLanguage[];
  handleCommand?: (command: string, context: ExecutionContext) => boolean;
}
