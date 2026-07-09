import type { WorkspaceConfig } from '../workspace-config.js';
import { ToolchainPreset } from '../types.js';
import { DEFAULT_IMAGE } from './defaults.js';

export const RAYLIB_DEMO_CODE = `// raylib interactive demo — compiled in the browser via Emscripten.
// Click ▶ to build and render to the canvas tab.
// Hold left mouse button to drag the ball. Scroll wheel to resize it.
#include <raylib.h>
#include <emscripten/emscripten.h>
#include <cmath>

static constexpr int W = 800;
static constexpr int H = 600;
static float t      = 0.f;
static float radius = 32.f;
static float cx     = W * 0.5f;
static float cy     = H * 0.5f;
static bool  held   = false;

static void update_draw_frame() {
    float dt = GetFrameTime();
    t += dt;

    // Scroll wheel resizes ball
    float wheel = GetMouseWheelMove();
    if (wheel != 0.f) {
        radius += wheel * 4.f;
        if (radius < 8.f)  radius = 8.f;
        if (radius > 80.f) radius = 80.f;
    }

    // Hold LMB to drag ball
    if (IsMouseButtonDown(MOUSE_BUTTON_LEFT)) {
        Vector2 mp = GetMousePosition();
        cx = mp.x;
        cy = mp.y;
        held = true;
    } else {
        held = false;
        cx = W * 0.5f + 300.f * std::sin(t * 1.2f);
        cy = H * 0.5f + 200.f * std::cos(t * 1.4f);
    }

    BeginDrawing();
    ClearBackground({ 17, 17, 27, 255 });

    for (int x = 0; x < W; x += 40) DrawLine(x, 0, x, H, { 40, 40, 60, 255 });
    for (int y = 0; y < H; y += 40) DrawLine(0, y, W, y, { 40, 40, 60, 255 });

    DrawCircleV({ cx, cy }, radius, { 137, 180, 250, 255 });
    DrawCircleLines((int)cx, (int)cy, radius + 5.f, { 205, 214, 244, 100 });

    // HUD
    DrawRectangle(0, 0, 255, 82, { 0, 0, 0, 140 });
    DrawText("raylib + Emscripten",   8,  6, 18, { 205, 214, 244, 255 });
    DrawText(TextFormat("FPS %d  r=%.0f", GetFPS(), radius), 8, 30, 16, { 166, 227, 161, 255 });
    DrawText("Hold LMB: drag ball",   8, 52, 15, { 148, 226, 213, 255 });
    DrawText("Scroll: resize ball",   8, 68, 15, { 148, 226, 213, 255 });

    EndDrawing();
}

int main() {
    InitWindow(W, H, "raylib demo");
    emscripten_set_main_loop(update_draw_frame, 0, 1);
    CloseWindow();
    return 0;
}
`;

export const CPP_RAYLIB_PRESET: WorkspaceConfig = {
    id: 'cpp-raylib',
    label: 'C++ raylib — Bouncing Ball',
    description: 'raylib graphics demo compiled in the browser with Emscripten',
    version: 2,
    compile: {
        // Direct clang + wasm-ld two-step path.
        // raylib is not an emsdk port — em++ triggers ports/__init__.py which
        // fails in the WASM sandbox. toolchain='raylib-cpp' selects argv builders
        // and the raylib-runtime.mjs canvas module.
        tool: 'clang',
        args: [],
        output: 'main.wasm',
        toolchain: ToolchainPreset.Raylib_CPP,
        sourceDetect: { extensions: ['.cpp', '.c'], entryPoint: 'raylib-main.cpp' },
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
        'raylib-main.cpp': { encoding: 'text', content: RAYLIB_DEMO_CODE },
        'workspace-preview.svg': { encoding: 'text', content: DEFAULT_IMAGE },
    },
};
