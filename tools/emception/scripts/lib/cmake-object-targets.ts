import { spawnSync } from 'node:child_process';
import { readFileSync } from 'node:fs';
import path from 'node:path';

export interface CMakeObjectBuildOptions {
    buildDirectory: string;
    concurrency: number;
    objectDirectory: string;
    objectFiles: string[];
    targetSubdirectory: string;
}

export interface CMakeObjectBuildPlan {
    arguments: string[];
    executable: string;
    workingDirectory: string;
}

function readCacheEntry(cache: string, key: string): string {
    const prefix = `${key}:`;
    const line = cache.split(/\r?\n/u).find(entry => entry.startsWith(prefix));
    const separator = line?.indexOf('=') ?? -1;
    if (!line || separator < 0) {
        throw new Error(`CMake cache entry is missing: ${key}`);
    }
    return line.slice(separator + 1).trim();
}

export function createCMakeObjectBuildPlan(options: CMakeObjectBuildOptions): CMakeObjectBuildPlan {
    if (!Number.isSafeInteger(options.concurrency) || options.concurrency <= 0) {
        throw new Error('CMake object build concurrency must be a positive integer.');
    }
    if (options.objectFiles.length === 0) {
        throw new Error('At least one CMake object file is required.');
    }

    const cache = readFileSync(path.join(options.buildDirectory, 'CMakeCache.txt'), 'utf8');
    const generator = readCacheEntry(cache, 'CMAKE_GENERATOR');
    const makefileGenerators = new Set(['Unix Makefiles', 'MSYS Makefiles', 'MinGW Makefiles']);

    if (makefileGenerators.has(generator)) {
        return {
            executable: readCacheEntry(cache, 'CMAKE_MAKE_PROGRAM'),
            arguments: [
                '-C',
                options.targetSubdirectory,
                '-j',
                String(options.concurrency),
                ...options.objectFiles,
            ],
            workingDirectory: options.buildDirectory,
        };
    }

    const targetDirectory = path.relative(
        options.buildDirectory,
        path.join(options.targetSubdirectory, options.objectDirectory),
    ).split(path.sep).join('/');
    return {
        executable: 'cmake',
        arguments: [
            '--build',
            options.buildDirectory,
            '--parallel',
            String(options.concurrency),
            '--target',
            ...options.objectFiles.map(objectFile => `${targetDirectory}/${objectFile}`),
        ],
        workingDirectory: options.buildDirectory,
    };
}

export function buildCMakeObjectFiles(options: CMakeObjectBuildOptions): void {
    const plan = createCMakeObjectBuildPlan(options);
    const result = spawnSync(plan.executable, plan.arguments, {
        cwd: plan.workingDirectory,
        env: process.env,
        stdio: 'inherit',
    });
    if (result.error) {
        throw result.error;
    }
    if (result.status !== 0) {
        throw new Error(`${path.basename(plan.executable)} failed to build CMake object files with exit ${result.status}.`);
    }
}
