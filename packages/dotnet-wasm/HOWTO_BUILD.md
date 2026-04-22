# How to Build @game-guild/dotnet-wasm

Complete guide to building the C# compiler from source.

## Prerequisites

### Required Software

1. **.NET 8 SDK** (8.0.100 or later)
   ```bash
   # Check version
   dotnet --version
   
   # Install from https://dotnet.microsoft.com/download
   ```

2. **wasm-tools workload**
   ```bash
   # Install
   dotnet workload install wasm-tools
   
   # Verify
   dotnet workload list
   ```

3. **Node.js 18+**
   ```bash
   # Check version
   node --version
   npm --version
   ```

4. **Build tools** (Linux/macOS)
   ```bash
   # Ubuntu/Debian
   sudo apt-get install build-essential
   
   # macOS
   xcode-select --install
   ```

## Quick Build

```bash
# From packages/dotnet-wasm directory
npm install
npm run setup
```

This runs the complete build pipeline:
1. Compiles C# to WebAssembly
2. Compresses assets
3. Builds TypeScript
4. Generates type definitions

## Build Steps Explained

### Step 1: Install Dependencies

```bash
npm install
```

**What it does:**
- Installs TypeScript compiler
- Installs Vite bundler
- Installs development dependencies
- Does NOT install .NET dependencies (handled by `dotnet restore`)

### Step 2: Build .NET Runtime

```bash
npm run build-runtime
```

**Executes:** `build-dotnet.sh`

**Process:**
1. Checks .NET SDK availability
2. Installs wasm-tools if missing
3. Cleans previous build artifacts
4. Restores NuGet packages:
   - Microsoft.CodeAnalysis.CSharp 4.9.2
   - Basic.Reference.Assemblies.Net80 1.7.0
   - System.Text.Json
5. Publishes for `browser-wasm` runtime:
   ```bash
   dotnet publish -c Release -r browser-wasm \
     /p:RunAOTCompilation=false
   ```
6. Removes package.json conflicts
7. Creates `public/managed/` directory
8. Copies `AppBundle/_framework/*` to `public/managed/`
9. Copies custom `main.js`
10. Final cleanup

**Output:**
- ~200 WASM files in `public/managed/`
- `blazor.boot.json` manifest
- ~50MB of runtime assets

### Step 3: Compress Assets

```bash
npm run compress
```

**Executes:** `compress-assets.sh`

**Process:**
```bash
cd public/managed
for file in *.wasm *.js *.json; do
  gzip -9 -k "$file"
done
```

**Output:**
- `.gz` versions of all files
- Original files preserved
- ~60% size reduction

### Step 4: Build TypeScript

```bash
npm run build
```

**Executes:** `tsc && vite build`

**Process:**
1. **TypeScript compilation:**
   - Compiles `src/**/*.ts`
   - Generates type definitions
   - Outputs to `dist/`

2. **Vite bundling:**
   - Bundles dependencies
   - Minifies code
   - Generates sourcemaps

**Output:**
- `dist/index.js` - Main bundle
- `dist/index.d.ts` - Type definitions
- `dist/runtime/` - Runtime files

## Build Script Details

### build-dotnet.sh

```bash
#!/bin/bash
set -e

# Configuration
PROJECT_DIR="./dotnet-runtime"
OUTPUT_DIR="./public/managed"

# Check .NET SDK
if ! command -v dotnet &> /dev/null; then
    echo "Error: .NET SDK not found"
    exit 1
fi

# Install workload if needed
if ! dotnet workload list | grep -q "wasm-tools"; then
    echo "Installing wasm-tools workload..."
    dotnet workload install wasm-tools
fi

# Clean previous build
cd "$PROJECT_DIR"
rm -rf bin obj

# Remove package.json conflicts (before build)
find . -name "package.json" -not -path "*/node_modules/*" -delete

# Restore packages
dotnet restore

# Publish for browser-wasm
dotnet publish -c Release -r browser-wasm \
    /p:RunAOTCompilation=false \
    /p:WasmBuildNative=false

# Create output directory
mkdir -p "../$OUTPUT_DIR"

# Clean old output
rm -rf "../$OUTPUT_DIR"/*

# Copy framework files
cp -r "bin/Release/net8.0/browser-wasm/AppBundle/_framework/"* "../$OUTPUT_DIR/"

# Copy custom main.js
cp main.js "../$OUTPUT_DIR/"

# Remove package.json conflicts (after copy)
find "../$OUTPUT_DIR" -name "package.json" -delete

echo "✓ .NET runtime built successfully"
```

### compress-assets.sh

```bash
#!/bin/bash
set -e

MANAGED_DIR="./public/managed"

if [ ! -d "$MANAGED_DIR" ]; then
    echo "Error: $MANAGED_DIR not found. Run build-runtime first."
    exit 1
fi

cd "$MANAGED_DIR"

echo "Compressing assets..."
for file in *.wasm *.js *.json; do
    if [ -f "$file" ]; then
        gzip -9 -k "$file"
        echo "  ✓ $file → $file.gz"
    fi
done

echo "✓ Assets compressed"
```

## Output Structure

After a complete build:

```
dotnet-wasm/
├── public/
│   └── managed/              # .NET runtime assets
│       ├── blazor.boot.json
│       ├── dotnet.js
│       ├── dotnet.native.wasm
│       ├── dotnet.native.js
│       ├── main.js           # Our custom entry point
│       ├── *.wasm            # ~200 Webcil assemblies
│       └── *.gz              # Compressed versions
├── dist/
│   ├── index.js              # Bundled TypeScript
│   ├── index.d.ts            # Type definitions
│   └── runtime/              # Runtime loader
└── dotnet-runtime/
    ├── bin/Release/          # .NET build output
    └── obj/                  # .NET intermediate files
```

