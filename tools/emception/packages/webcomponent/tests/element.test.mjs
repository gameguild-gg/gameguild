// Smoke test for @emception/webcomponent. Polyfills the bare minimum of
// the DOM (customElements registry + HTMLElement + CustomEvent) so the
// module can be imported under raw Node and we can verify lifecycle events
// and attribute parsing without bringing in jsdom.

import assert from 'node:assert/strict';
import { test } from 'node:test';

// --- Minimal DOM shim ----------------------------------------------------

class FakeRegistry {
    constructor() { this.map = new Map(); }
    define(name, ctor) {
        if (this.map.has(name)) throw new Error('already defined: ' + name);
        this.map.set(name, ctor);
    }
    get(name) { return this.map.get(name); }
}

class FakeAttr { constructor(name, value) { this.name = name; this.value = value; } }

class FakeShadowRoot {
    constructor() { this.children = []; this.innerHTML = ''; }
    querySelector() { return new FakeDiv(); }
}

class FakeDiv {
    constructor() { this.hidden = false; this.appended = []; }
    append(...c) { this.appended.push(...c); }
}

class FakeCustomEvent {
    constructor(type, init = {}) {
        this.type = type;
        this.detail = init.detail;
        this.bubbles = !!init.bubbles;
        this.composed = !!init.composed;
    }
}

class FakeHTMLElement {
    constructor() {
        this._attrs = new Map();
        this._listeners = new Map();
        this.shadowRoot = null;
        this.dispatched = [];
    }
    get attributes() {
        return Array.from(this._attrs.entries()).map(([n, v]) => new FakeAttr(n, v));
    }
    setAttribute(n, v) { this._attrs.set(n, String(v)); }
    getAttribute(n) { return this._attrs.has(n) ? this._attrs.get(n) : null; }
    hasAttribute(n) { return this._attrs.has(n); }
    attachShadow() {
        this.shadowRoot = new FakeShadowRoot();
        return this.shadowRoot;
    }
    addEventListener(type, fn) {
        if (!this._listeners.has(type)) this._listeners.set(type, []);
        this._listeners.get(type).push(fn);
    }
    removeEventListener(type, fn) {
        const arr = this._listeners.get(type);
        if (!arr) return;
        const i = arr.indexOf(fn);
        if (i >= 0) arr.splice(i, 1);
    }
    dispatchEvent(ev) {
        this.dispatched.push(ev);
        const arr = this._listeners.get(ev.type);
        if (arr) for (const fn of arr.slice()) fn(ev);
        return true;
    }
    querySelector() { return null; }
}

globalThis.customElements = new FakeRegistry();
globalThis.HTMLElement = FakeHTMLElement;
globalThis.CustomEvent = FakeCustomEvent;

// --- Import target -------------------------------------------------------

const wc = await import('../dist/index.js');

test('exports ELEMENT_NAME, EmceptionRunElement, registerEmceptionRun', () => {
    assert.equal(wc.ELEMENT_NAME, 'emception-run');
    assert.equal(typeof wc.EmceptionRunElement, 'function');
    assert.equal(typeof wc.registerEmceptionRun, 'function');
});

test('auto-registers <emception-run>', () => {
    assert.equal(globalThis.customElements.get('emception-run'), wc.EmceptionRunElement);
});

test('registerEmceptionRun is idempotent', () => {
    const result = wc.registerEmceptionRun();
    assert.equal(result, wc.EmceptionRunElement);
});

test('registerEmceptionRun under a custom tag adds a fresh registration', () => {
    const result = wc.registerEmceptionRun('emception-other');
    assert.equal(result, wc.EmceptionRunElement);
    assert.equal(globalThis.customElements.get('emception-other'), wc.EmceptionRunElement);
});

test('readConfig returns parsed ViewConfigInput from attributes', () => {
    const el = new wc.EmceptionRunElement();
    el.setAttribute('preset', 'cpp');
    el.setAttribute('autorun', '');
    el.setAttribute('flags', '-O2 -Wall');
    const cfg = el.readConfig();
    assert.equal(cfg.preset, 'cpp');
    assert.equal(cfg.autorun, true);
    assert.deepEqual(cfg.workspace?.flags, ['-O2', '-Wall']);
});

test('api property stores and clears the browser API', () => {
    const el = new wc.EmceptionRunElement();
    const api = { workspace: {}, run() {} };

    el.api = api;
    assert.equal(el.api, api);

    el.api = null;
    assert.equal(el.api, null);
});

test('run compiles C++ and dispatches exit and ready events', async () => {
    const el = new wc.EmceptionRunElement();
    el.connectedCallback();
    el.setAttribute('preset', 'cpp');
    el.setAttribute('source', '#include <iostream>\nint main() { std::cout << "hello"; }');

    const writes = [];
    const runs = [];
    const api = {
        workspace: {
            async writeFile(path, bytes) {
                writes.push({ path, source: new TextDecoder().decode(bytes) });
            },
        },
        async run(tool, argv, options) {
            runs.push({ tool, argv });
            if (tool === 'wasi-run') {
                options.stdout?.(new TextEncoder().encode('hello'));
            }
            return { exitCode: 0, stdout: '', stderr: '', durationMs: 1, timedOut: false };
        },
    };

    el.api = api;
    await el.run();

    assert.deepEqual(writes, [{
        path: '/home/user/main.cpp',
        source: '#include <iostream>\nint main() { std::cout << "hello"; }',
    }]);
    assert.deepEqual(runs.map(({ tool }) => tool), ['clang', 'wasm-ld', 'wasi-run']);
    assert.deepEqual(el.dispatched.map(({ type }) => type), ['emception-exit', 'emception-ready']);
    assert.deepEqual(el.dispatched[0].detail, { exitCode: 0, finalPhase: 'run' });
});
