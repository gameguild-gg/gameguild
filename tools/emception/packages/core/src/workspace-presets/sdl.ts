import type { WorkspaceConfig } from '../workspace-config.js';
import { ToolchainPreset } from '../types.js';
import { DEFAULT_IMAGE } from './defaults.js';

export const SDL_DEMO_CODE = `// SDL3 + Dear ImGui demo — compiled in the browser via Emscripten
// Click ▶ to build and render to the SDL Canvas tab.
// Uses SDL3 app-lifecycle callbacks — no emscripten main-loop call needed.
#define SDL_MAIN_USE_CALLBACKS
#include <SDL3/SDL.h>
#include <SDL3/SDL_main.h>
#include <imgui/imgui.h>
#include <imgui/imgui_impl_sdl3.h>
#include <imgui/imgui_impl_sdlrenderer3.h>
#include <math.h>

static SDL_Window   *window   = nullptr;
static SDL_Renderer *renderer = nullptr;
static float t      = 0.f;
static float speed  = 1.0f;
static float radius = 32.f;

static void draw_filled_circle(SDL_Renderer *r, float cx, float cy, float rad) {
    for (float dy = -rad; dy <= rad; dy += 1.f) {
        float dx = sqrtf(rad * rad - dy * dy);
        SDL_RenderLine(r, cx - dx, cy + dy, cx + dx, cy + dy);
    }
}

SDL_AppResult SDL_AppInit(void **appstate, int argc, char *argv[]) {
    SDL_Init(SDL_INIT_VIDEO);
    SDL_CreateWindowAndRenderer("SDL3 + ImGui Demo", 800, 600, 0, &window, &renderer);

    IMGUI_CHECKVERSION();
    ImGui::CreateContext();
    ImGui::StyleColorsDark();
    ImGui_ImplSDL3_InitForSDLRenderer(window, renderer);
    ImGui_ImplSDLRenderer3_Init(renderer);
    return SDL_APP_CONTINUE;
}

SDL_AppResult SDL_AppIterate(void *appstate) {
    // The browser IDE callback driver calls SDL_AppIterate directly without
    // calling SDL_AppEvent, so we must poll events here for ImGui to receive
    // mouse/keyboard input.  In SDL3's native callback loop SDL_AppEvent has
    // already consumed all queued events, so SDL_PollEvent returns immediately
    // and there is no double-processing.
    {
        SDL_Event ev;
        while (SDL_PollEvent(&ev)) {
            ImGui_ImplSDL3_ProcessEvent(&ev);
            if (ev.type == SDL_EVENT_QUIT) return SDL_APP_SUCCESS;
        }
    }

    t += 0.016f * speed;

    // ImGui new frame
    ImGui_ImplSDLRenderer3_NewFrame();
    ImGui_ImplSDL3_NewFrame();
    ImGui::NewFrame();

    float cx = 400.f + 300.f * sinf(t * 1.2f);
    float cy = 300.f + 200.f * cosf(t * 1.4f);

    // Controls window
    ImGui::SetNextWindowPos({10, 10}, ImGuiCond_Once);
    ImGui::SetNextWindowSize({220, 120}, ImGuiCond_Once);
    ImGui::Begin("Controls");
    ImGui::SliderFloat("Speed",  &speed,  0.1f, 3.0f);
    ImGui::SliderFloat("Radius", &radius, 8.f,  80.f);
    ImGui::Text("Ball  x=%.0f  y=%.0f", cx, cy);
    ImGui::Text("FPS   %.1f", ImGui::GetIO().Framerate);
    ImGui::End();

    // Scene
    SDL_SetRenderDrawColor(renderer, 17, 17, 27, 255);
    SDL_RenderClear(renderer);

    SDL_SetRenderDrawColor(renderer, 40, 40, 60, 255);
    for (float x = 0; x < 800; x += 40)
        SDL_RenderLine(renderer, x, 0, x, 600);
    for (float y = 0; y < 600; y += 40)
        SDL_RenderLine(renderer, 0, y, 800, y);

    SDL_SetRenderDrawColor(renderer, 137, 180, 250, 255);
    draw_filled_circle(renderer, cx, cy, radius);

    // ImGui render
    ImGui::Render();
    ImGui_ImplSDLRenderer3_RenderDrawData(ImGui::GetDrawData(), renderer);

    SDL_RenderPresent(renderer);
    return SDL_APP_CONTINUE;
}

SDL_AppResult SDL_AppEvent(void *appstate, SDL_Event *event) {
    ImGui_ImplSDL3_ProcessEvent(event);
    if (event->type == SDL_EVENT_QUIT) return SDL_APP_SUCCESS;
    return SDL_APP_CONTINUE;
}

void SDL_AppQuit(void *appstate, SDL_AppResult result) {
    ImGui_ImplSDLRenderer3_Shutdown();
    ImGui_ImplSDL3_Shutdown();
    ImGui::DestroyContext();
    SDL_DestroyRenderer(renderer);
    SDL_DestroyWindow(window);
    SDL_Quit();
}
`;

export const CPP_SDL3_PRESET: WorkspaceConfig = {
    id: 'cpp-sdl3',
    label: 'C++ SDL3 — Bouncing Ball',
    description: 'SDL3 graphics demo compiled in the browser with Emscripten',
    version: 1,
    compile: {
        // Direct clang + wasm-ld two-step path.
        // SDL3 is not an emsdk port — using emcc would trigger ports/__init__.py
        // which fails in the WASM sandbox. toolchain='sdl-cpp' selects the argv
        // builders and runtime module without any file-extension heuristics.
        tool: 'clang',
        args: [],
        output: 'main.wasm',
        toolchain: ToolchainPreset.SDL_CPP,
        sourceDetect: { extensions: ['.cpp', '.c'], entryPoint: 'sdl-main.cpp' },
    },
    run: {
        type: 'canvas',
    },
    features: {
        canvas: true,
        terminalInput: false,
        showTestButton: false,
    },
    files: {
        'sdl-main.cpp': { encoding: 'text', content: SDL_DEMO_CODE },
        'workspace-preview.svg': { encoding: 'text', content: DEFAULT_IMAGE },
    },
};
