# Contributing to @game-guild/dotnet-wasm

Thank you for your interest in contributing! This document provides guidelines and setup instructions.

## Development Setup

### Prerequisites

- **Node.js** 18+ with npm
- **.NET 8 SDK** ([download](https://dotnet.microsoft.com/download))
- **Git**

### Initial Setup

```bash
# Clone the repository
git clone https://github.com/game-guild/gameguild.git
cd gameguild/packages/dotnet-wasm

# Install dependencies
npm install

# Install .NET WASM workload
dotnet workload install wasm-tools

# Build everything
npm run setup
```

## Project Structure

```
dotnet-wasm/
├── dotnet-runtime/          # C# project that compiles to WASM
│   ├── Program.cs           # Roslyn wrapper with JSExport
│   ├── main.js              # JavaScript entry point
│   └── RoslynWrapper.csproj # .NET project file
├── src/                     # TypeScript source
│   ├── index.ts             # Public API
│   ├── csharp/
│   │   └── runtime-loader.ts # .NET runtime loader
│   └── runtime/             # Copied runtime files (auto-generated)
├── public/                  # Static assets
│   ├── managed/             # .NET assemblies (auto-generated)
│   └── test.html            # Test page
├── dist/                    # Build output (auto-generated)
└── examples/                # Usage examples
```

## Development Workflow

### 1. Making Changes to C# Code

When modifying `dotnet-runtime/Program.cs`:

```bash
# Rebuild .NET runtime
npm run build-runtime

# Test changes
npm run dev
# Open http://localhost:5173/test.html
```

### 2. Making Changes to TypeScript Code

When modifying files in `src/`:

```bash
# Watch mode for development
npm run dev

# Or build once
npm run build
```

### 3. Testing

```bash
# Start dev server
npm run dev

# Open browser
open http://localhost:5173/test.html

# Test single file compilation (Tab 1)
# Test multiple files (Tab 2)
```

### 4. Full Build

```bash
# Complete build from scratch
npm run setup
```

This runs:
1. `build-runtime` - Compiles C# to WASM
2. `compress` - Compresses assets
3. `build` - Builds TypeScript
4. Type checks and lints

## Build Scripts

### `build-dotnet.sh`

Compiles the C# Roslyn wrapper to WebAssembly.

**What it does:**
1. Checks for .NET SDK
2. Installs wasm-tools workload if needed
3. Restores NuGet packages
4. Publishes for browser-wasm target
5. Copies output to `public/managed/`
6. Cleans up npm workspace conflicts

### `compress-assets.sh`

Compresses WASM and JS files with gzip.

**What it does:**
1. Compresses all files in `public/managed/`
2. Creates `.gz` versions alongside originals
3. Preserves originals for development

## Code Style

### TypeScript

- Use TypeScript strict mode
- Prefer `async/await` over callbacks
- Document public APIs with JSDoc
- Use meaningful variable names

```typescript
/**
 * Compile and execute C# code
 */
async execute(code: string): Promise<CSharpResult> {
  // Implementation
}
```

### C#

- Follow .NET naming conventions
- Use XML documentation comments
- Keep methods focused and small

```csharp
/// <summary>
/// Compiles and runs C# code, returning output
/// </summary>
[JSExport]
public static string CompileAndRun(string code)
{
    // Implementation
}
```

## Common Tasks

### Adding a New C# API

1. Add method in `dotnet-runtime/Program.cs`:
```csharp
[JSExport]
public static string MyNewMethod(string input)
{
    return $"Processed: {input}";
}
```

2. Expose in `dotnet-runtime/main.js`:
```javascript
window.CSharpCompiler = {
    compileAndRun: exports.RoslynWrapper.Program.CompileAndRun,
    myNewMethod: exports.RoslynWrapper.Program.MyNewMethod
};
```

3. Add TypeScript wrapper in `src/index.ts`:
```typescript
async myNewMethod(input: string): Promise<string> {
  return (window as any).CSharpCompiler.myNewMethod(input)
}
```

4. Rebuild:
```bash
npm run build-runtime
npm run build
```

### Adding a NuGet Package

1. Edit `dotnet-runtime/RoslynWrapper.csproj`:
```xml
<ItemGroup>
  <PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
</ItemGroup>
```

2. Rebuild:
```bash
npm run build-runtime
```

### Debugging

#### C# Compilation Issues

Check the browser console for detailed error messages:
```javascript
const result = window.CSharpCompiler.compileAndRun(code)
console.log(result) // Shows compilation errors
```

#### Runtime Loading Issues

Enable diagnostic tracing in `dotnet-runtime/main.js`:
```javascript
const { ... } = await dotnet
    .withDiagnosticTracing(true)  // Enable tracing
    .create();
```

#### Build Issues

```bash
# Clean everything
rm -rf dotnet-runtime/bin dotnet-runtime/obj
rm -rf public/managed/*
rm -rf dist/

# Rebuild from scratch
npm run setup
```

## Testing Guidelines

### Manual Testing

1. **Single File**: Test basic C# execution
2. **Multiple Files**: Test namespace imports
3. **Error Handling**: Test compilation errors
4. **Performance**: Monitor execution time

### Adding Examples

Add new examples to `examples/` directory:

```typescript
// examples/linq-example.ts
import { CSharpCompiler } from '@game-guild/dotnet-wasm'

const compiler = new CSharpCompiler()
await compiler.initialize()

const result = await compiler.execute(`
using System;
using System.Linq;

class Program {
    static void Main() {
        var numbers = new[] { 1, 2, 3, 4, 5 };
        var sum = numbers.Sum();
        Console.WriteLine($"Sum: {sum}");
    }
}
`)

console.log(result.output)
```

## Pull Request Process

1. **Fork** the repository
2. **Create** a feature branch: `git checkout -b feature/my-feature`
3. **Make** your changes
4. **Test** thoroughly using `test.html`
5. **Commit** with clear messages: `git commit -m "Add multi-file compilation support"`
6. **Push**: `git push origin feature/my-feature`
7. **Create** a Pull Request

### PR Checklist

- [ ] Code builds without errors (`npm run build`)
- [ ] Changes tested in browser
- [ ] Documentation updated if API changed
- [ ] No console errors or warnings
- [ ] Follows existing code style

## Release Process

Only for maintainers:

```bash
# Update version in package.json
npm version patch  # or minor, major

# Build
npm run setup

# Publish
npm publish
```

## Architecture Decisions

### Why Blazor?

.NET 8 browser-wasm is Blazor-centric and doesn't support standalone initialization. Attempting to use `dotnet.js` without Blazor fails due to:
- Missing `blazor.boot.json` manifest
- SHA256 integrity verification requirements
- Assembly lazy-loading mechanism

### Why JSExport?

JSExport provides clean JavaScript↔C# interop:
- Type-safe method calls
- Automatic marshaling
- Better than string-based reflection

### Why Basic.Reference.Assemblies?

In WASM, `Assembly.Location` returns empty string. Basic.Reference.Assemblies provides in-memory metadata references needed by Roslyn for compilation.

## Getting Help

- **Issues**: [GitHub Issues](https://github.com/game-guild/gameguild/issues)
- **Discussions**: [GitHub Discussions](https://github.com/game-guild/gameguild/discussions)
- **Discord**: [Game Guild Discord](https://discord.gg/gameguild)

## License

By contributing, you agree that your contributions will be licensed under the MIT License.
