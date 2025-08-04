'use client';

import type React from 'react';
import { useCallback, useState } from 'react';
import type { CodeFile, ProgrammingLanguage } from '../../components/editor/ui/source-code/types';
import { getExecutor } from './executors/executor-factory';
import type { ExecutionContext } from './executors/types';
import { getTestRunner } from './test-runners';
import type { TestCase } from './test-runners/types';

interface UseCodeExecutionOptions {
  files: CodeFile[];
  selectedLanguage: ProgrammingLanguage;
  clearTerminalOnRun: boolean;
  addOutput: (output: string | string[]) => void;
  clearTerminal: () => void;
  testCases?: Record<string, TestCase[]>;
  setTestResults?: (results: Record<string, { passed: boolean; actual: string; expected: string }[]>) => void;
}

export function useCodeExecution(options: UseCodeExecutionOptions) {
  const { files, selectedLanguage, clearTerminalOnRun, addOutput, clearTerminal, testCases, setTestResults } = options;
  const [isExecuting, setIsExecuting] = useState(false);
  const [input, setInput] = useState<string>('');

  // Function to capture output for test cases
  const captureOutput = useCallback((output: string | string[]) => {
    if (Array.isArray(output)) {
      setCapturedOutput((prev) => [...prev, ...output]);
    } else {
      setCapturedOutput((prev) => [...prev, output]);
    }
  }, []);

  // Function to normalize output for comparison
  const normalizeOutput = useCallback((output: unknown): string => {
    // Convert output to string if it's not already
    const outputStr = typeof output === 'string' ? output : String(output || '');

    // Remove trailing whitespace from each line and join with newlines
    return outputStr
      .split('\n')
      .map((line) => line.trimRight())
      .join('\n')
      .trim();
  }, []);

  // Handle terminal commands
  const handleTerminalCommand = useCallback(
    (command: string): boolean => {
      if (command.toLowerCase() === 'clear') {
        clearTerminal();
        return true;
      }

      // Get the appropriate executor for the selected language
      const executor = getExecutor(selectedLanguage);

      // If the executor has a command handler, use it
      if (executor.handleCommand) {
        const context: ExecutionContext = {
          files,
          selectedLanguage,
          addOutput,
          clearTerminal,
          setIsExecuting,
        };

        return executor.handleCommand(command, context);
      }

      // Default command handling
      if (command.toLowerCase().includes('help')) {
        addOutput('Available commands: help, clear, echo [text]');
        return true;
      } else if (command.toLowerCase().startsWith('echo ')) {
        addOutput(command.substring(5));
        return true;
      }

      return false;
    },
    [selectedLanguage, addOutput, clearTerminal, files, setIsExecuting],
  );

  // Stop execution
  const handleStopExecution = useCallback(() => {
    // Stop any ongoing execution
    const executor = getExecutor(selectedLanguage);

    // Check if the executor has a stop method before calling it
    if (executor && typeof executor.stop === 'function') {
      executor.stop();
    } else {
      console.warn(`Executor for ${selectedLanguage} does not have a stop method`);
    }

    setIsExecuting(false);
    addOutput('Execution stopped by user.');

    // Reset test execution state
    setIsCapturingOutput(false);
    setCapturedOutput([]);
    setCurrentTestFileId(null);
    setCurrentTestIndex(-1);

    // Reset callbacks
    window.promptCallback = () => {};
    window.confirmCallback = () => {};
    window.alertCallback = () => {};
  }, [addOutput, selectedLanguage]);

  // Execute code
  const handleExecute = useCallback(
    async (activeFileId: string) => {
      // If already executing, don't start another execution
      if (isExecuting) return;

      // Clear terminal if the option is enabled, otherwise just add a separator
      if (clearTerminalOnRun) {
        clearTerminal();
      } else {
        // Add a separator line instead of clearing the output
        addOutput('--- New Execution ---');
      }

      // Reset global callbacks
      window.promptCallback = () => {};
      window.confirmCallback = () => {};
      window.alertCallback = () => {};

      // Get the appropriate executor for the selected language
      const executor = getExecutor(selectedLanguage);

      // Create execution context
      const context: ExecutionContext = {
        files,
        selectedLanguage,
        addOutput,
        clearTerminal,
        setIsExecuting,
      };

      // Check if the language is compiled
      if (executor.isCompiled) {
        addOutput(`Executing ${selectedLanguage.toUpperCase()} code (compiled language)...`);
      } else {
        addOutput(`Executing ${selectedLanguage.toUpperCase()} code (interpreted language)...`);
      }

      // Execute the code
      await executor.execute(activeFileId, context);
    },
    [isExecuting, clearTerminalOnRun, clearTerminal, addOutput, files, selectedLanguage],
  );

  // Run tests for a file
  const runTests = useCallback(
    async (fileId: string) => {
      if (!testCases || !setTestResults) return;

      // Safely get file cases or empty array if not defined
      const fileCases = testCases[fileId] || [];
      if (fileCases.length === 0) {
        addOutput('No test cases defined for this file.');
        return;
      }

      // Set executing state
      setIsExecuting(true);

      // Get the test type from the first test case
      const testType = fileCases[0]?.type || 'simple';

      // Clear previous test results
      setTestResults({
        ...testCases,
        [fileId]: fileCases.map(() => ({ passed: false, actual: '', expected: '' })),
      });

      // Clear terminal if option is enabled
      if (clearTerminalOnRun) {
        clearTerminal();
      }

      // Find the file being tested
      const file = files.find((f) => f.id === fileId);
      if (!file) {
        addOutput('Error: File not found');
        setIsExecuting(false);
        return;
      }

      addOutput(`--- Running Tests for ${file.name} ---`);

      // Check if the language is cpp/hpp and warn about lack of compiler
      if (selectedLanguage === 'cpp' || selectedLanguage === 'hpp') {
        addOutput('WARNING: C++ compiler not yet implemented. Tests will not be executed.');
      }

      // Get the appropriate test runner for the language and test type
      const testRunner = getTestRunner(selectedLanguage, testType);

      if (testRunner) {
        try {
          // Execute the test runner with all necessary options
          await testRunner({
            fileId,
            file,
            fileCases,
            files,
            selectedLanguage,
            addOutput,
            clearTerminal,
            setIsExecuting,
            setTestResults,
            normalizeOutput,
          });
        } catch (error) {
          console.error('Error running tests:', error);
          addOutput(`Error running tests: ${error}`);

          // Update test results with error
          setTestResults({
            ...testCases,
            [fileId]: fileCases.map(() => ({
              passed: false,
              actual: `Error: ${error}`,
              expected: 'Successful test execution',
            })),
          });
        }
      } else {
        addOutput(`${testType} tests not yet implemented for ${selectedLanguage}`);
      }

      // Reset execution state
      setIsExecuting(false);
      addOutput('--- Test execution complete ---');
    },
    [testCases, setTestResults, selectedLanguage, files, clearTerminal, addOutput, clearTerminalOnRun, normalizeOutput],
  );

  const getExecutorForLanguage = (language: ProgrammingLanguage) => {
    return getExecutor(language);
  };

  const handleTerminalInput = (e: React.KeyboardEvent<HTMLInputElement>) => {
    if (e.key === 'Enter') {
      e.preventDefault();

      // Check if we're waiting for alert acknowledgment
      if (window.__awaitingAlertAck && window.alertCallback) {
        // Call the alert callback to acknowledge the alert
        window.alertCallback();
        addOutput(`> OK`);
        setInput('');
        return;
      }

      // Check if we're waiting for prompt input
      if (window.__awaitingPromptInput && window.promptCallback) {
        // Call the prompt callback with the input value
        window.promptCallback(input);
        addOutput(`> ${input}`);
        setInput('');
        return;
      }

      // Check if we're waiting for confirm input
      if (window.__awaitingConfirmInput && window.confirmCallback) {
        // Call the confirm callback with the input value
        window.confirmCallback(input);
        addOutput(`> ${input}`);
        setInput('');
        return;
      }

      // Handle normal terminal commands
      if (input.trim()) {
        // Add the input to the terminal output
        addOutput(`$ ${input}`);

        // Check if the active executor has a command handler
        const activeExecutor = getExecutorForLanguage(selectedLanguage);
        if (activeExecutor && activeExecutor.handleCommand) {
          const handled = activeExecutor.handleCommand(input, {
            files,
            selectedLanguage,
            addOutput,
            setIsExecuting,
          });

          if (handled) {
            setInput('');
            return;
          }
        }

        // If not handled by the executor, try to execute the command
        if (input.trim().toLowerCase() === 'clear') {
          setOutput([]);
        } else if (input.trim().toLowerCase() === 'run') {
          handleExecute(''); // Assuming executeCode is meant to be handleExecute
        } else {
          addOutput(`Unknown command: ${input}`);
        }

        setInput('');
      }
    }
  };

  return {
    isExecuting,
    setIsExecuting,
    handleExecute,
    handleStopExecution,
    handleTerminalCommand,
    runTests,
    handleTerminalInput,
    setInput,
  };
}
