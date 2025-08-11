'use client';

import { useEffect } from 'react';
import { useLexicalComposerContext } from '@lexical/react/LexicalComposerContext';
import { $insertNodes } from 'lexical';
import { INSERT_GALLERY_COMMAND } from './floating-content-insert-plugin';
import { $createGalleryNode } from '../nodes/gallery-node';

export function GalleryPlugin() {
  const [editor] = useLexicalComposerContext();

  useEffect(() => {
    if (!editor) return;

    return editor.registerCommand(
      INSERT_GALLERY_COMMAND,
      () => {
        editor.update(() => {
          const galleryNode = $createGalleryNode();
          $insertNodes([galleryNode]);
        });
        return true;
      },
      1,
    );
  }, [editor]);

  return null;
}
