/**
 * Pinned default versions for all emception build tools.
 *
 * Every build script reads process.env.TOOL_VERSION first; if absent it uses
 * the constant here. Override any single tool by setting the corresponding env
 * var — no source changes needed.
 *
 * cpython: pinned to the Python bundled with emsdk 5.0.7 (3.13.3).
 * sdl3:    not listed — build-sdl3.ts uses the emsdk port system (-sUSE_SDL=3).
 * rlights: header-only inside the raylib source tree; no separate version pin.
 * curl-lite: local custom implementation; version string tracks real curl releases.
 */
export const PINNED = {
    EMSDK_VERSION: '5.0.7',
    CMAKE_VERSION: '3.31.12',
    NINJA_VERSION: '1.13.2',
    BINARYEN_VERSION: '129',
    BROTLI_VERSION: '1.2.0',
    PYTHON_VERSION: '3.13.3',
    IMGUI_VERSION: 'v1.92.7',
    /** resolveAvailableLLVMRelease() is called with this value; if 23.0.0 tarball
     *  is not yet published it walks to the closest available 23.x release. */
    LLVM_VERSION: '23.0.0',
    // Raylib family
    RAYLIB_VERSION: '6.0',
    RAYGUI_VERSION: '4.0',
    PHYSAC_VERSION: '1.1',
    // Allegro 5: native Emscripten platform support (-DPLATFORM=Emscripten).
    ALLEGRO_VERSION: '5.2.10.1',
    // curl-lite: local custom implementation; version string tracks real curl releases
    CURL_LITE_VERSION: '8.20.0',
} as const;
