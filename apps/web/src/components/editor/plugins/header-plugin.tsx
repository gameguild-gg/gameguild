'use client';

import { useEffect } from 'react';
import { useLexicalComposerContext } from '@lexical/react/LexicalComposerContext';
import { $insertNodes } from 'lexical';
import { INSERT_HEADER_COMMAND } from './floating-content-insert-plugin';
import { $createHeaderNode } from '../nodes/header-node';
import type { HeaderData } from '../nodes/header-node';

export function HeaderPlugin() {
  const [editor] = useLexicalComposerContext();

  useEffect(() => {
    if (!editor) return;

    return editor.registerCommand(
      INSERT_HEADER_COMMAND,
      (payload: HeaderData) => {
        editor.update(() => {
          const headerNode = $createHeaderNode(payload);
          $insertNodes([headerNode]);
        });
        return true;
      },
      1,
    );
  }, [editor]);

  return null;
}
