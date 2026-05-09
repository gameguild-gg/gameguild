// Headless IDE — canvas output only.
//
// The SDL canvas is shown; the editor, terminal, and file explorer are
// all hidden. Pass `api` to feed a pre-booted EmceptionAPI so the canvas
// starts rendering as soon as the WASM toolchain is ready.
//
// Use case: interactive demos/simulations where the code itself is not
// meant to be editable by the viewer.
//
// Drop into any React 19 app that has @emception/ide + @emception/browser
// installed.

'use client';

import { createEmception } from '@emception/browser';
import { Ide } from '@emception/ide';
import type { EmceptionAPI } from 'emception';
import { useEffect, useRef } from 'react';

const SDL_SOURCE = `#include <SDL2/SDL.h>
int main() {
  SDL_Init(SDL_INIT_VIDEO);
  SDL_Window* win = SDL_CreateWindow("demo", 0, 0, 320, 240, 0);
  SDL_Renderer* ren = SDL_CreateRenderer(win, -1, 0);
  for (int i = 0; i < 60; i++) {
    SDL_SetRenderDrawColor(ren, i * 4, 0, 255 - i * 4, 255);
    SDL_RenderClear(ren);
    SDL_RenderPresent(ren);
    SDL_Delay(16);
  }
  SDL_DestroyRenderer(ren);
  SDL_DestroyWindow(win);
  SDL_Quit();
  return 0;
}`;

export function Demo() {
    const apiRef = useRef<EmceptionAPI | null>(null);

    useEffect(() => {
        const ctrl = new AbortController();
        createEmception({ manifestUrl: '/cdn/manifest.json', tty: 'none', signal: ctrl.signal })
            .then((api) => {
                apiRef.current = api;
            })
            .catch(() => { });
        return () => ctrl.abort();
    }, []);

    return (
        <Ide
            workspaceName="headless-demo"
            api={apiRef.current}
            defaultFiles={{ 'main.cpp': { content: SDL_SOURCE, visibility: 'public' } }}
            enableFileExplorer={false}
            enableTabs={false}
            enableTerminal={false}
            enableCanvas={true}
            canvasPath="/user/sdl-canvas"
        />
    );
}

// --- standalone mount (optional) ----------------------------------------
import { createRoot } from 'react-dom/client';

const root = document.getElementById('root');
if (root) {
    createRoot(root).render(<Demo />);
}
