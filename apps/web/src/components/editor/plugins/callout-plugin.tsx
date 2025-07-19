'use client';

import { useEffect } from 'react';
import { useLexicalComposerContext } from '@lexical/react/LexicalComposerContext';
import { $insertNodes } from 'lexical';
import { INSERT_CALLOUT_COMMAND } from './floating-content-insert-plugin';
import { $createCalloutNode } from '../nodes/callout-node';
import type { CalloutData } from '../nodes/callout-node';

export function CalloutPlugin() {
  const [editor] = useLexicalComposerContext();

  useEffect(() => {
    if (!editor) return;

    return editor.registerCommand(
      INSERT_CALLOUT_COMMAND,
      (payload: Partial<CalloutData> = {}) => {
        editor.update(() => {
          const calloutNode = $createCalloutNode(payload);
          $insertNodes([calloutNode]);
        });
        return true;
      },
      1,
    );
  }, [editor]);

  return null;
}
