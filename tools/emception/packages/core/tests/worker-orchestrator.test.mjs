/**
 * WorkerOrchestrator tests — real MessageChannel pairs, no mocks.
 *
 * Each test creates a MessageChannel where:
 *   - port1 is wrapped in messagePortTransport() and given to WorkerOrchestrator
 *   - port2 simulates the "worker side" — receives requests, sends responses
 *
 * Timing notes (macOS):
 *   - Cross-port message delivery can be delayed; setTimeout(r, 20) is used
 *     instead of setImmediate() for reliable delivery.
 *   - All ports are unref()'d to prevent the test runner from hanging.
 */

import assert from 'node:assert/strict';
import { test } from 'node:test';
import { MessageChannel } from 'node:worker_threads';
import { messagePortTransport, WorkerOrchestrator } from '../dist/index.js';

/* ------------------------------------------------------------------ */
/*  Helpers                                                             */
/* ------------------------------------------------------------------ */

/** Create a MessageChannel pair. */
function pair() {
    const ch = new MessageChannel();
    return ch;
}

/** Wrap a port as a WorkerOrchestrator, close port2 on cleanup. */
function makeOrch(port1, opts = {}) {
    const transport = messagePortTransport(port1);
    return new WorkerOrchestrator(transport, opts);
}

/** Wait for a message on port2 with a timeout. */
function nextMsg(port, timeoutMs = 500) {
    return new Promise((resolve, reject) => {
        const timer = setTimeout(() => {
            port.off('message', handler);
            reject(new Error(`nextMsg timed out after ${timeoutMs}ms`));
        }, timeoutMs);
        function handler(msg) {
            clearTimeout(timer);
            port.off('message', handler);
            resolve(msg);
        }
        port.on('message', handler);
    });
}

/** Delay helper. */
const delay = (ms) => new Promise((r) => setTimeout(r, ms));

/* ------------------------------------------------------------------ */
/*  Boot tests                                                          */
/* ------------------------------------------------------------------ */

test('boot — resolves when worker sends booted', async () => {
    const ch = pair();
    const orch = makeOrch(ch.port1);

    // Start the boot and capture the sent message
    const bootPromise = orch.boot('http://localhost/manifest.json', { origin: 'http://localhost' });

    const msg = await nextMsg(ch.port2);
    assert.equal(msg.type, 'boot');
    assert.equal(msg.manifestUrl, 'http://localhost/manifest.json');
    assert.equal(msg.origin, 'http://localhost');

    // Worker side sends booted
    ch.port2.postMessage({ type: 'booted' });

    await bootPromise; // should resolve
    await orch.dispose();
    ch.port2.close();
});

test('boot — rejects when worker sends bootError', async () => {
    const ch = pair();
    const orch = makeOrch(ch.port1);

    const bootPromise = orch.boot('http://localhost/manifest.json');

    await nextMsg(ch.port2); // consume the boot message

    ch.port2.postMessage({ type: 'bootError', error: 'WASM load failed' });

    await assert.rejects(bootPromise, (err) => {
        assert.match(err.message, /WASM load failed/);
        return true;
    });
    await orch.dispose();
    ch.port2.close();
});

test('boot — dispose mid-boot cancels the promise', async () => {
    const ch = pair();
    const orch = makeOrch(ch.port1);

    const bootPromise = orch.boot('http://localhost/manifest.json');

    await nextMsg(ch.port2); // consume boot message (don't reply)

    // Dispose before worker responds
    await orch.dispose(new Error('cancelled'));

    await assert.rejects(bootPromise); // should reject
    ch.port2.close();
});

test('boot — toolVersions propagated to worker', async () => {
    const ch = pair();
    const orch = makeOrch(ch.port1);

    const bootPromise = orch.boot('http://localhost/manifest.json', {
        toolVersions: { pythonMajorMinor: '3.11', pythonMajorMinorCompact: '311' },
    });

    const msg = await nextMsg(ch.port2);
    assert.deepEqual(msg.toolVersions, { pythonMajorMinor: '3.11', pythonMajorMinorCompact: '311' });

    ch.port2.postMessage({ type: 'booted' });
    await bootPromise;
    await orch.dispose();
    ch.port2.close();
});

