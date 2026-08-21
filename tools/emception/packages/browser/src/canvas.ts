import { ToolchainPreset } from 'emception';
import type { EmceptionAPI, ToolResult } from 'emception';
import { TOOLCHAIN_PRESETS } from './presets.js';
import type { NativePreset } from './presets.js';
import { startCanvasArtifact } from './canvas-runtime.js';
import type { CanvasSession, CanvasStartOptions } from './canvas-runtime.js';

export type CanvasToolchain =
    | ToolchainPreset.SDL_CPP
    | ToolchainPreset.SDL_C
    | ToolchainPreset.Raylib_CPP
    | ToolchainPreset.Raylib_C
    | ToolchainPreset.Allegro_CPP
    | ToolchainPreset.Allegro_C;

export interface CanvasBuildOptions {
    toolchain: CanvasToolchain;
    sourcePath: string;
    cwd?: string;
    objectPath?: string;
    wasmPath?: string;
    onStdout?: (text: string) => void;
    onStderr?: (text: string) => void;
}

interface CanvasBuildBase {
    compile: ToolResult;
}

export interface CanvasCompileFailure extends CanvasBuildBase {
    phase: 'compile';
}

export interface CanvasLinkFailure extends CanvasBuildBase {
    phase: 'link';
    link: ToolResult;
}

export interface CanvasArtifact extends CanvasBuildBase {
    phase: 'ready';
    link: ToolResult;
    runtimeProfile: string;
    runtimePath: string;
    wasmPath: string;
    runtimeGlue: Uint8Array;
    wasm: Uint8Array;
}

export type CanvasBuildResult = CanvasCompileFailure | CanvasLinkFailure | CanvasArtifact;

export interface CanvasAPI {
    build(options: CanvasBuildOptions): Promise<CanvasBuildResult>;
    start(artifact: CanvasArtifact, options: CanvasStartOptions): Promise<CanvasSession>;
    buildAndStart(build: CanvasBuildOptions, start: CanvasStartOptions): Promise<CanvasBuildResult | CanvasSession>;
    stop(): void;
}

type CanvasHostAPI = Pick<EmceptionAPI, 'run' | 'workspace'>;

function runtimeProfile(toolchain: CanvasToolchain): string {
    switch (toolchain) {
        case ToolchainPreset.SDL_CPP:
        case ToolchainPreset.SDL_C:
            return 'sdl3-runtime';
        case ToolchainPreset.Raylib_CPP:
        case ToolchainPreset.Raylib_C:
            return 'raylib-runtime';
        case ToolchainPreset.Allegro_CPP:
        case ToolchainPreset.Allegro_C:
            return 'allegro-runtime';
    }
}

function nativePreset(toolchain: CanvasToolchain): NativePreset {
    const preset = TOOLCHAIN_PRESETS[toolchain];
    if (!('compileTool' in preset)) throw new Error(`Canvas toolchain '${toolchain}' is not a native preset`);
    return preset;
}

function outputSink(callback: ((text: string) => void) | undefined): ((chunk: Uint8Array) => void) | 'capture' {
    if (!callback) return 'capture';
    const decoder = new TextDecoder();
    return (chunk) => callback(decoder.decode(chunk, { stream: true }));
}

export function createCanvasAPI(api: CanvasHostAPI): CanvasAPI {
    let activeSession: CanvasSession | null = null;
    return {
        async build(options) {
            const preset = nativePreset(options.toolchain);
            const cwd = options.cwd ?? '/home/user/default';
            const objectPath = options.objectPath ?? '/tmp/emception-canvas-main.o';
            const wasmPath = options.wasmPath ?? `${cwd.replace(/\/$/, '')}/main.wasm`;
            const paths = { sourcePath: options.sourcePath, objectPath, wasmPath };
            const runOptions = {
                cwd,
                stdout: outputSink(options.onStdout),
                stderr: outputSink(options.onStderr),
                preloadBundles: [...preset.bundlesToPreload],
            };

            const compile = await api.run(preset.compileTool, preset.compileArgv(paths), runOptions);
            if (compile.exitCode !== 0) return { phase: 'compile', compile };

            const link = await api.run(preset.linkTool, preset.linkArgv(paths), runOptions);
            if (link.exitCode !== 0) return { phase: 'link', compile, link };

            const profile = runtimeProfile(options.toolchain);
            const runtimePath = `/usr/lib/emscripten/${profile}.mjs`;
            const [runtimeGlue, wasm] = await Promise.all([
                api.workspace.readFile(runtimePath),
                api.workspace.readFile(wasmPath),
            ]);
            if (!runtimeGlue) throw new Error(`Canvas runtime glue is missing from the toolchain release: ${runtimePath}`);
            if (!wasm) throw new Error(`Canvas build did not produce its WASM artifact: ${wasmPath}`);
            return {
                phase: 'ready',
                compile,
                link,
                runtimeProfile: profile,
                runtimePath,
                wasmPath,
                runtimeGlue,
                wasm,
            };
        },
        async start(artifact, options) {
            activeSession?.stop();
            activeSession = await startCanvasArtifact(artifact, options);
            return activeSession;
        },
        async buildAndStart(build, start) {
            const result = await this.build(build);
            return result.phase === 'ready' ? this.start(result, start) : result;
        },
        stop() {
            activeSession?.stop();
            activeSession = null;
        },
    };
}
