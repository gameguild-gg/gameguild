import type { WorkspaceConfig } from 'emception';

// ── C++ stdin/stdout starter with grading plan ───────────────────

const STARTER_CPP = `#include <iostream>
#include <string>
int main() {
  std::string line;
  std::getline(std::cin, line);
  std::cout << line << std::endl;
  return 0;
}
`;

// ── SDL3 graphics starter ─────────────────────────────────────────
// ponytail: minimal SDL3 app-lifecycle callback skeleton — compiles
// against precompiled libSDL3.a (emcmake) via emcc -sUSE_SDL=3.

const STARTER_SDL = `// SDL3 graphics starter — compile with emcc -sUSE_SDL=3
#define SDL_MAIN_USE_CALLBACKS
#include <SDL3/SDL.h>
#include <SDL3/SDL_main.h>

static SDL_Window   *window   = NULL;
static SDL_Renderer *renderer = NULL;

SDL_AppResult SDL_AppInit(void **appstate, int argc, char *argv[]) {
  SDL_Init(SDL_INIT_VIDEO);
  SDL_CreateWindowAndRenderer("SDL3 Assignment", 640, 480, 0, &window, &renderer);
  return SDL_APP_CONTINUE;
}

SDL_AppResult SDL_AppIterate(void *appstate) {
  SDL_SetRenderDrawColor(renderer, 30, 30, 45, 255);
  SDL_RenderClear(renderer);
  SDL_RenderPresent(renderer);
  return SDL_APP_CONTINUE;
}

SDL_AppResult SDL_AppEvent(void *appstate, SDL_Event *event) {
  return event->type == SDL_EVENT_QUIT ? SDL_APP_SUCCESS : SDL_APP_CONTINUE;
}

void SDL_AppQuit(void *appstate, SDL_AppResult result) {
  SDL_DestroyRenderer(renderer);
  SDL_DestroyWindow(window);
  SDL_Quit();
}
`;

// ── raylib graphics starter ───────────────────────────────────────
// ponytail: minimal raylib loop — compiled against precompiled
// libraylib.a via emcc + -lraylib.

const STARTER_RAYLIB = `// raylib graphics starter
#include "raylib.h"

int main(void) {
  InitWindow(640, 480, "raylib Assignment");
  SetTargetFPS(60);
  while (!WindowShouldClose()) {
    BeginDrawing();
    ClearBackground((Color){30, 30, 45, 255});
    DrawText("Hello raylib", 240, 220, 24, RAYWHITE);
    EndDrawing();
  }
  CloseWindow();
  return 0;
}
`;

// ── Allegro 5 graphics starter ────────────────────────────────────
// ponytail: minimal Allegro 5 loop — compiled via the clang + wasm-ld
// two-step against precompiled liballegro*.a (CDN 'allegro' bundle).
// Mirrors the e2e-verified demo shape: emscripten_set_main_loop drives
// the frame callback (blocking while-loops would freeze the main thread).

const STARTER_ALLEGRO = `// Allegro 5 graphics starter
#include <allegro5/allegro.h>
#include <allegro5/allegro_primitives.h>
#include <emscripten/emscripten.h>

static ALLEGRO_DISPLAY* display = NULL;

static void draw_frame(void) {
  al_clear_to_color(al_map_rgb(30, 30, 45));
  al_draw_filled_circle(320, 240, 48, al_map_rgb(137, 180, 250));
  al_flip_display();
}

int main(void) {
  if (!al_init()) return 1;
  al_init_primitives_addon();
  display = al_create_display(640, 480);
  if (!display) return 1;
  emscripten_set_main_loop(draw_frame, 0, 1);
  return 0;
}
`;

export type CodingLanguage = 'cpp' | 'c' | 'sdl-cpp' | 'raylib-cpp' | 'allegro-cpp';

type AssessmentSampleWorkspace = WorkspaceConfig & {
  readonly layout: {
    readonly activeFile: string;
    readonly openTabs: readonly { readonly path: string; readonly group: string }[];
    readonly expandedDirs?: readonly string[];
  };
};

export interface AssignmentSample {
  workspaceConfig: AssessmentSampleWorkspace;
}

