"use client"

import { useEffect } from "react"
import type { editor } from "monaco-editor"

interface PythonSyntaxHighlighterProps {
  monaco: typeof import("monaco-editor") | null
  editor: editor.IStandaloneCodeEditor | null
}

export function PythonSyntaxHighlighter({ monaco, editor }: PythonSyntaxHighlighterProps) {
  useEffect(() => {
    if (!monaco || !editor) return

    // Register Python language if not already registered
    const languages = monaco.languages.getLanguages()
    const pythonLanguage = languages.find((lang) => lang.id === "python")

    if (!pythonLanguage) {
      console.log("Registering Python language")

      // Define Python tokens and syntax highlighting rules
      monaco.languages.register({ id: "python" })

      monaco.languages.setMonarchTokensProvider("python", {
        defaultToken: "invalid",
        tokenPostfix: ".python",

        keywords: [
          "and",
          "as",
          "assert",
          "async",
          "await",
          "break",
          "class",
          "continue",
          "def",
          "del",
          "elif",
          "else",
          "except",
          "exec",
          "finally",
          "for",
          "from",
          "global",
          "if",
          "import",
          "in",
          "is",
          "lambda",
          "nonlocal",
          "not",
          "or",
          "pass",
          "print",
          "raise",
          "return",
          "try",
          "while",
          "with",
          "yield",
          "False",
          "None",
          "True",
        ],

        builtins: [
          "abs",
          "all",
          "any",
          "bin",
          "bool",
          "bytearray",
          "callable",
          "chr",
          "classmethod",
          "compile",
          "complex",
          "delattr",
          "dict",
          "dir",
          "divmod",
          "enumerate",
          "eval",
          "filter",
          "float",
          "format",
          "frozenset",
          "getattr",
          "globals",
          "hasattr",
          "hash",
          "help",
          "hex",
          "id",
          "input",
          "int",
          "isinstance",
          "issubclass",
          "iter",
          "len",
          "list",
          "locals",
          "map",
          "max",
          "memoryview",
          "min",
          "next",
          "object",
          "oct",
          "open",
          "ord",
          "pow",
          "property",
          "range",
          "repr",
          "reversed",
          "round",
          "set",
          "setattr",
          "slice",
          "sorted",
          "staticmethod",
          "str",
          "sum",
          "super",
          "tuple",
          "type",
          "vars",
          "zip",
          "__import__",
          "NotImplemented",
          "Ellipsis",
          "__debug__",
        ],

        brackets: [
          { open: "{", close: "}", token: "delimiter.curly" },
          { open: "[", close: "]", token: "delimiter.bracket" },
          { open: "(", close: ")", token: "delimiter.parenthesis" },
        ],

        tokenizer: {
          root: [
            { include: "@whitespace" },
            { include: "@numbers" },
            { include: "@strings" },

            [/[,:;]/, "delimiter"],
            [/[{}[\]()]/, "@brackets"],

            [/@[a-zA-Z_]\w*/, "tag"],
            [
              /[a-zA-Z_]\w*/,
              {
                cases: {
                  "@keywords": "keyword",
                  "@builtins": "type.identifier",
                  "@default": "identifier",
                },
              },
            ],
          ],

          whitespace: [
            [/\s+/, "white"],
            [/(^#.*$)/, "comment"],
            [/'''/, "string", "@endDocString"],
            [/"""/, "string", "@endDblDocString"],
          ],

          endDocString: [
            [/[^']+/, "string"],
            [/\\'/, "string"],
            [/'''/, "string", "@popall"],
            [/'/, "string"],
          ],

          endDblDocString: [
            [/[^"]+/, "string"],
            [/\\"/, "string"],
            [/"""/, "string", "@popall"],
            [/"/, "string"],
          ],

          numbers: [
            [/\d*\.\d+([eE][-+]?\d+)?/, "number.float"],
            [/0[xX][0-9a-fA-F]+/, "number.hex"],
            [/\d+/, "number"],
          ],

          strings: [
            [/'/, "string", "@singleQuotedString"],
            [/"/, "string", "@doubleQuotedString"],
          ],

          singleQuotedString: [
            [/[^\\']+/, "string"],
            [/\\./, "string.escape"],
            [/'/, "string", "@pop"],
          ],

          doubleQuotedString: [
            [/[^\\"]+/, "string"],
            [/\\./, "string.escape"],
            [/"/, "string", "@pop"],
          ],
        },
      })
    }

    // Set the language for the current model if it's Python
    const model = editor.getModel()
    if (model && model.getLanguageId() === "python") {
      monaco.editor.setModelLanguage(model, "python")
    }

    return () => {
      // Cleanup if needed
    }
  }, [monaco, editor])

  return null
}
