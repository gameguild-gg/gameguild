'use client';

import { useEffect, useState } from 'react';
import type { editor } from 'monaco-editor';
import { YAMLSyntaxHighlighter } from './yaml-syntax-highlighter';
import { YAMLCompletionProvider } from './yaml-completion-provider';

interface YAMLLanguageServiceProps {
  monaco: typeof import('monaco-editor') | null;
  editor: editor.IStandaloneCodeEditor | null;
  code: string;
  enabled: boolean;
}

export function YAMLLanguageService({ monaco, editor, code, enabled }: YAMLLanguageServiceProps) {
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
      <YAMLSyntaxHighlighter monaco={monacoInstance} editor={editorInstance} />
      <YAMLCompletionProvider monaco={monacoInstance} editor={editorInstance} />
    </>
  );
}
