/**
 * End-to-end test: Compile & Run button → xterm output.
 *
 * This test boots the Emception web IDE in a real browser, waits for the
 * WASM toolchain to become ready, clicks "Compile & Run", and verifies
 * the expected output ("Hello from WebAssembly!") appears in the xterm
 * terminal.  It also demonstrates interactive terminal input.
 *
 * The WASM boot can take 30-60 s (first run), and emcc compilation is
 * typically 10-60 s on a fast machine, so timeouts are generous.
 *
 * All [Emception:*] browser console logs are captured and dumped after
 * each test for full pipeline visibility: boot → VFS → WASM loading →
 * process spawn → compilation → WASM generation → execution.
 *
 * NOTE: xterm.js 5.x uses a canvas renderer — the visible text is painted
 * on a <canvas>, not in DOM text nodes.  The only DOM-accessible text lives
 * in xterm's accessibility / live-region layer.  We use Playwright's
 * `toContainText()` which delegates to the accessibility tree, so it sees
 * the same text as the page snapshot.
 */

import { expect, test, type Page } from '@playwright/test';

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

/** Locator for the terminal container. */
const terminal = (page: Page) => page.getByTestId('terminal');

/** Locator for the status badge. */
const status = (page: Page) => page.getByTestId('status');

/** Locator for the compile button. */
const compileBtn = (page: Page) => page.getByTestId('compile-button');

/**
 * Focus the interactive TTYBridge terminal.
 *
 * There is now a single xterm Terminal instance shared between Ide.tsx and
 * TTYBridge, so there is only one `textarea[aria-label="Terminal input"]`.
 */
async function focusShellTerminal(page: Page) {
    const textarea = page.locator('textarea[aria-label="Terminal input"]');
    await textarea.focus();
}

interface CapturedLog {
    timestamp: number;
    type: string;
    text: string;
}

/**
 * Set up browser console log capture for [Emception:*] messages.
 * Returns an array that will be populated with captured log entries.
 * ALL [Emception:*] logs are printed in real-time for debugging.
 */
function captureEmceptionLogs(page: Page): CapturedLog[] {
    const logs: CapturedLog[] = [];
    const t0 = Date.now();

    page.on('console', (msg) => {
        const text = msg.text();
        const type = msg.type();

        // Capture and print ALL [Emception:*] logs in real-time
        if (text.includes('[Emception:')) {
            const ts = `+${((Date.now() - t0) / 1000).toFixed(1)}s`;
            logs.push({ timestamp: Date.now() - t0, type, text });
            console.log(`  ${ts} [${type}] ${text}`);
        }

        // Also capture errors/warnings for debugging
        if (type === 'error' || type === 'warning') {
            if (!text.includes('[Emception:')) {
                logs.push({ timestamp: Date.now() - t0, type, text });
                console.log(`  [browser ${type}] ${text}`);
            }
        }
    });

    page.on('pageerror', (err) => {
        logs.push({ timestamp: Date.now() - t0, type: 'pageerror', text: err.message });
        console.log(`  [browser pageerror] ${err.message}`);
    });

    return logs;
}

/** Dump captured logs to stdout with relative timestamps. */
function dumpLogs(logs: CapturedLog[], label: string) {
    console.log(`\n===== ${label}: ${logs.length} log entries =====`);
    for (const log of logs) {
        const ts = `+${(log.timestamp / 1000).toFixed(1)}s`;
        const prefix = log.type === 'log' ? '' : ` [${log.type}]`;
        console.log(`  ${ts}${prefix} ${log.text}`);
    }
    console.log(`===== END ${label} =====\n`);
}

/** Check that a specific [Emception:*] milestone appears in the logs. */
function assertLogContains(logs: CapturedLog[], pattern: string, label: string) {
    const found = logs.some(l => l.text.includes(pattern));
    if (!found) {
        dumpLogs(logs, `MISSING MILESTONE: ${label}`);
    }
    expect(found, `Expected log milestone: "${label}" (pattern: "${pattern}")`).toBe(true);
}

// ---------------------------------------------------------------------------
// Tests
// ---------------------------------------------------------------------------

