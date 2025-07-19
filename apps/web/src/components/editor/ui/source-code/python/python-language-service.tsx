'use client';

import { useState, useEffect } from 'react';
import type { editor } from 'monaco-editor';
import { PythonSyntaxHighlighter } from './python-syntax-highlighter';
import { PythonTypeChecker } from './python-type-checker';
import { PythonCompletionProvider } from './python-completion-provider';

interface PythonLanguageServiceProps {
  monaco: typeof import('monaco-editor') | null;
  editor: editor.IStandaloneCodeEditor | null;
  code: string;
  enabled: boolean;
}

export function PythonLanguageService({ monaco, editor, code, enabled }: PythonLanguageServiceProps) {
  const [editorInstance, setEditorInstance] = useState<editor.IStandaloneCodeEditor | null>(null);
  const [monacoInstance, setMonacoInstance] = useState<typeof import('monaco-editor') | null>(null);

  useEffect(() => {
    if (monaco && editor && enabled) {
      setEditorInstance(editor);
      setMonacoInstance(monaco);
    }
  }, [monaco, editor, enabled]);

  if (!enabled || !editorInstance || !monacoInstance) {
    return null;
  }

  return (
    <>
      <PythonSyntaxHighlighter monaco={monacoInstance} editor={editorInstance} />
      <PythonTypeChecker monaco={monacoInstance} editor={editorInstance} code={code} />
      <PythonCompletionProvider monaco={monacoInstance} editor={editorInstance} />
    </>
  );
}
