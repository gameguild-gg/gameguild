# @gameguild/emception-react

React 19 components for [emception](https://github.com/gameguild-gg/gameguild/tree/main/tools/emception).

## Live Demo

Try it at [gameguild-gg.github.io/gameguild/](https://gameguild-gg.github.io/gameguild/) — features a live IDE with working templates for C++, SDL3, Raylib, CMake, and Python.

## Install

```bash
npm install @gameguild/emception-react @gameguild/emception-webcomponent @gameguild/emception-browser
```

## `<EmceptionRun>`

Declarative wrapper around the `<emception-run>` custom element.

```tsx
'use client';

import { useCallback } from 'react';
import { EmceptionRun, useEmception } from '@gameguild/emception-react';
import { createEmception } from '@gameguild/emception-browser';

// Register the custom element on the client (NOT in the server bundle).
import '@gameguild/emception-webcomponent';

export function Demo() {
  const create = useCallback((signal: AbortSignal) => {
    return createEmception({ tty: 'none', signal });
  }, []);
  const { api, status, error } = useEmception({ create });

  if (status === 'error') return <pre>{String(error)}</pre>;

  return (
    <EmceptionRun
      api={api}
      preset="cpp"
      autorun
      source="int main(){ return 0; }"
      onStdout={(p) => console.log(p.chunk)}
      onExit={(p) => console.log('exit', p.code)}
    />
  );
}
```

### Props

- All `ViewConfigInput` fields as **camelCase** props (e.g. `manifestUrl`,
  `seedUrl`, `buildUrl`, `autorun`, `canvas`, `showHidden`, …).
- Typed event-handler props derived from `EmceptionEventMap`:
  `onReady`, `onStdout`, `onStderr`, `onExit`, `onTestReport`,
  `onTestCase`, …
- `api?: EmceptionAPI | null` — attaches a pre-built API to the host
  element via its setter.
- `className`, `style`, `children` — forwarded.

Non-primitive props (e.g. a fully-shaped `workspace` object) are not
projected as attributes; pass them through `useEmception`'s `create`
factory and attach the resulting `api` instead.

### Imperative ref

```tsx
const ref = useRef<EmceptionRunHandle>(null);
// ref.current.element  → HTMLElement | null
// ref.current.api      → EmceptionAPI | null
```

## `useEmception(opts)`

```ts
const { api, status, error } = useEmception({
  create: (signal) => createEmception({ signal }),
  skip: false,
});
```

- `create(signal)` — your factory. Wrap in `useCallback` to avoid
  rebuilding the API on every render.
- `skip` — short-circuit (e.g. for SSR).
- Returns `{ api, status: 'idle' | 'loading' | 'ready' | 'error', error }`.
- Aborts in-flight builds on unmount and disposes the `api`.

## SSR

`@gameguild/emception-react` itself does **not** import
`@gameguild/emception-webcomponent` — that package self-registers the custom
element on import, which would fail on the server. Register the
element from a client-only entry (e.g. inside a `'use client'`
component or `useEffect`).
