import type { WorkspaceConfig } from '../workspace-config.js';
import { ToolchainPreset } from '../types.js';

export const ALLEGRO_DEMO_CODE = `// Allegro 5 interactive demo — compiled in the browser via Emscripten.
// Click ▶ to build and render to the canvas tab.
// Hold left mouse button to drag the ball. Scroll wheel to resize it.
#include <allegro5/allegro.h>
#include <allegro5/allegro_primitives.h>
#include <allegro5/allegro_font.h>
#include <emscripten/emscripten.h>
#include <cmath>

static constexpr int W = 800;
static constexpr int H = 600;
static ALLEGRO_DISPLAY*     display  = nullptr;
static ALLEGRO_EVENT_QUEUE* queue    = nullptr;
static ALLEGRO_FONT*        font     = nullptr;
static float                t        = 0.f;
static float                cx       = W * 0.5f;
static float                cy       = H * 0.5f;
static float                radius   = 32.f;
static bool                 dragging = false;

static void update_draw_frame() {
    t += 1.f / 60.f;

    // Drain pending mouse events
    ALLEGRO_EVENT ev;
    while (al_get_next_event(queue, &ev)) {
        if (ev.type == ALLEGRO_EVENT_MOUSE_BUTTON_DOWN) {
            dragging = true;
            cx = (float)ev.mouse.x;
            cy = (float)ev.mouse.y;
        } else if (ev.type == ALLEGRO_EVENT_MOUSE_BUTTON_UP) {
            dragging = false;
        } else if (ev.type == ALLEGRO_EVENT_MOUSE_AXES) {
            if (dragging) {
                cx = (float)ev.mouse.x;
                cy = (float)ev.mouse.y;
            }
            if (ev.mouse.dz != 0) {
                radius += ev.mouse.dz * 4.f;
                if (radius < 8.f)  radius = 8.f;
                if (radius > 80.f) radius = 80.f;
            }
        }
    }

    if (!dragging) {
        cx = W * 0.5f + 300.f * std::sin(t * 1.2f);
        cy = H * 0.5f + 200.f * std::cos(t * 1.4f);
    }

    al_clear_to_color(al_map_rgb(17, 17, 27));

    for (int x = 0; x < W; x += 40)
        al_draw_line(x, 0, x, H, al_map_rgb(40, 40, 60), 1.f);
    for (int y = 0; y < H; y += 40)
        al_draw_line(0, y, W, y, al_map_rgb(40, 40, 60), 1.f);

    al_draw_filled_circle(cx, cy, radius, al_map_rgb(137, 180, 250));
    al_draw_circle(cx, cy, radius + 5.f, al_map_rgba(205, 214, 244, 100), 1.5f);

    // HUD background
    al_draw_filled_rectangle(0, 0, 230, 60, al_map_rgba(0, 0, 0, 140));
    if (font) {
        al_draw_text(font, al_map_rgb(205, 214, 244),  8,  8, ALLEGRO_ALIGN_LEFT, "Allegro 5 + Emscripten");
        al_draw_text(font, al_map_rgb(148, 226, 213),  8, 24, ALLEGRO_ALIGN_LEFT, "Hold LMB: drag ball");
        al_draw_text(font, al_map_rgb(148, 226, 213),  8, 40, ALLEGRO_ALIGN_LEFT, "Scroll: resize ball");
    }

    al_flip_display();
}

int main() {
    if (!al_init()) return 1;
    al_init_primitives_addon();
    al_init_font_addon();
    al_install_mouse();

    display = al_create_display(W, H);
    if (!display) return 1;
    al_set_window_title(display, "Allegro 5 demo");

    font = al_create_builtin_font();

    queue = al_create_event_queue();
    al_register_event_source(queue, al_get_mouse_event_source());

    emscripten_set_main_loop(update_draw_frame, 0, 1);
    return 0;
}
`;

export const CPP_ALLEGRO_PRESET: WorkspaceConfig = {
    id: 'cpp-allegro',
    label: 'C++ Allegro 5 — Bouncing Ball',
    description: 'Allegro 5 graphics demo compiled in the browser with Emscripten',
    version: 1,
    compile: {
        // Direct clang + wasm-ld two-step path.
        // toolchain='allegro-cpp' selects the argv builders and loads
        // allegro-runtime.mjs as the canvas runtime.
        tool: 'clang',
        args: [],
        output: 'main.wasm',
        toolchain: ToolchainPreset.Allegro_CPP,
        sourceDetect: { extensions: ['.cpp', '.c'], entryPoint: 'allegro-main.cpp' },
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
        'allegro-main.cpp': { encoding: 'text', content: ALLEGRO_DEMO_CODE },
    },
};
