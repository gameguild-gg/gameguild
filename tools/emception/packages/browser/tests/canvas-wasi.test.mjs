import assert from 'node:assert/strict';
import test from 'node:test';
import { createCanvasWasiImports } from '../dist/canvas-wasi.js';

function fixture() {
  const memory = new WebAssembly.Memory({ initial: 1 });
  const view = new DataView(memory.buffer);
  const bytes = new Uint8Array(memory.buffer);
  const stdout = [];
  const stderr = [];
  const imports = createCanvasWasiImports(() => memory, text => stdout.push(text), text => stderr.push(text));
  function write(fd, text) {
    const encoded = new TextEncoder().encode(text);
    bytes.set(encoded, 64);
    view.setUint32(0, 64, true);
    view.setUint32(4, encoded.byteLength, true);
    const result = imports.fd_write(fd, 0, 1, 16);
    return { result, count: view.getUint32(16, true) };
  }
  return { memory, stdout, stderr, write };
}

test('canvas WASI forwards fd 1 to stdout and fd 2 to stderr without mixing', () => {
  const instance = fixture();
  assert.deepEqual(instance.write(1, 'standard output'), { result: 0, count: 15 });
  assert.deepEqual(instance.write(2, 'diagnostic'), { result: 0, count: 10 });
  assert.deepEqual(instance.stdout, ['standard output']);
  assert.deepEqual(instance.stderr, ['diagnostic']);
});

test('canvas WASI rejects unknown descriptors without forwarding data', () => {
  const instance = fixture();
  assert.equal(instance.write(99, 'do not route').result, 8);
  assert.deepEqual(instance.stdout, []);
  assert.deepEqual(instance.stderr, []);
});

test('the retained single-sink form still receives both standard channels', () => {
  const instance = fixture();
  const output = [];
  const imports = createCanvasWasiImports(() => instance.memory, text => output.push(text));
  instance.write(1, 'same sink');
  assert.equal(imports.fd_write(1, 0, 1, 16), 0);
  assert.equal(imports.fd_write(2, 0, 1, 16), 0);
  assert.deepEqual(output, ['same sink', 'same sink']);
});
