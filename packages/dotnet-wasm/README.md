# DotNet Web - C# Compiler & Runtime for Browser

Complete C# compilation and execution environment running 100% in the browser using Mono WebAssembly and Roslyn.

## Features

- ✅ **Full C# Support**: Compile and run C# code in the browser
- ✅ **Roslyn Compiler**: Microsoft's official C# compiler running in WebAssembly
- ✅ **Mono Runtime**: Execute compiled IL assemblies with Mono WASM interpreter
- ✅ **Zero Server Dependencies**: Everything runs client-side
- ✅ **Cached Assets**: Smart caching for fast subsequent loads
- ✅ **Compressed Delivery**: Gzip-compressed runtime files for minimal download size

## Architecture

```
User C# Code
    ↓
Roslyn Compiler (in Mono WASM)
    ↓
IL Assembly (in memory)
    ↓
Mono WASM Interpreter
    ↓
Execution Result
```

## Project Structure

```
dotnet-web/
├── public/
│   ├── dotnet.wasm.gz       # Mono WebAssembly runtime (compressed)
│   ├── dotnet.js.gz         # Mono JS loader (compressed)
│   ├── managed/             # .NET managed assemblies
│   │   ├── System.*.dll.gz
│   │   ├── Microsoft.CodeAnalysis.*.dll.gz
│   │   └── RoslynWrapper.dll.gz
│   └── icu.dat.gz          # Internationalization data (compressed)
├── src/
│   ├── index.ts            # Main API
│   ├── main.ts             # Entry point & orchestration
│   └── csharp/
│       ├── runtime-loader.ts  # Mono WASM loader
│       ├── compiler.ts        # Roslyn compilation
│       └── executor.ts        # IL execution
└── dotnet-runtime/
    ├── RoslynWrapper.csproj   # .NET project
    └── RoslynWrapper.cs       # Roslyn API wrapper
```

## Building the Runtime

### Prerequisites

- .NET 8 SDK
- dotnet-wasm-pack (for Mono WASM)

### Build Steps

1. **Build RoslynWrapper**:
```bash
cd dotnet-runtime
dotnet publish -c Release -r browser-wasm
```

2. **Compress Assets**:
```bash
npm run compress-assets
```

3. **Build TypeScript**:
```bash
npm run build
```

## Usage

```typescript
import { CSharpCompiler } from '@gameguild/dotnet-web'

// Initialize compiler
const compiler = new CSharpCompiler()
await compiler.initialize()

// Compile and run C# code
const code = `
using System;

class Program {
    static void Main() {
        Console.WriteLine("Hello from C#!");
    }
}
`

const result = await compiler.execute(code)
console.log(result.stdout) // "Hello from C#!"
console.log(result.exitCode) // 0
```

## Integration with Code Studio

This package is designed to be consumed by the `dotnet-runner.ts` in the Code Studio project:

```typescript
// In code-studio/runners/dotnet-runner.ts
import { CSharpCompiler } from '@gameguild/dotnet-web'

export class DotNetRunner implements CodeRunner {
  private compiler = new CSharpCompiler()
  
  async execute(code: string): Promise<RunnerResult> {
    return await this.compiler.execute(code)
  }
}
```

## Technical Details

### Mono WebAssembly

We use Mono's WebAssembly build which includes:
- Complete .NET runtime
- JIT compilation support
- Full BCL (Base Class Library)
- Threading support (with proper headers)

### Roslyn Compilation

The compilation happens entirely in the browser:
1. C# source code → Roslyn SyntaxTree
2. SyntaxTree → Compilation
3. Compilation → IL Assembly (in memory)
4. IL Assembly → Execution in Mono

### Performance

- **First Load**: ~10-15 seconds (downloading runtime)
- **Subsequent Loads**: < 1 second (cached)
- **Compilation**: ~500ms - 2s (depends on code size)
- **Execution**: Near-native speed with Mono JIT

## License

MIT
