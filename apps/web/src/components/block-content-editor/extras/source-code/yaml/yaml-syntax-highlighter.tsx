"use client"

import { useEffect } from "react"
import type { editor } from "monaco-editor"

interface YAMLSyntaxHighlighterProps {
  monaco: typeof import("monaco-editor") | null
  editor: editor.IStandaloneCodeEditor | null
}

export function YAMLSyntaxHighlighter({ monaco, editor }: YAMLSyntaxHighlighterProps) {
  useEffect(() => {
    if (!monaco || !editor) return

    // Register YAML language if not already registered
    const languages = monaco.languages.getLanguages()
    const yamlLanguage = languages.find((lang) => lang.id === "yaml")

    if (!yamlLanguage) {
      console.log("Registering YAML language")

      monaco.languages.register({ id: "yaml" })

      monaco.languages.setMonarchTokensProvider("yaml", {
        tokenizer: {
          root: [
            [/:\s/, "keyword"], // Key-value separator
            [/-\s/, "keyword"], // List item indicator
            [/(\s+)([\w.-]+:)/, ["white", "keyword"]], // Key-value pair with indentation
            [/(\s+)(-)/, ["white", "keyword"]], // List item with indentation
            [/(\s+)(#.*)/, ["white", "comment"]], // Comments
            [/(\s+)(".*")/, ["white", "string"]], // Double-quoted string
            [/(\s+)('.*')/, ["white", "string"]], // Single-quoted string
            [/([>|])([0-9]*)(\+|-)?\s*$/, "string.multiline.start"], // Multiline string start
            [/([a-zA-Z_][\w-]*:)/, "keyword"], // Key
            [/([ \t]+)([\w$.-]+)/, ["white", "string"]], // Values
            [/&[a-zA-Z0-9_]+/, "annotation"], // Anchors
            [/\*[a-zA-Z0-9_]+/, "annotation"], // Aliases
            [/![a-zA-Z0-9_]+/, "type"], // Type tags
            [/true|false|null/, "constant.language"], // Boolean and null values
            [/\d+/, "number"], // Numbers
            [/(\s+)#.*$/, "comment"], // Comments
          ],
        },
      })
    }

    // Set the language for the current model if it's YAML
    const model = editor.getModel()
    if (model && model.getLanguageId() === "yaml") {
      monaco.editor.setModelLanguage(model, "yaml")
    }

    return () => {
      // Cleanup if needed
    }
  }, [monaco, editor])

  return null
}
