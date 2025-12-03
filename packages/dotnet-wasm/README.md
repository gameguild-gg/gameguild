# @game-guild/dotnet-wasm

C# Compiler and Runtime for Browser - 100% Client-Side Execution

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![.NET 8](https://img.shields.io/badge/.NET-8.0-purple.svg)](https://dotnet.microsoft.com/)

## Overview

Complete C# compilation and execution environment running entirely in the browser using .NET 8 WebAssembly and Roslyn compiler. No server required.

## Features

- ✅ **Full C# 12 Support** - Latest C# language features
- ✅ **Roslyn Compiler** - Microsoft's official C# compiler in WASM
- ✅ **Multi-file Projects** - Support for multiple .cs files with imports
- ✅ **.NET 8 Runtime** - Modern .NET runtime via Blazor WebAssembly
- ✅ **Zero Server Dependencies** - Everything runs client-side
- ✅ **Smart Caching** - Fast subsequent loads with browser caching
- ✅ **Compressed Assets** - Gzip compression for minimal download (~15MB)

## Quick Start

### Installation

```bash
npm install @game-guild/dotnet-wasm
```

### Basic Usage

```typescript
import { CSharpCompiler } from '@game-guild/dotnet-wasm'

// Initialize compiler (one-time setup)
const compiler = new CSharpCompiler()
await compiler.initialize()

// Compile and run C# code
const result = await compiler.execute(`
using System;

class Program 
{
    static void Main() 
    {
        Console.WriteLine("Hello from C# in WASM!");
        Console.WriteLine($"Current time: {DateTime.Now}");
    }
}
`)

console.log(result.output)
// Output:
// Hello from C# in WASM!
// Current time: 12/3/2025 10:30:45 AM
```

### Multiple Files

```typescript
const files = [
  {
    name: 'Person.cs',
    content: `
using System;

namespace MyApp 
{
    public class Person 
    {
        public string Name { get; set; }
        public void Greet() => Console.WriteLine($"Hello, I'm {Name}!");
    }
}
`
  }
]

const mainCode = `
using System;
using MyApp;

class Program 
{
    static void Main() 
    {
        var person = new Person { Name = "Alice" };
        person.Greet();
    }
}
`

const result = await compiler.executeMultiple(mainCode, files)
console.log(result.output) // "Hello, I'm Alice!"
```

## API Reference

### `CSharpCompiler`

Main class for compiling and executing C# code.

#### Methods

##### `initialize(): Promise<void>`

Initialize the .NET runtime. Must be called before executing code.

##### `execute(code: string): Promise<CSharpResult>`

Compile and execute a single C# file.

**Parameters:**
- `code` - C# source code containing a `Main` method

**Returns:** `CSharpResult`

##### `executeMultiple(mainCode: string, files: CSharpFile[]): Promise<CSharpResult>`

Compile and execute multiple C# files.

**Parameters:**
- `mainCode` - Main program code containing the `Main` method
- `files` - Array of additional C# files

**Returns:** `CSharpResult`

### Types

```typescript
interface CSharpResult {
  output?: string        // Program output (Console.WriteLine)
  error?: string         // Compilation or runtime error
  executionTime: number  // Execution time in milliseconds
}

interface CSharpFile {
  name: string    // File name (e.g., "Models/Person.cs")
  content: string // C# source code
}
```

## Examples

See the `/examples` directory for complete examples:

- **Basic Hello World** - Simple console output
- **LINQ Queries** - Working with collections
- **Multiple Files** - Classes across multiple files
- **Object-Oriented** - Inheritance, interfaces, polymorphism

## Architecture

### Stack

- **.NET 8 SDK** - `browser-wasm` runtime identifier
- **Roslyn 4.9.2** - Microsoft.CodeAnalysis.CSharp compiler
- **Basic.Reference.Assemblies.Net80** - In-memory assembly references
- **Blazor WebAssembly** - Runtime loader and bootstrap

### Workflow

```
User C# Code
    ↓
Roslyn Parser (C# → Syntax Tree)
    ↓
Roslyn Compiler (Syntax Tree → IL Assembly)
    ↓
Assembly.Load (IL → Executable)
    ↓
Reflection (Find & Invoke Main)
    ↓
Execution Result
```

### File Structure

```
public/managed/          # .NET Runtime Files (~50MB)
├── dotnet.native.wasm   # .NET WASM runtime (2.8MB)
├── dotnet.js            # Runtime loader
├── blazor.boot.json     # Assembly manifest
├── main.js              # JSExport bridge
├── RoslynWrapper.wasm   # Our compiler wrapper
├── System.*.wasm        # BCL assemblies
└── Microsoft.CodeAnalysis.*.wasm  # Roslyn

dist/
├── dotnet-web.es.js     # ES Module build
└── dotnet-web.umd.js    # UMD build
```

## Performance

- **First Load**: ~5-8s (downloads + initialization)
- **Cached Load**: ~1-2s (initialization only)
- **Simple Compilation**: ~500ms
- **Complex Compilation**: ~1-2s
- **Bundle Size**: ~50MB uncompressed, ~15MB with gzip

## Limitations

- **Threading**: Not supported (WASM is single-threaded)
- **File I/O**: In-memory only, no real filesystem access
- **Network**: Fetch API works, raw sockets don't
- **Size**: Large initial download (~50MB assemblies)

## Browser Support

- ✅ Chrome 90+
- ✅ Firefox 88+
- ✅ Safari 15.4+
- ✅ Edge 90+

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md) for development setup and guidelines.

## Building

See [HOWTO_BUILD.md](HOWTO_BUILD.md) for detailed build instructions.

## License

MIT - see [LICENSE](LICENSE) file for details.

## Credits

- Built with [.NET 8](https://dotnet.microsoft.com/)
- Powered by [Roslyn](https://github.com/dotnet/roslyn)
- Uses [Basic.Reference.Assemblies](https://github.com/jaredpar/basic-reference-assemblies)

---

Made with ❤️ by [Game Guild](https://gameguild.gg)
