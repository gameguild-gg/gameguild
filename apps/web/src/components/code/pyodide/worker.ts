import { expose } from 'comlink';
import { PyodideWorkerAPI } from './pyodide.api';
import { PyodideInterface } from 'pyodide';

// Declare the global loadPyodide function that will be available after importScripts
declare global {
  function loadPyodide(): Promise<PyodideInterface>;
  function importScripts(...urls: string[]): void;
}

let pyodide: PyodideInterface | null = null;

const loadPyodideInstance = async () => {
  if (!pyodide) {
    try {
      // Load Pyodide script from CDN
      await new Promise<void>((resolve, reject) => {
        try {
          // Use the latest version from CDN
          importScripts('https://cdn.jsdelivr.net/pyodide/v0.27.5/full/pyodide.js');
          resolve();
        } catch (error) {
          reject(new Error('Failed to load Pyodide script'));
        }
      });

      // Now initialize Pyodide
      pyodide = await loadPyodide();
      return true; // Indicates that Pyodide was loaded
    } catch (error) {
      throw new Error('Pyodide failed to initialize');
    }
  }
  return true;
};

const executePython = async (code: string, onOutput: (output: string) => void, onError: (error: string) => void) => {
  try {
    await loadPyodideInstance();

    if (!pyodide) {
      throw new Error('Pyodide not initialized');
    }

    pyodide.setStdout({
      batched: (output: string) => {
        onOutput(output); // Envia saída para o callback
      },
    });

    pyodide.setStderr({
      batched: (error: string) => {
        onError(error); // Envia erro para o callback
      },
    });

    await pyodide.runPythonAsync(code);
  } catch (error) {
    onError((error as Error).message);
  }
};

const workerApi: PyodideWorkerAPI = {
  loadPyodideInstance,
  executePython,
};

expose(workerApi);
