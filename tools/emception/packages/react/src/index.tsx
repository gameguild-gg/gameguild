// @emception/react — React surface.
//
// Provides:
//   - `<EmceptionRun>` — declarative React wrapper around the
//     `<emception-run>` custom element. Accepts camelCase props that
//     mirror the kebab-case attribute schema, plus typed event-handler
//     props (onReady, onStdout, onStderr, onExit, onTestReport, …) and
//     an optional `api` prop to attach a pre-built `EmceptionAPI`.
//   - `useEmception(opts)` — hook that lazily builds an `EmceptionAPI`
//     via the orchestrator passed in `opts.create`. The hook is
//     deliberately framework-agnostic about *how* the API gets built;
//     it just memoizes the builder + cleans up on unmount.
//     will provide a `createEmception` factory that satisfies this
//     interface.
//
// We intentionally do NOT import `@emception/webcomponent` from this
// file. That package self-registers `<emception-run>` as a side effect
// on import, which is the wrong behavior for SSR-friendly React libs
// (React renders strings on the server with no `customElements`).
// Consumers should import the webcomponent in a client-only entry
// point (e.g. inside `useEffect` or via a `'use client'` boundary).

import { forwardRef, useEffect, useImperativeHandle, useRef, useState, type CSSProperties, type ReactNode, type Ref } from 'react';

import { type EmceptionAPI, type ViewConfigInput, type EmceptionEventMap, type EmceptionEventName, EVENT_DOM_NAMES, camelToKebab } from 'emception';

// --- Types ---------------------------------------------------------------

type EventHandlerProps = {
  [E in EmceptionEventName as `on${Capitalize<CamelCase<E>>}`]?: (payload: EmceptionEventMap[E]) => void;
};

type CamelCase<S extends string> = S extends `${infer Head}-${infer Tail}` ? `${Head}${Capitalize<CamelCase<Tail>>}` : S;

/**
 * Props for `<EmceptionRun>`. Kebab-case attributes from the canonical
 * `ATTRIBUTE_SCHEMA` are exposed as camelCase props (preset, manifestUrl,
 * cflags, includePaths, …). `style`, `className`, and `children` are
 * forwarded to the underlying custom element.
 */
export type EmceptionRunProps = ViewConfigInput &
  EventHandlerProps & {
    /** Pre-built EmceptionAPI to attach. Optional — consumers may
     *  also create their own and assign it via the `api` setter on
     *  the underlying element. */
    api?: EmceptionAPI | null;
    className?: string;
    style?: CSSProperties;
    children?: ReactNode;
  };

/**
 * Imperative handle exposed via `ref`. Mirrors the most useful slice of
 * the underlying custom element so callers can read attributes / get
 * the API without touching the DOM directly.
 */
export interface EmceptionRunHandle {
  readonly element: HTMLElement | null;
  readonly api: EmceptionAPI | null;
}

// --- <EmceptionRun> ------------------------------------------------------

const KNOWN_EVENT_NAMES: readonly EmceptionEventName[] = Object.keys(EVENT_DOM_NAMES) as EmceptionEventName[];

function camelEventToKey(name: EmceptionEventName): keyof EventHandlerProps {
  // 'test-report' → 'onTestReport'
  const camel = name
    .split('-')
    .map((p: string, i: number) => (i === 0 ? p : p.charAt(0).toUpperCase() + p.slice(1)))
    .join('');
  return ('on' + camel.charAt(0).toUpperCase() + camel.slice(1)) as keyof EventHandlerProps;
}

const HANDLER_KEYS: ReadonlyMap<EmceptionEventName, keyof EventHandlerProps> = (() => {
  const m = new Map<EmceptionEventName, keyof EventHandlerProps>();
  for (const name of KNOWN_EVENT_NAMES) m.set(name, camelEventToKey(name));
  return m;
})();

/**
 * The set of prop keys that should be projected onto the host element
 * as kebab-case attributes (vs. the prop keys that are React-internal).
 * Computed once.
 */
const ATTRIBUTE_PROP_KEYS: ReadonlyArray<Extract<keyof ViewConfigInput, string>> = [
  'preset',
  'manifestUrl',
  'workspace',
  'source',
  'seedUrl',
  'buildUrl',
  'seedPolicy',
  'autorun',
  'canvas',
  'showHidden',
  'showSolution',
  'stdin',
  'stdout',
  'stderr',
];

function isAttributePrimitive(v: unknown): v is string | number | boolean {
  return typeof v === 'string' || typeof v === 'number' || typeof v === 'boolean';
}

