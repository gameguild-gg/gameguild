// Workspace configuration types.
//
// `WorkspaceConfig` is the runtime/UI-agnostic descriptor for an emception
// workspace: which files seed the VFS, how to compile and run them, how to
// test them, what layout the IDE should boot into, and which UI features
// the workspace requires (canvas, terminal input, test button).
//
// The IDE (`@emception/ide`) consumes this directly to render. The bare-runner
// surfaces (`@emception/react`, `@emception/webcomponent`) consume the
// `compile` / `run` / `files` slices to drive headless execution.
//
// Moved here from `@emception/ide` so non-IDE consumers can use the same
// shape without pulling in React/Monaco.

/** UI hint for which dock group an open tab should appear in. */
export type DockGroup = 'main' | 'right' | 'bottom';

/** How the IDE should execute the build artefact. */
export type RunType = 'canvas' | 'wasi-terminal' | 'cmake-build' | 'python-script';

/**
 * Which canvas runtime preset to use for this workspace.
 * When set, the IDE uses this instead of heuristics (tool name, workspace id,
 * or compile args) to select the correct runtime module and build preset.
 * Mirrors the key names in BrowserBuildPresets (@emception/browser).
 */
export type CanvasPresetName = 'sdl' | 'raylib' | 'allegro';

export interface CompileConfig {
  tool: string;
  args: string[];
  cwd?: string;
  output: string;
  /** Canvas runtime preset — set for SDL3/raylib/Allegro workspaces. */
  canvasPreset?: CanvasPresetName;
  sourceDetect?: {
    extensions: string[];
    entryPoint?: string;
  };
}

export interface RunConfig {
  type: RunType;
  tool?: string;
  args?: string[];
}

export interface TestConfig {
  tool: string;
  compileArgs?: string[];
  runArgs: string[];
  framework?: 'doctest' | 'pytest' | 'unittest' | 'custom';
}

export interface WorkspaceFeatures {
  canvas?: boolean;
  terminalInput?: boolean;
  showTestButton?: boolean;
}

export interface LayoutTabConfig {
  path: string;
  group: DockGroup;
}

export interface LayoutConfig {
  activeFile: string;
  openTabs: LayoutTabConfig[];
  expandedDirs?: string[];
}

export interface BundleFile {
  encoding: 'text' | 'base64';
  content: string;
}

export interface WorkspaceConfig {
  id: string;
  label: string;
  description?: string;
  version?: number;
  compile: CompileConfig;
  run: RunConfig;
  test?: TestConfig;
  features: WorkspaceFeatures;
  layout: LayoutConfig;
  files: Record<string, BundleFile>;
}

// ── Default file contents (used by the built-in presets) ────────

export const DEFAULT_CODE = `#include <iostream>
#include <string>
int main() {
  std::string name;
  std::cout << "Enter your name: ";
  std::getline(std::cin, name);
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

// raylib bouncing ball (C++) — uses raylib's web-friendly main-loop callback.
// Compile with:
//   em++ raylib-main.cpp -I/usr/include -I/usr/include/raylib \
//        -lraylib -sUSE_GLFW=3 -sFULL_ES2=1 -sALLOW_MEMORY_GROWTH=1 -O1 \
//        -o /home/user/main.js
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

// Allegro 5 bouncing ball (C++) — native Emscripten platform.
// Compile via clang+wasm-ld (BROWSER_BUILD_PRESETS.allegro); the runtime mjs
// supplies emscripten_set_main_loop + WebGL2 (GLFW3) just like raylib.
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

// ── SDL3 + OpenGL ES 3.0 demo ──────────────────────────────────

export const SDL_OPENGL_DEMO_CODE = `// SDL3 + OpenGL ES 3 (WebGL2) demo — compiled in the browser via Emscripten.
// Click \u25B6 to build, then interact with the SDL Canvas tab.
//
// This demo teaches the core OpenGL pipeline step-by-step:
//   1. Create an OpenGL ES 3 context with SDL3
//   2. Write and compile vertex + fragment shaders (GLSL ES 3.00)
//   3. Upload geometry using a VAO and interleaved VBO
//   4. Upload uniforms each frame (rotation angle, colour tint)
//   5. Overlay live controls with Dear ImGui
#define SDL_MAIN_USE_CALLBACKS
#include <SDL3/SDL.h>
#include <SDL3/SDL_main.h>
#include <GLES3/gl3.h>
#include <imgui/imgui.h>
#include <imgui/imgui_impl_sdl3.h>
#include <imgui/imgui_impl_opengl3.h>
#include <math.h>

