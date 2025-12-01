# Quick Start Guide

## Build Steps

### 1. Install Dependencies
```bash
npm install
```

### 2. Build .NET Runtime
```bash
./build-dotnet.sh
```

This compiles RoslynWrapper to WebAssembly and outputs to `public/managed/`.

### 3. Compress Assets
```bash
./compress-assets.sh
```

This creates `.gz` versions of all runtime files.

### 4. Build TypeScript
```bash
npm run build
```

## Development

### Run Demo Server
```bash
npm run dev
```

Open http://localhost:5173 to test the compiler.

## Deployment

### Copy to Web App
```bash
# From this directory
cp -r public/* ../../web/public/dotnet/
```

### Verify Files
Ensure these files exist in your web app's public directory:
```
public/dotnet/
├── managed/
    ├── dotnet.native.wasm.gz
    ├── dotnet.js.gz
    ├── RoslynWrapper.dll.gz
    ├── System.*.dll.gz
    └── Microsoft.CodeAnalysis.*.dll.gz
```

## Usage

```typescript
import { CSharpCompiler } from '@gameguild/dotnet-web'

const compiler = new CSharpCompiler('/dotnet')
await compiler.initialize()

const result = await compiler.execute(`
using System;

class Program {
    static void Main() {
        Console.WriteLine("Hello!");
    }
}
`)

console.log(result.stdout) // "Hello!"
```

## Troubleshooting

### Build fails
- Ensure .NET 8 SDK is installed: `dotnet --version`
- Run `dotnet restore` in `dotnet-runtime/`

### Runtime not loading
- Check browser console for 404 errors
- Verify files are in `/dotnet/` path
- Check CORS headers if loading from different origin

### Compilation errors
- Check that all required assemblies are in `public/managed/`
- Rebuild with `./build-dotnet.sh`

## Next Steps

See [DOTNET_RUNNER_SETUP.md](../../DOTNET_RUNNER_SETUP.md) for detailed documentation.
