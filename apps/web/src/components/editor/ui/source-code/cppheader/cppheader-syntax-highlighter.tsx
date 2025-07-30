'use client';

import { useEffect } from 'react';
import type { editor } from 'monaco-editor';

interface CppHeaderSyntaxHighlighterProps {
  monaco: typeof import('monaco-editor') | null;
  editor: editor.IStandaloneCodeEditor | null;
}

export function CppHeaderSyntaxHighlighter({ monaco, editor }: CppHeaderSyntaxHighlighterProps) {
  useEffect(() => {
    if (!monaco || !editor) return;

    // Register C++ Header language if not already registered
    const languages = monaco.languages.getLanguages();
    const cppHeaderLanguage = languages.find((lang) => lang.id === 'cppheader');

    if (!cppHeaderLanguage) {
      console.log('Registering C++ Header language');

      // Define C++ Header tokens and syntax highlighting rules
      monaco.languages.register({ id: 'cppheader' });

      monaco.languages.setMonarchTokensProvider('cppheader', {
        defaultToken: 'invalid',
        tokenPostfix: '.cppheader',

        keywords: [
          'alignas',
          'alignof',
          'and',
          'and_eq',
          'asm',
          'auto',
          'bitand',
          'bitor',
          'bool',
          'break',
          'case',
          'catch',
          'char',
          'char8_t',
          'char16_t',
          'char32_t',
          'class',
          'compl',
          'concept',
          'const',
          'consteval',
          'constexpr',
          'constinit',
          'const_cast',
          'continue',
          'co_await',
          'co_return',
          'co_yield',
          'decltype',
          'default',
          'delete',
          'do',
          'double',
          'dynamic_cast',
          'else',
          'enum',
          'explicit',
          'export',
          'extern',
          'false',
          'float',
          'for',
          'friend',
          'goto',
          'if',
          'inline',
          'int',
          'long',
          'mutable',
          'namespace',
          'new',
          'noexcept',
          'not',
          'not_eq',
          'nullptr',
          'operator',
          'or',
          'or_eq',
          'private',
          'protected',
          'public',
          'register',
          'reinterpret_cast',
          'requires',
          'return',
          'short',
          'signed',
          'sizeof',
          'static',
          'static_assert',
          'static_cast',
          'struct',
          'switch',
          'template',
          'this',
          'thread_local',
          'throw',
          'true',
          'try',
          'typedef',
          'typeid',
          'typename',
          'union',
          'unsigned',
          'using',
          'virtual',
          'void',
          'volatile',
          'wchar_t',
          'while',
          'xor',
          'xor_eq',
        ],

        operators: [
          '=',
          '>',
          '<',
          '!',
          '~',
          '?',
          ':',
          '==',
          '<=',
          '>=',
          '!=',
          '&&',
          '||',
          '++',
          '--',
          '+',
          '-',
          '*',
          '/',
          '&',
          '|',
          '^',
          '%',
          '<<',
          '>>',
          '>>>',
          '+=',
          '-=',
          '*=',
          '/=',
          '&=',
          '|=',
          '^=',
          '%=',
          '<<=',
          '>>=',
          '>>>=',
        ],

        symbols: /[=><!~?:&|+\-*/^%]+/,

        escapes: /\\(?:[abfnrtv\\"']|x[0-9A-Fa-f]{1,4}|u[0-9A-Fa-f]{4}|U[0-9A-Fa-f]{8})/,

        // The main tokenizer for our languages
        tokenizer: {
          root: [
            // Identifiers and keywords
            [
              /[a-z_$][\w$]*/,
              {
                cases: {
                  '@keywords': 'keyword',
                  '@default': 'identifier',
                },
              },
            ],

            // Preprocessor directives
            [/^\s*#\s*\w+/, 'keyword.directive'],

            // Whitespace
            { include: '@whitespace' },

            // Delimiters and operators
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

            // Numbers
            [/\d*\.\d+([eE][-+]?\d+)?/, 'number.float'],
            [/0[xX][0-9a-fA-F]+/, 'number.hex'],
            [/\d+/, 'number'],

            // Delimiter: after number because of .\d floats
            [/[;,.]/, 'delimiter'],

            // Strings
            [/"([^"\\]|\\.)*$/, 'string.invalid'], // non-terminated string
            [/"/, { token: 'string.quote', bracket: '@open', next: '@string' }],

            // Characters
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
           [/[ \t\r]+/, "white"],
           [/\/\*/, "comment", "@comment"],
           [/\/\/.*$/, "comment"],
         ],
       },
     })
   }

    // Set the language for the current model if it's C++ Header
    const model = editor.getModel();
    if (model && model.getLanguageId() === 'cppheader') {
      monaco.editor.setModelLanguage(model, 'cppheader');
    }

    return () => {
      // Cleanup if needed
    };
  }, [monaco, editor]);

  return null;
}
