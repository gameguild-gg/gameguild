'use client';

import { useEffect } from 'react';
import type { editor } from 'monaco-editor';

interface XMLCompletionProviderProps {
  monaco: typeof import('monaco-editor') | null;
  editor: editor.IStandaloneCodeEditor | null;
}

export function XMLCompletionProvider({ monaco, editor }: XMLCompletionProviderProps) {
  useEffect(() => {
    if (!monaco || !editor) return;

    // Register XML completion provider
    const disposable = monaco.languages.registerCompletionItemProvider('xml', {
      provideCompletionItems: () => {
        const suggestions = [
          {
            label: 'XML-tag',
            kind: monaco.languages.CompletionItemKind.Snippet,
            insertText: '<${1:tag}>$0</${1:tag}>',
            insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
            range: null,
          },
        ];
        return { suggestions: suggestions };
      },
    });

    return () => {
      disposable.dispose();
    };
  }, [monaco, editor]);

  return null;
}
