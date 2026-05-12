# Code Studio

A powerful, browser-based code execution environment built with React, Monaco Editor, and WebAssembly. Code Studio provides a complete IDE experience with multi-file support, customizable layouts, and execution capabilities for multiple programming languages.

## Architecture Overview

Code Studio is built on a modular architecture that separates concerns into distinct layers:

```
┌─────────────────────────────────────────────────────────────┐
│                    Code Studio Editor                        │
│  (Main orchestration layer - code-studio-editor.tsx)        │
└─────────────────────────────────────────────────────────────┘
                            │
        ┌───────────────────┼───────────────────┐
        │                   │                   │
┌───────▼────────┐  ┌──────▼──────┐  ┌────────▼─────────┐
│  File System   │  │   Layout    │  │   Execution      │
│   Management   │  │  Management │  │    Runners       │
└────────────────┘  └─────────────┘  └──────────────────┘
        │                   │                   │
┌───────▼────────┐  ┌──────▼──────┐  ┌────────▼─────────┐
│ Monaco Editor  │  │  Grid/Panel │  │  WASM Runtime    │
│  Integration   │  │   System    │  │   (Runno/WASI)   │
└────────────────┘  └─────────────┘  └──────────────────┘
```

## Core Components

### 1. Code Studio Editor (`code-studio-editor.tsx`)

The main orchestration component that manages:
- **File State Management**: Tracks all files, folders, and their hierarchy
- **Editor Instances**: Manages multiple editor instances (unique/shared)
- **Execution Coordination**: Handles code execution through the unified runner system
- **Layout Management**: Controls display configurations and panel arrangements
- **User Interactions**: Coordinates file operations, tab management, and settings

**Key Features:**
- Multi-file project support with folder structure
- Real-time code execution with terminal output
- Customizable panel layouts (explorer, editor, output)
- Progressive compilation feedback for compiled languages
- Interactive input support for programs requiring stdin

### 2. File System Layer

#### File Explorer (`file-system/file-explorer.tsx`)
- Tree view of files and folders
- Drag & drop support
- File/folder creation, deletion, and renaming
- Context menu operations

#### File Operations (`file-operations.ts`)
Utility functions for:
- Creating, updating, and deleting files
- Managing folder structures
- Path normalization and validation
- File tree manipulation

#### Monaco File System (`monaco-file-system.ts`)
- Integrates with Monaco Editor's virtual file system
- Syncs file changes between UI and editor
- Manages file models and their lifecycle
- Handles TypeScript type definitions

### 3. Layout System

The layout system provides flexible panel arrangements with multiple display configurations:

#### Display Manager (`display-manager.tsx`)
- Manages multiple display configurations
- Handles display switching and creation
- Coordinates panel state across displays

#### Grid System (`grid-utils.ts`, `grid-drop-zone.tsx`)
- Responsive grid-based layout (12x12, 24x12, 12x24)
- Drag & drop panel positioning
- Dynamic resizing and repositioning
- Three aspect ratios: 2:1 (landscape), 1:1 (square), 1:2 (portrait)

#### Panel Types
- **Explorer**: File tree navigation
- **Editor**: Code editing with Monaco
- **Output**: Terminal/console output

#### Default Layouts (`default-layouts.ts`)
Pre-configured layouts for common use cases:
- Classic IDE (explorer + editor + output)
- Editor-focused (large editor + minimal output)
- Split-screen (dual editors)
- Multi-display configurations

### 4. Editor Integration

#### Monaco Code Editor (`monaco-code-editor.tsx`)
- Full-featured code editor using Monaco (VS Code engine)
- Syntax highlighting via Shiki (45+ languages)
- IntelliSense and auto-completion
- Multi-cursor editing
- Find & replace
- Code folding
- Minimap support

#### Editor Instance Management (`editor-instance-switch.tsx`)
Two modes:
- **Unique**: Each display has its own editor instance and file tabs
- **Multiple**: All displays share the same editor state and tabs

### 5. Execution System

#### Unified Code Runner (`runners/index.ts`)

A unified interface for executing code across multiple languages with consistent API:

```typescript
interface CodeRunner {
  execute(code: string): Promise<ExecutionResult>
  executeWithFiles(files: CodeFile[]): Promise<ExecutionResult>
  cleanup?(): Promise<void>
}
```

**Supported Languages:**

##### Interpreted Languages (Client-side execution)
- **JavaScript** (`javascript-runner.ts`): Direct browser execution
- **TypeScript** (`typescript-runner.ts`): esbuild transpilation → execution
- **Python** (`python-runner.ts`): Pyodide (Python in WASM)
- **Lua** (`lua-runner.ts`): Fengari (Lua in WASM)
- **PHP** (`php-runner.ts`): PHP-CGI 8.2.0 (WASI runtime)
- **SQL** (`sql-runner.ts`): SQLite 3.x (WASI runtime)

