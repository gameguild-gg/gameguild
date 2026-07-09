import type { WorkspaceConfig } from '../workspace-config.js';
import { ToolchainPreset } from '../types.js';
import { DEFAULT_IMAGE } from './defaults.js';

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
    SDL_Init(SDL_INIT_VIDEO);
    SDL_GL_SetAttribute(SDL_GL_CONTEXT_PROFILE_MASK, SDL_GL_CONTEXT_PROFILE_ES);
    SDL_GL_SetAttribute(SDL_GL_CONTEXT_MAJOR_VERSION, 3);
    SDL_GL_SetAttribute(SDL_GL_CONTEXT_MINOR_VERSION, 0);
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

export const CPP_SDL3_OPENGL_PRESET: WorkspaceConfig = {
    id: 'cpp-sdl3-opengl',
    label: 'C++ SDL3 — OpenGL ES 3.0',
    description: 'SDL3 + raw OpenGL ES 3 (WebGL2) with Dear ImGui — learn the graphics pipeline',
    version: 1,
    compile: {
        tool: 'clang',
        args: [],
        output: 'main.wasm',
        toolchain: ToolchainPreset.SDL_CPP,
        sourceDetect: { extensions: ['.cpp', '.c'], entryPoint: 'sdl-opengl.cpp' },
    },
    run: { type: 'canvas' },
    features: { canvas: true, terminalInput: false, showTestButton: false },
    files: {
        'sdl-opengl.cpp': { encoding: 'text', content: SDL_OPENGL_DEMO_CODE },
        'workspace-preview.svg': { encoding: 'text', content: DEFAULT_IMAGE },
    },
};
