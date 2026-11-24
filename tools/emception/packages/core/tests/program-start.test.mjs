import assert from 'node:assert/strict';
import test from 'node:test';
import { MessageChannel } from 'node:worker_threads';
import { messagePortTransport, WorkerOrchestrator } from '../dist/index.js';

function fixture(t) {
  const channel = new MessageChannel();
  const runtime = new WorkerOrchestrator(messagePortTransport(channel.port1));
  t.after(async () => {
    await runtime.dispose();
    channel.port2.close();
  });
  const sent = () => new Promise((resolve) => channel.port2.once('message', resolve));
  const receive = (message) => channel.port2.postMessage(message);
  const finish = (id) => receive({ type: 'runResult', id, exitCode: 0, stdout: 'ok', stderr: '' });
  return { runtime, sent, receive, finish };
}

test('native program start is opt-in, correlated, once-only and removed after completion', async (t) => {
  const f = fixture(t);
  let starts = 0;
  const pending = f.runtime.run('wasi-run', ['program.wasm'], { onProgramStart: () => starts++ });
  const request = await f.sent();
  f.receive({ type: 'programStart', id: request.id + 99 });
  f.receive({ type: 'programStart', id: request.id });
  f.receive({ type: 'programStart', id: request.id });
  f.finish(request.id);
  assert.equal((await pending).stdout, 'ok');
  assert.equal(request.options.wantProgramStart, true);
  assert.equal(starts, 1);
  f.receive({ type: 'programStart', id: request.id });
  const next = f.runtime.run('wasi-run', ['next.wasm']);
  const nextRequest = await f.sent();
  f.finish(nextRequest.id);
  await next;
  assert.equal(starts, 1);
});

test('legacy callers request no program-start notification and keep their result', async (t) => {
  const f = fixture(t);
  const pending = f.runtime.run('wasi-run', ['program.wasm']);
  const request = await f.sent();
  assert.equal(Object.hasOwn(request.options, 'wantProgramStart'), false);
  f.receive({ type: 'programStart', id: request.id });
  f.finish(request.id);
  assert.deepEqual(await pending, { exitCode: 0, stdout: 'ok', stderr: '', durationMs: 0, timedOut: false });
});

test('disposing a pending run removes its optional start observer', async (t) => {
  const f = fixture(t);
  let starts = 0;
  const pending = f.runtime.run('wasi-run', ['program.wasm'], { onProgramStart: () => starts++ });
  const rejected = assert.rejects(pending);
  const request = await f.sent();
  await f.runtime.dispose();
  await rejected;
  f.receive({ type: 'programStart', id: request.id });
  assert.equal(starts, 0);
});
