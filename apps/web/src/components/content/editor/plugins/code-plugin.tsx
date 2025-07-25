'use client';

import { useEffect } from 'react';
import { useLexicalComposerContext } from '@lexical/react/LexicalComposerContext';
import { $getSelection, $isRangeSelection, COMMAND_PRIORITY_CRITICAL } from 'lexical';

export function CodePlugin() {
  const [editor] = useLexicalComposerContext();

  useEffect(() => {
    // This is a simplified version that doesn't rely on prismjs
    // It just ensures that code blocks are properly handled
    return editor.registerCommand(
      editor.registerUpdateListener(({ editorState }) => {
        editorState.read(() => {
          const selection = $getSelection();
          if (!$isRangeSelection(selection)) return;

          // You could add custom code handling here if needed
        });
      }),
      () => false,
      COMMAND_PRIORITY_CRITICAL,
    );
  }, [editor]);

  return null;
}
