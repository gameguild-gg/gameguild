'use client';

import { useEffect } from 'react';
import type { editor } from 'monaco-editor';

interface LuaSyntaxHighlighterProps {
  monaco: typeof import('monaco-editor') | null;
  editor: editor.IStandaloneCodeEditor | null;
}

export function LuaSyntaxHighlighter({ monaco, editor }: LuaSyntaxHighlighterProps) {
  useEffect(() => {
    if (!monaco || !editor) return;

    // Register Lua language if not already registered
    const languages = monaco.languages.getLanguages();
    const luaLanguage = languages.find((lang) => lang.id === 'lua');

    if (!luaLanguage) {
      console.log('Registering Lua language');

      // Define Lua tokens and syntax highlighting rules
      monaco.languages.register({ id: 'lua' });

      monaco.languages.setMonarchTokensProvider('lua', {
        defaultToken: 'invalid',
        tokenPostfix: '.lua',

        keywords: [
          'and',
          'break',
          'do',
          'else',
          'elseif',
          'end',
          'false',
          'for',
          'function',
          'goto',
          'if',
          'in',
          'local',
          'nil',
          'not',
          'or',
          'repeat',
          'return',
          'then',
          'true',
          'until',
          'while',
        ],

        builtins: [
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
          // String library
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
          // Table library
          'table.concat',
          'table.insert',
          'table.move',
          'table.pack',
          'table.remove',
          'table.sort',
          'table.unpack',
          // Math library
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
          // IO library
          'io.close',
          'io.flush',
          'io.input',
          'io.lines',
          'io.open',
          'io.output',
          'io.popen',
          'io.read',
          'io.tmpfile',
          'io.type',
          'io.write',
          // OS library
          'os.clock',
          'os.date',
          'os.difftime',
          'os.execute',
          'os.exit',
          'os.getenv',
          'os.remove',
          'os.rename',
          'os.setlocale',
          'os.time',
          'os.tmpname',
        ],

        brackets: [
          { open: '{', close: '}', token: 'delimiter.curly' },
          { open: '[', close: ']', token: 'delimiter.square' },
          { open: '(', close: ')', token: 'delimiter.parenthesis' },
        ],

        // Lua specific tokens
        symbols: /[=><!~?:&|+\-*/^%.]+/,
        escapes: /\\(?:[abfnrtv\\"']|x[0-9A-Fa-f]{1,4}|u[0-9A-Fa-f]{4}|U[0-9A-Fa-f]{8})/,

        tokenizer: {
          root: [
            // Identifiers and keywords
            [
              /[a-zA-Z_]\w*/,
              {
                cases: {
                  '@keywords': 'keyword',
                  '@builtins': 'type.identifier',
                  '@default': 'identifier',
                },
              },
            ],

            // Whitespace
            { include: '@whitespace' },

            // Strings
            [/"([^"\\]|\\.)*$/, 'string.invalid'], // non-terminated string
            [/'([^'\\]|\\.)*$/, 'string.invalid'], // non-terminated string
            [/"/, 'string', '@string_double'],
            [/'/, 'string', '@string_single'],
            [/\[(=*)\[/, 'string', '@string_multiline'],

            // Delimiters and operators
            [/[{}()[\]]/, '@brackets'],
            [/[<>](?!@symbols)/, '@brackets'],
            [
              /@symbols/,
              {
                cases: {
                  '@operators': 'delimiter',
                  '@default': '',
                },
              },
            ],

            // Numbers
            [/\d*\.\d+([eE][-+]?\d+)?/, 'number.float'],
            [/0[xX][0-9a-fA-F]+/, 'number.hex'],
            [/\d+/, 'number'],

            // Delimiter
            [/[;,.]/, 'delimiter'],
          ],

          comment: [
            [/[^\-\-[\]]+/, 'comment'],
            [/\]\]/, 'comment', '@pop'],
            [/--\[\]/, 'comment'],
          ],

          string_double: [
            [/[^\\"]+/, 'string'],
            [/@escapes/, 'string.escape'],
            [/\\./, 'string.escape.invalid'],
            [/"/, 'string', '@pop'],
          ],

          string_single: [
            [/[^\\']+/, 'string'],
            [/@escapes/, 'string.escape'],
            [/\\./, 'string.escape.invalid'],
            [/'/, 'string', '@pop'],
          ],

          string_multiline: [
            [/[^\]]+/, 'string'],
            [
              /\](=*)\]/,
              {
                cases: {
                  '$1==$S2': { token: 'string', next: '@pop' },
                  '@default': 'string',
                },
              },
            ],
            [/./, 'string'],
          ],

          whitespace: [
            [/[ \t\r\n]+/, 'white'],
            [/--\[([=]*)\[/, 'comment', '@comment'],
            [/--.*$/, 'comment'],
          ],
        },
      });
    }

    // Set the language for the current model if it's Lua
    const model = editor.getModel();
    if (model && model.getLanguageId() === 'lua') {
      monaco.editor.setModelLanguage(model, 'lua');
    }

    return () => {
      // Cleanup if needed
    };
  }, [monaco, editor]);

  return null;
}
