'use client';

import { useState, useEffect } from 'react';
import type { editor } from 'monaco-editor';
import { CSyntaxHighlighter } from './c-syntax-highlighter';
import { CTypeChecker } from './c-type-checker';
import { CCompletionProvider } from './c-completion-provider';

interface CLanguageServiceProps {
  monaco: typeof import('monaco-editor') | null;
  editor: editor.IStandaloneCodeEditor | null;
  code: string;
  enabled: boolean;
}

export function CLanguageService({ monaco, editor, code, enabled }: CLanguageServiceProps) {
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
      <CSyntaxHighlighter monaco={monacoInstance} editor={editorInstance} />
      <CTypeChecker monaco={monacoInstance} editor={editorInstance} code={code} />
      <CCompletionProvider monaco={monacoInstance} editor={editorInstance} />
    </>
  );
}