## Troubleshooting

### Error: "No .NET SDKs were found"

**Solution:**
```bash
# Download from https://dotnet.microsoft.com/download
# Or use package manager:
# Ubuntu/Debian
wget https://dot.net/v1/dotnet-install.sh
chmod +x dotnet-install.sh
./dotnet-install.sh --channel 8.0
```

### Error: "Workload 'wasm-tools' not installed"

**Solution:**
```bash
dotnet workload install wasm-tools
```

### Error: "npm workspace conflicts"

**Symptom:** Multiple `@microsoft/dotnet-runtime` workspaces detected

**Solution:** Already handled by build script, but manually:
```bash
find public/managed -name "package.json" -delete
```

### Error: "public/managed not found"

**Solution:**
```bash
mkdir -p public/managed
npm run build-runtime
```

### Error: "CS0246: The type or namespace name could not be found"

**Symptom:** Missing assembly references in C# code

**Solution:**
1. Check NuGet package versions in `RoslynWrapper.csproj`
2. Restore packages:
   ```bash
   cd dotnet-runtime
   dotnet restore
   ```

### Error: "Cannot wait on monitors on this runtime"

**Symptom:** Threading error in Roslyn

**Solution:** Already fixed with `concurrentBuild: false` in `Program.cs`

### Build is slow

**Optimization:**
```bash
# Skip compression during development
npm run build-runtime
npm run dev
```

### Large file sizes

**Expected sizes:**
- `dotnet.native.wasm` - ~40MB
- All assemblies - ~50MB total
- Compressed - ~20MB total

**If larger:**
- Check for duplicate files
- Ensure compression ran
- Verify no development builds mixed in

## Development Build

For faster iteration during development:

```bash
# Terminal 1: Watch C# changes
cd dotnet-runtime
dotnet watch publish -c Release -r browser-wasm

# Terminal 2: Watch TypeScript changes  
npm run dev
```

**Benefits:**
- Automatic rebuilds on file changes
- Faster incremental compilation
- Live reload with Vite

## Production Build

For deployment:

```bash
# Clean everything
rm -rf dotnet-runtime/bin dotnet-runtime/obj
rm -rf public/managed/*
rm -rf dist/

# Full rebuild
npm run setup

# Verify output
ls -lh public/managed/*.wasm.gz
```

**Checklist:**
- [ ] All `.wasm` files have `.wasm.gz` versions
- [ ] `blazor.boot.json` present
- [ ] `main.js` contains custom code
- [ ] `dist/` has bundled output
- [ ] Type definitions generated

## Advanced Configuration

### Reduce Bundle Size

Edit `dotnet-runtime/RoslynWrapper.csproj`:

```xml
<PropertyGroup>
  <!-- Enable trimming -->
  <PublishTrimmed>true</PublishTrimmed>
  
  <!-- Aggressive trimming -->
  <TrimMode>link</TrimMode>
  
  <!-- Remove unused locales -->
  <InvariantGlobalization>true</InvariantGlobalization>
</PropertyGroup>
```

**Warning:** May break reflection-based code

### Enable AOT Compilation

```xml
<PropertyGroup>
  <RunAOTCompilation>true</RunAOTCompilation>
  <WasmBuildNative>true</WasmBuildNative>
</PropertyGroup>
```

**Trade-offs:**
- ✅ Faster runtime performance
- ❌ Much slower build times
- ❌ Larger file sizes

### Custom Runtime Configuration

Edit `dotnet-runtime/main.js`:

```javascript
const { getAssemblyExports, getConfig } = await dotnet
    .withDiagnosticTracing(true)      // Enable logging
    .withDebugging(0)                  // Debugging level
    .withMainAssembly("RoslynWrapper") // Main assembly name
    .create();
```

## File Manifest

### Critical Files

**Do not modify:**
- `public/managed/blazor.boot.json` - Auto-generated manifest
- `public/managed/dotnet.*.js` - Runtime bootstrapper
- `public/managed/*.wasm` - Compiled assemblies

**Safe to modify:**
- `public/managed/main.js` - Custom entry point
- `dotnet-runtime/Program.cs` - Roslyn wrapper
- `src/**/*.ts` - TypeScript API

### Auto-generated (gitignored)

```
dotnet-runtime/bin/
dotnet-runtime/obj/
public/managed/
dist/
node_modules/
*.gz
```

## CI/CD Integration

### GitHub Actions Example

```yaml
name: Build

on: [push, pull_request]

jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      
      - uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '8.0.x'
      
      - uses: actions/setup-node@v3
        with:
          node-version: '18'
      
      - name: Install .NET workload
        run: dotnet workload install wasm-tools
      
      - name: Build
        run: |
          cd packages/dotnet-wasm
          npm install
          npm run setup
      
      - name: Upload artifacts
        uses: actions/upload-artifact@v3
        with:
          name: dist
          path: packages/dotnet-wasm/dist/
```

## Performance Metrics

**Build times (M1 MacBook Pro):**
- `build-runtime`: ~45 seconds
- `compress`: ~10 seconds
- `build` (TypeScript): ~5 seconds
- **Total:** ~60 seconds

**File sizes:**
- Uncompressed: ~50MB
- Compressed: ~20MB
- Transferred (gzip): ~15MB (browser caches rest)

## Next Steps

After building successfully:
1. Test with `npm run dev` → `http://localhost:5173/test.html`
2. Try single file compilation
3. Try multiple files with imports
4. See `CONTRIBUTING.md` for development workflow

## Support

If you encounter build issues:
1. Check this troubleshooting section
2. Search [GitHub Issues](https://github.com/game-guild/gameguild/issues)
3. Ask in [Discussions](https://github.com/game-guild/gameguild/discussions)
