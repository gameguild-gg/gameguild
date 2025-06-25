"use client"

import { useEffect, useState } from "react"
import type { editor } from "monaco-editor"

interface LuaTypeCheckerProps {
  monaco: typeof import("monaco-editor") | null
  editor: editor.IStandaloneCodeEditor | null
  code: string
}

interface TypeError {
  line: number
  column: number
  message: string
  severity: "error" | "warning" | "info"
}

export function LuaTypeChecker({ monaco, editor, code }: LuaTypeCheckerProps) {
  const [errors, setErrors] = useState<TypeError[]>([])

  useEffect(() => {
    if (!monaco || !editor) return

    // Create a marker collection for Lua type errors
    const model = editor.getModel()
    if (!model) return

    // Simple Lua type checking
    const luaTypeCheck = (code: string): TypeError[] => {
      const errors: TypeError[] = []
      const lines = code.split("\n")

      // Check for type annotations in Lua comments
      lines.forEach((line, lineIndex) => {
        // Check for type annotations in comments (LuaLS style)
        // Example: ---@type string or --@type number
        const typeAnnotationMatch = line.match(/---?@type\s+(\w+)/)
        if (typeAnnotationMatch) {
          const typeName = typeAnnotationMatch[1]

          // Check for valid Lua types
          const validTypes = ["string", "number", "boolean", "table", "function", "thread", "userdata", "nil", "any"]
          if (!validTypes.includes(typeName.toLowerCase())) {
            errors.push({
              line: lineIndex + 1,
              column: line.indexOf(typeName),
              message: `Unknown type annotation: ${typeName}`,
              severity: "warning",
            })
          }

          // Check for value assignment with potential type mismatch
          const nextLine = lines[lineIndex + 1]
          if (nextLine && nextLine.includes("=")) {
            const varMatch = nextLine.match(/\s*(\w+)\s*=\s*(.+)/)
            if (varMatch) {
              const varName = varMatch[1]
              const value = varMatch[2].trim()

              // Simple type checking based on literal values
              if (
                typeName.toLowerCase() === "number" &&
                !/^-?\d+(\.\d+)?$/.test(value) &&
                !value.startsWith("tonumber(")
              ) {
                errors.push({
                  line: lineIndex + 2,
                  column: nextLine.indexOf(value),
                  message: `Type mismatch: expected number, got ${value}`,
                  severity: "warning",
                })
              } else if (
                typeName.toLowerCase() === "string" &&
                !value.startsWith('"') &&
                !value.startsWith("'") &&
                !value.startsWith("[[") &&
                !value.startsWith("tostring(")
              ) {
                errors.push({
                  line: lineIndex + 2,
                  column: nextLine.indexOf(value),
                  message: `Type mismatch: expected string, got ${value}`,
                  severity: "warning",
                })
              } else if (typeName.toLowerCase() === "boolean" && value !== "true" && value !== "false") {
                errors.push({
                  line: lineIndex + 2,
                  column: nextLine.indexOf(value),
                  message: `Type mismatch: expected boolean, got ${value}`,
                  severity: "warning",
                })
              } else if (typeName.toLowerCase() === "table" && !value.startsWith("{") && value !== "{}") {
                errors.push({
                  line: lineIndex + 2,
                  column: nextLine.indexOf(value),
                  message: `Type mismatch: expected table, got ${value}`,
                  severity: "warning",
                })
              }
            }
          }
        }

        // Check for common Lua errors
        // Missing 'then' in if statement
        if (line.match(/^\s*if\s+.+\s*$/)) {
          if (!line.includes("then")) {
            errors.push({
              line: lineIndex + 1,
              column: line.indexOf("if"),
              message: "Missing 'then' in if statement",
              severity: "error",
            })
          }
        }

        // Missing 'do' in for/while loops
        if (line.match(/^\s*for\s+.+\s*$/)) {
          if (!line.includes("do")) {
            errors.push({
              line: lineIndex + 1,
              column: line.indexOf("for"),
              message: "Missing 'do' in for loop",
              severity: "error",
            })
          }
        }

        if (line.match(/^\s*while\s+.+\s*$/)) {
          if (!line.includes("do")) {
            errors.push({
              line: lineIndex + 1,
              column: line.indexOf("while"),
              message: "Missing 'do' in while loop",
              severity: "error",
            })
          }
        }

        // Check for unclosed blocks
        if (line.includes("function") && !line.includes("end") && !line.match(/function\s*$$[^)]*$$\s*$/)) {
          // This is a simple check and might have false positives
          const nextFewLines = lines.slice(lineIndex + 1, lineIndex + 5).join("\n")
          if (!nextFewLines.includes("end")) {
            errors.push({
              line: lineIndex + 1,
              column: line.indexOf("function"),
              message: "Function block might be missing 'end'",
              severity: "warning",
            })
          }
        }
      })

      return errors
    }

    // Run type checking
    const typeErrors = luaTypeCheck(code)
    setErrors(typeErrors)

    // Set markers in the editor
    const markers = typeErrors.map((error) => ({
      startLineNumber: error.line,
      startColumn: error.column,
      endLineNumber: error.line,
      endColumn: error.column + 1,
      message: error.message,
      severity:
        error.severity === "error"
          ? monaco.MarkerSeverity.Error
          : error.severity === "warning"
            ? monaco.MarkerSeverity.Warning
            : monaco.MarkerSeverity.Info,
    }))

    monaco.editor.setModelMarkers(model, "lua-type-checker", markers)

    return () => {
      // Clear markers when component unmounts
      if (model) {
        monaco.editor.setModelMarkers(model, "lua-type-checker", [])
      }
    }
  }, [monaco, editor, code])

  return null
}
