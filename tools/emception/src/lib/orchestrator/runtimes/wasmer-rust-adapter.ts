// @wasmer/wasi v1.x is dynamically imported to avoid breaking Next.js SSR/webpack
// (the published package is missing its WASM binary; a graceful fallback handles unavailability).
// TODO: migrate to @wasmer/sdk when upgrading Wasmer support.
import type { ToolResult } from '../tool-runner';
import type { RuntimeAdapter, RuntimeAdapterContext } from './runtime-adapter';

type WasmerWasiModule = typeof import('@wasmer/wasi');

export class WasmerRustAdapter implements RuntimeAdapter {
    readonly name = 'wasmer-browser';

    private static wasmerModulePromise: Promise<WasmerWasiModule | null> | null = null;

    private static async tryLoadWasmer(): Promise<WasmerWasiModule | null> {
        if (!this.wasmerModulePromise) {
            this.wasmerModulePromise = (async () => {
                try {
                    // webpackIgnore keeps webpack/Next.js from statically bundling this import
                    const mod: WasmerWasiModule = await import(/* webpackIgnore: true */ '@wasmer/wasi');
                    await mod.init();
                    return mod;
                } catch {
                    return null;
                }
            })();
        }
        return this.wasmerModulePromise;
    }

    private static async ensureWasmerInitialized(): Promise<WasmerWasiModule> {
        const mod = await WasmerRustAdapter.tryLoadWasmer();
        if (!mod) {
            throw new Error(
                '@wasmer/wasi runtime not available (WASM binary missing or load failed)',
            );
        }
        return mod;
    }

    async run(context: RuntimeAdapterContext): Promise<ToolResult> {
        try {
            context.log('Rust runtime=wasmer-browser (adapter path)');
            return await this.runViaWasmer(context);
        } catch (error) {
            const message = error instanceof Error ? error.message : String(error);
            context.log(`Wasmer adapter failed: ${message}`);
            context.log('Falling back to internal WASI runtime for rustc');

            const fallback = await context.runWasiFallback(
                ['wasi-run', context.modulePath, ...context.argv.slice(1)],
                { ...context.options, enableFS: true },
            );

            const fallbackStderr = fallback.stderr
                ? `[wasmer-fallback] ${message}\n${fallback.stderr}`
                : `[wasmer-fallback] ${message}`;

            return {
                ...fallback,
                stderr: fallbackStderr,
            };
        }
    }

    private async runViaWasmer(context: RuntimeAdapterContext): Promise<ToolResult> {
        const { WASI } = await WasmerRustAdapter.ensureWasmerInitialized();

        const wasmBytes = await context.vfs.fetchFile(context.modulePath);
        if (!wasmBytes) {
            throw new Error(`rustc module not found at ${context.modulePath}`);
        }

        const wasmBinary = wasmBytes.buffer.slice(
            wasmBytes.byteOffset,
            wasmBytes.byteOffset + wasmBytes.byteLength,
        );

        const target = this.resolveRustTarget(context.argv);
        const preloadedFiles = await this.preloadRustFiles(context, target);

        const args = ['/usr/lib/rust/bin/rustc', ...context.argv.slice(1)];
        const env = {
            SYSROOT: '/usr/lib/rust',
            RUSTC_ICE: '0',
            HOME: '/home/user',
            TMPDIR: '/tmp',
        };

        const wasi = new (WASI as unknown as new (config: unknown) => any)({ args, env });

        for (const [path, data] of preloadedFiles) {
            this.writeFileToWasmerFs(wasi, path, data);
        }

        const module = await WebAssembly.compile(wasmBinary);
        const instance = await wasi.instantiate(module, {});
        const exitCode = Number(await wasi.start(instance));

        const stdout = String(wasi.getStdoutString?.() ?? '');
        const stderr = String(wasi.getStderrString?.() ?? '');

        if (stdout) context.options.onStdout?.(stdout);
        if (stderr) context.options.onStderr?.(stderr);

        const outputPath = this.resolveOutputPath(context.argv, context.options.cwd);
        if (outputPath) {
            const output = this.readFileFromWasmerFs(wasi, outputPath);
            if (output && output.length > 0) {
                context.vfs.writeFileSync(outputPath, output);
                context.log(`Wasmer persisted output artifact: ${outputPath} (${output.length}B)`);
            }
        }

        return {
            exitCode,
            stdout,
            stderr,
        };
    }