// ─── Shaders ──────────────────────────────────────────────────────────────
// GLSL ES 3.00 targets WebGL2 / OpenGL ES 3.0 (what Emscripten uses).

static const char* VERT_SRC = R"glsl(#version 300 es
layout(location = 0) in vec2 aPos;    // position in clip space (-1..1)
layout(location = 1) in vec3 aColor;  // per-vertex colour
uniform float uAngle;                  // rotation in radians (set each frame)
out vec3 vColor;
void main() {
    float c = cos(uAngle), s = sin(uAngle);
    // Rotate around origin with a 2-D rotation matrix:
    //   [ c  -s ] [ x ]
    //   [ s   c ] [ y ]
    gl_Position = vec4(c*aPos.x - s*aPos.y,
                       s*aPos.x + c*aPos.y, 0.0, 1.0);
    vColor = aColor;
})glsl";

static const char* FRAG_SRC = R"glsl(#version 300 es
precision mediump float;
in  vec3 vColor;
uniform vec3 uTint;   // colour multiplier — use the ImGui picker to change it
out vec4 fragColor;
void main() { fragColor = vec4(vColor * uTint, 1.0); })glsl";

// ─── App state ────────────────────────────────────────────────────────────
static SDL_Window*   window    = nullptr;
static SDL_GLContext glctx     = nullptr;
static GLuint        vao = 0, vbo = 0, prog = 0;
static GLint         uAngleLoc = -1, uTintLoc = -1;
static float         angle     = 0.f;
static float         speed     = 1.0f;
static float         tint[]    = {1.f, 1.f, 1.f};

static GLuint compileShader(GLenum type, const char* src) {
    GLuint s = glCreateShader(type);
    glShaderSource(s, 1, &src, nullptr);
    glCompileShader(s);
    return s;
}

SDL_AppResult SDL_AppInit(void** appstate, int argc, char* argv[]) {
    SDL_GL_SetAttribute(SDL_GL_CONTEXT_PROFILE_MASK, SDL_GL_CONTEXT_PROFILE_ES);
    SDL_GL_SetAttribute(SDL_GL_CONTEXT_MAJOR_VERSION, 3);
    SDL_GL_SetAttribute(SDL_GL_CONTEXT_MINOR_VERSION, 0);
    SDL_Init(SDL_INIT_VIDEO);
    window = SDL_CreateWindow("SDL3 + OpenGL ES 3", 800, 600, SDL_WINDOW_OPENGL | SDL_WINDOW_RESIZABLE);
    glctx  = SDL_GL_CreateContext(window);
    SDL_GL_MakeCurrent(window, glctx);

    // Step 2 – compile shaders and link the program
    GLuint vs = compileShader(GL_VERTEX_SHADER,   VERT_SRC);
    GLuint fs = compileShader(GL_FRAGMENT_SHADER, FRAG_SRC);
    prog = glCreateProgram();
    glAttachShader(prog, vs); glAttachShader(prog, fs);
    glLinkProgram(prog);
    glDeleteShader(vs); glDeleteShader(fs);   // safe to delete after linking

    uAngleLoc = glGetUniformLocation(prog, "uAngle");
    uTintLoc  = glGetUniformLocation(prog, "uTint");

    // Step 3 – upload triangle geometry
    // Each vertex has 5 floats: position (xy) then colour (rgb), interleaved in one VBO.
    float verts[] = {
    //   x       y     r      g      b
         0.0f,  0.6f, 1.00f, 0.35f, 0.35f,   // top       — red
        -0.55f,-0.4f, 0.35f, 1.00f, 0.35f,   // bot-left  — green
         0.55f,-0.4f, 0.35f, 0.35f, 1.00f,   // bot-right — blue
    };
    glGenVertexArrays(1, &vao);
    glBindVertexArray(vao);
    glGenBuffers(1, &vbo);
    glBindBuffer(GL_ARRAY_BUFFER, vbo);
    glBufferData(GL_ARRAY_BUFFER, sizeof(verts), verts, GL_STATIC_DRAW);
    // attribute 0 = position (2 floats, stride = 5 floats, offset = 0)
    glVertexAttribPointer(0, 2, GL_FLOAT, GL_FALSE, 5*sizeof(float), (void*)0);
    glEnableVertexAttribArray(0);
    // attribute 1 = colour  (3 floats, stride = 5 floats, offset = 2 floats)
    glVertexAttribPointer(1, 3, GL_FLOAT, GL_FALSE, 5*sizeof(float),
                          (void*)(2*sizeof(float)));
    glEnableVertexAttribArray(1);
    glBindVertexArray(0);

    // Step 4 – initialise Dear ImGui with the OpenGL3 backend
    ImGui::CreateContext();
    ImGui::StyleColorsDark();
    ImGui_ImplSDL3_InitForOpenGL(window, glctx);
    ImGui_ImplOpenGL3_Init("#version 300 es");
    return SDL_APP_CONTINUE;
}

