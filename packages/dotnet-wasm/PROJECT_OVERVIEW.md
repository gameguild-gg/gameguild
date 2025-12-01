# DotNet Runner - Project Overview

## 📋 Summary

Complete C# compiler and runtime implementation for web browsers, enabling full C# compilation and execution without any server dependencies. Built using Mono WebAssembly and Microsoft's Roslyn compiler.

## 🎯 Key Features

✅ **Full C# 12 Support** - Modern C# features  
✅ **Roslyn Compiler** - Microsoft's official C# compiler  
✅ **Mono WASM Runtime** - Execute compiled IL assemblies  
✅ **100% Client-Side** - No server required  
✅ **Smart Caching** - Fast subsequent loads with IndexedDB  
✅ **Compressed Assets** - Gzip compression for minimal downloads  
✅ **Type Safety** - Full TypeScript support  
✅ **Error Reporting** - Detailed compilation and runtime errors  

## 📁 Project Structure

```
gameguild/
├── apps/
│   ├── runners/
│   │   └── dotnet-web/              # Standalone C# compiler package
│   │       ├── src/
│   │       │   ├── index.ts         # Main API
│   │       │   ├── types.ts         # Type definitions
│   │       │   └── csharp/
│   │       │       ├── runtime-loader.ts  # Mono WASM loader
│   │       │       ├── compiler.ts        # Roslyn compilation
│   │       │       └── executor.ts        # IL execution
│   │       ├── dotnet-runtime/
│   │       │   ├── RoslynWrapper.csproj   # .NET project
│   │       │   └── RoslynWrapper.cs       # Roslyn API wrapper
│   │       ├── public/                     # Runtime assets (generated)
│   │       ├── package.json
│   │       ├── build-dotnet.sh            # Build script
│   │       ├── compress-assets.sh         # Compression script
│   │       └── integrate.sh               # Integration script
│   │
│   └── web/
│       └── src/components/editor/extras/code-studio/
│           └── runners/
│               ├── dotnet-runner.ts       # Code Studio integration
│               ├── index.ts               # Runner registry (modified)
│               ├── DOTNET_RUNNER_SETUP.md # Detailed setup guide
│               └── DOTNET_EXAMPLES.ts     # Usage examples
```

## 🔄 Architecture Flow

```
┌─────────────────┐
│  User C# Code   │
└────────┬────────┘
         │
         ▼
┌─────────────────────────────┐
│   dotnet-runner.ts          │
│   (Code Studio Integration) │
└────────┬────────────────────┘
         │
         ▼
┌─────────────────────────────┐
│   CSharpCompiler             │
│   (Main API)                 │
└────────┬────────────────────┘
         │
    ┌────┴────┐
    │         │
    ▼         ▼
┌────────┐ ┌────────┐
│Compiler│ │Executor│
└───┬────┘ └───┬────┘
    │          │
    └────┬─────┘
         ▼
┌─────────────────┐
│  Mono WASM      │
│  Runtime        │
└─────────────────┘
         │
         ▼
┌─────────────────┐
│  Roslyn         │
│  (C# Compiler)  │
└─────────────────┘
         │
         ▼
┌─────────────────┐
│  IL Assembly    │
│  (In Memory)    │
└─────────────────┘
         │
         ▼
┌─────────────────┐
│  Execution      │
│  Result         │
└─────────────────┘
```

## 🚀 Quick Start

### Prerequisites
- Node.js 18+
- .NET 8 SDK
- gzip

### Build Everything
```bash
cd apps/runners/dotnet-web
npm install
npm run setup
```

This runs all build steps:
1. Builds RoslynWrapper to WebAssembly
2. Compresses all assets
3. Builds TypeScript
4. Copies files to web app

### Use in Code Studio
```typescript
import { UnifiedCodeRunner } from './runners'

const runner = new UnifiedCodeRunner()

const code = `
using System;
class Program {
    static void Main() {
        Console.WriteLine("Hello from C#!");
    }
}
`

const result = await runner.run('csharp', code)
console.log(result.stdout) // "Hello from C#!"
```

