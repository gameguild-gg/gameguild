// Single-file IDE variant — no workspace, no tabs, no file explorer.
//
// Best for: inline code playgrounds, blog embeds, quiz questions.
// The IDE renders only the editor + terminal. File state is NOT
// persisted to localStorage (enableWorkspace={false}).
//
// Drop into any React 19 app that has @emception/ide installed.
// See packages/ide/README.md for full IdeProps reference.

'use client';

import { Ide } from '@emception/ide';

const STARTER_SOURCE = `#include <stdio.h>
int main() {
  printf("hello, world\\n");
  return 0;
}
`;

export function Demo() {
    return (
        <Ide
            workspaceName="single-file-demo"
            enableFileExplorer={false}
            enableTabs={false}
            enableWorkspace={false}
            defaultFiles={{
                'main.cpp': { content: STARTER_SOURCE, visibility: 'public' },
            }}
        />
    );
}

// --- standalone mount (optional) ----------------------------------------
import { createRoot } from 'react-dom/client';

const root = document.getElementById('root');
if (root) {
    createRoot(root).render(<Demo />);
}
