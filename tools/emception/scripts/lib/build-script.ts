/**
 * Boilerplate-eliminating wrapper for build/deploy scripts.
 *
 * Every entrypoint script in `scripts/` used to repeat the same six lines:
 *   - import shelljs, set `shell.config.fatal = true`
 *   - import + call `enableBuildKeepalive(label)`
 *   - import + call `setupEmsdk(EMSDK_VERSION)` (when emsdk is needed)
 *   - resolve `ROOT = process.cwd()`
 *   - wrap the body in try/catch for consistent error reporting
 *
 * `defineBuildScript` collapses all of that into one declarative call:
 *
 *     defineBuildScript({
 *         label: 'build-ninja',
 *         requireEmsdk: true,
 *         run: async ({ root, log, step }) => {
 *             await step('configure', () => shell.exec('emcmake cmake ...'));
 *             await step('compile',   () => shell.exec('emmake make -j8'));
 *         },
 *     });
 *
 * Benefits:
 *   - One place to evolve cross-cutting concerns (logging, timing, CI flags).
 *   - Newcomers writing a new build step copy ~5 lines, not ~25.
 *   - Per-step timing makes it obvious which phase of a long build is slow.
 */

import shell from 'shelljs';
import { setupEmsdk } from './emsdk.ts';
import { enableBuildKeepalive } from './keepalive.ts';
import { PINNED } from './pinned-versions.ts';

export interface BuildScriptContext {
    /** Repository-root-relative working directory (= `process.cwd()` at script start). */
    readonly root: string;
    /** Resolved emsdk version, or `null` when `requireEmsdk` was false. */
    readonly emsdkVersion: string | null;
    /** Structured logger. Honours `EMCEPTION_LOG_LEVEL` (debug|info|warn|error). */
    log(message: string, ...rest: unknown[]): void;
    /** Wrap a phase of the build with a banner + duration measurement. */
    step<T>(name: string, fn: () => T | Promise<T>): Promise<T>;
}

export interface BuildScriptOptions {
    /** Short label used for keepalive heartbeats and step banners. */
    label: string;
    /** When true, calls `setupEmsdk(EMSDK_VERSION)` before `run`. Default: false. */
    requireEmsdk?: boolean;
    /** Disable `shell.config.fatal`. Default: leave fatal=true. */
    nonFatalShell?: boolean;
    /** The actual build logic. */
    run: (ctx: BuildScriptContext) => void | Promise<void>;
}

function formatDuration(ms: number): string {
    if (ms < 1000) return `${ms}ms`;
    const s = ms / 1000;
    if (s < 60) return `${s.toFixed(1)}s`;
    const m = Math.floor(s / 60);
    const rem = (s - m * 60).toFixed(1);
    return `${m}m${rem}s`;
}

/**
 * Define and immediately execute a build script.
 *
 * Resolves the returned promise on success and calls `process.exit(1)` on
 * any thrown error so that `run-s` chains halt at the failed step.
 */
export function defineBuildScript(options: BuildScriptOptions): Promise<void> {
    const { label, requireEmsdk = false, nonFatalShell = false, run } = options;

    enableBuildKeepalive(label);

    if (!nonFatalShell) {
        shell.config.fatal = true;
    }

    const root = process.cwd();
    const emsdkVersion = requireEmsdk
        ? (process.env.EMSDK_VERSION || PINNED.EMSDK_VERSION)
        : null;
    if (requireEmsdk && emsdkVersion) {
        setupEmsdk(emsdkVersion);
    }

    const log = (message: string, ...rest: unknown[]) => {
        console.log(`[${label}] ${message}`, ...rest);
    };

    const step = async <T>(name: string, fn: () => T | Promise<T>): Promise<T> => {
        log(`▶ ${name}`);
        const start = Date.now();
        try {
            const result = await fn();
            log(`✓ ${name} (${formatDuration(Date.now() - start)})`);
            return result;
        } catch (err) {
            log(`✗ ${name} (${formatDuration(Date.now() - start)})`);
            throw err;
        }
    };

    const ctx: BuildScriptContext = { root, emsdkVersion, log, step };

    return Promise.resolve()
        .then(() => run(ctx))
        .then(() => {
            log('done');
        })
        .catch((err: unknown) => {
            const msg = err instanceof Error ? (err.stack || err.message) : String(err);
            console.error(`[${label}] FAILED: ${msg}`);
            process.exit(1);
        });
}
