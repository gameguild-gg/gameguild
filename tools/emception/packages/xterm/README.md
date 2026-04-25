# @emception/xterm

[xterm.js](https://xtermjs.org/) bridge for [emception](https://github.com/gameguild-gg/gameguild/tree/main/tools/emception). Wraps a `Terminal` as a `StdinInput` / `StdoutSink` consumable by `@emception/browser`'s `EmceptionAPI`.

## Install

```bash
npm install @emception/xterm @xterm/xterm
```

`@xterm/xterm` is a peer dependency.

## Use

```ts
import { Terminal } from '@xterm/xterm';
import '@xterm/xterm/css/xterm.css';
import { fromXterm, toXterm } from '@emception/xterm';
import { createEmception } from '@emception/browser';

const xterm = new Terminal();
xterm.open(document.getElementById('term')!);

const em = await createEmception({
  stdin: fromXterm(xterm), // line-buffered + local echo by default
  stdout: toXterm(xterm),
  stderr: toXterm(xterm),
});

await em.run('./a.out');
```

### Raw mode

```ts
fromXterm(xterm, { raw: true });
```

Raw disables line buffering + local echo — every keypress is forwarded verbatim. Useful for full-screen TUI programs (vim, ncurses).

## What this package is

This is intentionally a thin shim. The actual stream wiring lives in `@emception/core` (Phase 2). Importing `@emception/xterm` adds:

- `fromXterm(terminal, opts?)` → `XtermStdin` adapter shape.
- `toXterm(terminal)` → `XtermStdout` adapter shape.
- `TTYBridge` — low-level helper used internally to plumb keypress events into `ReadableStream<Uint8Array>`.
- Type unions `XtermStdinInput` / `XtermStdoutSink` that extend `StdinInput` / `StdoutSink` so TS callers get autocomplete.

The xterm peer dep lives **here only** so non-terminal embeds (`@emception/node`, headless graders, the bare `<emception-run>` custom element) don't pay the bundle cost.
