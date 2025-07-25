'use client';

import { useEffect } from 'react';
import { useLexicalComposerContext } from '@lexical/react/LexicalComposerContext';
import { $insertNodes } from 'lexical';
import { INSERT_SOURCE_CODE_COMMAND } from './floating-content-insert-plugin';
import { $createSourceCodeNode } from '../nodes/source-code-node';

export function SourceCodePlugin() {
  const [editor] = useLexicalComposerContext();

  useEffect(() => {
    if (!editor) return;

    return editor.registerCommand(
      INSERT_SOURCE_CODE_COMMAND,
      () => {
        editor.update(() => {
          const sourceCodeNode = $createSourceCodeNode();
          $insertNodes([sourceCodeNode]);
        });
        return true;
      },
      1,
    );
  }, [editor]);

  return null;
}
