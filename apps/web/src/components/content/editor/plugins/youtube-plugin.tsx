'use client';

import { useEffect } from 'react';
import { useLexicalComposerContext } from '@lexical/react/LexicalComposerContext';
import { $insertNodes } from 'lexical';
import { $createYouTubeNode, type YouTubeData } from '../nodes/youtube-node';
import { INSERT_YOUTUBE_COMMAND } from './floating-content-insert-plugin';

export function YouTubePlugin() {
  const [editor] = useLexicalComposerContext();

  useEffect(() => {
    if (!editor) return;

    return editor.registerCommand(
      INSERT_YOUTUBE_COMMAND,
      (payload: YouTubeData) => {
        editor.update(() => {
          const youtubeNode = $createYouTubeNode({
            ...payload,
            isNew: true, // This flag will be used to show the size control automatically
          });
          $insertNodes([youtubeNode]);
        });
        return true;
      },
      1,
    );
  }, [editor]);

  return null;
}
