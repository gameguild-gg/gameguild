# Emception

A C/C++ toolchain that runs in the browser via WebAssembly. Compile, link,
test, and execute projects without a server-side compiler.

## Packages

| Package | Responsibility |
| --- | --- |
| `@gameguild/emception-toolchain` | versioned manifest, compressed sysroot, generated glue/WASM pairs, ABI profiles |
| `emception` | runtime-neutral contracts, VFS/build/test engines, presets |
| `@gameguild/emception-browser` | Worker boot, persistence, streams, canvas build/execution |
| `@gameguild/emception-xterm` | xterm stream adapter |
| `@gameguild/emception-react` | React bindings |
| `@gameguild/emception-webcomponent` | framework-free `<emception-run>` |
| `@gameguild/emception-ide` | vanilla React IDE and `<emception-ide>` custom element |

The root `emception-workspace` package is private; publishable code lives under
`packages/`.

## Quick start

```ts
import { createEmception } from '@gameguild/emception-browser';

const emception = await createEmception({ tty: 'none' });
await emception.workspace.writeFile(
  '/home/user/main.c',
  new TextEncoder().encode('#include <stdio.h>\nint main(){puts("hi");}'),
);

const compile = await emception.run('clang', [
  '/home/user/main.c',
  '-o',
  '/home/user/a.out',
]);
if (compile.exitCode === 0) {
  console.log((await emception.run('/home/user/a.out', [])).stdout);
}
emception.dispose();
```

`createEmception()` defaults to the version-matched manifest published by
`@gameguild/emception-toolchain`. Pass `manifestUrl` only when self-hosting the
same canonical release.

## Vanilla IDE

```tsx
import { Ide } from '@gameguild/emception-ide';

export function Playground() {
  return <Ide workspaceName="lesson-3" enableCanvas />;
}
```

The IDE owns presentation state only. It consumes `createEmception()` and the
public `canvas.buildAndStart()` surface; generated glue and raw WASM
instantiation remain inside the release pipeline and Browser package.

## Development

```bash
cd tools/emception
pnpm run test:packages
pnpm run typecheck:packages
pnpm run build:packages
```

The complete SDK and artifact release is built with `pnpm run build:all`.

## Documentation

- [Architecture](./docs/architecture.md) — ownership boundaries and glue/WASM coupling
- [Building from source](./docs/build.md) — canonical staged release pipeline
- [Virtual filesystem](./docs/vfs.md) — LazyFS, overlays, persistence, Asyncify

## Repository layout

```text
tools/emception/
├── packages/
│   ├── toolchain/     # canonical generated artifact release
│   ├── core/          # runtime-neutral package
│   ├── browser/       # browser runtime adapter
│   ├── ide/           # vanilla IDE presentation
│   ├── react/         # React bindings
│   ├── webcomponent/  # custom elements
│   └── xterm/         # terminal adapter
├── scripts/           # tool builds, staging, patching, packaging
├── docs/
├── sysroot/           # mutable build workspace (generated)
└── build/stage/       # frozen release input (generated)
```

## License

MIT.