    private resolveRustTarget(argv: string[]): string {
        const targetIdx = argv.findIndex((arg) => arg === '--target');
        if (targetIdx >= 0 && targetIdx + 1 < argv.length) {
            return argv[targetIdx + 1] || 'wasm32-wasip1';
        }

        const inlineTarget = argv.find((arg) => arg.startsWith('--target='));
        if (inlineTarget) {
            return inlineTarget.slice('--target='.length) || 'wasm32-wasip1';
        }

        return 'wasm32-wasip1';
    }

    private async preloadRustFiles(
        context: RuntimeAdapterContext,
        target: string,
    ): Promise<Map<string, Uint8Array>> {
        const files = new Map<string, Uint8Array>();
        const bundleName = context.vfs.getBundleForFile(context.modulePath) ?? 'rustc';

        await context.vfs.preloadBundle(bundleName);

        const allFilePaths = context.vfs.getBundleFilePaths(bundleName);
        const selected = allFilePaths.filter((path) => {
            if (!path.includes('/rustlib/')) return true;
            return path.includes(`/rustlib/${target}/`);
        });

        const BATCH = 8;
        for (let i = 0; i < selected.length; i += BATCH) {
            const batch = selected.slice(i, i + BATCH).filter((path) => !path.endsWith('rustc.wasm'));
            const results = await Promise.all(batch.map((path) => context.vfs.fetchFile(path)));
            for (let j = 0; j < batch.length; j++) {
                const data = results[j];
                if (data) files.set(batch[j]!, data);
            }
        }

        for (const arg of context.argv) {
            if (!arg.startsWith('/') || arg.startsWith('--')) continue;
            if (files.has(arg)) continue;
            const data = await context.vfs.fetchFile(arg);
            if (data) files.set(arg, data);
        }

        context.log(`Wasmer preloaded ${files.size} rust files (target=${target})`);
        return files;
    }

    private resolveOutputPath(argv: string[], cwd?: string): string | null {
        const outIndex = argv.lastIndexOf('-o');
        if (outIndex < 0 || outIndex + 1 >= argv.length) return null;
        const raw = argv[outIndex + 1];
        if (!raw) return null;
        if (raw.startsWith('/')) return raw;
        const base = cwd && cwd.startsWith('/') ? cwd : '/home/user';
        return `${base.replace(/\/$/, '')}/${raw}`;
    }

    private ensureWasmerDir(wasi: any, dirPath: string): void {
        if (!dirPath || dirPath === '/') return;
        const parts = dirPath.split('/').filter(Boolean);
        let current = '';
        for (const part of parts) {
            current += `/${part}`;
            try {
                wasi.fs.createDir(current);
            } catch {
                // already exists
            }
        }
    }

    private writeFileToWasmerFs(wasi: any, path: string, data: Uint8Array): void {
        const lastSlash = path.lastIndexOf('/');
        const dirPath = lastSlash > 0 ? path.slice(0, lastSlash) : '/';
        this.ensureWasmerDir(wasi, dirPath);
        try {
            const file = wasi.fs.open(path, { create: true, write: true });
            file.seek?.(0);
            file.write(data);
            file.flush?.();
        } catch {
            // best effort: missing files can still be lazily opened by fallback path
        }
    }

    private readFileFromWasmerFs(wasi: any, path: string): Uint8Array | null {
        try {
            const file = wasi.fs.open(path, { read: true });
            const content = file.read();
            return content instanceof Uint8Array ? content : null;
        } catch {
            return null;
        }
    }
}
