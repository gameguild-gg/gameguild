import { Button } from '@/components/ui/button';
import { Card } from '@/components/ui/card';
import Editor, { OnMount } from '@monaco-editor/react';
import confetti from 'canvas-confetti';
import { Play } from 'lucide-react';
import React, { useState } from 'react';

export type CodeLanguage = 'c' | 'cpp' | 'python' | 'javascript' | 'typescript' | 'rust' | 'c#' | 'lua' | 'sql';

export function triggerConfetti(): void {
  const duration = 1000; // 1 second
  const animationEnd = Date.now() + duration;
  const defaults = { startVelocity: 30, spread: 360, ticks: 60, zIndex: 0 };

  function randomInRange(min: number, max: number): number {
    return Math.random() * (max - min) + min;
  }

  const interval = setInterval(() => {
    const timeLeft = animationEnd - Date.now();

    if (timeLeft <= 0) {
      return clearInterval(interval);
    }

    const particleCount = 50 * (timeLeft / duration);

    confetti({
      ...defaults,
      particleCount,
      origin: { x: randomInRange(0.1, 0.3), y: Math.random() - 0.2 },
    });
    confetti({
      ...defaults,
      particleCount,
      origin: { x: randomInRange(0.7, 0.9), y: Math.random() - 0.2 },
    });
  }, 250);
}

export interface MarkdownCodeActivityProps {
  code: string;
  description: string;
  language: CodeLanguage;
  expectedOutput?: string;
  stdin?: string;
  height?: number;
}

