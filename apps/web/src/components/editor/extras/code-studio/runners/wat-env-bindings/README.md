# WAT Environment Bindings

WebAssembly Component Model bindings implementing **WASI Preview 2** and custom GameGuild interfaces.

## Architecture

```
wat-env-bindings/
├── types.ts                 # Component Model type definitions
├── index.ts                 # Main entry point
│
├── wasi/                    # WASI Preview 2 standard interfaces
│   ├── cli.ts              # wasi:cli/* (stdin/stdout/stderr, exit, environment)
│   ├── clocks.ts           # wasi:clocks/* (wall-clock, monotonic-clock)
│   ├── io.ts               # wasi:io/* (streams, poll)
│   ├── random.ts           # wasi:random/*
│   ├── filesystem.ts       # wasi:filesystem/*
│   └── index.ts
│
├── gameguild/              # GameGuild custom interfaces
│   ├── console.ts          # gameguild:runtime/console@0.1.0
│   ├── debug.ts            # gameguild:runtime/debug@0.1.0
│   ├── test.ts             # gameguild:runtime/test@0.1.0
│   └── index.ts
│
├── builtin/                # JavaScript builtin bindings
│   ├── math.ts             # Math API
│   ├── date.ts             # Date API
│   ├── number.ts           # Number types (I8, I16, I32, I64, U8, U16, U32, U64, F32, F64)
│   ├── performance.ts      # Performance API
│   ├── crypto.ts           # Crypto API
│   ├── string.ts           # String utilities
│   ├── object.ts           # Object operations
│   ├── reflect.ts          # Reflect API
│   ├── js.ts               # JavaScript environment
│   ├── window.ts           # Window API stubs
│   ├── document.ts         # Document API stubs
│   ├── dom.ts              # DOM utilities
│   ├── memory.ts           # Memory helpers
│   └── index.ts
│
└── legacy/                 # Legacy flat imports (backward compat)
    ├── env.ts              # env namespace (C/Emscripten style)
    ├── assemblyscript.ts   # assembly/index (AssemblyScript)
    ├── go.ts               # go/GOImports (Go WASM)
    ├── wasi.ts             # WASI Preview 1
    └── index.ts
```

## Usage

### Component Model (Modern)

```typescript
import { createComponentModelBindings } from './wat-env-bindings'

const imports = createComponentModelBindings(
  (text) => console.log(text),
  (text) => console.error(text)
)
```

### Legacy Mode

```typescript
import { createWatEnvironment } from './wat-env-bindings'

const imports = createWatEnvironment(stdout, stderr)
```

## WASI Preview 2 Interfaces

- `wasi:cli/*` - CLI environment, stdin/stdout/stderr, exit
- `wasi:clocks/*` - Wall clock and monotonic clock
- `wasi:io/*` - Streams and polling
- `wasi:random/*` - Random number generation
- `wasi:filesystem/*` - Filesystem operations

## GameGuild Interfaces

- `gameguild:runtime/console@0.1.0` - Console logging
- `gameguild:runtime/debug@0.1.0` - Debug utilities
- `gameguild:runtime/test@0.1.0` - Test framework

## References

- [Component Model](https://github.com/WebAssembly/component-model)
- [WASI Preview 2](https://github.com/WebAssembly/WASI/tree/main/preview2)
- [WIT Specification](https://github.com/WebAssembly/component-model/blob/main/design/mvp/WIT.md)