/* ------------------------------------------------------------------ */
/*  run() tests                                                         */
/* ------------------------------------------------------------------ */

test('run — request/response round-trip', async () => {
    const ch = pair();
    const orch = makeOrch(ch.port1);

    const runPromise = orch.run('clang', ['/home/user/main.c', '-o', '/home/user/a.out']);

    const msg = await nextMsg(ch.port2);
    assert.equal(msg.type, 'run');
    assert.equal(msg.tool, 'clang');
    assert.deepEqual(msg.argv, ['/home/user/main.c', '-o', '/home/user/a.out']);
    const runId = msg.id;

    ch.port2.postMessage({ type: 'runResult', id: runId, exitCode: 0, stdout: '', stderr: '' });

    const result = await runPromise;
    assert.equal(result.exitCode, 0);
    assert.equal(result.stdout, '');
    assert.equal(result.stderr, '');
    assert.equal(result.timedOut, false);
    assert.equal(result.durationMs, 0);

    await orch.dispose();
    ch.port2.close();
});

test('run — non-zero exit code preserved', async () => {
    const ch = pair();
    const orch = makeOrch(ch.port1);

    const runPromise = orch.run('clang', ['/bad.c']);

    const msg = await nextMsg(ch.port2);
    ch.port2.postMessage({
        type: 'runResult',
        id: msg.id,
        exitCode: 1,
        stdout: '',
        stderr: 'error: file not found\n',
    });

    const result = await runPromise;
    assert.equal(result.exitCode, 1);
    assert.equal(result.stderr, 'error: file not found\n');

    await orch.dispose();
    ch.port2.close();
});

test('run — per-run onStdout/onStderr callbacks called', async () => {
    const ch = pair();
    const orch = makeOrch(ch.port1);

    const stdoutLines = [];
    const stderrLines = [];

    const runPromise = orch.run('clang', ['/home/user/main.c'], {
        onStdout: (t) => stdoutLines.push(t),
        onStderr: (t) => stderrLines.push(t),
    });

    const msg = await nextMsg(ch.port2);
    const runId = msg.id;

    // Worker sends incremental output
    ch.port2.postMessage({ type: 'stdout', id: runId, text: 'hello\n' });
    ch.port2.postMessage({ type: 'stderr', id: runId, text: 'warning\n' });
    ch.port2.postMessage({ type: 'stdout', id: runId, text: 'world\n' });

    // Give messages time to arrive before the final result
    await delay(20);
    ch.port2.postMessage({ type: 'runResult', id: runId, exitCode: 0, stdout: 'hello\nworld\n', stderr: 'warning\n' });

    const result = await runPromise;
    assert.equal(result.exitCode, 0);

    // Per-run callbacks should have been called
    assert.deepEqual(stdoutLines, ['hello\n', 'world\n']);
    assert.deepEqual(stderrLines, ['warning\n']);

    await orch.dispose();
    ch.port2.close();
});

test('run — global onStdout/onStderr options also called', async () => {
    const ch = pair();

    const globalStdout = [];
    const globalStderr = [];
    const orch = makeOrch(ch.port1, {
        onStdout: (id, t) => globalStdout.push({ id, t }),
        onStderr: (id, t) => globalStderr.push({ id, t }),
    });

    const runPromise = orch.run('clang', ['/home/user/main.c']);

    const msg = await nextMsg(ch.port2);
    const runId = msg.id;

    ch.port2.postMessage({ type: 'stdout', id: runId, text: 'output\n' });
    ch.port2.postMessage({ type: 'stderr', id: runId, text: 'err\n' });

    await delay(20);
    ch.port2.postMessage({ type: 'runResult', id: runId, exitCode: 0, stdout: 'output\n', stderr: 'err\n' });

    await runPromise;

    assert.deepEqual(globalStdout, [{ id: runId, t: 'output\n' }]);
    assert.deepEqual(globalStderr, [{ id: runId, t: 'err\n' }]);

    await orch.dispose();
    ch.port2.close();
});

