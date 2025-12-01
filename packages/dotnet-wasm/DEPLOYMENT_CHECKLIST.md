# C# Runner - Deployment Checklist

Use this checklist to ensure your C# runner is properly deployed and working.

## 📋 Pre-Deployment

### Development Environment
- [ ] .NET 8 SDK installed (`dotnet --version` shows 8.0.x)
- [ ] Node.js 18+ installed (`node --version` shows 18.x+)
- [ ] gzip available (`gzip --version` works)
- [ ] Git repository is clean

## 🔨 Build Process

### Step 1: Build Runtime
```bash
cd apps/runners/dotnet-web
```

- [ ] Run `npm install`
- [ ] Run `./build-dotnet.sh`
- [ ] Verify `public/managed/` contains .dll files
- [ ] Verify `public/dotnet.wasm` exists
- [ ] Verify `public/dotnet.js` exists

### Step 2: Compress Assets
- [ ] Run `./compress-assets.sh`
- [ ] Verify `public/dotnet.wasm.gz` exists
- [ ] Verify `public/dotnet.js.gz` exists
- [ ] Verify `public/managed/*.dll.gz` files exist

### Step 3: Build TypeScript
- [ ] Run `npm run build`
- [ ] Verify `dist/` directory exists
- [ ] Verify `dist/index.js` exists
- [ ] No TypeScript errors

### Step 4: Integration
- [ ] Run `./integrate.sh`
- [ ] Verify files copied to `../../web/public/dotnet/`
- [ ] Check console output for "Integration Complete"

## 🌐 Web App Configuration

### Next.js Configuration
- [ ] Open `apps/web/next.config.ts`
- [ ] Verify CORS headers configured:
```typescript
async headers() {
  return [
    {
      source: '/dotnet/:path*',
      headers: [
        { key: 'Cross-Origin-Embedder-Policy', value: 'require-corp' },
        { key: 'Cross-Origin-Opener-Policy', value: 'same-origin' },
      ],
    },
  ]
}
```

### Static File Serving
- [ ] Verify `/dotnet/` path is publicly accessible
- [ ] Test: `curl http://localhost:3000/dotnet/dotnet.wasm.gz` returns file
- [ ] Content-Encoding header set for .gz files

## 🧪 Testing

### Local Testing
- [ ] Run development server: `cd apps/web && npm run dev`
- [ ] Open browser: `http://localhost:3000`
- [ ] Open browser console (F12)
- [ ] Navigate to Code Studio
- [ ] Select C# language

### Basic Test
- [ ] Paste this code:
```csharp
using System;
class Program {
    static void Main() {
        Console.WriteLine("Hello from C#!");
    }
}
```
- [ ] Click "Run"
- [ ] Verify output: "Hello from C#!"
- [ ] Verify exit code: 0

### Advanced Test
- [ ] Test LINQ code:
```csharp
using System;
using System.Linq;
class Program {
    static void Main() {
        var nums = new[] {1,2,3,4,5};
        Console.WriteLine(nums.Sum());
    }
}
```
- [ ] Verify output: "15"

### Error Test
- [ ] Test compilation error:
```csharp
using System;
class Program {
    static void Main() {
        Console.WriteLine("Missing semicolon")
    }
}
```
- [ ] Verify error message displayed in stderr

### Performance Test
- [ ] First run: Note initialization time (5-10 seconds is normal)
- [ ] Second run: Should be much faster (<1 second)
- [ ] Check browser DevTools → Network tab
- [ ] Verify files loaded from cache on second run

## 🔍 Verification

### File Verification
```bash
# Check web app public directory
ls -lh apps/web/public/dotnet/

# Should see:
# dotnet.wasm.gz (~5 MB)
# dotnet.js.gz (~200 KB)
# managed/ (directory)
# managed/RoslynWrapper.dll.gz
# managed/System.*.dll.gz (many files)
```

### Browser Console Checks
Open DevTools Console and verify:
- [ ] No 404 errors for /dotnet/ files
- [ ] No CORS errors
- [ ] See "[DotNet] Loading runtime script..."
- [ ] See "[DotNet] Runtime initialized successfully"

### Network Panel Checks
Open DevTools Network tab:
- [ ] Files loaded with Content-Encoding: gzip
- [ ] Total download size ~18 MB (first load)
- [ ] Subsequent loads use cache (0 bytes transferred)

## 🚀 Production Deployment

### Build for Production
- [ ] Run `npm run build` in web app
- [ ] Verify `.next/` directory created
- [ ] Test production build locally: `npm run start`

### CDN Setup (Optional)
- [ ] Upload `/dotnet/` directory to CDN
- [ ] Update `basePath` in CSharpCompiler initialization
- [ ] Test loading from CDN URL

### Monitoring
- [ ] Add error tracking (Sentry, etc.)
- [ ] Monitor bundle size
- [ ] Track initialization times
- [ ] Monitor cache hit rates

## 📊 Performance Benchmarks

Expected performance metrics:

| Metric | Target | Actual |
|--------|--------|--------|
| First Load Time | 5-10s | _____ |
| Cached Load Time | <1s | _____ |
| Compilation Time | 0.5-2s | _____ |
| Total Download Size | ~18 MB | _____ |
| Cache Hit Rate | >90% | _____ |

## ❌ Common Issues

### Issue: "Failed to load DotNet Web module"
- [ ] Verify `dotnet-web` package is built
- [ ] Check import path in `dotnet-runner.ts`
- [ ] Run `npm run build` in dotnet-web directory

### Issue: "404 on /dotnet/dotnet.wasm.gz"
- [ ] Run `./integrate.sh` to copy files
- [ ] Verify files in `apps/web/public/dotnet/`
- [ ] Restart development server

### Issue: "CORS error"
- [ ] Add headers in `next.config.ts`
- [ ] Restart server after config change
- [ ] Check browser console for specific error

### Issue: Slow performance
- [ ] Verify files are gzip compressed
- [ ] Check IndexedDB cache is working
- [ ] Monitor Network tab for cache hits

### Issue: Compilation errors
- [ ] Check RoslynWrapper.dll is present
- [ ] Verify all System.*.dll files are available
- [ ] Check browser console for missing assemblies

## ✅ Final Checks

Before marking as complete:
- [ ] All build steps complete without errors
- [ ] Basic C# code compiles and runs
- [ ] LINQ code works
- [ ] Error messages display correctly
- [ ] Second run is fast (cached)
- [ ] No console errors
- [ ] Documentation is complete
- [ ] Team members can build and test

## 🎉 Success Criteria

Your deployment is successful when:
1. ✅ Users can write C# code in Code Studio
2. ✅ Code compiles in browser
3. ✅ Execution results display correctly
4. ✅ Performance is acceptable
5. ✅ Subsequent runs are fast

## 📝 Sign-Off

- [ ] Deployment completed by: ________________
- [ ] Date: ________________
- [ ] Verified by: ________________
- [ ] Production ready: Yes / No

---

**Next Steps After Deployment:**
1. Monitor error rates
2. Collect user feedback
3. Track performance metrics
4. Plan feature enhancements