##### Compiled Languages (3-stage pipeline)
- **C** (`c-runner.ts`): clang → wasm-ld → WASI execution
- **C++** (`cpp-runner.ts`): clang++ → wasm-ld → WASI execution

**Compilation Pipeline for C/C++:**

```
Stage 1: Compile to Object File
┌──────────────────────────────────────────┐
│  Source Code (.c/.cpp)                   │
│           ↓                              │
│  clang.wasm (LLVM Compiler)             │
│           ↓                              │
│  Object File (.o)                        │
└──────────────────────────────────────────┘

Stage 2: Link to WebAssembly
┌──────────────────────────────────────────┐
│  Object File (.o)                        │
│           ↓                              │
│  wasm-ld (LLVM Linker)                  │
│    + libc.a, libc++.a, libc++abi.a      │
│           ↓                              │
│  WebAssembly Binary (.wasm)             │
└──────────────────────────────────────────┘

Stage 3: Execute
┌──────────────────────────────────────────┐
│  WebAssembly Binary (.wasm)             │
│           ↓                              │
│  WASI Runtime (WASIWorkerHost)          │
│           ↓                              │
│  stdout, stderr, exit code              │
└──────────────────────────────────────────┘
```

#### Progressive Feedback System

All runners support progressive feedback through callbacks:

