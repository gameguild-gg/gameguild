'use client';

import { useEffect, useState } from 'react';
import type { editor } from 'monaco-editor';

interface PythonTypeCheckerProps {
  monaco: typeof import('monaco-editor') | null;
  editor: editor.IStandaloneCodeEditor | null;
  code: string;
}

interface TypeError {
  line: number;
  column: number;
  message: string;
  severity: 'error' | 'warning' | 'info';
}

export function PythonTypeChecker({ monaco, editor, code }: PythonTypeCheckerProps) {
  const [errors, setErrors] = useState<TypeError[]>([]);

  useEffect(() => {
    if (!monaco || !editor) return;

    // Create a marker collection for Python type errors
    const model = editor.getModel();
    if (!model) return;

    // Simple Python type checking
    const pythonTypeCheck = (code: string): TypeError[] => {
      const errors: TypeError[] = [];
      const lines = code.split('\n');

      // Simple type annotation check
      lines.forEach((line, lineIndex) => {
        // Check for type annotations
        if (line.includes(':') && !line.trim().startsWith('#')) {
          // Check for variable type annotations (var: type = value)
          const typeAnnotationMatch = line.match(/\s*(\w+)\s*:\s*(\w+)/);
          if (typeAnnotationMatch) {
            const varName = typeAnnotationMatch[1];
            const typeName = typeAnnotationMatch[2];

            // Check for valid Python types
            const validTypes = ['int', 'float', 'str', 'bool', 'list', 'dict', 'tuple', 'set', 'None', 'Any'];
            if (!validTypes.includes(typeName) && !typeName.startsWith('List[') && !typeName.startsWith('Dict[')) {
              errors.push({
                line: lineIndex + 1,
                column: line.indexOf(typeName),
                message: `Unknown type annotation: ${typeName}`,
                severity: 'warning',
              });
            }

            // Check for value assignment with potential type mismatch
            if (line.includes('=')) {
              const valueMatch = line.match(/=\s*(.+)/);
              if (valueMatch) {
                const value = valueMatch[1].trim();

                // Simple type checking based on literal values
                if (typeName === 'int' && !/^-?\d+$/.test(value) && !value.startsWith('int(')) {
                  errors.push({
                    line: lineIndex + 1,
                    column: line.indexOf(value),
                    message: `Type mismatch: expected int, got ${value}`,
                    severity: 'error',
                  });
                } else if (typeName === 'float' && !/^-?\d+\.\d+$/.test(value) && !value.startsWith('float(')) {
                  errors.push({
                    line: lineIndex + 1,
                    column: line.indexOf(value),
                    message: `Type mismatch: expected float, got ${value}`,
                    severity: 'error',
                  });
                } else if (typeName === 'str' && !value.startsWith('"') && !value.startsWith("'") && !value.startsWith('str(')) {
                  errors.push({
                    line: lineIndex + 1,
                    column: line.indexOf(value),
                    message: `Type mismatch: expected str, got ${value}`,
                    severity: 'error',
                  });
                } else if (typeName === 'bool' && value !== 'True' && value !== 'False' && !value.startsWith('bool(')) {
                  errors.push({
                    line: lineIndex + 1,
                    column: line.indexOf(value),
                    message: `Type mismatch: expected bool, got ${value}`,
                    severity: 'error',
                  });
                }
              }
            }
          }

          // Check for function return type annotations
          const funcMatch = line.match(/def\s+(\w+)\s*$$.*$$\s*->\s*(\w+)/);
          if (funcMatch) {
            const funcName = funcMatch[1];
            const returnType = funcMatch[2];

            // Check for valid Python types
            const validTypes = ['int', 'float', 'str', 'bool', 'list', 'dict', 'tuple', 'set', 'None', 'Any'];
            if (!validTypes.includes(returnType) && !returnType.startsWith('List[') && !returnType.startsWith('Dict[')) {
              errors.push({
                line: lineIndex + 1,
                column: line.indexOf(returnType),
                message: `Unknown return type: ${returnType}`,
                severity: 'warning',
              });
            }
          }
        }

        // Check for indentation errors
        if (line.startsWith(' ') && !line.startsWith('  ') && !line.startsWith('    ')) {
          errors.push({
            line: lineIndex + 1,
            column: 0,
            message: 'Inconsistent indentation. Use 2 or 4 spaces.',
            severity: 'warning',
          });
        }
      });

      return errors;
    };

    // Run type checking
    const typeErrors = pythonTypeCheck(code);
    setErrors(typeErrors);

    // Set markers in the editor
    const markers = typeErrors.map((error) => ({
      startLineNumber: error.line,
      startColumn: error.column,
      endLineNumber: error.line,
      endColumn: error.column + 1,
      message: error.message,
      severity: error.severity === 'error' ? monaco.MarkerSeverity.Error : error.severity === 'warning' ? monaco.MarkerSeverity.Warning : monaco.MarkerSeverity.Info,
    }));

    monaco.editor.setModelMarkers(model, 'python-type-checker', markers);

    return () => {
      // Clear markers when component unmounts
      if (model) {
        monaco.editor.setModelMarkers(model, 'python-type-checker', []);
      }
    };
  }, [monaco, editor, code]);

  return null;
}
