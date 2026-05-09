// Seed hashing verification — deterministic and order-insensitive.

import assert from 'node:assert/strict';
import test from 'node:test';

import { hashSeed, normalizeSeedEntry } from '../dist/index.js';

test('hash is stable across runs for the same seed', () => {
    const seed = { 'main.cpp': 'int main(){}', 'README.md': '# hi' };
    assert.equal(hashSeed(seed), hashSeed(seed));
});

test('hash is order-insensitive', () => {
    const a = { 'a.txt': 'A', 'b.txt': 'B', 'c.txt': 'C' };
    const b = { 'c.txt': 'C', 'a.txt': 'A', 'b.txt': 'B' };
    assert.equal(hashSeed(a), hashSeed(b));
});

test('content change changes hash', () => {
    const a = { 'main.cpp': 'int main(){}' };
    const b = { 'main.cpp': 'int main(){return 0;}' };
    assert.notEqual(hashSeed(a), hashSeed(b));
});

test('visibility flip changes hash', () => {
    const a = { 'x.cpp': { content: 'X' } };
    const b = { 'x.cpp': { content: 'X', visibility: 'hidden' } };
    assert.notEqual(hashSeed(a), hashSeed(b));
});

test('normalizeSeedEntry collapses string shorthand', () => {
    assert.deepEqual(normalizeSeedEntry('hello'), { content: 'hello' });
    const obj = { content: 'x', visibility: 'hidden' };
    assert.equal(normalizeSeedEntry(obj), obj); // pass-through
});

test('hash is 16 hex chars (FNV-1a 64-bit)', () => {
    const h = hashSeed({ 'a.txt': 'A' });
    assert.match(h, /^[0-9a-f]{16}$/);
});
