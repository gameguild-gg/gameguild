'use client';

import { useEffect } from 'react';
import { useLexicalComposerContext } from '@lexical/react/LexicalComposerContext';
import { $insertNodes } from 'lexical';
import { INSERT_DIVIDER_COMMAND } from './floating-content-insert-plugin';
import { $createDividerNode } from '../nodes/divider-node';
import type { DividerData } from '../nodes/divider-node';

export function DividerPlugin() {
  const [editor] = useLexicalComposerContext();

  useEffect(() => {
    if (!editor) return;

    return editor.registerCommand(
      INSERT_DIVIDER_COMMAND,
      (payload: Partial<DividerData> = {}) => {
        editor.update(() => {
          const dividerNode = $createDividerNode(payload);
          $insertNodes([dividerNode]);
        });
        return true;
      },
      1,
    );
  }, [editor]);

  return null;
}
