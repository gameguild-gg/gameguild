// Tabs-only IDE variant — shows the tab bar but hides the file-explorer
// sidebar and the docking UI. A good middle ground between the full IDE
// and the single-file view for assignments with 2-4 files.
//
// Drop into any React 19 app that has @emception/ide installed.
// See packages/ide/README.md for full IdeProps reference.

'use client';

import { Ide } from '@emception/ide';

const FILES = {
    'main.cpp': {
        content: `#include "greet.h"
int main() { greet("world"); return 0; }
`,
        visibility: 'public' as const,
    },
    'greet.h': {
        content: `#pragma once
void greet(const char* who);
`,
        visibility: 'public' as const,
    },
    'greet.cpp': {
        content: `#include <stdio.h>
#include "greet.h"
void greet(const char* who) { printf("hello, %s\\n", who); }
`,
        visibility: 'public' as const,
    },
};

export function Demo() {
    return (
        <Ide
            workspaceName="tabs-only-demo"
            defaultFiles={FILES}
            enableFileExplorer={false}
        />
    );
}

// --- standalone mount (optional) ----------------------------------------
import { createRoot } from 'react-dom/client';

const root = document.getElementById('root');
if (root) {
    createRoot(root).render(<Demo />);
}