export const EmceptionRun = forwardRef<EmceptionRunHandle, EmceptionRunProps>(function EmceptionRun(props, ref) {
  const { api, className, style, children, ...rest } = props;
  const elRef = useRef<HTMLElement | null>(null);
  const apiRef = useRef<EmceptionAPI | null>(null);

  // Project camelCase view-config props onto the element as kebab
  // attributes. Anything non-primitive (e.g. workspace object) is
  // skipped — those scenarios should set `api` directly via
  // `createEmception` and pass the result through `props.api`.
  const attrProps: Record<string, string> = {};
  for (const key of ATTRIBUTE_PROP_KEYS) {
    const v = (rest as Record<string, unknown>)[key];
    if (!isAttributePrimitive(v)) continue;
    const attrName = camelToKebab(key);
    attrProps[attrName] = typeof v === 'boolean' ? (v ? '' : 'false') : String(v);
  }

  // Wire api setter and event listeners on the element.
  useEffect(
    () => {
      const el = elRef.current;
      if (!el) return;
      // Event listeners: re-derive from props each effect tick.
      const listeners: Array<[string, EventListener]> = [];
      for (const name of KNOWN_EVENT_NAMES) {
        const propKey = HANDLER_KEYS.get(name)!;
        const handler = (rest as Record<string, unknown>)[propKey] as ((payload: unknown) => void) | undefined;
        if (typeof handler !== 'function') continue;
        const domName = EVENT_DOM_NAMES[name];
        const fn: EventListener = (ev) => handler((ev as CustomEvent).detail);
        el.addEventListener(domName, fn);
        listeners.push([domName, fn]);
      }
      return () => {
        for (const [type, fn] of listeners) el.removeEventListener(type, fn);
      };
      // We intentionally re-subscribe whenever any handler prop
      // identity changes — React's stable-callback discipline
      // (useCallback) controls churn here.
      // eslint-disable-next-line react-hooks/exhaustive-deps
    },
    KNOWN_EVENT_NAMES.map((n) => (rest as Record<string, unknown>)[HANDLER_KEYS.get(n)!]),
  );

  useEffect(() => {
    const el = elRef.current as (HTMLElement & { api?: EmceptionAPI | null }) | null;
    if (!el) return;
    if (api !== undefined) {
      el.api = api;
      apiRef.current = api;
    }
    return () => {
      if (api !== undefined && el) el.api = null;
      apiRef.current = null;
    };
  }, [api]);

  useImperativeHandle(
    ref,
    (): EmceptionRunHandle => ({
      get element() {
        return elRef.current;
      },
      get api() {
        return apiRef.current;
      },
    }),
    [],
  );

  // React 19 supports rendering custom elements with arbitrary
  // attributes. The React.JSX namespace doesn't ship a typed entry
  // for `<emception-run>`, so we fall back to `any` for the JSX
  // intrinsic; the surface is fully typed via `EmceptionRunProps`.
  const Tag = 'emception-run' as unknown as 'div';
  return (
    <Tag ref={elRef as Ref<HTMLDivElement>} className={className} style={style} {...attrProps}>
      {children}
    </Tag>
  );
});

// --- useEmception() ------------------------------------------------------

export interface UseEmceptionOptions {
  /**
   * Factory that builds an `EmceptionAPI`. `createEmception` from
   * `@emception/browser`
   * that satisfies this signature. Kept generic so the hook itself
   * has no runtime dependency on either adapter.
   */
  create: (signal: AbortSignal) => Promise<EmceptionAPI>;
  /** If true, `create` is not invoked. Useful for SSR. */
  skip?: boolean;
}

export interface UseEmceptionResult {
  api: EmceptionAPI | null;
  status: 'idle' | 'loading' | 'ready' | 'error';
  error: unknown;
}

/**
 * Build an `EmceptionAPI` once on mount and dispose it on unmount.
 * Re-runs `create` if its identity changes — wrap your factory in
 * `useCallback` to keep it stable.
 */
export function useEmception(opts: UseEmceptionOptions): UseEmceptionResult {
  const { create, skip } = opts;
  const [state, setState] = useState<UseEmceptionResult>({
    api: null,
    status: skip ? 'idle' : 'loading',
    error: null,
  });

  useEffect(() => {
    if (skip) {
      setState({ api: null, status: 'idle', error: null });
      return;
    }
    const ctrl = new AbortController();
    let disposed = false;
    let createdApi: EmceptionAPI | null = null;
    setState({ api: null, status: 'loading', error: null });
    create(ctrl.signal).then(
      (api) => {
        if (disposed) {
          api.dispose();
          return;
        }
        createdApi = api;
        setState({ api, status: 'ready', error: null });
      },
      (err) => {
        if (disposed) return;
        setState({ api: null, status: 'error', error: err });
      },
    );
    return () => {
      disposed = true;
      ctrl.abort();
      if (createdApi) createdApi.dispose();
    };
  }, [create, skip]);

  return state;
}

// --- Type exports --------------------------------------------------------

export type { EmceptionAPI, EmceptionEventMap, EmceptionEventName, ViewConfigInput };
