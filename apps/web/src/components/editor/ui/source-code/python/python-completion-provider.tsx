'use client';

import { useEffect } from 'react';
import type { editor } from 'monaco-editor';

interface PythonCompletionProviderProps {
  monaco: typeof import('monaco-editor') | null;
  editor: editor.IStandaloneCodeEditor | null;
}

export function PythonCompletionProvider({ monaco, editor }: PythonCompletionProviderProps) {
  useEffect(() => {
    if (!monaco || !editor) return;

    // Register Python completion provider
    const disposable = monaco.languages.registerCompletionItemProvider('python', {
      provideCompletionItems: (model, position) => {
        const word = model.getWordUntilPosition(position);
        const range = {
          startLineNumber: position.lineNumber,
          endLineNumber: position.lineNumber,
          startColumn: word.startColumn,
          endColumn: word.endColumn,
        };

        // Python keywords
        const keywords = [
          'and',
          'as',
          'assert',
          'async',
          'await',
          'break',
          'class',
          'continue',
          'def',
          'del',
          'elif',
          'else',
          'except',
          'exec',
          'finally',
          'for',
          'from',
          'global',
          'if',
          'import',
          'in',
          'is',
          'lambda',
          'nonlocal',
          'not',
          'or',
          'pass',
          'print',
          'raise',
          'return',
          'try',
          'while',
          'with',
          'yield',
          'False',
          'None',
          'True',
        ];

        // Python built-in functions
        const builtins = [
          'abs',
          'all',
          'any',
          'bin',
          'bool',
          'bytearray',
          'callable',
          'chr',
          'classmethod',
          'compile',
          'complex',
          'delattr',
          'dict',
          'dir',
          'divmod',
          'enumerate',
          'eval',
          'filter',
          'float',
          'format',
          'frozenset',
          'getattr',
          'globals',
          'hasattr',
          'hash',
          'help',
          'hex',
          'id',
          'input',
          'int',
          'isinstance',
          'issubclass',
          'iter',
          'len',
          'list',
          'locals',
          'map',
          'max',
          'memoryview',
          'min',
          'next',
          'object',
          'oct',
          'open',
          'ord',
          'pow',
          'property',
          'range',
          'repr',
          'reversed',
          'round',
          'set',
          'setattr',
          'slice',
          'sorted',
          'staticmethod',
          'str',
          'sum',
          'super',
          'tuple',
          'type',
          'vars',
          'zip',
          '__import__',
        ];

        // Python types for type annotations
        const types = [
          'int',
          'float',
          'str',
          'bool',
          'list',
          'dict',
          'tuple',
          'set',
          'None',
          'Any',
          'List',
          'Dict',
          'Tuple',
          'Set',
          'Optional',
          'Union',
          'Callable',
          'Type',
        ];

        // Common modules
        const modules = [
          'os',
          'sys',
          'math',
          'random',
          'datetime',
          'time',
          'json',
          're',
          'collections',
          'itertools',
          'functools',
          'typing',
          'pathlib',
          'shutil',
          'subprocess',
        ];

        // Get the line text before the cursor
        const textUntilPosition = model.getValueInRange({
          startLineNumber: position.lineNumber,
          startColumn: 1,
          endLineNumber: position.lineNumber,
          endColumn: position.column,
        });

        // Check if we're in an import statement
        const isImport = /^\s*(?:from|import)\s+/.test(textUntilPosition);

        // Check if we're after a dot (for method suggestions)
        const isDot = /\.\s*$/.test(textUntilPosition);

        // Check if we're in a type annotation
        const isTypeAnnotation = /:\s*$/.test(textUntilPosition);

        // Create suggestions based on context
        let suggestions = [];

        if (isImport) {
          suggestions = modules.map((module) => ({
            label: module,
            kind: monaco.languages.CompletionItemKind.Module,
            insertText: module,
            range,
          }));
        } else if (isDot) {
          // Method suggestions based on common types
          const stringMethods = ['strip', 'split', 'replace', 'upper', 'lower', 'find', 'format'];
          const listMethods = ['append', 'extend', 'insert', 'remove', 'pop', 'clear', 'index', 'count', 'sort', 'reverse'];
          const dictMethods = ['get', 'keys', 'values', 'items', 'update', 'pop', 'clear'];

          suggestions = [
            ...stringMethods.map((method) => ({
              label: method,
              kind: monaco.languages.CompletionItemKind.Method,
              insertText: method,
              range,
            })),
            ...listMethods.map((method) => ({
              label: method,
              kind: monaco.languages.CompletionItemKind.Method,
              insertText: method,
              range,
            })),
            ...dictMethods.map((method) => ({
              label: method,
              kind: monaco.languages.CompletionItemKind.Method,
              insertText: method,
              range,
            })),
          ];
        } else if (isTypeAnnotation) {
          suggestions = types.map((type) => ({
            label: type,
            kind: monaco.languages.CompletionItemKind.Class,
            insertText: type,
            range,
          }));
        } else {
          // Default suggestions
          suggestions = [
            ...keywords.map((keyword) => ({
              label: keyword,
              kind: monaco.languages.CompletionItemKind.Keyword,
              insertText: keyword,
              range,
            })),
            ...builtins.map((builtin) => ({
              label: builtin,
              kind: monaco.languages.CompletionItemKind.Function,
              insertText: builtin,
              range,
            })),
            // Common code snippets
            {
              label: 'def',
              kind: monaco.languages.CompletionItemKind.Snippet,
              insertText: 'def ${1:function_name}(${2:parameters}):\n\t${3:pass}',
              insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
              range,
            },
            {
              label: 'class',
              kind: monaco.languages.CompletionItemKind.Snippet,
              insertText: 'class ${1:ClassName}:\n\tdef __init__(self, ${2:parameters}):\n\t\t${3:pass}',
              insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
              range,
            },
            {
              label: 'if',
              kind: monaco.languages.CompletionItemKind.Snippet,
              insertText: 'if ${1:condition}:\n\t${2:pass}',
              insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
              range,
            },
            {
              label: 'for',
              kind: monaco.languages.CompletionItemKind.Snippet,
              insertText: 'for ${1:item} in ${2:iterable}:\n\t${3:pass}',
              insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
              range,
            },
            {
              label: 'while',
              kind: monaco.languages.CompletionItemKind.Snippet,
              insertText: 'while ${1:condition}:\n\t${2:pass}',
              insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
              range,
            },
            {
              label: 'try',
              kind: monaco.languages.CompletionItemKind.Snippet,
              insertText: 'try:\n\t${1:pass}\nexcept ${2:Exception} as ${3:e}:\n\t${4:pass}',
              insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
              range,
            },
          ];
        }

        return {
          suggestions,
        };
      },
    });

    return () => {
      disposable.dispose();
    };
  }, [monaco, editor]);

  return null;
}