SDL_AppResult SDL_AppIterate(void* appstate) {
    // Re-assert the GL context each frame; on Emscripten the browser may
    // unbind the WebGL context between requestAnimationFrame callbacks.
    SDL_GL_MakeCurrent(window, glctx);

    // The IDE callback driver calls SDL_AppIterate each frame directly,
    // without going through SDL_AppEvent — so we poll here for ImGui input.
    {
        SDL_Event ev;
        while (SDL_PollEvent(&ev)) {
            ImGui_ImplSDL3_ProcessEvent(&ev);
            if (ev.type == SDL_EVENT_QUIT) return SDL_APP_SUCCESS;
        }
    }

    angle += 0.016f * speed;

    // Build the ImGui controls window
    ImGui_ImplOpenGL3_NewFrame();
    ImGui_ImplSDL3_NewFrame();
    ImGui::NewFrame();

    ImGui::SetNextWindowPos({10, 10}, ImGuiCond_Once);
    ImGui::SetNextWindowSize({250, 150}, ImGuiCond_Once);
    ImGui::Begin("OpenGL Controls");
    ImGui::SliderFloat("Speed", &speed, 0.0f, 5.0f);
    ImGui::ColorEdit3("Tint",  tint);
    ImGui::Separator();
    ImGui::Text("angle  %.2f rad", angle);
    ImGui::Text("FPS    %.1f", ImGui::GetIO().Framerate);
    ImGui::End();

    // GL render pass
    glViewport(0, 0, 800, 600);
    glClearColor(0.07f, 0.07f, 0.11f, 1.0f);
    glClear(GL_COLOR_BUFFER_BIT);

    glUseProgram(prog);
    glUniform1f(uAngleLoc, angle);
    glUniform3f(uTintLoc, tint[0], tint[1], tint[2]);
    glBindVertexArray(vao);
    glDrawArrays(GL_TRIANGLES, 0, 3);   // 3 vertices = 1 triangle
    glBindVertexArray(0);

    ImGui::Render();
    ImGui_ImplOpenGL3_RenderDrawData(ImGui::GetDrawData());

    SDL_GL_SwapWindow(window);   // no-op in Emscripten; browser handles the swap
    return SDL_APP_CONTINUE;
}

SDL_AppResult SDL_AppEvent(void* appstate, SDL_Event* event) {
    ImGui_ImplSDL3_ProcessEvent(event);
    if (event->type == SDL_EVENT_QUIT) return SDL_APP_SUCCESS;
    return SDL_APP_CONTINUE;
}

void SDL_AppQuit(void* appstate, SDL_AppResult result) {
    ImGui_ImplOpenGL3_Shutdown();
    ImGui_ImplSDL3_Shutdown();
    ImGui::DestroyContext();
    glDeleteBuffers(1, &vbo);
    glDeleteVertexArrays(1, &vao);
    glDeleteProgram(prog);
    SDL_GL_DestroyContext(glctx);
    SDL_DestroyWindow(window);
    SDL_Quit();
}
`;

// ── Workspace bundle helpers ────────────────────────────────────

/** Parse a `.workspace.json` bundle string into a `WorkspaceConfig`. Throws on invalid input. */
export function parseWorkspaceBundle(json: string): WorkspaceConfig {
  const raw = JSON.parse(json);
  if (!raw || typeof raw !== 'object') throw new Error('Invalid workspace bundle: not an object');
  if (!raw.id || typeof raw.id !== 'string') throw new Error('Invalid workspace bundle: missing id');
  if (!raw.compile || !raw.run || !raw.features || !raw.layout || !raw.files) {
    throw new Error('Invalid workspace bundle: missing required fields (compile, run, features, layout, files)');
  }
  return raw as WorkspaceConfig;
}

/** Resolve `{sourceFile}` placeholder in `args` arrays with the actual source path. */
export function resolveArgs(args: string[], sourceFile: string): string[] {
  return args.map((a) => a.replace(/\{sourceFile\}/g, sourceFile));
}
