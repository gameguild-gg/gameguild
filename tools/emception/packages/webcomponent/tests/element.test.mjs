// Smoke test for @emception/webcomponent. Polyfills the bare minimum of
// the DOM (customElements registry + HTMLElement + CustomEvent) so the
// module can be imported under raw Node and we can verify event relay
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
const core = await import('../../core/dist/index.js');

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
    el.setAttribute('cflags', '-O2 -Wall');
    const cfg = el.readConfig();
    assert.equal(cfg.preset, 'cpp');
    assert.equal(cfg.autorun, true);
    assert.deepEqual(cfg.workspace?.build?.cflags, ['-O2', '-Wall']);
});

test('attaching api wires every event and detaching unwires them', () => {
    const el = new wc.EmceptionRunElement();
    const subscribed = new Map();
    const api = {
        on(name, fn) {
            if (!subscribed.has(name)) subscribed.set(name, []);
            subscribed.get(name).push(fn);
            return () => {
                const arr = subscribed.get(name);
                arr.splice(arr.indexOf(fn), 1);
            };
        },
    };
    el.api = api;
    for (const name of Object.keys(core.EVENT_DOM_NAMES)) {
        assert.ok(subscribed.get(name)?.length, 'expected listener for ' + name);
    }
    el.api = null;
    for (const arr of subscribed.values()) assert.equal(arr.length, 0);
});

test('dispatches CustomEvent emception-stdout when stdout fires', () => {
    const el = new wc.EmceptionRunElement();
    const fired = [];
    el.addEventListener('emception-stdout', (ev) => fired.push(ev.detail));
    let stdoutHandler;
    const api = {
        on(name, fn) {
            if (name === 'stdout') stdoutHandler = fn;
            return () => undefined;
        },
    };
    el.api = api;
    stdoutHandler({ chunk: 'hello' });
    assert.equal(fired.length, 1);
    assert.deepEqual(fired[0], { chunk: 'hello' });
});
