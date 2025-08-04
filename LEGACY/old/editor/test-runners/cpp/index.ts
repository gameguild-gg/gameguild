import type { TestRunnerFunction } from '../types';

export const cppTestRunners: Record<string, TestRunnerFunction> = {
  inout: async ({ file, fileCases, addOutput, setTestResults }) => {
    // Inform the user that C++ testing is not yet fully implemented
    addOutput('C++ test execution is not yet fully implemented.');

    try {
      // There is no C++ compiler yet, so we'll just return appropriate messages
      const results = fileCases.map((test) => ({
        passed: false,
        expected: 'C++ execution',
        actual: 'C++ compiler not yet implemented',
      }));

      // Update test results
      setTestResults({
        [file.id]: results,
      });

      // Return a message
      addOutput('Cannot execute C++ tests. Compiler not yet implemented.');
    } catch (error) {
      console.error('Error in C++ test runner:', error);
      addOutput(`Error: ${error}`);
    }
  },
  predicate: async ({ file, fileCases, addOutput, setTestResults }) => {
    // Inform the user that C++ testing is not yet fully implemented
    addOutput('C++ test execution is not yet fully implemented.');

    try {
      // There is no C++ compiler yet, so we'll just return appropriate messages
      const results = fileCases.map((test) => ({
        passed: false,
        expected: 'C++ execution',
        actual: 'C++ compiler not yet implemented',
      }));

      // Update test results
      setTestResults({
        [file.id]: results,
      });

      // Return a message
      addOutput('Cannot execute C++ tests. Compiler not yet implemented.');
    } catch (error) {
      console.error('Error in C++ test runner:', error);
      addOutput(`Error: ${error}`);
    }
  },
};