## 📦 Components

### 1. DotNet Web Package (`apps/runners/dotnet-web/`)

Standalone TypeScript package that provides:
- Mono WASM runtime loading
- Roslyn C# compilation
- IL assembly execution
- Caching and optimization

**Main Files:**
- `src/index.ts` - Public API
- `src/csharp/runtime-loader.ts` - Loads Mono WASM
- `src/csharp/compiler.ts` - Wraps Roslyn compiler
- `src/csharp/executor.ts` - Executes IL assemblies

### 2. RoslynWrapper (`dotnet-runtime/`)

C# project compiled to WebAssembly:
- Exposes Roslyn compilation API
- Handles assembly references
- Manages compilation options
- Emits IL assemblies

**Key File:**
- `RoslynWrapper.cs` - Main program that invokes Roslyn

### 3. DotNet Runner (`runners/dotnet-runner.ts`)

Integration layer for Code Studio:
- Implements `CodeRunner` interface
- Manages compiler lifecycle
- Handles errors and timeouts
- Provides progress callbacks

## 🔧 Build Process

### Step 1: Build RoslynWrapper
```bash
./build-dotnet.sh
```
Compiles C# to WebAssembly → `public/managed/`

### Step 2: Compress Assets
```bash
./compress-assets.sh
```
Gzip all .dll and .wasm files → `.gz` versions

### Step 3: Build TypeScript
```bash
npm run build
```
Compiles TypeScript → `dist/`

### Step 4: Integrate
```bash
./integrate.sh
```
Copies files → `apps/web/public/dotnet/`

## 📊 Performance

| Metric | Value |
|--------|-------|
| First Load | ~18 MB download (gzipped) |
| Subsequent Loads | ~0 MB (cached) |
| Initialization | ~5-10 seconds (first time) |
| Initialization | <1 second (cached) |
| Compilation | ~500ms - 2s |
| Execution | Near-native speed |

## 🎓 Usage Examples

### Basic Execution
```typescript
const result = await runner.run('csharp', code)
```

### With Timeout
```typescript
const runner = new UnifiedCodeRunner({
  timeout: 10000 // 10 seconds
})
```

### With Progress Callback
```typescript
const runner = new UnifiedCodeRunner({
  onProgress: (msg) => console.log(msg)
})
```

### Preload Compiler
```typescript
import { preloadDotNetCompiler } from './runners'

// During app startup
await preloadDotNetCompiler()
```

## 🐛 Troubleshooting

### "Failed to load DotNet Web module"
→ Run `npm run setup` in `apps/runners/dotnet-web/`

### "Failed to fetch /dotnet/dotnet.wasm.gz"
→ Run `./integrate.sh` to copy files to web app

### Slow initialization
→ Normal on first load. Files are cached for subsequent loads.

### Compilation errors
→ Check stderr output for detailed Roslyn error messages

## 📚 Documentation

- **[QUICK_START.md](../../runners/dotnet-web/QUICK_START.md)** - Quick build guide
- **[DOTNET_RUNNER_SETUP.md](./DOTNET_RUNNER_SETUP.md)** - Detailed setup
- **[DOTNET_EXAMPLES.ts](./DOTNET_EXAMPLES.ts)** - Code examples
- **[README.md](../../runners/dotnet-web/README.md)** - Package overview

## 🔮 Future Enhancements

- [ ] Multi-file C# projects
- [ ] NuGet package support
- [ ] Blazor component compilation
- [ ] C# script (.csx) support
- [ ] Debugging support with source maps
- [ ] REPL mode for interactive C#
- [ ] Worker thread execution for UI responsiveness
- [ ] .NET Standard library expansion

## 📝 License

MIT

## 🙏 Credits

- **Mono Project** - WebAssembly runtime
- **Roslyn** - C# compiler
- **GameGuild Team** - Integration and packaging

---

**Status:** ✅ Ready for use  
**Version:** 1.0.0  
**Last Updated:** 2025-01-29