test('run — env and cwd propagated to worker', async () => {
    const ch = pair();
    const orch = makeOrch(ch.port1);

    const runPromise = orch.run('clang', ['/home/user/main.c'], {
        env: { CC: 'clang', OPTFLAGS: '-O2' },
        cwd: '/home/user',
    });

    const msg = await nextMsg(ch.port2);
    assert.deepEqual(msg.options.env, { CC: 'clang', OPTFLAGS: '-O2' });
    assert.equal(msg.options.cwd, '/home/user');
    assert.equal(msg.options.wantStdin, false);

    ch.port2.postMessage({ type: 'runResult', id: msg.id, exitCode: 0, stdout: '', stderr: '' });
    await runPromise;

    await orch.dispose();
    ch.port2.close();
});

test('run — wantStdin forwarded', async () => {
    const ch = pair();
    const orch = makeOrch(ch.port1);

    const runPromise = orch.run('wasi-run', ['/home/user/a.out'], { wantStdin: true });

    const msg = await nextMsg(ch.port2);
    assert.equal(msg.options.wantStdin, true);

    ch.port2.postMessage({ type: 'runResult', id: msg.id, exitCode: 0, stdout: '', stderr: '' });
    await runPromise;

    await orch.dispose();
    ch.port2.close();
});

/* ------------------------------------------------------------------ */
/*  VFS tests                                                           */
/* ------------------------------------------------------------------ */

test('getFile — returns null for missing file', async () => {
    const ch = pair();
    const orch = makeOrch(ch.port1);

    const getPromise = orch.getFile('/not/there');

    const msg = await nextMsg(ch.port2);
    assert.equal(msg.type, 'getFile');
    assert.equal(msg.path, '/not/there');

    ch.port2.postMessage({ type: 'getFileResult', id: msg.id, data: null });

    const result = await getPromise;
    assert.equal(result, null);

    await orch.dispose();
    ch.port2.close();
});

test('getFile — returns Uint8Array for existing file', async () => {
    const ch = pair();
    const orch = makeOrch(ch.port1);

    const getPromise = orch.getFile('/home/user/main.c');

    const msg = await nextMsg(ch.port2);
    const data = new Uint8Array([104, 101, 108, 108, 111]); // "hello"
    ch.port2.postMessage({ type: 'getFileResult', id: msg.id, data });

    const result = await getPromise;
    assert.ok(result instanceof Uint8Array);
    assert.deepEqual([...result], [104, 101, 108, 108, 111]);

    await orch.dispose();
    ch.port2.close();
});

test('writeFile — resolves on ok:true', async () => {
    const ch = pair();
    const orch = makeOrch(ch.port1);

    const writePromise = orch.writeFile('/home/user/main.c', new Uint8Array([65, 66]));

    const msg = await nextMsg(ch.port2);
    assert.equal(msg.type, 'writeFile');
    assert.equal(msg.path, '/home/user/main.c');

    ch.port2.postMessage({ type: 'writeFileResult', id: msg.id, ok: true });

    await writePromise; // should not throw

    await orch.dispose();
    ch.port2.close();
});

test('writeFile — rejects on ok:false', async () => {
    const ch = pair();
    const orch = makeOrch(ch.port1);

    const writePromise = orch.writeFile('/readonly/file', new Uint8Array([1]));

    const msg = await nextMsg(ch.port2);
    ch.port2.postMessage({ type: 'writeFileResult', id: msg.id, ok: false, error: 'EROFS' });

    await assert.rejects(writePromise, /EROFS/);

    await orch.dispose();
    ch.port2.close();
});

