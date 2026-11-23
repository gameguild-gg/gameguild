import { CODING_TOOLCHAIN, type CodingLearnerDelivery } from '@game-guild/coding';
import { createCodingGraphicsSession } from '../graphics-session';

// Synthetic visual/input calibration, not an exercise fixture or pixel judge.
const source = `#define SDL_MAIN_USE_CALLBACKS
#include <SDL3/SDL.h>
#include <SDL3/SDL_main.h>
#include <stdio.h>
static SDL_Window *window;
static SDL_Renderer *renderer;
static int frame = 0;
static bool selected = false;
SDL_AppResult SDL_AppInit(void **state, int argc, char **argv) {
  if (!SDL_Init(SDL_INIT_VIDEO)) return SDL_APP_FAILURE;
  if (!SDL_CreateWindowAndRenderer("Canvas probe", 320, 180, SDL_WINDOW_RESIZABLE, &window, &renderer)) return SDL_APP_FAILURE;
  return SDL_APP_CONTINUE;
}
SDL_AppResult SDL_AppIterate(void *state) {
  SDL_Event event;
  while (SDL_PollEvent(&event)) {
    if (event.type == SDL_EVENT_KEY_DOWN && event.key.key == SDLK_SPACE) {
      selected = !selected;
      puts("space received");
    }
  }
  SDL_SetRenderDrawColor(renderer, selected ? 170 : 20, selected ? 45 : 48, selected ? 65 : 80, 255);
  SDL_RenderClear(renderer);
  SDL_SetRenderDrawColor(renderer, 245, 245, 245, 255);
  SDL_FRect marker = { float((frame++ * 3) % 280), 135.0f, 20.0f, 20.0f };
  SDL_RenderFillRect(renderer, &marker);
  SDL_RenderPresent(renderer);
  return SDL_APP_CONTINUE;
}
SDL_AppResult SDL_AppEvent(void *state, SDL_Event *event) { return SDL_APP_CONTINUE; }
void SDL_AppQuit(void *state, SDL_AppResult result) {
  SDL_DestroyRenderer(renderer); SDL_DestroyWindow(window); SDL_Quit();
}
`;

export function createGraphicsBrowserFixture(canvas: HTMLCanvasElement, candidateKey: string) {
  const delivery: CodingLearnerDelivery = {
    schemaVersion: 1, type: 'coding-assignment', exerciseType: 'standard-io',
    environment: { language: 'cpp', runtime: 'sdl3' }, toolchain: { key: 'cpp-emception', ...CODING_TOOLCHAIN },
    files: [{ id: 'main', path: '/home/user/main.cpp', encoding: 'text', content: source, mutable: true }],
    publicTests: [], limits: { timeLimitMs: 30_000, outputLimitBytes: 2_000_000, maxFileCount: 16 },
    entryPoint: { path: '/home/user/main.cpp' }, allowLearnerCreatedFiles: false,
  };
  const session = createCodingGraphicsSession({ candidateKey, delivery, manifestUrl: '/emception/manifest.json' });
  return { session, run: () => session.run(canvas) };
}

export function inspectGraphicsPixels(canvas: HTMLCanvasElement) {
  return new Promise<{ background: number[]; marker: number[] }>((resolve, reject) => {
    requestAnimationFrame(() => {
      const gl = canvas.getContext('webgl2') ?? canvas.getContext('webgl');
      if (!gl) { reject(new Error('The graphics target has no WebGL context.')); return; }
      const pixels = new Uint8Array(canvas.width * canvas.height * 4);
      gl.readPixels(0, 0, canvas.width, canvas.height, gl.RGBA, gl.UNSIGNED_BYTE, pixels);
      const index = ((canvas.height - 10) * canvas.width + 10) * 4;
      const marker: number[] = [];
      for (let x = 0; x < canvas.width; x++) {
        const offset = ((canvas.height - 145) * canvas.width + x) * 4;
        if (pixels[offset] > 230 && pixels[offset + 1] > 230 && pixels[offset + 2] > 230) marker.push(x);
      }
      resolve({ background: [...pixels.slice(index, index + 4)], marker });
    });
  });
}
