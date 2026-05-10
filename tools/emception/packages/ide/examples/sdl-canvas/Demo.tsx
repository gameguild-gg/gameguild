// SDL canvas demo via @gameguild/emception-ide.
//
// Shows the full IDE — editor, terminal, and the SDL2 canvas — with a
// starter C program that draws an animated gradient.
//
// The canvas is rendered to a virtual path (/user/sdl-canvas by default).
// Requires COOP/COEP headers — use the COI service worker from
// the bundled `emception/cdn/*` payload in production.
//
// Drop into any React 19 app that has @gameguild/emception-ide installed.
// See packages/ide/README.md for full IdeProps reference.

'use client';

import { Ide } from '@gameguild/emception-ide';

const SDL_SOURCE = `#include <SDL2/SDL.h>
#include <math.h>

int main() {
  if (SDL_Init(SDL_INIT_VIDEO) != 0) return 1;

  SDL_Window*   win = SDL_CreateWindow("gradient", 0, 0, 400, 300, 0);
  SDL_Renderer* ren = SDL_CreateRenderer(win, -1, SDL_RENDERER_ACCELERATED);

  for (int frame = 0; ; frame++) {
    SDL_Event e;
    while (SDL_PollEvent(&e))
      if (e.type == SDL_QUIT) goto done;

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

done:
  SDL_DestroyRenderer(ren);
  SDL_DestroyWindow(win);
  SDL_Quit();
  return 0;
}
`;

export function Demo() {
  return (
    <Ide
      workspaceName="sdl-canvas-demo"
      defaultFiles={{
        'main.cpp': { content: SDL_SOURCE, visibility: 'public' },
      }}
      enableCanvas
      canvasPath="/user/sdl-canvas"
      // Build flags required for SDL2 in Emscripten.
      workspaceConfig={{
        build: {
          std: 'c++17',
          cflags: ['-sUSE_SDL=2', '-sMIN_WEBGL_VERSION=2'],
          sources: ['main.cpp'],
          output: 'a.out',
        },
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
