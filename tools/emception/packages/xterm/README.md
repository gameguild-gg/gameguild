# @gameguild/emception-xterm

[xterm.js](https://xtermjs.org/) bridge for [emception](https://github.com/gameguild-gg/gameguild/tree/main/tools/emception). Wraps a `Terminal` as a `StdinInput` / `StdoutSink` consumable by `@gameguild/emception-browser`'s `EmceptionAPI`.

## Live Demo

Try it at [gameguild-gg.github.io/gameguild/](https://gameguild-gg.github.io/gameguild/) — features a live IDE with working templates for C++, SDL3, Raylib, CMake, and Python.

## Install

```bash
npm install @gameguild/emception-xterm @xterm/xterm
```

`@xterm/xterm` is a peer dependency.

## Use

```ts
import { Terminal } from '@xterm/xterm';
import '@xterm/xterm/css/xterm.css';
import { fromXterm, toXterm } from '@gameguild/emception-xterm';
import { createEmception } from '@gameguild/emception-browser';

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

This is intentionally a thin shim. The actual stream wiring lives in `emception`. Importing `@gameguild/emception-xterm` adds:

- `fromXterm(terminal, opts?)` → `XtermStdin` adapter shape.
- `toXterm(terminal)` → `XtermStdout` adapter shape.
- `TTYBridge` — low-level helper used internally to plumb keypress events into `ReadableStream<Uint8Array>`.
- Type unions `XtermStdinInput` / `XtermStdoutSink` that extend `StdinInput` / `StdoutSink` (from `emception`) so TS callers get autocomplete.

The xterm peer dep lives **here only** so non-terminal embeds (headless graders, the bare `<emception-run>` custom element) don't pay the bundle cost.
