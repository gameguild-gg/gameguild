'use client';

import { useEffect } from 'react';
import type { editor, languages } from 'monaco-editor';

interface CCompletionProviderProps {
  monaco: typeof import('monaco-editor') | null;
  editor: editor.IStandaloneCodeEditor | null;
}

export function CCompletionProvider({ monaco, editor }: CCompletionProviderProps) {
  useEffect(() => {
    if (!monaco || !editor) return;

    // Register C completion provider
    const disposable = monaco.languages.registerCompletionItemProvider('c', {
      provideCompletionItems: (model, position) => {
        const word = model.getWordUntilPosition(position);
        const range = {
          startLineNumber: position.lineNumber,
          endLineNumber: position.lineNumber,
          startColumn: word.startColumn,
          endColumn: word.endColumn,
        };

        // C keywords
        const keywords = [
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
        ];

        // C types
        const types = [
          'bool',
          'complex',
          'imaginary',
          'FILE',
          'size_t',
          'time_t',
          'wchar_t',
          'int8_t',
          'int16_t',
          'int32_t',
          'int64_t',
          'uint8_t',
          'uint16_t',
          'uint32_t',
          'uint64_t',
          'intptr_t',
          'uintptr_t',
          'ptrdiff_t',
        ];

        // C standard library functions
        const stdlibFunctions = [
          // stdio.h
          'printf',
          'scanf',
          'fprintf',
          'fscanf',
          'sprintf',
          'sscanf',
          'fopen',
          'fclose',
          'fread',
          'fwrite',
          'fseek',
          'ftell',
          'rewind',
          'fgetc',
          'fputc',
          'fgets',
          'fputs',
          'getchar',
          'putchar',
          'gets',
          'puts',
          'perror',
          'remove',
          'rename',

          // stdlib.h
          'malloc',
          'calloc',
          'realloc',
          'free',
          'exit',
          'abort',
          'atexit',
          'system',
          'getenv',
          'atoi',
          'atol',
          'atof',
          'strtol',
          'strtoul',
          'strtod',
          'rand',
          'srand',
          'qsort',
          'bsearch',
          'abs',
          'labs',
          'div',
          'ldiv',

          // string.h
          'strcpy',
          'strncpy',
          'strcat',
          'strncat',
          'strcmp',
          'strncmp',
          'strchr',
          'strrchr',
          'strstr',
          'strlen',
          'strerror',
          'memcpy',
          'memmove',
          'memcmp',
          'memchr',
          'memset',

          // math.h
          'sin',
          'cos',
          'tan',
          'asin',
          'acos',
          'atan',
          'atan2',
          'sinh',
          'cosh',
          'tanh',
          'exp',
          'log',
          'log10',
          'pow',
          'sqrt',
          'ceil',
          'floor',
          'fabs',
          'ldexp',
          'frexp',
          'modf',
          'fmod',

          // time.h
          'time',
          'clock',
          'difftime',
          'mktime',
          'asctime',
          'ctime',
          'gmtime',
          'localtime',
          'strftime',

          // ctype.h
          'isalnum',
          'isalpha',
          'iscntrl',
          'isdigit',
          'isgraph',
          'islower',
          'isprint',
          'ispunct',
          'isspace',
          'isupper',
          'isxdigit',
          'tolower',
          'toupper',
        ];

        // Preprocessor directives
        const preprocessor = ['include', 'define', 'undef', 'ifdef', 'ifndef', 'if', 'else', 'elif', 'endif', 'error', 'pragma', 'line'];

        // Get the line text before the cursor
        const textUntilPosition = model.getValueInRange({
          startLineNumber: position.lineNumber,
          startColumn: 1,
          endLineNumber: position.lineNumber,
          endColumn: position.column,
        });

        // Check if we're in a preprocessor directive
        const isPreprocessor = /^\s*#/.test(textUntilPosition);

        // Check if we're after a dot or arrow (for struct/union member access)
        const isDot = /\.\s*$/.test(textUntilPosition);
        const isArrow = /->\s*$/.test(textUntilPosition);

        // Check if we're after a struct or union keyword
        const isStructOrUnion = /\b(struct|union)\s+$/.test(textUntilPosition);

        // Create suggestions based on context
        let suggestions: (
          | {
              label: string;
              kind: languages.CompletionItemKind;
              insertText: string;
              range: { startLineNumber: number; endLineNumber: number; startColumn: number; endColumn: number };
            }
          | {
              label: string;
              kind: languages.CompletionItemKind;
              insertText: string;
              insertTextRules: languages.CompletionItemInsertTextRule;
              range: { startLineNumber: number; endLineNumber: number; startColumn: number; endColumn: number };
            }
        )[] = [];

        if (isPreprocessor) {
          // After # we suggest preprocessor directives
          if (/^\s*#\s*$/.test(textUntilPosition)) {
            suggestions = preprocessor.map((directive) => ({
              label: directive,
              kind: monaco.languages.CompletionItemKind.Keyword,
              insertText: directive,
              range,
            }));
          }
          // After #include we suggest common headers
          else if (/^\s*#\s*include\s+$/.test(textUntilPosition)) {
            const headers = [
              '<stdio.h>',
              '<stdlib.h>',
              '<string.h>',
              '<math.h>',
              '<time.h>',
              '<ctype.h>',
              '<stdbool.h>',
              '<stdint.h>',
              '<assert.h>',
              '<limits.h>',
              '<float.h>',
              '<errno.h>',
              '<signal.h>',
              '<setjmp.h>',
              '<stdarg.h>',
            ];

            suggestions = headers.map((header) => ({
              label: header,
              kind: monaco.languages.CompletionItemKind.File,
              insertText: header,
              range,
            }));
          }
        } else if (isDot || isArrow) {
          // Struct/union member access - would need semantic analysis for accurate suggestions
          // For now, just provide some common struct members for standard types
          const fileMembers = ['name', 'size', 'mode', 'position', 'eof', 'error'];
          const timeMembers = ['tm_sec', 'tm_min', 'tm_hour', 'tm_mday', 'tm_mon', 'tm_year', 'tm_wday', 'tm_yday', 'tm_isdst'];

          suggestions = [
            ...fileMembers.map((member) => ({
              label: member,
              kind: monaco.languages.CompletionItemKind.Field,
              insertText: member,
              range,
            })),
            ...timeMembers.map((member) => ({
              label: member,
              kind: monaco.languages.CompletionItemKind.Field,
              insertText: member,
              range,
            })),
          ];
        } else if (isStructOrUnion) {
          // After struct or union keyword, suggest common struct/union names
          const commonStructs = ['Node', 'Point', 'Rectangle', 'Circle', 'Person', 'Student', 'Employee', 'Data', 'Config'];

          suggestions = commonStructs.map((name) => ({
            label: name,
            kind: monaco.languages.CompletionItemKind.Class,
            insertText: name,
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
            ...types.map((type) => ({
              label: type,
              kind: monaco.languages.CompletionItemKind.Class,
              insertText: type,
              range,
            })),
            ...stdlibFunctions.map((func) => ({
              label: func,
              kind: monaco.languages.CompletionItemKind.Function,
              insertText: func,
              range,
            })),
            // Common code snippets
            {
              label: 'main',
              kind: monaco.languages.CompletionItemKind.Snippet,
              insertText: 'int main(int argc, char *argv[]) {\n\t${1:// Your code here}\n\treturn 0;\n}',
              insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
              range,
            },
            {
              label: 'if',
              kind: monaco.languages.CompletionItemKind.Snippet,
              insertText: 'if (${1:condition}) {\n\t${2:// Your code here}\n}',
              insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
              range,
            },
            {
              label: 'ifelse',
              kind: monaco.languages.CompletionItemKind.Snippet,
              insertText: 'if (${1:condition}) {\n\t${2:// Your code here}\n} else {\n\t${3:// Alternative code}\n}',
              insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
              range,
            },
            {
              label: 'for',
              kind: monaco.languages.CompletionItemKind.Snippet,
              insertText: 'for (${1:int i = 0}; ${2:i < n}; ${3:i++}) {\n\t${4:// Your code here}\n}',
              insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
              range,
            },
            {
              label: 'while',
              kind: monaco.languages.CompletionItemKind.Snippet,
              insertText: 'while (${1:condition}) {\n\t${2:// Your code here}\n}',
              insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
              range,
            },
            {
              label: 'do',
              kind: monaco.languages.CompletionItemKind.Snippet,
              insertText: 'do {\n\t${1:// Your code here}\n} while (${2:condition});',
              insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
              range,
            },
            {
              label: 'switch',
              kind: monaco.languages.CompletionItemKind.Snippet,
              insertText:
                'switch (${1:expression}) {\n\tcase ${2:value1}:\n\t\t${3:// Your code here}\n\t\tbreak;\n\tcase ${4:value2}:\n\t\t${5:// Your code here}\n\t\tbreak;\n\tdefault:\n\t\t${6:// Default code}\n\t\tbreak;\n}',
              insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
              range,
            },
            {
              label: 'struct',
              kind: monaco.languages.CompletionItemKind.Snippet,
              insertText: 'struct ${1:StructName} {\n\t${2:int member1};\n\t${3:char member2};\n};',
              insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
              range,
            },
            {
              label: 'function',
              kind: monaco.languages.CompletionItemKind.Snippet,
              insertText: '${1:void} ${2:functionName}(${3:parameters}) {\n\t${4:// Your code here}\n}',
              insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
              range,
            },
            {
              label: 'malloc',
              kind: monaco.languages.CompletionItemKind.Snippet,
              insertText:
                '${1:type} *${2:ptr} = (${1:type} *)malloc(${3:size} * sizeof(${1:type}));\nif (${2:ptr} == NULL) {\n\t${4:// Handle allocation failure}\n}',
              insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
              range,
            },
            {
              label: 'fopen',
              kind: monaco.languages.CompletionItemKind.Snippet,
              insertText: 'FILE *${1:fp} = fopen("${2:filename}", "${3:r}");\nif (${1:fp} == NULL) {\n\t${4:// Handle file opening failure}\n}',
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
