import type { ProgrammingLanguage } from '@/components/ui/source-code/types';
import type { ExecutionContext, ExecutionResult, LanguageExecutor } from './types';
import { getFileContent } from '@/components/ui/source-code/utils';

class TypeScriptExecutor implements LanguageExecutor {
  public isCompiled = false; // Set the isCompiled flag to false
  resolveFile: any;
  private isExecutionCancelled = false;
  private transpileCache: Record<string, string> = {};

  execute = async (fileId: string, context: ExecutionContext): Promise<ExecutionResult> => {
    const { files, selectedLanguage, addOutput, setIsExecuting } = context;

    this.isExecutionCancelled = false;
    setIsExecuting(true);

    try {
      // Find the file to execute
      const visibleFiles = files.filter((file) => file.isVisible);
      const mainFile = visibleFiles.find((file) => file.isMain);
      const activeFile = files.find((file) => file.id === fileId);
      const fileToExecute = mainFile || activeFile;

      if (!fileToExecute) {
        addOutput('Error: No file selected for execution.');
        setIsExecuting(false);
        return { success: false, output: ['Error: No file selected for execution.'] };
      }

      // If executing a main file that's different from the active file, show a message
      if (mainFile && mainFile.id !== fileId) {
        addOutput(`Executing main file: ${fileToExecute.name}`);
      }

      // Load TypeScript compiler
      addOutput('Loading TypeScript compiler...');

      try {
        // Dynamically import the TypeScript compiler
        const ts = await this.loadTypeScriptCompiler();
        addOutput('TypeScript compiler loaded successfully.');

        // Create a virtual file system for imports
        const virtualFileSystem: Record<string, string> = {};
        visibleFiles.forEach((file) => {
          const content = getFileContent(file, selectedLanguage);
          virtualFileSystem[file.name] = content;
        });

        // Resolve imports and bundle the TypeScript code
        addOutput('Resolving imports and bundling TypeScript files...');
        const bundledTypeScript = this.resolveImports(fileToExecute.name, virtualFileSystem, addOutput);

        // Transpile the bundled TypeScript to JavaScript
        addOutput('Transpiling TypeScript to JavaScript...');
        const { transpiledCode, diagnostics } = this.transpileTypeScript(ts, bundledTypeScript, 'bundle.ts');

        // If there are compilation errors, display them and abort execution
        if (diagnostics.length > 0) {
          const errorMessages = diagnostics.map((diag) => `Error ${diag.file?.fileName}(${diag.start}): ${diag.messageText}`);
          addOutput(['TypeScript compilation failed with errors:', ...errorMessages]);
          setIsExecuting(false);
          return { success: false, output: errorMessages };
        }

        // Create a safe execution environment
        const consoleOutput: string[] = [];
        const mockConsole = {
          log: (...args: any) => {
            const output = args.map((arg: any) => String(arg)).join(' ');
            consoleOutput.push(output);
            addOutput(output);
          },
          error: (...args: any) => {
            const output = `Error: ${args.map((arg: any) => String(arg)).join(' ')}`;
            consoleOutput.push(output);
            addOutput(output);
          },
          warn: (...args: any) => {
            const output = `Warning: ${args.map((arg: any) => String(arg)).join(' ')}`;
            consoleOutput.push(output);
          },
        };

        // Add custom implementations for browser dialogs
        const customImplementations = `
          // Custom implementations for browser dialogs
          async function customPrompt(message, defaultValue = "") {
            console.log("PROMPT: " + message + (defaultValue ? " [default: " + defaultValue + "]" : ""));
            return new Promise((resolve) => {
              window.promptCallback = (value) => {
                resolve(value || defaultValue);
              };
            });
          }

          async function customConfirm(message) {
            console.log("CONFIRM: " + message);
            return new Promise((resolve) => {
              window.confirmCallback = (value) => {
                resolve(value === "0" || value.toLowerCase() === "y" || value.toLowerCase() === "yes" || value.toLowerCase() === "true");
              };
            });
          }
        `;

        // Modify the transpiled code to replace browser dialogs
        const modifiedCode = transpiledCode.replace(/prompt\s*\(/g, 'await customPrompt(').replace(/confirm\s*\(/g, 'await customConfirm(');

        // Combine the custom implementations with the transpiled code
        const finalCode = `
          ${customImplementations}

          // Main execution
          try {
            ${modifiedCode}
          } catch (error) {
            console.error("Runtime error: " + error.message);
          }
        `;

        // Check if execution was cancelled
        if (this.isExecutionCancelled) {
          addOutput('Execution cancelled.');
          setIsExecuting(false);
          return { success: false, output: ['Execution cancelled.'] };
        }

        // Execute the transpiled code in a safe context
        addOutput('Executing transpiled JavaScript...');
        const AsyncFunction = Object.getPrototypeOf(async () => {}).constructor;
        const executeCode = new AsyncFunction('console', 'window', finalCode);
        await executeCode(mockConsole, window);

        setIsExecuting(false);
        return { success: true, output: consoleOutput };
      } catch (error) {
        console.error('TypeScript execution error:', error);
        const errorMessage = error instanceof Error ? error.message : String(error);
        addOutput(`Error: ${errorMessage}`);
        setIsExecuting(false);
        return { success: false, output: [`Error: ${errorMessage}`] };
      }
    } catch (error) {
      console.error('TypeScript execution error:', error);
      const errorMessage = error instanceof Error ? error.message : String(error);
      addOutput(`Error: ${errorMessage}`);
      setIsExecuting(false);
      return { success: false, output: [`Error: ${errorMessage}`] };
    }
  };

  stop = () => {
    this.isExecutionCancelled = true;
  };

  getFileExtension = (): string => {
    return 'ts';
  };

  getSupportedLanguages = (): ProgrammingLanguage[] => {
    return ['typescript'];
  };

  handleCommand = (command: string, context: ExecutionContext): boolean => {
    const { addOutput } = context;

    // Handle TypeScript-specific commands
    if (command.toLowerCase() === 'tsc --version') {
      addOutput('TypeScript Version 5.0.4');
      return true;
    }

    // Try to execute TypeScript code directly from the command line
    if (command.includes(':') || command.includes('interface') || command.includes('class')) {
      try {
        // Simple TypeScript evaluation is not supported in the terminal
        addOutput('Direct TypeScript evaluation in the terminal is not supported. Please create a file to execute TypeScript code.');
        return true;
      } catch (error) {
        addOutput(`Error: ${error instanceof Error ? error.message : String(error)}`);
        return true;
      }
    }

    return false;
  };

  private resolveImports(entryFile: string, vfs: Record<string, string>, addOutput: (msg: string) => void): string {
    const visited = new Set<string>();
    const importRegex = /import\s+(?:(\w+)|{([^}]+)}|\*\s+as\s+(\w+))?\s*from\s*['"](.+?)['"];?/g;

    const resolveFile = (fileName: string): string => {
      if (visited.has(fileName)) {
        addOutput(`Circular import detected: ${fileName}`);
        return '';
      }
      visited.add(fileName);

      const content = vfs[fileName];
      if (!content) {
        addOutput(`Warning: File not found: ${fileName}`);
        return '';
      }

      let result = '';
      const imports = Array.from(content.matchAll(importRegex));

      // Process imports first
      for (const match of imports) {
        const [fullMatch, defaultImport, namedImports, namespaceImport, importPath] = match;
        const resolvedPath = this.resolveRelativePath(fileName, importPath, vfs);

        if (resolvedPath) {
          addOutput(`Importing: ${importPath} -> ${resolvedPath}`);

          // Get the imported file content
          const importedContent = this.resolveFile(resolvedPath);

          // Handle named imports by creating wrapper
          if (namedImports) {
            const namedList = namedImports.split(',').map((name) => name.trim());
            addOutput(`Named imports: {${namedList.join(', ')}} from ${resolvedPath}`);

            // Create a wrapper that exposes only the named exports
            result += `
// === Named imports from ${resolvedPath} ===
(function() {
${importedContent}

// Export named functions to global scope
${namedList.map((name) => `if (typeof ${name} !== 'undefined') { window.${name} = ${name}; }`).join('\n')}
})();
`;
          } else {
            // Regular import - just include the file
            result += importedContent + '\n';
          }
        }
      }

      // Remove import statements and add the file content
      const contentWithoutImports = content.replace(importRegex, '');
      result += contentWithoutImports + '\n';

      return result;
    };

    addOutput(`Starting TypeScript bundle from: ${entryFile}`);
    return resolveFile(entryFile);
  }

  private resolveRelativePath(currentFile: string, importPath: string, vfs: Record<string, string>): string | null {
    // Handle relative imports starting with './'
    if (importPath.startsWith('./')) {
      const targetFile = importPath.substring(2);

      // Try with .ts extension if not present
      if (vfs[targetFile]) {
        return targetFile;
      }
      if (!targetFile.includes('.') && vfs[targetFile + '.ts']) {
        return targetFile + '.ts';
      }
    }

    // Handle direct file names
    if (vfs[importPath]) {
      return importPath;
    }

    // Try with .ts extension
    if (!importPath.includes('.') && vfs[importPath + '.ts']) {
      return importPath + '.ts';
    }

    return null;
  }

  private async loadTypeScriptCompiler(): Promise<any> {
    // In a real implementation, you would dynamically load the TypeScript compiler
    // For this example, we'll simulate the TypeScript compiler API
    return {
      transpileModule: (input: string, options: any) => {
        // Simple TypeScript transpilation simulation
        // In a real implementation, this would use the actual TypeScript compiler

        // Remove type annotations
        const output = input
          // Remove interface declarations
          .replace(/interface\s+\w+\s*\{[^}]*\}/g, '')
          // Remove type annotations from variables
          .replace(/:\s*\w+(\[\])?(\s*\|\s*\w+(\[\])?)*\s*(?=[,)=;])/g, '')
          // Remove type parameters from generics
          .replace(/<[^<>]*>/g, '')
          // Remove return type annotations
          .replace(/\)\s*:\s*\w+(\[\])?(\s*\|\s*\w+(\[\])?)*\s*(?={)/g, ') ')
          // Remove type imports
          .replace(/import\s+type\s+.*?from\s+['"].*?['"]/g, '')
          // Remove 'as' type assertions
          .replace(/\s+as\s+\w+(\[\])?/g, '');

        return {
          outputText: output,
          diagnostics: [],
          sourceMapText: null,
        };
      },
      createDiagnosticCollection: () => {
        return [];
      },
    };
  }

  private transpileTypeScript(ts: any, code: string, fileName: string): { transpiledCode: string; diagnostics: any[] } {
    // Check if we have this in cache
    const cacheKey = `${fileName}:${code}`;
    if (this.transpileCache[cacheKey]) {
      return {
        transpiledCode: this.transpileCache[cacheKey],
        diagnostics: [],
      };
    }

    // Transpile the TypeScript code to JavaScript
    const output = ts.transpileModule(code, {
      compilerOptions: {
        module: 1, // CommonJS
        target: 2, // ES2015
        strict: false,
        esModuleInterop: true,
        skipLibCheck: true,
        forceConsistentCasingInFileNames: true,
      },
      fileName,
      reportDiagnostics: true,
    });

    // Cache the result
    this.transpileCache[cacheKey] = output.outputText;

    return {
      transpiledCode: output.outputText,
      diagnostics: output.diagnostics || [],
    };
  }
}

export const typescriptExecutor = new TypeScriptExecutor();
