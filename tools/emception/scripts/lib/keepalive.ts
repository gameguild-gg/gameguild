/**
 * Cross-platform build-output keepalive.
 *
 * Long-running build steps (LLVM, CPython, CMake) can be silent for many
 * minutes, which causes CI runners (e.g. GitHub Actions) to assume the job
 * is hung and cancel it. This helper spawns a small detached Node child
 * that periodically prints a heartbeat line to stdout. It works the same
 * on Linux, macOS, and Windows because it spawns `node` directly — no
 * shell, no bash, no PowerShell wrapping required.
 *
 * Behaviour:
 *   - Auto-enabled when `CI=true` (GitHub Actions, GitLab, etc.)
 *   - Force-enable with `EMCEPTION_BUILD_KEEPALIVE=1`
 *   - Disable with `EMCEPTION_BUILD_KEEPALIVE=0`
 *   - Interval (seconds) tunable via `EMCEPTION_BUILD_KEEPALIVE_SECS`
 *
 * The child inherits the parent's stdout/stderr so its output appears
 * inline with the build log. It is killed when the parent process exits.
 */

import { spawn } from 'child_process';

let started = false;

export function enableBuildKeepalive(label: string): void {
    if (started) return;
    started = true;

    const explicitlyEnabled = process.env.EMCEPTION_BUILD_KEEPALIVE === '1';
    const explicitlyDisabled = process.env.EMCEPTION_BUILD_KEEPALIVE === '0';
    const enabled = explicitlyEnabled || (!explicitlyDisabled && process.env.CI === 'true');
    if (!enabled) return;

    const rawSecs = Number(process.env.EMCEPTION_BUILD_KEEPALIVE_SECS);
    const secs = Number.isFinite(rawSecs) && rawSecs > 0 ? Math.floor(rawSecs) : 60;

    // Heartbeat program runs in a separate Node process so it isn't blocked
    // by synchronous shell.exec calls in the parent.
    const childCode = [
        "const label = process.argv[1];",
        "const secs = Number(process.argv[2]);",
        "const start = Date.now();",
        "const tick = () => {",
        "  const elapsed = Math.floor((Date.now() - start) / 1000);",
        "  process.stdout.write('[keepalive][' + label + '] alive ' + elapsed + 's @ ' + new Date().toISOString() + '\\n');",
        "};",
        "setInterval(tick, secs * 1000);",
    ].join('\n');

    const child = spawn(process.execPath, ['-e', childCode, label, String(secs)], {
        stdio: ['ignore', 'inherit', 'inherit'],
        windowsHide: true,
    });

    // Don't let the heartbeat keep the parent event loop alive.
    child.unref();

    const cleanup = () => {
        try { child.kill(); } catch { /* ignore */ }
    };
    process.once('exit', cleanup);
    process.once('SIGINT', () => { cleanup(); process.exit(130); });
    process.once('SIGTERM', () => { cleanup(); process.exit(143); });

    console.log(`[keepalive] enabled for ${label} (interval: ${secs}s, child pid: ${child.pid ?? 'n/a'})`);
}
