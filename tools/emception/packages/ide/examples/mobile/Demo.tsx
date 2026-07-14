// Mobile-friendly IDE variant.
//
// Disables panels that don't work well on small screens: the file-explorer
// sidebar and the docking chrome. Enables the terminal and limits the editor
// height via `style` so the keyboard doesn't cover the terminal.
//
// Drop into any React 19 + Next.js / Vite / CRA app that has
// @emception/ide installed.
// See packages/ide/README.md for full IdeProps reference.

'use client';

import { Ide } from '@emception/ide';

const STARTER_SOURCE = `#include <stdio.h>
int main() {
  printf("hello from mobile\\n");
  return 0;
}
`;

export function Demo() {
    return (
        <div style={{ height: '100dvh', display: 'flex', flexDirection: 'column' }}>
            <Ide
                workspaceName="mobile-demo"
                defaultFiles={{
                    'main.cpp': { content: STARTER_SOURCE, visibility: 'public' },
                }}
                // Sidebar + docking chrome take too much horizontal space on phones.
                enableFileExplorer={false}
                // Keep the terminal so users can see compiler errors.
                enableTerminal
                style={{
                    flex: 1,
                    minHeight: 0,
                    // Let the editor breathe above a software keyboard (~40 % viewport).
                    maxHeight: '60dvh',
                }}
            />
        </div>
    );
}

// --- standalone mount (optional) ----------------------------------------
import { createRoot } from 'react-dom/client';

const root = document.getElementById('root');
if (root) {
    createRoot(root).render(<Demo />);
}