export const ASSIGNMENT_SAMPLES: Record<CodingLanguage, AssignmentSample> = {
  cpp: {
    workspaceConfig: {
      id: 'cpp',
      label: 'C++ Assignment',
      description: 'Stdin/stdout starter with hidden doctest case',
      version: 1,
      compile: {
        tool: 'clang',
        args: [],
        cwd: '/home/user',
        output: '/home/user/main.wasm',
        sourceDetect: { extensions: ['.cpp', '.c'], entryPoint: '/user/main.cpp' },
      },
      run: {
        type: 'wasi-terminal',
        tool: 'wasi-run',
        args: ['wasi-run', '/home/user/main.wasm'],
      },
      features: {
        canvas: false,
        terminalInput: true,
        showTestButton: false,
      },
      layout: {
        activeFile: '/user/main.cpp',
        openTabs: [{ path: '/user/main.cpp', group: 'main' }],
        expandedDirs: ['/user'],
      },
      files: {
        '/user/main.cpp': { encoding: 'text', content: STARTER_CPP },
      },
    },
  },

  // ponytail: C language is structurally identical to C++ for the starter
  // template — same stdin/stdout flow, different file extension. Instructors
  // pick this when the curriculum targets pure C.
  c: {
    workspaceConfig: {
      id: 'c',
      label: 'C Assignment',
      description: 'C stdin/stdout starter',
      version: 1,
      compile: {
        tool: 'clang',
        args: [],
        cwd: '/home/user',
        output: '/home/user/main.wasm',
        sourceDetect: { extensions: ['.c'], entryPoint: '/user/main.c' },
      },
      run: {
        type: 'wasi-terminal',
        tool: 'wasi-run',
        args: ['wasi-run', '/home/user/main.wasm'],
      },
      features: {
        canvas: false,
        terminalInput: true,
        showTestButton: false,
      },
      layout: {
        activeFile: '/user/main.c',
        openTabs: [{ path: '/user/main.c', group: 'main' }],
        expandedDirs: ['/user'],
      },
      files: {
        '/user/main.c': {
          encoding: 'text',
          content: `#include <stdio.h>
int main(void) {
  char line[256];
  if (!fgets(line, sizeof(line), stdin)) return 0;
  printf("%s", line);
  return 0;
}
`,
        },
      },
    },
  },

  'sdl-cpp': {
    workspaceConfig: {
      id: 'sdl-cpp',
      label: 'SDL3 C++ Assignment',
      description: 'SDL3 graphics starter — emcc + libSDL3',
      version: 1,
      compile: {
        tool: 'emcc',
        args: [
          'emcc',
          '{sourceFile}',
          '-sUSE_SDL=3',
          '-I/usr/include',
          '-sALLOW_MEMORY_GROWTH=1',
          '-sENVIRONMENT=web',
          '-O1',
          '-o',
          '/home/user/main.wasm',
        ],
        cwd: '/home/user',
        output: '/home/user/main.wasm',
        sourceDetect: { extensions: ['.cpp', '.c'], entryPoint: '/user/sdl-main.cpp' },
      },
      run: {
        type: 'canvas',
      },
      features: {
        canvas: true,
        terminalInput: false,
        showTestButton: false,
      },
      layout: {
        activeFile: '/user/sdl-main.cpp',
        openTabs: [
          { path: '/user/sdl-main.cpp', group: 'main' },
          { path: '/user/sdl-canvas', group: 'right' },
        ],
        expandedDirs: ['/user'],
      },
      files: {
        '/user/sdl-main.cpp': { encoding: 'text', content: STARTER_SDL },
      },
    },
  },

  'raylib-cpp': {
    workspaceConfig: {
      id: 'raylib-cpp',
      label: 'raylib C++ Assignment',
      description: 'raylib graphics starter — emcc + libraylib',
      version: 1,
      compile: {
        tool: 'emcc',
        args: [
          'emcc',
          '{sourceFile}',
          '-I/usr/include',
          '-L/usr/lib',
          '-lraylib',
          '-sALLOW_MEMORY_GROWTH=1',
          '-sENVIRONMENT=web',
          '-O1',
          '-o',
          '/home/user/main.wasm',
        ],
        cwd: '/home/user',
        output: '/home/user/main.wasm',
        sourceDetect: { extensions: ['.cpp', '.c'], entryPoint: '/user/raylib-main.cpp' },
      },
      run: {
        type: 'canvas',
      },
      features: {
        canvas: true,
        terminalInput: false,
        showTestButton: false,
      },
      layout: {
        activeFile: '/user/raylib-main.cpp',
        openTabs: [
          { path: '/user/raylib-main.cpp', group: 'main' },
          { path: '/user/sdl-canvas', group: 'right' },
        ],
        expandedDirs: ['/user'],
      },
      files: {
        '/user/raylib-main.cpp': { encoding: 'text', content: STARTER_RAYLIB },
      },
    },
  },

  'allegro-cpp': {
    workspaceConfig: {
      id: 'allegro-cpp',
      label: 'Allegro 5 C++ Assignment',
      description: 'Allegro 5 graphics starter — clang + wasm-ld',
      version: 1,
      compile: {
        // Canvas path compiles via TOOLCHAIN_PRESETS['allegro-cpp'] argv
        // builders — args stay empty like the core cpp-allegro preset.
        tool: 'clang',
        args: [],
        cwd: '/home/user',
        output: '/home/user/main.wasm',
        sourceDetect: { extensions: ['.cpp', '.c'], entryPoint: '/user/allegro-main.cpp' },
      },
      run: {
        type: 'canvas',
      },
      features: {
        canvas: true,
        terminalInput: false,
        showTestButton: false,
      },
      layout: {
        activeFile: '/user/allegro-main.cpp',
        openTabs: [
          { path: '/user/allegro-main.cpp', group: 'main' },
          { path: '/user/sdl-canvas', group: 'right' },
        ],
        expandedDirs: ['/user'],
      },
      files: {
        '/user/allegro-main.cpp': { encoding: 'text', content: STARTER_ALLEGRO },
      },
    },
  },
};