test('listDir — returns entries array', async () => {
    const ch = pair();
    const orch = makeOrch(ch.port1);

    const listPromise = orch.listDir('/home/user');

    const msg = await nextMsg(ch.port2);
    assert.equal(msg.type, 'listDir');
    assert.equal(msg.path, '/home/user');

    ch.port2.postMessage({ type: 'listDirResult', id: msg.id, entries: ['main.c', 'a.out'] });

    const entries = await listPromise;
    assert.deepEqual(entries, ['main.c', 'a.out']);

    await orch.dispose();
    ch.port2.close();
});

test('resetVfs — resolves on ok:true', async () => {
    const ch = pair();
    const orch = makeOrch(ch.port1);

    const resetPromise = orch.resetVfs();

    const msg = await nextMsg(ch.port2);
    assert.equal(msg.type, 'resetVfs');

    ch.port2.postMessage({ type: 'resetVfsResult', id: msg.id, ok: true });

    await resetPromise; // should not throw

    await orch.dispose();
    ch.port2.close();
});

test('resetVfs — rejects on ok:false', async () => {
    const ch = pair();
    const orch = makeOrch(ch.port1);

    const resetPromise = orch.resetVfs();

    const msg = await nextMsg(ch.port2);
    ch.port2.postMessage({ type: 'resetVfsResult', id: msg.id, ok: false, error: 'VFS locked' });

    await assert.rejects(resetPromise, /VFS locked/);

    await orch.dispose();
    ch.port2.close();
});

/* ------------------------------------------------------------------ */
/*  Shell notification routing                                          */
/* ------------------------------------------------------------------ */

test('shell notifications — routed to callbacks', async () => {
    const ch = pair();

    const events = [];
    const orch = makeOrch(ch.port1, {
        onShellOutput: (t) => events.push({ k: 'output', t }),
        onShellWrite: (t) => events.push({ k: 'write', t }),
        onShellClear: () => events.push({ k: 'clear' }),
        onShellSetEcho: (e) => events.push({ k: 'setEcho', e }),
        onShellExclusiveStdin: (e) => events.push({ k: 'exclusive', e }),
        onShellReadByte: () => events.push({ k: 'readByte' }),
    });

    // Send several shell notifications from the "worker"
    ch.port2.postMessage({ type: 'shellOutput', text: 'hello\n' });
    ch.port2.postMessage({ type: 'shellWrite', text: 'partial' });
    ch.port2.postMessage({ type: 'shellClear' });
    ch.port2.postMessage({ type: 'shellSetEcho', enabled: true });
    ch.port2.postMessage({ type: 'shellExclusiveStdin', enter: true });
    ch.port2.postMessage({ type: 'shellReadByte' });

    await delay(30); // wait for all messages to be delivered

    assert.deepEqual(events, [
        { k: 'output', t: 'hello\n' },
        { k: 'write', t: 'partial' },
        { k: 'clear' },
        { k: 'setEcho', e: true },
        { k: 'exclusive', e: true },
        { k: 'readByte' },
    ]);

    await orch.dispose();
    ch.port2.close();
});

/* ------------------------------------------------------------------ */
/*  sendStdinByte                                                       */
/* ------------------------------------------------------------------ */

test('sendStdinByte — sends stdin message to worker', async () => {
    const ch = pair();
    const orch = makeOrch(ch.port1);

    orch.sendStdinByte(42, 65); // run id=42, byte='A'

    const msg = await nextMsg(ch.port2);
    assert.equal(msg.type, 'stdin');
    assert.equal(msg.id, 42);
    assert.equal(msg.byte, 65);

    await orch.dispose();
    ch.port2.close();
});

/* ------------------------------------------------------------------ */
/*  isDisposed                                                          */
/* ------------------------------------------------------------------ */

test('isDisposed — false before dispose, true after', async () => {
    const ch = pair();
    const orch = makeOrch(ch.port1);

    assert.equal(orch.isDisposed, false);
    await orch.dispose();
    assert.equal(orch.isDisposed, true);

    ch.port2.close();
});
