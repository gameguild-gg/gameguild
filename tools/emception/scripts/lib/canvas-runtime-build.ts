import fs from 'fs';
import os from 'os';
import path from 'path';
import shell from 'shelljs';

export type CanvasRuntimeBuild = {
    readonly compiler: string;
    readonly sourcePath: string;
    readonly libraryPaths: readonly string[];
    readonly includeDirectories: readonly string[];
    readonly flags: readonly string[];
    readonly outputDirectory: string;
    readonly runtimeName: string;
};

export type CanvasRuntimePair = {
    readonly gluePath: string;
    readonly wasmPath: string;
};

export class CanvasRuntimeBuildError extends Error {
    readonly runtimeName: string;
    readonly exitCode: number;

    constructor(runtimeName: string, exitCode: number) {
        super(`Failed to build ${runtimeName} canvas runtime (exit ${exitCode})`);
        this.name = 'CanvasRuntimeBuildError';
        this.runtimeName = runtimeName;
        this.exitCode = exitCode;
    }
}

export function buildCanvasRuntimePair(build: CanvasRuntimeBuild): CanvasRuntimePair {
    const temporaryDirectory = fs.mkdtempSync(path.join(os.tmpdir(), 'emception-canvas-runtime-'));
    const temporaryGluePath = path.join(temporaryDirectory, `${build.runtimeName}.mjs`);
    const temporaryWasmPath = path.join(temporaryDirectory, `${build.runtimeName}.wasm`);

    try {
        const result = shell.exec(
            [
                `"${build.compiler}"`,
                `"${build.sourcePath}"`,
                ...build.libraryPaths.map((libraryPath) => `"${libraryPath}"`),
                ...build.includeDirectories.map((includeDirectory) => `-I"${includeDirectory}"`),
                ...build.flags,
                `-o "${temporaryGluePath}"`,
            ].join(' '),
            { silent: false, fatal: false },
        );

        if (result.code !== 0) {
            throw new CanvasRuntimeBuildError(build.runtimeName, result.code);
        }

        fs.mkdirSync(build.outputDirectory, { recursive: true });
        const gluePath = path.join(build.outputDirectory, `${build.runtimeName}.mjs`);
        const wasmPath = path.join(build.outputDirectory, `${build.runtimeName}.wasm`);
        fs.copyFileSync(temporaryGluePath, gluePath);
        fs.copyFileSync(temporaryWasmPath, wasmPath);
        return { gluePath, wasmPath };
    } finally {
        fs.rmSync(temporaryDirectory, { recursive: true, force: true });
    }
}
