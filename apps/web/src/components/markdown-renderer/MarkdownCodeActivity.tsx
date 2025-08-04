import React, { useState } from 'react';
import { Button } from '@/components/ui/button';
import { Card } from '@/components/ui/card';
import Editor, { OnMount } from '@monaco-editor/react';
import { Play } from 'lucide-react';
import confetti from 'canvas-confetti';

export type CodeLanguage = 'c' | 'cpp' | 'python' | 'javascript' | 'typescript' | 'rust' | 'c#' | 'lua';

export function triggerConfetti() {
  const duration = 1000; // 1 second
  const animationEnd = Date.now() + duration;
  const defaults = { startVelocity: 30, spread: 360, ticks: 60, zIndex: 0 };

  function randomInRange(min: number, max: number) {
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
  expectedOutput: string;
  stdin: string;
  height?: number;
}

export function MarkdownCodeActivity(params: MarkdownCodeActivityProps) {
  const [stdErr, setStdErr] = useState<string>('');
  const [stdOut, setStdOut] = useState<string>('');
  const [code, setCode] = useState<string>(params.code);
  const [isCorrect, setIsCorrect] = useState<boolean | null>(null);
  const [isRunning, setIsRunning] = useState(false);

  const onEditorDidMount: OnMount = (editor) => {
    const updateHeight = () => {
      const contentHeight = editor.getContentHeight();
      const currentWidth = editor.getLayoutInfo().width;

      editor.getContentHeight();
      editor.layout({ width: currentWidth, height: contentHeight });
    };
    editor.onDidContentSizeChange(updateHeight);

    updateHeight();
  };

  const handleRunCode = async () => {
    setIsRunning(true);
    setStdErr('');
    setStdOut('');
    
    // Simulate code execution for demo purposes
    setTimeout(() => {
      // This is a simplified version - in a real implementation, you'd have actual code execution
      const mockOutput = `Running ${params.language} code...
Output: Hello, World!
Execution completed.`;
      
      setStdOut(mockOutput);
      setIsRunning(false);
      
      // For demo purposes, always show as correct
      setIsCorrect(true);
      triggerConfetti();
    }, 2000);
  };

  return (
    <>
      <Card className="container flex flex-auto flex-col p-4 gap-4 shadow-lg border border-gray-300">
        <p className="text-lg font-bold">{params.description}</p>
        <Card className="bg-[#1e1e1e] text-white p-4 font-mono text-sm">
          <Editor
            defaultLanguage={params.language}
            theme="vs-dark"
            value={code}
            onChange={(value) => setCode(value || '')}
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

        {/* Área de saída */}
        {!isCorrect && (
          <>
            <Card className="bg-[#2d2d2d] text-white p-4 min-h-fit font-mono">
              {isRunning && <p>Running {params.language} code...</p>}
              {!isRunning && stdOut && (
                <>
                  <p className="text-green-100">Output:</p>
                  <p className="text-green-400">{stdOut}</p>
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

        {/* Botões */}
        {!isCorrect && (
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