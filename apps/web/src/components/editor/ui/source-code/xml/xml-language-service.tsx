'use client';

import { useEffect, useState } from 'react';
import type { editor } from 'monaco-editor';
import { XMLSyntaxHighlighter } from './xml-syntax-highlighter';
import { XMLCompletionProvider } from './xml-completion-provider';

interface XMLLanguageServiceProps {
  monaco: typeof import('monaco-editor') | null;
  editor: editor.IStandaloneCodeEditor | null;
  code: string;
  enabled: boolean;
}

export function XMLLanguageService({ monaco, editor, code, enabled }: XMLLanguageServiceProps) {
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
      <XMLSyntaxHighlighter monaco={monacoInstance} editor={editorInstance} />
      <XMLCompletionProvider monaco={monacoInstance} editor={editorInstance} />
    </>
  );
}