```typescript
const runner = new UnifiedCodeRunner({
  language: 'cpp',
  onProgress: (message) => {
    terminal.write(`\x1b[36m${message}\x1b[0m\r\n`)
  },
  onRequestInput: async (prompt) => {
    return await getInputFromUser(prompt)
  }
})
```

**Progress stages for C/C++:**
1. "Loading compiler..." (loading clang.wasm)
2. "Compiling {language} code..." (compilation stage)
3. "Linking WebAssembly..." (linking stage)
4. "Running program..." (execution stage)

#### WASM Loader (`runners/wasm-loader.ts`)

Efficient loading and caching of WebAssembly binaries:

**Features:**
- **Client-side decompression**: Fetches .wasm.gz and .tar.gz files, decompresses with pako
- **IndexedDB caching**: Persistent cache with version control
- **Blob URL generation**: Required for Web Workers (cannot use relative paths)
- **Filesystem extraction**: Extracts .tar.gz archives for C/C++ standard libraries

**Cache Structure:**
```typescript
interface CachedWasm {
  version: number // CACHE_FORMAT_VERSION for invalidation
  buffer: ArrayBuffer
  timestamp: number
}

interface CachedFileSystem {
  version: number
  files: Record<string, {
    content: Uint8Array // base64 encoded in chunks
    mode: number
    timestamps: {
      access: number
      modify: number
    }
  }>
}
```

**WASM Assets:**
- `clang.wasm.gz`: ~10.13 MB (LLVM C/C++ compiler)
- `wasm-ld.wasm.gz`: ~6.46 MB (LLVM linker)
- `clang-fs.tar.gz`: ~1.71 MB (409 files: C/C++ headers + runtime libraries)
- `python-3.11.3.wasm.gz`: ~9.50 MB (CPython 3.11.3 WASI - alternative runner)
- `python-3.11.3.tar.gz`: ~3.88 MB (Python 3.11.3 standard library)
- `php-cgi.wasm.gz`: ~3.95 MB (PHP 8.2.0 interpreter)
- `sqlite.wasm.gz`: ~1.23 MB (SQLite 3.x database engine)
- `ruby.wasm.gz`: ~7.3 MB (Ruby 3.2.0 interpreter)
- `pyodide.asm.wasm.gz`: ~3.07 MB (Python interpreter via Pyodide)
- `pyodide.asm.js.gz`: ~0.22 MB (Pyodide runtime)
- `python_stdlib.zip`: ~2.23 MB (Python standard library)
- `esbuild.wasm.gz`: ~3.46 MB (TypeScript/JavaScript bundler)
- `quickjs-asyncify.wasm.gz`: ~0.37 MB (QuickJS engine)
- `wabt` (npm package): WebAssembly Binary Toolkit for WAT compilation

**Total compressed size:** ~53.49 MB

**Note**: Python has two runner implementations available. Switch between them in `runners/index.ts` via `RUNNER_SELECTION.PYTHON_RUNNER`.

### 6. Terminal Integration

#### XTerm Terminal (`xterm-terminal.tsx`)
- Full terminal emulation using xterm.js
- ANSI color support
- Interactive input handling
- Scrollback buffer
- Copy/paste support
- Resizable viewport

**Terminal Features:**
- Real-time output streaming during execution
- Progressive compilation feedback (cyan colored)
- Error messages with stack traces
- Exit code reporting
- Execution time tracking

### 7. Mode System

#### Execution Mode (`execution`)
- Write and run code with live output
- Multi-file project support
- Terminal with stdin/stdout/stderr
- Progressive compilation feedback

**Supported Languages:**
JavaScript, TypeScript, Python, Lua, C, C++, PHP, SQL

#### Test Mode (`test`)
- Automated test case execution
- Three test types:
  - **Simple**: Input/output matching
  - **Function**: Function call testing with arguments
  - **Custom**: Custom validation code
- Test result visualization
- Pass/fail indicators

**Supported Languages:**
JavaScript, TypeScript, Python, Lua, C, C++, PHP, SQL

### 8. Settings & Configuration

#### Settings Menu (`settings-menu.tsx`)
Configurable options:
- **Editor**: Line numbers, font size, word wrap, minimap
- **Theme**: Light/dark mode, Shiki theme selection
- **Execution**: Clear on run, auto-run on file change
- **Layout**: Panel visibility, aspect ratio, grid editing

#### Theme System
**Shiki Themes:**
- GitHub (dark/light)
- GitHub Default (dark/light)
- GitHub Dimmed (dark/light)
- Plus (dark/light)

Auto-switches based on system theme preference.

## Data Flow

### File Editing Flow
```
User types in Monaco Editor
        ↓
monaco-code-editor.tsx captures onChange
        ↓
Updates file content in state
        ↓
Syncs to Monaco file system (if TS/JS)
        ↓
Updates file tree display
        ↓
Marks file as modified (unsaved indicator)
```

### Code Execution Flow
```
User clicks "Run" button
        ↓
code-studio-editor.tsx → handleExecute()
        ↓
Clears terminal output
        ↓
Creates UnifiedCodeRunner with callbacks
        ↓
Runner loads WASM binaries (if needed)
        ↓
Executes code with progressive feedback
        ↓
onProgress → writes to terminal (cyan)
        ↓
Completion → writes final output
        ↓
Displays stdout, stderr, exit code, time
```

### Layout Change Flow
```
User drags panel in grid editor
        ↓
grid-drop-zone.tsx captures drop event
        ↓
Validates new position/size
        ↓
layout-operations.ts updates config
        ↓
display-manager.tsx re-renders panels
        ↓
Persists to CodeStudioData state
```

## State Management

### Primary State (CodeStudioData)
```typescript
interface CodeStudioData {
  id: string
  files: CodeFile[]
  folders: FileTreeFolder[]
  openTabs: string[]
  activeFileId?: string
  mode: CodeStudioMode
  language: SupportedLanguage
  
  // UI Configuration
  readonly?: boolean
  showLineNumbers?: boolean
  fontSize?: number
  theme?: "light" | "dark" | "system"
  shikiTheme?: ShikiTheme
  
  // Layout
  layout?: LayoutConfig
  
  // Execution
  clearOnRun?: boolean
  autoRun?: boolean
  
  // Testing
  testCases?: Record<string, TestCase[]>
  showTests?: boolean
}
```

### Layout State
```typescript
interface LayoutConfig {
  displays: DisplayConfig[]
  activeDisplayId: string
  editMode: boolean
}

interface DisplayConfig {
  id: string
  name: string
  aspectRatio: AspectRatio // "2:1" | "1:1" | "1:2"
  panels: PanelConfig[]
  uniqueOpenTabs?: string[] // For unique editor instances
  uniqueActiveFileId?: string
}

interface PanelConfig {
  id: string
  type: PanelType // "explorer" | "editor" | "output"
  row: number
  col: number
  rowSpan: number
  colSpan: number
  editorInstance?: EditorInstance // "multiple" | "unique"
}
```

## File System Structure

### Virtual File Tree
```typescript
interface CodeFile {
  id: string
  name: string
  content: string
  language: SupportedLanguage
  isMain: boolean // Entry point for execution
  isVisible: boolean
  path: string // "src/components/Button.tsx"
}

interface FileTreeFolder {
  id: string
  name: string
  path: string
  isExpanded: boolean
  children: (CodeFile | FileTreeFolder)[]
  type: "folder"
}
```

### Path Resolution
- Root-relative paths: `/src/utils/helper.ts`
- Nested folders: `/src/components/ui/Button.tsx`
- Path normalization handles: `./`, `../`, multiple slashes
- Monaco URI scheme: `file:///path/to/file.ts`

## Multi-File Project Support

### TypeScript/JavaScript
- Automatic module resolution
- Import/export support
- Type definitions (.d.ts)
- esbuild bundling for execution

### C/C++
- Header file support (.h, .hpp)
- Multi-file compilation
- Standard library access via clang-fs.tar.gz
- Proper include paths: `/sys/include`, `/sys/include/c++/v1`

### Python
- Multi-module support
- Relative imports
- Package structure

### Lua
- require() support
- Module system

## Performance Optimizations

### WASM Caching
- IndexedDB persistence
- Version-based invalidation
- Compressed storage
- Lazy loading (load on first use)

### Editor Optimizations
- Virtual scrolling for large files
- Debounced file system sync
- Model reuse across tabs
- Lazy Monaco initialization

### Execution Optimizations
- Web Workers for compilation (non-blocking UI)
- Blob URLs for WASM (faster than fetch)
- Chunked output streaming
- Progressive feedback reduces perceived latency

## Browser Compatibility

### Requirements
- **SharedArrayBuffer**: Required for WASM threading
- **Cross-Origin Headers**: COOP/COEP for SharedArrayBuffer
- **IndexedDB**: For WASM caching
- **Web Workers**: For non-blocking execution
- **Modern JavaScript**: ES2020+ features

### Tested Browsers
- Chrome 90+ ✅
- Firefox 89+ ✅
- Safari 15.2+ ✅
- Edge 90+ ✅

## Security Considerations

### Sandbox Execution
- All code runs in Web Workers (isolated from main thread)
- WASI sandbox limits filesystem/network access
- No access to user's local filesystem
- Memory limits enforced by browser

### Cross-Origin Policy
```javascript
// Required headers for SharedArrayBuffer
Cross-Origin-Opener-Policy: same-origin
Cross-Origin-Embedder-Policy: require-corp
```

## Future Enhancements

### Planned Languages
- **Go**: TinyGo WASM compiler
- **Rust**: rustc with wasm32-wasi target
- **C#**: Blazor WASM runtime
- **Ruby**: ruby.wasm
- **Assembly variants**: x86, ARM, MIPS, RISC-V, PowerPC (display only)

### Planned Features
- Collaborative editing (WebRTC/WebSocket)
- Git integration (isomorphic-git)
- Package manager support (npm, pip, cargo)
- Debugger integration
- Performance profiling
- Code completion improvements
- LSP integration for advanced languages

## Development

### Adding a New Language Runner

1. Create runner file in `runners/`:
```typescript
// runners/your-language-runner.ts
import { CodeRunner, ExecutionResult, RunnerOptions } from './types'

export class YourLanguageRunner implements CodeRunner {
  constructor(private options: RunnerOptions) {}
  
  async execute(code: string): Promise<ExecutionResult> {
    // Implementation
  }
  
  async executeWithFiles(files: CodeFile[]): Promise<ExecutionResult> {
    // Multi-file support
  }
}
```

2. Register in `runners/index.ts`:
```typescript
case 'yourlang':
  return new YourLanguageRunner(this.options)
```

3. Update `types.ts`:
```typescript
export type SupportedLanguage = 
  | "javascript"
  | "yourlang" // Add here
  // ...
```

4. Add language config:
```typescript
yourlang: {
  label: "Your Language",
  monacoLanguage: "yourlang",
  defaultExtension: ".yl",
  supportsExecution: true,
  defaultTemplate: "// Your template"
}
```

### Project Structure
```
code-studio/
├── code-studio-editor.tsx       # Main component
├── types.ts                     # TypeScript interfaces
├── file-system/                 # File tree components
│   └── file-explorer.tsx
├── runners/                     # Execution engines
│   ├── index.ts                 # Unified runner
│   ├── javascript-runner.ts
│   ├── typescript-runner.ts
│   ├── python-runner.ts
│   ├── lua-runner.ts
│   ├── c-runner.ts
│   ├── cpp-runner.ts
│   ├── wasm-loader.ts           # WASM loading/caching
│   ├── esbuild-shared.ts        # esbuild utilities
│   └── types.ts                 # Runner interfaces
├── monaco-code-editor.tsx       # Monaco integration
├── monaco-file-system.ts        # Virtual FS sync
├── xterm-terminal.tsx           # Terminal component
├── display-manager.tsx          # Layout orchestration
├── grid-utils.ts                # Grid calculations
├── grid-drop-zone.tsx           # Drag & drop
├── resizable-panel.tsx          # Panel component
├── file-tabs.tsx                # Tab bar
├── settings-menu.tsx            # Settings UI
├── mode-selector.tsx            # Mode switching
└── *-operations.ts              # Utility functions
```

## License

This component is part of the GameGuild project and follows the project's license terms.

## Contributing

Contributions are welcome! Please follow the project's contribution guidelines and ensure:
- Code is properly typed (TypeScript strict mode)
- All new languages include proper runner implementation
- Layout changes maintain grid constraints
- Performance optimizations are tested with large files
- Documentation is updated accordingly