test.describe('Compile & Run', () => {
    // ------------------------------------------------------------------
    // 1. Default hello-world — full compile + run pipeline with log verification
    // ------------------------------------------------------------------
    test('default hello-world compiles and prints output', async ({ page }) => {
        const logs = captureEmceptionLogs(page);

        // Navigate and wait for the page to fully load
        await page.goto('/', { waitUntil: 'networkidle' });

        try {
            // Wait for the toolchain to boot
            console.log('Waiting for boot (Ready status)...');
            await expect(status(page)).toHaveText('Ready', { timeout: 120_000 });
            await expect(compileBtn(page)).toBeEnabled();

            // Verify boot milestones in logs
            assertLogContains(logs, 'BOOT START', 'Boot started');
            assertLogContains(logs, 'BOOT COMPLETE', 'Boot completed');

            // The MiniShell banner should already be in the terminal
            await expect(terminal(page)).toContainText('Browser Toolchain Shell', { timeout: 10_000 });

            // Switch to cpp-terminal workspace so the compile path uses WASI (not SDL3)
            await page.getByTestId('workspace-picker').selectOption('cpp-terminal');

            // Override the editor with a simple hello-world (no stdin) so the
            // test doesn't depend on whichever DEFAULT_CODE is in Ide.tsx.
            await page.evaluate(() => {
                const model = (window as any).monaco?.editor?.getModels?.()?.[0];
                if (model) {
                    model.setValue([
                        '#include <iostream>',
                        'int main() {',
                        '  std::cout << "Hello from WebAssembly!" << std::endl;',
                        '  return 0;',
                        '}',
                    ].join('\n'));
                }
            });

            console.log('Boot complete. Clicking Compile & Run...');

            // Click "Compile & Run"
            await compileBtn(page).click();

            // Status should transition to Compiling
            await expect(status(page)).toHaveText('Compiling...', { timeout: 5_000 });
            console.log('Compilation started. Waiting for completion (up to 5 min)...');

            // Wait for status to change from "Compiling..." to anything else
            // (could be success, failure, or error)
            await expect(status(page)).not.toHaveText('Compiling...', { timeout: 300_000 });

            const finalStatus = await status(page).textContent();
            console.log(`Final status: "${finalStatus}"`);

            // If it's an error state, dump logs and fail with the error message
            if (finalStatus && /Error/.test(finalStatus)) {
                dumpLogs(logs, 'COMPILE ERROR');
                expect(finalStatus, 'Compilation ended with error status').not.toMatch(/Error/);
            }

            // Dump logs for debugging if not successful
            if (finalStatus && !/Compilation successful/.test(finalStatus)) {
                dumpLogs(logs, 'COMPILE NOT SUCCESSFUL');
            }

            // Verify it was successful
            expect(finalStatus).toMatch(/Compilation successful/);

            console.log('Compilation successful! Checking for output...');

            // Verify the program output appears in the terminal
            await expect(terminal(page)).toContainText('Hello from WebAssembly!', { timeout: 60_000 });

            console.log('Output verified!');

            // Dump full log timeline
            dumpLogs(logs, 'HELLO-WORLD PIPELINE');

            // Verify key pipeline milestones from the logs
            // Boot
            assertLogContains(logs, 'Step 1/6 done: manifest loaded', 'Manifest loaded');
            assertLogContains(logs, 'Step 2/6 done: LazyFS ready', 'LazyFS initialized');
            assertLogContains(logs, 'Step 5/6 done: all components created', 'Components created');

            // Compile (emcc run)
            assertLogContains(logs, 'COMPILE & RUN START', 'IDE compile started');
            assertLogContains(logs, 'RUN: emcc', 'emcc tool run started');
            assertLogContains(logs, 'Step 1/4', 'Module factory loading');
            assertLogContains(logs, 'Step 2/4', 'Process instantiation');
            assertLogContains(logs, 'Step 3/4', 'Process FS population');
            assertLogContains(logs, 'Step 4/4', 'callMain execution');
            assertLogContains(logs, 'RUN COMPLETE: emcc', 'emcc completed');

            // Compilation output
            assertLogContains(logs, 'Compilation output: main.wasm=', 'Output files generated');

            // Execution (standalone WASI runtime)
            assertLogContains(logs, 'WASI RUN', 'WASI runtime started');
            assertLogContains(logs, 'WASI COMPLETE', 'WASI execution completed');

            assertLogContains(logs, 'COMPILE & RUN COMPLETE', 'Full pipeline completed');

        } finally {
            // Always dump logs, even on failure
            if (logs.length > 0) {
                dumpLogs(logs, 'FINAL LOG DUMP (test end)');
            }
        }
    });

    // ------------------------------------------------------------------
    // 2. Terminal is interactive after boot
    // ------------------------------------------------------------------
    test('terminal is interactive after boot', async ({ page }) => {
        const logs = captureEmceptionLogs(page);

        await page.goto('/', { waitUntil: 'networkidle' });
        await expect(status(page)).toHaveText('Ready', { timeout: 120_000 });

        // Verify the MiniShell banner is in the terminal log
        const term = terminal(page);
        await expect(term).toContainText('Browser Toolchain Shell', { timeout: 10_000 });
        await expect(term).toContainText('Type "help" for available commands', { timeout: 5_000 });

        // Focus the TTYBridge terminal (the second xterm textarea)
        await focusShellTerminal(page);

        // Type a command — MiniShell echoes each character and processes on Enter
        await page.keyboard.type('echo hello', { delay: 50 });
        await page.keyboard.press('Enter');

        // MiniShell's echo builtin writes "hello" via tty.writeLine
        await expect(term).toContainText('hello', { timeout: 15_000 });

        dumpLogs(logs, 'INTERACTIVE TERMINAL');
        assertLogContains(logs, 'BOOT COMPLETE', 'Boot completed');
    });

    // ------------------------------------------------------------------
    // 3. stdin — compile a program that reads from stdin, type input,
    //    verify the program echoes it back.
    // ------------------------------------------------------------------
    test('stdin works — program reads user input', async ({ page }) => {
        const logs = captureEmceptionLogs(page);

        await page.goto('/', { waitUntil: 'networkidle' });
        await expect(status(page)).toHaveText('Ready', { timeout: 120_000 });
        await expect(compileBtn(page)).toBeEnabled();

        // Switch to cpp-terminal workspace so the compile path uses WASI (not SDL3)
        await page.getByTestId('workspace-picker').selectOption('cpp-terminal');

        // Replace editor content with a C program that reads stdin
        const stdinProgram = [
            '#include <stdio.h>',
            'int main() {',
            '    char buf[64];',
            '    printf("PROMPT:\\n");',
            '    fflush(stdout);',
            '    if (fgets(buf, sizeof buf, stdin)) {',
            '        printf("GOT:%s\\n", buf);',
            '    } else {',
            '        printf("GOT:EOF\\n");',
            '    }',
            '    return 0;',
            '}',
        ].join('\n');

        // Set editor content via Monaco API
        await page.evaluate((code) => {
            // Monaco editor is accessible via the first editor instance
            const model = (window as any).monaco?.editor?.getModels?.()?.[0];
            if (model) {
                model.setValue(code);
            }
        }, stdinProgram);

        // Click compile & run
        console.log('Compiling stdin test program...');
        await compileBtn(page).click();
        await expect(status(page)).toHaveText('Compiling...', { timeout: 5_000 });
        await expect(status(page)).not.toHaveText('Compiling...', { timeout: 300_000 });

        const finalStatus = await status(page).textContent();
        console.log(`Compilation status: "${finalStatus}"`);
        expect(finalStatus).toMatch(/Compilation successful/);

        // Wait for "PROMPT:" in the terminal (program is now waiting for stdin)
        const term = terminal(page);
        await expect(term).toContainText('PROMPT:', { timeout: 60_000 });
        console.log('Program printed PROMPT — now typing input...');

        // Focus the terminal and type input
        await focusShellTerminal(page);
        await page.keyboard.type('hello', { delay: 50 });
        await page.keyboard.press('Enter');

        // The program should echo back what we typed
        await expect(term).toContainText('GOT:hello', { timeout: 30_000 });
        console.log('stdin test passed — program received input!');

        // Verify fd_read was actually called (diagnostic log)
        assertLogContains(logs, 'fd_read called', 'fd_read was invoked');
        assertLogContains(logs, 'WASI COMPLETE', 'WASI execution completed');

        dumpLogs(logs, 'STDIN TEST');
    });

    // ------------------------------------------------------------------
    // 4. stdin backspace — verify that backspace editing works correctly
    //    so the WASM program receives the edited text, not raw bytes.
    // ------------------------------------------------------------------
    test('stdin backspace editing works correctly', async ({ page }) => {
        const logs = captureEmceptionLogs(page);

        await page.goto('/', { waitUntil: 'networkidle' });
        await expect(status(page)).toHaveText('Ready', { timeout: 120_000 });
        await expect(compileBtn(page)).toBeEnabled();

        // Switch to cpp-terminal workspace so the compile path uses WASI (not SDL3)
        await page.getByTestId('workspace-picker').selectOption('cpp-terminal');

        // C program that prints what it reads from stdin
        await page.evaluate((code) => {
            const model = (window as any).monaco?.editor?.getModels?.()?.[0];
            if (model) model.setValue(code);
        }, [
            '#include <stdio.h>',
            'int main() {',
            '    char buf[64];',
            '    printf("PROMPT:\\n");',
            '    fflush(stdout);',
            '    if (fgets(buf, sizeof buf, stdin)) {',
            '        printf("GOT:%s", buf);',
            '    }',
            '    return 0;',
            '}',
        ].join('\n'));

        // Compile & run
        await compileBtn(page).click();
        await expect(status(page)).toHaveText('Compiling...', { timeout: 5_000 });
        await expect(status(page)).not.toHaveText('Compiling...', { timeout: 300_000 });
        expect(await status(page).textContent()).toMatch(/Compilation successful/);

        // Wait for the program to prompt for input
        const term = terminal(page);
        await expect(term).toContainText('PROMPT:', { timeout: 60_000 });

        // Type "alu", backspace to delete 'u', then "ex" → should yield "alex"
        await focusShellTerminal(page);
        await page.keyboard.type('alu', { delay: 50 });
        await page.keyboard.press('Backspace');
        await page.keyboard.type('ex', { delay: 50 });
        await page.keyboard.press('Enter');

        // The program should print the edited text, not the raw keystrokes
        await expect(term).toContainText('GOT:alex', { timeout: 30_000 });

        dumpLogs(logs, 'BACKSPACE TEST');
    });
});
