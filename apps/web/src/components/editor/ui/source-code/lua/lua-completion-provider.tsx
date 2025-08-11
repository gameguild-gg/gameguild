'use client';

import { useEffect } from 'react';
import type { editor } from 'monaco-editor';

interface LuaCompletionProviderProps {
  monaco: typeof import('monaco-editor') | null;
  editor: editor.IStandaloneCodeEditor | null;
}

export function LuaCompletionProvider({ monaco, editor }: LuaCompletionProviderProps) {
  useEffect(() => {
    if (!monaco || !editor) return;

    // Register Lua completion provider
    const disposable = monaco.languages.registerCompletionItemProvider('lua', {
      provideCompletionItems: (model, position) => {
        const word = model.getWordUntilPosition(position);
        const range = {
          startLineNumber: position.lineNumber,
          endLineNumber: position.lineNumber,
          startColumn: word.startColumn,
          endColumn: word.endColumn,
        };

        // Lua keywords
        const keywords = ['and', 'break', 'do', 'else', 'elseif', 'end', 'false', 'for', 'function', 'goto', 'if', 'in', 'local', 'nil', 'not', 'or', 'repeat', 'return', 'then', 'true', 'until', 'while'];

        // Lua built-in functions
        const builtins = [
          'assert',
          'collectgarbage',
          'dofile',
          'error',
          'getmetatable',
          'ipairs',
          'load',
          'loadfile',
          'next',
          'pairs',
          'pcall',
          'print',
          'rawequal',
          'rawget',
          'rawset',
          'require',
          'select',
          'setmetatable',
          'tonumber',
          'tostring',
          'type',
          'xpcall',
        ];

        // Lua standard libraries
        const stringLib = [
          'string.byte',
          'string.char',
          'string.dump',
          'string.find',
          'string.format',
          'string.gmatch',
          'string.gsub',
          'string.len',
          'string.lower',
          'string.match',
          'string.rep',
          'string.reverse',
          'string.sub',
          'string.upper',
        ];

        const tableLib = ['table.concat', 'table.insert', 'table.move', 'table.pack', 'table.remove', 'table.sort', 'table.unpack'];

        const mathLib = [
          'math.abs',
          'math.acos',
          'math.asin',
          'math.atan',
          'math.ceil',
          'math.cos',
          'math.deg',
          'math.exp',
          'math.floor',
          'math.fmod',
          'math.huge',
          'math.log',
          'math.max',
          'math.min',
          'math.modf',
          'math.pi',
          'math.rad',
          'math.random',
          'math.randomseed',
          'math.sin',
          'math.sqrt',
          'math.tan',
        ];

        const ioLib = ['io.close', 'io.flush', 'io.input', 'io.lines', 'io.open', 'io.output', 'io.popen', 'io.read', 'io.tmpfile', 'io.type', 'io.write'];

        const osLib = ['os.clock', 'os.date', 'os.difftime', 'os.execute', 'os.exit', 'os.getenv', 'os.remove', 'os.rename', 'os.setlocale', 'os.time', 'os.tmpname'];

        // Get the line text before the cursor
        const textUntilPosition = model.getValueInRange({
          startLineNumber: position.lineNumber,
          startColumn: 1,
          endLineNumber: position.lineNumber,
          endColumn: position.column,
        });

        // Check if we're after a dot (for method suggestions)
        const isDot = /\.\s*$/.test(textUntilPosition);

        // Check if we're after a colon (for method calls)
        const isColon = /:\s*$/.test(textUntilPosition);

        // Check if we're in a require statement
        const isRequire = /require\s*\(\s*(['"]).*$/.test(textUntilPosition);

        // Create suggestions based on context
        let suggestions = [];

        if (isRequire) {
          // Common Lua modules
          const modules = ['string', 'table', 'math', 'io', 'os', 'debug', 'coroutine', 'package', 'utf8'];
          suggestions = modules.map((module) => ({
            label: module,
            kind: monaco.languages.CompletionItemKind.Module,
            insertText: module,
            range,
          }));
        } else if (isDot) {
          // Check which library we're accessing
          if (textUntilPosition.endsWith('string.')) {
            suggestions = stringLib.map((method) => ({
              label: method.split('.')[1],
              kind: monaco.languages.CompletionItemKind.Method,
              insertText: method.split('.')[1],
              range,
            }));
          } else if (textUntilPosition.endsWith('table.')) {
            suggestions = tableLib.map((method) => ({
              label: method.split('.')[1],
              kind: monaco.languages.CompletionItemKind.Method,
              insertText: method.split('.')[1],
              range,
            }));
          } else if (textUntilPosition.endsWith('math.')) {
            suggestions = mathLib.map((method) => ({
              label: method.split('.')[1],
              kind: monaco.languages.CompletionItemKind.Method,
              insertText: method.split('.')[1],
              range,
            }));
          } else if (textUntilPosition.endsWith('io.')) {
            suggestions = ioLib.map((method) => ({
              label: method.split('.')[1],
              kind: monaco.languages.CompletionItemKind.Method,
              insertText: method.split('.')[1],
              range,
            }));
          } else if (textUntilPosition.endsWith('os.')) {
            suggestions = osLib.map((method) => ({
              label: method.split('.')[1],
              kind: monaco.languages.CompletionItemKind.Method,
              insertText: method.split('.')[1],
              range,
            }));
          } else {
            // Generic table methods
            const tableMethods = ['insert', 'remove', 'concat', 'sort', 'unpack'];
            suggestions = tableMethods.map((method) => ({
              label: method,
              kind: monaco.languages.CompletionItemKind.Method,
              insertText: method,
              range,
            }));
          }
        } else if (isColon) {
          // Common object methods
          const objectMethods = ['new', 'create', 'init', 'update', 'draw', 'destroy'];
          suggestions = objectMethods.map((method) => ({
            label: method,
            kind: monaco.languages.CompletionItemKind.Method,
            insertText: method,
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
              label: 'function',
              kind: monaco.languages.CompletionItemKind.Snippet,
              insertText: 'function ${1:name}(${2:params})\n\t${3:-- body}\nend',
              insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
              range,
            },
            {
              label: 'local function',
              kind: monaco.languages.CompletionItemKind.Snippet,
              insertText: 'local function ${1:name}(${2:params})\n\t${3:-- body}\nend',
              insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
              range,
            },
            {
              label: 'if',
              kind: monaco.languages.CompletionItemKind.Snippet,
              insertText: 'if ${1:condition} then\n\t${2:-- body}\nend',
              insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
              range,
            },
            {
              label: 'if-else',
              kind: monaco.languages.CompletionItemKind.Snippet,
              insertText: 'if ${1:condition} then\n\t${2:-- body}\nelse\n\t${3:-- else body}\nend',
              insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
              range,
            },
            {
              label: 'for',
              kind: monaco.languages.CompletionItemKind.Snippet,
              insertText: 'for ${1:i}=${2:1},${3:10} do\n\t${4:-- body}\nend',
              insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
              range,
            },
            {
              label: 'for-in',
              kind: monaco.languages.CompletionItemKind.Snippet,
              insertText: 'for ${1:k},${2:v} in pairs(${3:table}) do\n\t${4:-- body}\nend',
              insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
              range,
            },
            {
              label: 'while',
              kind: monaco.languages.CompletionItemKind.Snippet,
              insertText: 'while ${1:condition} do\n\t${2:-- body}\nend',
              insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
              range,
            },
            {
              label: 'repeat',
              kind: monaco.languages.CompletionItemKind.Snippet,
              insertText: 'repeat\n\t${1:-- body}\nuntil ${2:condition}',
              insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
              range,
            },
            {
              label: 'local',
              kind: monaco.languages.CompletionItemKind.Snippet,
              insertText: 'local ${1:name} = ${2:value}',
              insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
              range,
            },
            {
              label: 'table',
              kind: monaco.languages.CompletionItemKind.Snippet,
              insertText: 'local ${1:name} = {\n\t${2:key} = ${3:value},\n}',
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
