'use client';

import type React from 'react';
import { useCallback, useRef, useState } from 'react';

interface UseTerminalOptions {
  onCommand?: (command: string) => boolean;
  scrollToBottom?: () => void;
}

export function useTerminal(options: UseTerminalOptions = {}) {
  const { onCommand, scrollToBottom } = options;

  const [output, setOutput] = useState<string[]>([]);
  const [input, setInput] = useState('');
  const [commandHistory, setCommandHistory] = useState<string[]>([]);
  const [historyIndex, setHistoryIndex] = useState(-1);
  const [waitingForInput, setWaitingForInput] = useState(false);
  const [inputPrompt, setInputPrompt] = useState('');
  const [inputCallback, setInputCallback] = useState<((value: string) => void) | null>(null);
  const terminalInputRef = useRef<HTMLInputElement>(null);

  // Helper function to add output and scroll
  const addOutput = useCallback(
    (newOutput: string | string[]) => {
      const outputArray = Array.isArray(newOutput) ? newOutput : [newOutput];
      setOutput((prev) => [...prev, ...outputArray]);

      // Scroll to bottom after adding output
      if (scrollToBottom) {
        setTimeout(scrollToBottom, 10);
      }
    },
    [scrollToBottom],
  );

  // Clear terminal
  const clearTerminal = useCallback(() => {
    setOutput([]);
  }, []);

  // Handle terminal input
  const handleTerminalInput = useCallback(
    (e: React.KeyboardEvent<HTMLInputElement>) => {
      // Handle command history navigation with up/down arrows
      if (e.key === 'ArrowUp') {
        e.preventDefault();
        if (commandHistory.length > 0 && historyIndex < commandHistory.length - 1) {
          const newIndex = historyIndex + 1;
          setHistoryIndex(newIndex);
          setInput(commandHistory[commandHistory.length - 1 - newIndex]);
        }
        return;
      }

      if (e.key === 'ArrowDown') {
        e.preventDefault();
        if (historyIndex > 0) {
          const newIndex = historyIndex - 1;
          setHistoryIndex(newIndex);
          setInput(commandHistory[commandHistory.length - 1 - newIndex]);
        } else if (historyIndex === 0) {
          setHistoryIndex(-1);
          setInput('');
        }
        return;
      }

      if (e.key === 'Enter' && input.trim() !== '') {
        // Store the input value before clearing it
        const userInput = input.trim();

        // Add command to history
        setCommandHistory((prev) => [...prev, userInput]);
        setHistoryIndex(-1);

        // Display the input in the terminal with prompt
        addOutput(`$ ${userInput}`);
        setInput('');

        // Check if we're waiting for a prompt or confirm callback
        if (window.promptCallback) {
          // Store the callback before clearing it
          const callback = window.promptCallback;

          // Clear the callback and reset waiting state
          window.promptCallback = null;
          setWaitingForInput(false);

          // Call the callback with the user input
          setTimeout(() => {
            callback(userInput);
          }, 10);

          return;
        }

        if (window.confirmCallback) {
          // Store the callback before clearing it
          const callback = window.confirmCallback;

          // Clear the callback and reset waiting state
          window.confirmCallback = null;
          setWaitingForInput(false);

          // Call the callback with the user input
          setTimeout(() => {
            callback(userInput);
          }, 10);

          return;
        }

        // If we're waiting for input for a prompt/confirm, call the callback
        if (waitingForInput && inputCallback) {
          inputCallback(userInput);
          setWaitingForInput(false);
          setInputPrompt('');
          setInputCallback(null);
        } else if (onCommand) {
          // Otherwise handle as a normal terminal command
          const handled = onCommand(userInput);

          // If the command wasn't handled by the onCommand callback
          if (!handled) {
            if (userInput.toLowerCase() === 'clear') {
              clearTerminal();
            } else {
              addOutput(`Command not recognized: ${userInput}`);
            }
          }
        }

        // Auto-scroll to bottom after adding output
        if (scrollToBottom) {
          setTimeout(scrollToBottom, 50);
        }
      }
    },
    [commandHistory, historyIndex, input, waitingForInput, inputCallback, onCommand, addOutput, clearTerminal, scrollToBottom],
  );

  return {
    output,
    setOutput,
    input,
    setInput,
    commandHistory,
    setCommandHistory,
    historyIndex,
    setHistoryIndex,
    waitingForInput,
    setWaitingForInput,
    inputPrompt,
    setInputPrompt,
    inputCallback,
    setInputCallback,
    terminalInputRef,
    handleTerminalInput,
    clearTerminal,
    addOutput,
  };
}
