import assert from 'node:assert/strict';
import { test } from 'node:test';
import { EmceptionError } from '../dist/errors.js';
import { crc32, createZip, readZip } from '../dist/workspace/zip.js';

const enc = new TextEncoder();
const dec = new TextDecoder();

test('crc32 matches known vector for "123456789"', () => {
    // CRC32 of ASCII "123456789" is 0xCBF43926 (standard test vector).
    assert.equal(crc32(enc.encode('123456789')), 0xcbf43926);
});

test('crc32 of empty input is 0', () => {
    assert.equal(crc32(new Uint8Array(0)), 0);
});

test('createZip + readZip round-trip preserves contents', () => {
    const entries = [
        { path: 'main.cpp', data: enc.encode('int main(){ return 0; }\n') },
        { path: 'src/lib.h', data: enc.encode('#pragma once\n') },
        { path: 'empty.txt', data: new Uint8Array(0) },
    ];
    const buf = createZip(entries);
    const back = readZip(buf);
    assert.equal(back.length, 3);
    assert.equal(back[0].path, 'main.cpp');
    assert.equal(dec.decode(back[0].data), 'int main(){ return 0; }\n');
    assert.equal(back[1].path, 'src/lib.h');
    assert.equal(dec.decode(back[1].data), '#pragma once\n');
    assert.equal(back[2].path, 'empty.txt');
    assert.equal(back[2].data.length, 0);
});

test('createZip output starts with PK\\x03\\x04 (LFH signature)', () => {
    const buf = createZip([{ path: 'a', data: enc.encode('x') }]);
    assert.equal(buf[0], 0x50);
    assert.equal(buf[1], 0x4b);
    assert.equal(buf[2], 0x03);
    assert.equal(buf[3], 0x04);
});

test('createZip with reproducible date produces deterministic bytes', () => {
    const entries = [{ path: 'a', data: enc.encode('hello') }];
    const fixed = new Date(Date.UTC(2024, 0, 1, 0, 0, 0));
    const a = createZip(entries, { date: fixed });
    const b = createZip(entries, { date: fixed });
    assert.deepEqual(Array.from(a), Array.from(b));
});

test('createZip rejects empty path', () => {
    assert.throws(
        () => createZip([{ path: '', data: new Uint8Array(0) }]),
        EmceptionError,
    );
});

test('createZip rejects directory entries', () => {
    assert.throws(
        () => createZip([{ path: 'src/', data: new Uint8Array(0) }]),
        /directory entries/,
    );
});

test('createZip rejects non-Uint8Array data', () => {
    assert.throws(
        () => createZip([{ path: 'a', data: 'oops' }]),
        /Uint8Array/,
    );
});

test('readZip round-trips UTF-8 filenames', () => {
    const path = 'тест/файл.txt';
    const buf = createZip([{ path, data: enc.encode('привет') }]);
    const back = readZip(buf);
    assert.equal(back[0].path, path);
    assert.equal(dec.decode(back[0].data), 'привет');
});

test('readZip rejects malformed buffer', () => {
    assert.throws(() => readZip(new Uint8Array(10)), EmceptionError);
});

test('readZip rejects non-Uint8Array', () => {
    assert.throws(() => readZip([1, 2, 3]), /Uint8Array/);
});

test('readZip handles many entries (stress check)', () => {
    const entries = [];
    for (let i = 0; i < 100; i++) {
        entries.push({ path: `f${i}.txt`, data: enc.encode(`content-${i}`) });
    }
    const buf = createZip(entries);
    const back = readZip(buf);
    assert.equal(back.length, 100);
    for (let i = 0; i < 100; i++) {
        assert.equal(back[i].path, `f${i}.txt`);
        assert.equal(dec.decode(back[i].data), `content-${i}`);
    }
});

test('readZip rejects entries with unsupported compression method', () => {
    // Build a tiny ZIP and patch the central directory method to 8 (DEFLATE).
    const buf = createZip([{ path: 'a', data: enc.encode('x') }]);
    // EOCD is at the end (22 bytes); CD start is at LFH(30+1+1)=32 here. Patch
    // the central directory method field at cdStart+10.
    const view = new DataView(buf.buffer, buf.byteOffset, buf.byteLength);
    const eocd = buf.length - 22;
    const cdStart = view.getUint32(eocd + 16, true);
    view.setUint16(cdStart + 10, 8, true); // method = DEFLATE
    assert.throws(() => readZip(buf), /STORED/);
});
