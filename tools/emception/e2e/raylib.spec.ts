/**
 * End-to-end test: raylib C++ canvas path.
 *
 * Selects the "C++ raylib — Bouncing Ball" preset, clicks "Compile & Run",
 * and verifies:
 *   1. Status transitions through "Compiling..." → "Canvas done (…s)".
 *   2. Terminal shows "Canvas compile" and "Canvas rendering in canvas tab".
 *   3. The canvas element is visible with non-zero dimensions.
 *
 * Compilation links against precompiled libraylib.a so allow generous
 * timeouts (up to 10 minutes).
 */

import { expect, test, type Page } from '@playwright/test';

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

const terminal = (page: Page) => page.getByTestId('terminal');
const status = (page: Page) => page.getByTestId('status');
const compileBtn = (page: Page) => page.getByTestId('compile-button');
const canvasEl = (page: Page) => page.getByTestId('sdl-canvas');
const workspacePicker = (page: Page) => page.getByTestId('workspace-picker');

interface CapturedLog {
    timestamp: number;
    type: string;
    text: string;
}

function captureEmceptionLogs(page: Page): CapturedLog[] {
    const logs: CapturedLog[] = [];
    const t0 = Date.now();

    page.on('console', (msg) => {
        const text = msg.text();
        const type = msg.type();
        if (text.includes('[Emception:')) {
            logs.push({ timestamp: Date.now() - t0, type, text });
            console.log(`  +${((Date.now() - t0) / 1000).toFixed(1)}s [${type}] ${text}`);
        }
        if ((type === 'error' || type === 'warning') && !text.includes('[Emception:')) {
            logs.push({ timestamp: Date.now() - t0, type, text });
            console.log(`  [browser ${type}] ${text}`);
        }
    });

    page.on('pageerror', (err) => {
        logs.push({ timestamp: Date.now() - t0, type: 'pageerror', text: err.message });
        console.log(`  [pageerror] ${err.message}`);
    });

    return logs;
}

function dumpLogs(logs: CapturedLog[], label: string) {
    console.log(`\n===== ${label}: ${logs.length} log entries =====`);
    for (const log of logs) {
        const ts = `+${(log.timestamp / 1000).toFixed(1)}s`;
        const prefix = log.type === 'log' ? '' : ` [${log.type}]`;
        console.log(`  ${ts}${prefix} ${log.text}`);
    }
    console.log(`===== END ${label} =====\n`);
}

// ---------------------------------------------------------------------------
// Tests
// ---------------------------------------------------------------------------

test.describe('raylib C++ canvas', () => {
    test('raylib demo compiles and renders to canvas', async ({ page }) => {
        // Raylib uses clang+wasm-ld (not emcc) but links libraylib.a — allow 15 min.
        test.setTimeout(15 * 60_000);
        const logs = captureEmceptionLogs(page);

        // Start fresh — no persisted SDL/cmake workspace.
        await page.goto('/', { waitUntil: 'domcontentloaded' });
        await page.evaluate(() => localStorage.clear());
        await page.reload({ waitUntil: 'domcontentloaded' });

        // ── Boot ─────────────────────────────────────────────────────────────
        console.log('Waiting for toolchain boot (Ready status)…');
        await expect(status(page)).toHaveText('Ready', { timeout: 120_000 });
        await expect(compileBtn(page)).toBeEnabled();
        console.log('Boot complete.');

        // ── Select the raylib preset ──────────────────────────────────────────
        console.log('Selecting cpp-raylib preset…');
        await workspacePicker(page).selectOption('cpp-raylib');
        // Wait for the preset switch to settle (editor remounts).
        await page.waitForTimeout(1_000);

        // Verify raylib source in editor
        await page.waitForFunction(
            () => !!(window as any).monaco?.editor?.getModels?.()?.length,
            { timeout: 30_000 },
        );
        const editorContent: string = await page.evaluate(() => {
            const w = window as any;
            const model = w.monaco?.editor?.getModels?.()?.find(
                (m: any) => m.uri?.path?.includes('raylib-main'),
            );
            return (model?.getValue?.() as string) ?? '';
        });
        expect(editorContent).toContain('#include <raylib.h>');
        console.log('raylib demo code confirmed in editor.');

        // ── Compile ───────────────────────────────────────────────────────────
        console.log('Clicking Compile & Run…');
        await compileBtn(page).click();

        await expect(status(page)).toHaveText('Compiling...', { timeout: 10_000 });
        console.log('Compilation started. Waiting for em++ link + load (up to 10 min)…');

        await expect(status(page)).not.toHaveText('Compiling...', { timeout: 10 * 60_000 });

        const finalStatus = await status(page).textContent();
        console.log(`Final status: "${finalStatus}"`);

        if (finalStatus && /compilation failed/.test(finalStatus)) {
            dumpLogs(logs, 'RAYLIB COMPILE FAILED');
            expect(
                finalStatus,
                'raylib canvas compilation failed — check libraylib.a is in the sysroot',
            ).not.toMatch(/compilation failed/);
        }

        if (finalStatus && !/raylib done|done.*running/.test(finalStatus)) {
            dumpLogs(logs, 'UNEXPECTED STATUS');
        }

        expect(finalStatus).toMatch(/raylib done|done.*running/);
        console.log('raylib compilation succeeded!');

        // ── Terminal messages ─────────────────────────────────────────────────
        await expect(terminal(page)).toContainText('raylib detected', { timeout: 5_000 });
        await expect(terminal(page)).toContainText('rendering in canvas tab', { timeout: 5_000 });

        // ── Canvas element visible ────────────────────────────────────────────
        await expect(canvasEl(page)).toBeVisible({ timeout: 15_000 });

        const canvasSize = await canvasEl(page).boundingBox();
        console.log(`Canvas bounding box: ${JSON.stringify(canvasSize)}`);
        expect(canvasSize).not.toBeNull();
        expect(canvasSize!.width).toBeGreaterThan(100);
        expect(canvasSize!.height).toBeGreaterThan(100);

        console.log('raylib canvas is visible and has non-zero dimensions!');

        // ── WebGL context acquired ────────────────────────────────────────────
        const hasWebGL = await page.evaluate(() => {
            const c = document.querySelector<HTMLCanvasElement>('[data-testid="sdl-canvas"]');
            if (!c) return false;
            return !!(
                c.getContext('webgl2') ??
                c.getContext('webgl') ??
                c.getContext('experimental-webgl')
            );
        });
        expect(hasWebGL).toBe(true);
        console.log('raylib acquired a WebGL context.');

        dumpLogs(logs, 'raylib PASS');
    });
});
