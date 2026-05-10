// SDL canvas demo via @gameguild/emception-react.
//
// Uses <EmceptionRun> to compile and run an SDL2 gradient animation.
// The canvas output appears inside the <emception-run> shadow DOM.
// Requires COOP/COEP headers — use the COI service worker from
// the bundled `emception/cdn/*` payload in production.
//
// Drop into any React 19 app that has @gameguild/emception-react,
// @gameguild/emception-browser, and @gameguild/emception-webcomponent installed.

'use client';

import { createEmception } from '@gameguild/emception-browser';
import { EmceptionRun, useEmception } from '@gameguild/emception-react';
import '@gameguild/emception-webcomponent';
import { useCallback } from 'react';

const SDL_SOURCE = `#include <SDL2/SDL.h>
#include <math.h>

int main() {
  SDL_Init(SDL_INIT_VIDEO);
  SDL_Window*   win = SDL_CreateWindow("gradient", 0, 0, 400, 300, 0);
  SDL_Renderer* ren = SDL_CreateRenderer(win, -1, 0);

  for (int frame = 0; frame < 300; frame++) {
    float t = frame * 0.02f;
    for (int y = 0; y < 300; y++) {
      Uint8 r = (Uint8)(128 + 127 * sinf(t + y * 0.03f));
      Uint8 b = (Uint8)(128 + 127 * cosf(t + y * 0.02f));
      SDL_SetRenderDrawColor(ren, r, 80, b, 255);
      SDL_RenderDrawLine(ren, 0, y, 400, y);
    }
    SDL_RenderPresent(ren);
    SDL_Delay(16);
  }

  SDL_DestroyRenderer(ren);
  SDL_DestroyWindow(win);
  SDL_Quit();
  return 0;
}`;

export function SdlCanvasDemo() {
    const create = useCallback(
        (signal: AbortSignal) =>
            createEmception({ manifestUrl: '/cdn/manifest.json', tty: 'none', signal }),
        [],
    );

    const { api, status, error } = useEmception({ create });

    if (status === 'loading') return <p>Loading toolchain…</p>;
    if (status === 'error') return <pre>Failed to boot: {String(error)}</pre>;

    return (
        <EmceptionRun
            api={api}
            source={SDL_SOURCE}
            // SDL2 preset selects the Emscripten SDL2 port + canvas element.
            preset="sdl2"
            autorun
            onExit={(p) => console.log('[exit]', p.code)}
            style={{ minHeight: '20rem' }}
        />
    );
}
