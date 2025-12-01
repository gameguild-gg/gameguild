# IMPORTANT: Mono WASM Runtime Files

## ⚠️ Critical Notice

The C# runner requires **Mono WebAssembly runtime files** which are **NOT** included in this repository due to their large size (~50MB uncompressed).

## 📦 What You Need

You need to obtain these files:

```
public/
├── dotnet.wasm          (~15 MB)
├── dotnet.js            (~200 KB)
├── icudt.dat            (~5 MB - optional for internationalization)
└── managed/
    ├── System.Private.CoreLib.dll
    ├── System.Runtime.dll
    ├── System.Console.dll
    ├── System.Collections.dll
    ├── System.Linq.dll
    ├── Microsoft.CodeAnalysis.dll
    ├── Microsoft.CodeAnalysis.CSharp.dll
    └── ... (other assemblies)
```

## 🔧 How to Obtain Runtime Files

### Option 1: Build from .NET 8 SDK (Recommended)

This is what `build-dotnet.sh` does:

```bash
cd dotnet-runtime
dotnet publish -c Release -r browser-wasm -o ../public/managed
```

This will:
1. Compile RoslynWrapper.cs
2. Download Mono WASM runtime
3. Copy all necessary assemblies to `public/managed/`

**The runtime files come from the .NET SDK when publishing for browser-wasm target.**

### Option 2: Download from .NET Distribution

You can manually download Mono WASM from:
- https://github.com/dotnet/runtime/releases
- Look for `dotnet-runtime-8.0.x-browser-wasm` packages

### Option 3: Use Existing Build

If you have access to another project that uses .NET WASM, you can copy the runtime files from there.

## 📝 Step-by-Step Setup

### 1. Install .NET 8 SDK

**Ubuntu/Debian:**
```bash
wget https://dot.net/v1/dotnet-install.sh -O dotnet-install.sh
chmod +x dotnet-install.sh
./dotnet-install.sh --channel 8.0
```

**macOS:**
```bash
brew install dotnet@8
```

**Windows:**
Download from https://dotnet.microsoft.com/download/dotnet/8.0

### 2. Verify Installation

```bash
dotnet --version
# Should show: 8.0.x
```

### 3. Build Runtime

```bash
cd apps/runners/dotnet-web
./build-dotnet.sh
```

This will:
- Install workload for browser-wasm if needed
- Build RoslynWrapper
- **Download and extract Mono WASM runtime files**
- Place everything in `public/managed/`

### 4. Compress Assets

```bash
./compress-assets.sh
```

Creates `.gz` versions for web delivery.

### 5. Integrate

```bash
./integrate.sh
```

Copies to web app's public directory.

## 🚨 Common Issues

### "workload 'wasm-tools' not found"

**Solution:**
```bash
dotnet workload install wasm-tools
```

### "Cannot find runtime files"

**Solution:** The runtime files are downloaded automatically when you run:
```bash
dotnet publish -r browser-wasm
```

This is a standard .NET feature, not custom code.

### "Files are too large"

**Solution:** Use compression:
```bash
./compress-assets.sh
```

This reduces size by ~70%.

## 📊 Expected File Sizes

After building and compressing:

| File | Uncompressed | Gzipped |
|------|-------------|---------|
| dotnet.wasm | ~15 MB | ~5 MB |
| System.Private.CoreLib.dll | ~6 MB | ~2 MB |
| All managed assemblies | ~30 MB | ~10 MB |
| **Total** | **~50 MB** | **~18 MB** |

## ✅ Verification

After building, verify these files exist:

```bash
ls -lh apps/runners/dotnet-web/public/

# Should see:
# - dotnet.wasm
# - dotnet.wasm.gz
# - dotnet.js
# - dotnet.js.gz
# - managed/ (directory with many .dll files)
```

## 🎯 Why Not Include in Repository?

1. **Size**: ~50 MB uncompressed, ~18 MB compressed
2. **Updates**: Runtime updates frequently with .NET releases
3. **License**: Runtime files have separate licensing
4. **Build reproducibility**: Better to build from official .NET SDK

## 📚 Additional Resources

- [.NET WASM Documentation](https://learn.microsoft.com/en-us/aspnet/core/blazor/host-and-deploy/webassembly)
- [Mono WASM Runtime](https://github.com/dotnet/runtime/tree/main/src/mono/wasm)
- [Browser WASM Workload](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-workload-install)

## 🆘 Need Help?

If you encounter issues getting the runtime files:

1. Ensure .NET 8 SDK is properly installed
2. Try installing wasm-tools workload manually
3. Check your internet connection (runtime is downloaded)
4. Review build output for specific errors

## 🎉 Success Indicator

You'll know it worked when:

```bash
# This shows your compiled assembly + runtime files
ls public/managed/*.dll

# Should list 20+ DLL files including:
# - RoslynWrapper.dll
# - System.*.dll (many files)
# - Microsoft.CodeAnalysis.*.dll
```

---

**Summary:** You need .NET 8 SDK → Run `./build-dotnet.sh` → Runtime files downloaded automatically
