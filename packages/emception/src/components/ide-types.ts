export type TabType = 'text' | 'image' | 'canvas';
export type DockGroup = 'main' | 'right' | 'bottom';

export const WORKSPACE_STORAGE_KEY = 'gameguild.emception.workspace.v1';

export interface WorkspaceFile {
  path: string;
  type: TabType;
  content: string;
}

export interface OpenTab {
  id: string;
  path: string;
  type: TabType;
  group: DockGroup;
}

export interface TerminalTab {
  id: string;
  title: string;
}

export interface TreeNode {
  name: string;
  path: string;
  isDir: boolean;
  children: TreeNode[];
}

export const TERMINAL_THEME = {
  background: '#181825',
  foreground: '#cdd6f4',
  cursor: '#f5e0dc',
  selectionBackground: '#585b70',
} as const;

export const DEFAULT_CODE = `#include <iostream>
#include "greetings.h"
#include <string>
int main() {
  std::string name;
  std::cout << "Enter your name: ";
  std::getline(std::cin, name);
  std::cout << greeting() << std::endl;
  std::cout << "Hello, " << name << "! Welcome to WebAssembly!" << std::endl;
  return 0;
}
`;

export const DEFAULT_HEADER = `#pragma once

inline const char* greeting() {
  return "Welcome to multi-file mode!";
}
`;

export const DEFAULT_IMAGE = `data:image/svg+xml;utf8,${encodeURIComponent(
  `<svg xmlns="http://www.w3.org/2000/svg" width="800" height="520" viewBox="0 0 800 520">
      <defs>
        <linearGradient id="g" x1="0" y1="0" x2="1" y2="1">
          <stop offset="0%" stop-color="#313244" />
          <stop offset="100%" stop-color="#181825" />
        </linearGradient>
      </defs>
      <rect width="800" height="520" fill="url(#g)"/>
      <circle cx="190" cy="150" r="72" fill="#89b4fa" opacity="0.75"/>
      <circle cx="610" cy="370" r="90" fill="#f38ba8" opacity="0.55"/>
      <text x="50%" y="45%" font-size="42" text-anchor="middle" fill="#cdd6f4" font-family="Inter, Segoe UI, Arial">GameGuild Workspace</text>
      <text x="50%" y="55%" font-size="22" text-anchor="middle" fill="#a6adc8" font-family="Inter, Segoe UI, Arial">Image tab preview</text>
    </svg>`,
)}`;

// SDL3 bouncing ball — compiled against precompiled libSDL3.a (emcmake build).
// Compile with: emcc sdl-main.cpp /usr/lib/libSDL3.a -I/usr/include -s SINGLE_FILE=1 -s ALLOW_MEMORY_GROWTH=1 -O1 -o main.html
export const SDL_DEMO_CODE = `// SDL3 bouncing ball — compiled in the browser via Emscripten
// Click ▶ to build and render to the SDL Canvas tab.
// Uses SDL3 app-lifecycle callbacks — no emscripten main-loop call needed.
#define SDL_MAIN_USE_CALLBACKS
#include <SDL3/SDL.h>
#include <SDL3/SDL_main.h>
#include <math.h>

static SDL_Window   *window   = NULL;
static SDL_Renderer *renderer = NULL;
static float t = 0.f;

static void draw_filled_circle(SDL_Renderer *r, float cx, float cy, float radius) {
    for (float dy = -radius; dy <= radius; dy += 1.f) {
        float dx = sqrtf(radius * radius - dy * dy);
        SDL_RenderLine(r, cx - dx, cy + dy, cx + dx, cy + dy);
    }
}

SDL_AppResult SDL_AppInit(void **appstate, int argc, char *argv[]) {
    SDL_Init(SDL_INIT_VIDEO);
    SDL_CreateWindowAndRenderer("SDL3 Demo", 800, 600, 0, &window, &renderer);
    return SDL_APP_CONTINUE;
}

SDL_AppResult SDL_AppIterate(void *appstate) {
    t += 0.016f;

    SDL_SetRenderDrawColor(renderer, 17, 17, 27, 255);
    SDL_RenderClear(renderer);

    SDL_SetRenderDrawColor(renderer, 40, 40, 60, 255);
    for (float x = 0; x < 800; x += 40)
        SDL_RenderLine(renderer, x, 0, x, 600);
    for (float y = 0; y < 600; y += 40)
        SDL_RenderLine(renderer, 0, y, 800, y);

    float cx = 400.f + 300.f * sinf(t * 1.2f);
    float cy = 300.f + 200.f * cosf(t * 1.4f);
    SDL_SetRenderDrawColor(renderer, 137, 180, 250, 255);
    draw_filled_circle(renderer, cx, cy, 32.f);

    SDL_RenderPresent(renderer);
    return SDL_APP_CONTINUE;
}

SDL_AppResult SDL_AppEvent(void *appstate, SDL_Event *event) {
    if (event->type == SDL_EVENT_QUIT) return SDL_APP_SUCCESS;
    return SDL_APP_CONTINUE;
}

void SDL_AppQuit(void *appstate, SDL_AppResult result) {
    SDL_DestroyRenderer(renderer);
    SDL_DestroyWindow(window);
    SDL_Quit();
}
`;

export const INITIAL_FILES: Record<string, WorkspaceFile> = {
  '/src/sdl-main.cpp': { path: '/src/sdl-main.cpp', type: 'text', content: SDL_DEMO_CODE },
  '/src/main.cpp': { path: '/src/main.cpp', type: 'text', content: DEFAULT_CODE },
  '/src/greetings.h': { path: '/src/greetings.h', type: 'text', content: DEFAULT_HEADER },
  '/assets/workspace-preview.svg': { path: '/assets/workspace-preview.svg', type: 'image', content: DEFAULT_IMAGE },
  '/runtime/sdl-canvas': { path: '/runtime/sdl-canvas', type: 'canvas', content: '' },
};
