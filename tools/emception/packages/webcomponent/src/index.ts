// `<emception-run>` custom element — Phase 6.1.
//
// Surface:
//   - Attributes: kebab-case mirror of `ViewConfigInput`. Recognized names
//     come from the canonical `ATTRIBUTE_SCHEMA` exported by
//     `@emception/core` so the schema lives in exactly one place. Unknown
//     attributes are ignored (matching HTML's permissive parsing model).
//   - Slots:
//       <textarea slot="stdin">   — initial stdin payload
//       <canvas slot="canvas">    — rendering surface for SDL / GUI presets
//   - Events: re-broadcasts every `EmceptionEventName` as a CustomEvent
//     named `emception-<name>` (per `EVENT_DOM_NAMES`). The `detail`
//     payload is whatever the underlying `EmceptionAPI` emits.
//   - Properties: the element exposes `api: EmceptionAPI | null`. Setting
//     this attaches the element to a pre-built API; clearing detaches and
//     unsubscribes. The ability to *create* an API from attributes is
//     intentionally deferred — it's the job of `@emception/browser`'s
//     `createEmception()` (Phase 7.2 once orchestration lands) so the
//     webcomponent stays free of the worker plumbing and can be unit-
//     tested with a stub.
//
// All non-DOM work (attribute parsing, event-name mapping) is done by
// `@emception/core`; this file is the thinnest possible glue.

import {
    EVENT_DOM_NAMES,
    parseAttributesToInput,
    type EmceptionAPI,
    type EmceptionEventName,
    type ViewConfigInput,
} from '@emception/core';

export const ELEMENT_NAME = 'emception-run';

const ALL_EVENT_NAMES: readonly EmceptionEventName[] = Object.keys(
    EVENT_DOM_NAMES,
) as EmceptionEventName[];

const TEMPLATE = `
<style>
  :host { display: block; font-family: system-ui, sans-serif; }
  [part='shell'] { display: grid; gap: 0.5rem; }
  [part='output'] {
    background: #111; color: #eee; padding: 0.5rem;
    font: 12px ui-monospace, monospace; min-height: 4rem;
    white-space: pre-wrap; overflow: auto;
  }
  [part='canvas-slot'][hidden] { display: none; }
  [part='stdin-slot'][hidden] { display: none; }
</style>
<div part="shell">
  <div part="output"></div>
  <div part="canvas-slot"><slot name="canvas"></slot></div>
  <div part="stdin-slot"><slot name="stdin"></slot></div>
</div>
`;

type Unsubscribe = () => void;

/**
 * The custom element class. Exported so consumers can subclass or
 * register under a different tag name.
 */
export class EmceptionRunElement extends HTMLElement {
    static get observedAttributes(): string[] {
        return [
            'preset', 'manifest-url', 'workspace', 'source', 'seed-url',
            'build-url', 'seed-policy', 'autorun', 'canvas', 'show-hidden',
            'show-solution', 'std', 'output', 'cflags', 'cxxflags', 'ldflags',
            'libs', 'include-paths', 'lib-paths',
        ];
    }

    private outputEl: HTMLDivElement | null = null;
    private canvasSlotEl: HTMLDivElement | null = null;
    private stdinSlotEl: HTMLDivElement | null = null;
    private subscriptions: Unsubscribe[] = [];
    private currentApi: EmceptionAPI | null = null;

    connectedCallback(): void {
        if (!this.shadowRoot) {
            const root = this.attachShadow({ mode: 'open' });
            root.innerHTML = TEMPLATE;
        }
        const root = this.shadowRoot!;
        this.outputEl = root.querySelector('[part="output"]') as HTMLDivElement;
        this.canvasSlotEl = root.querySelector('[part="canvas-slot"]') as HTMLDivElement;
        this.stdinSlotEl = root.querySelector('[part="stdin-slot"]') as HTMLDivElement;
        this.refreshSlotVisibility();
    }

    disconnectedCallback(): void {
        this.detachApi();
    }

    attributeChangedCallback(): void {
        this.refreshSlotVisibility();
    }

    /**
     * Read the currently-attached `EmceptionAPI` (if any). Setting this
     * to a non-null value attaches all event listeners; setting to null
     * detaches them.
     */
    get api(): EmceptionAPI | null {
        return this.currentApi;
    }

    set api(next: EmceptionAPI | null) {
        if (this.currentApi === next) return;
        this.detachApi();
        if (next) this.attachApi(next);
    }

    /**
     * Snapshot the current element attributes as a `ViewConfigInput`
     * (kebab→camel + flat→nested via `ATTRIBUTE_SCHEMA`). Returns the
     * input shape; callers can run it through `normalizeViewConfig` if
     * they need defaults applied.
     */
    readConfig(): ViewConfigInput {
        const attrs: Record<string, string> = {};
        for (const a of Array.from(this.attributes)) attrs[a.name] = a.value;
        return parseAttributesToInput(attrs);
    }

    private attachApi(api: EmceptionAPI): void {
        this.currentApi = api;
        for (const name of ALL_EVENT_NAMES) {
            // The cast is necessary because the listener-payload type
            // varies per event; we relay the payload uniformly to a
            // CustomEvent so consumers re-narrow on the receiving side.
            const handler = ((payload: unknown) => this.relay(name, payload)) as never;
            this.subscriptions.push(api.on(name, handler));
        }
    }

    private detachApi(): void {
        if (!this.currentApi) return;
        for (const unsub of this.subscriptions) unsub();
        this.subscriptions = [];
        this.currentApi = null;
    }

    private relay(name: EmceptionEventName, payload: unknown): void {
        const domName = EVENT_DOM_NAMES[name];
        this.dispatchEvent(
            new CustomEvent(domName, { detail: payload, bubbles: true, composed: true }),
        );
        // Mirror text streams into the default output pane so a bare
        // `<emception-run>` (no consumer JS) still shows something useful.
        if ((name === 'stdout' || name === 'stderr') && this.outputEl) {
            const p = payload as { chunk?: string | Uint8Array };
            const chunk = typeof p?.chunk === 'string'
                ? p.chunk
                : p?.chunk instanceof Uint8Array
                    ? new TextDecoder().decode(p.chunk)
                    : '';
            if (chunk) this.outputEl.append(chunk);
        }
    }

    private refreshSlotVisibility(): void {
        if (!this.canvasSlotEl || !this.stdinSlotEl) return;
        const cfg = this.readConfig();
        const hasCanvasSlotted = !!this.querySelector(':scope > [slot="canvas"]');
        this.canvasSlotEl.hidden = !(cfg.canvas || hasCanvasSlotted);
        const hasStdinSlotted = !!this.querySelector(':scope > [slot="stdin"]');
        this.stdinSlotEl.hidden = !hasStdinSlotted;
    }
}

/**
 * Register the element with `window.customElements`. Idempotent — safe
 * to call multiple times. Returns the constructor that ended up
 * registered (which may be a previously-registered one).
 */
export function registerEmceptionRun(tag: string = ELEMENT_NAME): CustomElementConstructor {
    if (typeof customElements === 'undefined') {
        throw new Error(
            '@emception/webcomponent: customElements is not available in this environment.',
        );
    }
    const existing = customElements.get(tag);
    if (existing) return existing;
    customElements.define(tag, EmceptionRunElement);
    return EmceptionRunElement;
}

// Auto-register on first import in browser-like environments.
if (typeof customElements !== 'undefined' && !customElements.get(ELEMENT_NAME)) {
    customElements.define(ELEMENT_NAME, EmceptionRunElement);
}
