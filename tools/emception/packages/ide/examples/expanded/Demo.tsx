// Fullscreen / expanded IDE example.
//
// When `fullscreen` is true the IDE renders into a React portal attached
// to `document.body`, covers the full viewport, and calls
// `onFullscreenChange` when the user presses Escape or the built-in
// close button.
//
// Drop into any React 19 app that has @emception/ide installed.
// See packages/ide/README.md for full IdeProps reference.

'use client';

import { Ide } from '@emception/ide';
import { useState } from 'react';

const STARTER_SOURCE = `#include <stdio.h>
int main() {
  printf("hello from fullscreen IDE\\n");
  return 0;
}
`;

export function Demo() {
    const [expanded, setExpanded] = useState(false);

    return (
        <>
            <button type="button" onClick={() => setExpanded(true)}>
                Open fullscreen IDE
            </button>

            {expanded && (
                <Ide
                    workspaceName="expanded-demo"
                    fullscreen
                    onFullscreenChange={(isFullscreen) => {
                        if (!isFullscreen) setExpanded(false);
                    }}
                    defaultFiles={{
                        'main.cpp': { content: STARTER_SOURCE, visibility: 'public' },
                    }}
                />
            )}
        </>
    );
}

// --- standalone mount (optional) ----------------------------------------
import { createRoot } from 'react-dom/client';

const root = document.getElementById('root');
if (root) {
    createRoot(root).render(<Demo />);
}
