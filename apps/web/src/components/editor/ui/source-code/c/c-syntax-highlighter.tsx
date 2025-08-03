'use client';

import { useEffect } from 'react';
import type { editor } from 'monaco-editor';

interface CSyntaxHighlighterProps {
  monaco: typeof import('monaco-editor') | null;
  editor: editor.IStandaloneCodeEditor | null;
}

export function CSyntaxHighlighter({ monaco, editor }: CSyntaxHighlighterProps) {
  useEffect(() => {
    if (!monaco || !editor) return;

    // Register C language if not already registered
    const languages = monaco.languages.getLanguages();
    const cLanguage = languages.find((lang) => lang.id === 'c');

    if (!cLanguage) {
      console.log('Registering C language');

      // Define C tokens and syntax highlighting rules
      monaco.languages.register({ id: 'c' });

      monaco.languages.setMonarchTokensProvider('c', {
        defaultToken: 'invalid',
        tokenPostfix: '.c',

        keywords: [
          'auto',
          'break',
          'case',
          'char',
          'const',
          'continue',
          'default',
          'do',
          'double',
          'else',
          'enum',
          'extern',
          'float',
          'for',
          'goto',
          'if',
          'int',
          'long',
          'register',
          'return',
          'short',
          'signed',
          'sizeof',
          'static',
          'struct',
          'switch',
          'typedef',
          'union',
          'unsigned',
          'void',
          'volatile',
          'while',
          '_Alignas',
          '_Alignof',
          '_Atomic',
          '_Bool',
          '_Complex',
          '_Generic',
          '_Imaginary',
          '_Noreturn',
          '_Static_assert',
          '_Thread_local',
          'inline',
          'restrict',
        ],

        typeKeywords: ['bool', 'complex', 'imaginary', 'FILE', 'size_t', 'time_t', 'wchar_t', 'int8_t', 'int16_t', 'int32_t', 'int64_t', 'uint8_t', 'uint16_t', 'uint32_t', 'uint64_t', 'intptr_t', 'uintptr_t', 'ptrdiff_t'],

        operators: ['=', '>', '<', '!', '~', '?', ':', '==', '<=', '>=', '!=', '&&', '||', '++', '--', '+', '-', '*', '/', '&', '|', '^', '%', '<<', '>>', '+=', '-=', '*=', '/=', '&=', '|=', '^=', '%=', '<<=', '>>='],

        symbols: /[=><!~?:&|+\-*/^%]+/,

        escapes: /\\(?:[abfnrtv\\"']|x[0-9A-Fa-f]{1,4}|u[0-9A-Fa-f]{4}|U[0-9A-Fa-f]{8})/,

        // The main tokenizer for our languages
        tokenizer: {
          root: [
            // preprocessor directives
            [/^\s*#\s*\w+/, 'keyword.directive'],

            // identifiers and keywords
            [
              /[a-zA-Z_]\w*/,
              {
                cases: {
                  '@keywords': 'keyword',
                  '@typeKeywords': 'type',
                  '@default': 'identifier',
                },
              },
            ],

            // whitespace
            { include: '@whitespace' },

            // delimiters and operators
            [/[{}()[\]]/, '@brackets'],
            [/[<>](?!@symbols)/, '@brackets'],
            [
              /@symbols/,
              {
                cases: {
                  '@operators': 'operator',
                  '@default': '',
                },
              },
            ],

            // numbers
            [/\d*\.\d+([eE][-+]?\d+)?/, 'number.float'],
            [/0[xX][0-9a-fA-F]+/, 'number.hex'],
            [/\d+/, 'number'],

            // delimiter: after number because of .\d floats
            [/[;,.]/, 'delimiter'],

            // strings
            [/"([^"\\]|\\.)*$/, 'string.invalid'], // non-terminated string
            [/"/, { token: 'string.quote', bracket: '@open', next: '@string' }],

            // characters
            [/'[^\\']'/, 'string'],
            [/(')(@escapes)(')/, ['string', 'string.escape', 'string']],
            [/'/, 'string.invalid'],
          ],

          comment: [
            [/[^/*]+/, 'comment'],
            [/\/\*/, 'comment', '@push'], // nested comment
            ['\\*/', 'comment', '@pop'],
            [/[/*]/, 'comment'],
          ],

          string: [
            [/[^\\"]+/, 'string'],
            [/@escapes/, 'string.escape'],
            [/\\./, 'string.escape.invalid'],
            [/"/, { token: 'string.quote', bracket: '@close', next: '@pop' }],
          ],

          whitespace: [
            [/[ \t\r\n]+/, 'white'],
            [/\/\*/, 'comment', '@comment'],
            [/\/\/.*$/, 'comment'],
          ],
        },
      });
    }

    // Set the language for the current model if it's C
    const model = editor.getModel();
    if (model && model.getLanguageId() === 'c') {
      monaco.editor.setModelLanguage(model, 'c');
    }

    return () => {
      // Cleanup if needed
    };
  }, [monaco, editor]);

  return null;
}