export function MarkdownCodeActivity(params: MarkdownCodeActivityProps): React.ReactElement {
  const [stdErr, setStdErr] = useState<string>('');
  const [stdOut, setStdOut] = useState<string>('');
  const [code, setCode] = useState<string>(params.code);
  const [isCorrect, setIsCorrect] = useState<boolean | null>(null);
  const [isRunning, setIsRunning] = useState(false);


  const onEditorDidMount: OnMount = (editor) => {
    const updateHeight = (): void => {
      const contentHeight = editor.getContentHeight();
      const currentWidth = editor.getLayoutInfo().width;

      editor.getContentHeight();
      editor.layout({ width: currentWidth, height: contentHeight });
    };
    editor.onDidContentSizeChange(updateHeight);

    updateHeight();
  };

  const handleRunCode = async (): Promise<void> => {
    setIsRunning(true);
    setStdErr('');
    setStdOut('');
    setIsCorrect(null);


    try {
      let actualOutput = '';

      if (params.language === 'python' && code.includes('print(')) {
        const printMatch = code.match(/print\(['"]([^'"]*)['"]\)/);
        if (printMatch !== null && typeof printMatch[1] === 'string') {
          actualOutput = printMatch[1];
        }
      } else if (params.language === 'sql') {
        const { default: initSqlJs } = await import('sql.js');
        const SQL = await initSqlJs({ locateFile: (file: string) => `https://sql.js.org/dist/${file}` });
        const db = new SQL.Database();

        // Run setup statements from stdin first (e.g., CREATE TABLE, INSERT)
        if (params.stdin !== undefined && params.stdin.length > 0) {
          db.exec(params.stdin);
        }

        // Execute the user's code and capture results
        const results = db.exec(code);

        // Format all result sets
        const outputParts: string[] = [];
        for (const result of results) {
          if (result.values.length > 0) {
            const rows = result.values.map((row: Array<string | number | null>) =>
              row
                .map((cell: string | number | null) => {
                  if (cell === null || cell === undefined) return 'NULL';
                  if (typeof cell === 'number') return cell.toString();
                  return String(cell);
                })
                .join(' | '),
            );
            outputParts.push(rows.join('\n'));
          }
        }
        actualOutput = outputParts.join('\n');

        // If no results but query executed successfully, show a message for DDL statements
        if (actualOutput === '' && results.length === 0) {
          // Check if it's a SELECT that returned no rows vs DDL statement
          const upperCode = code.toUpperCase().trim();
          if (upperCode.startsWith('SELECT')) {
            actualOutput = '(no rows returned)';
          } else {
            actualOutput = 'Query executed successfully';
          }
        }

        db.close();
      }

      setStdOut(actualOutput);

      // Only validate if expectedOutput is provided and non-empty
      const expectedOutput = params.expectedOutput ?? '';
      const hasExpectedOutput = expectedOutput.trim().length > 0;
      if (hasExpectedOutput) {
        const isOutputCorrect = actualOutput.trim() === expectedOutput.trim();
        setIsCorrect(isOutputCorrect);

        if (isOutputCorrect) {
          triggerConfetti();
        }
      } else {
        // No expected output - just display result without validation
      }
    } catch (error) {
      const message = error instanceof Error ? error.message : 'Unknown error executing code';
      console.error('Code activity error:', message, error);
      setStdErr(message);
      setIsCorrect(false);
    } finally {
      setIsRunning(false);
    }
  };

  return (
    <>
      <Card className="w-full flex flex-auto flex-col p-4 gap-4 shadow-lg border border-gray-300">
        <p className="text-lg font-bold">{params.description}</p>
        <Card className="bg-[#1e1e1e] text-white p-4 font-mono text-sm">
          <Editor
            defaultLanguage={params.language}
            theme="vs-dark"
            value={code}
            onChange={(value) => setCode(value ?? '')}
            height={'100%'}
            width={'100%'}
            onMount={onEditorDidMount}
            options={{
              minimap: { enabled: false },
              fontSize: 14,
              lineNumbers: 'on',
              readOnly: isCorrect === true,
              domReadOnly: false,
              padding: { top: 0, bottom: 0 },
              scrollBeyondLastLine: false,
              automaticLayout: true,
            }}
          />
        </Card>

        {isCorrect === true && (
          <div className="p-3 rounded-md bg-green-100 border border-green-300 text-green-800 font-semibold text-center">Correct output!</div>
        )}

        {isCorrect === false && (
          <div className="p-3 rounded-md bg-red-100 border border-red-300 text-red-800">
            <p className="font-semibold mb-2">Incorrect output!</p>
            <div className="space-y-2">
              <div>
                <p className="text-sm font-medium">Expected:</p>
                <p className="font-mono bg-red-50 p-2 rounded">{params.expectedOutput}</p>
              </div>
              <div>
                <p className="text-sm font-medium">Your Output:</p>
                <p className="font-mono bg-red-50 p-2 rounded">{stdOut}</p>
              </div>
            </div>
          </div>
        )}

        {/* Output area - always show when no expected output, or when not yet validated */}
        {(isCorrect === null || (params.expectedOutput === undefined || params.expectedOutput.trim().length === 0)) && (
          <>
            <Card className="bg-[#2d2d2d] text-white p-4 min-h-fit font-mono">
              {isRunning && <p>Running {params.language} code...</p>}
              {!isRunning && stdOut && (
                <>
                  <p className="text-green-100">Output:</p>
                  <p className="text-green-400 whitespace-pre-wrap">{stdOut}</p>
                </>
              )}
              {stdErr &&
                stdErr.split('\n').map((line, index) => (
                  <p key={index} className="text-red-400">
                    {line}
                  </p>
                ))}
            </Card>
          </>
        )}

        {/* Botões - always show when no expected output (sandbox mode), or when not yet correct */}
        {(isCorrect !== true || params.expectedOutput === undefined || params.expectedOutput.trim().length === 0) && (
          <div className="flex justify-between">
            <Button
              variant="secondary"
              className="bg-[#2d2d2d] text-white hover:bg-[#3d3d3d]"
              onClick={handleRunCode}
              disabled={isRunning}
            >
              <Play className="w-4 h-4 mr-2" />
              {isRunning ? 'Running...' : 'Run'}
            </Button>
          </div>
        )}
      </Card>
    </>
  );
}