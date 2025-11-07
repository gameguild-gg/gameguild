# Understanding HTML5 publishing target

HTML5 target can be achieved by using web native solutions such as HTML, JavaScript and CSS, and enhanced by using WebAssembly (WASM) technologies.

## Understanding HTML5 for games

HTML is a markup language that is used to create web pages. It is a readable text-based language that is used to describe the structure and content of a web page. HTML is a markup language that is used to create web pages. It is a text-based language that is used to describe the structure and content of a web page.

In order to make games with HTML5 you either code it manually (create your own solution), use any HTML5 native framework/library/engine (such phaser, playcanvas, treejs and others), or use a game engine that can be compiled to WebAssembly.

## WebAssembly

WebAssembly (WASM) is a binary instruction format that is designed to be executed by web browsers. It is a low-level language that is close to the native machine code, and can be used to run high-performance applications on the web.

WebAssembly is a binary instruction format that is designed to be executed by web browsers. It is a low-level language that is close to the native machine code, and can be used to run high-performance applications on the web.

``` mermaid
graph TD
    C[C/C++] --> cclang[Emscripten - Clang - LLVM]
    cclang --> WASM[.wasm file]
    rust[Rust] --> rclang[Rust - LLVM]
    rclang --> WASM
    cs[C#] --> csc[.Net - IL]
    csc --> WASM
    go[Go] --> gclang[Go - Plan9]
    gclang --> WASM
    WASM --> WASMVM[WebAssembly VMs]
    WASMVM --> Browsers
    WASMVM --> Wasmer[Wasmer / Wasmtime]
    WASMVM --> V8[NodeJS]
    Browsers --> Native
    Wasmer --> Native
    V8 --> Native
    Native --> CPU
    Browsers --> WebGL
    WebGL --> GPU
    Browsers --> WebGPU
    WebGPU --> GPU
    CPU --> x86
    CPU --> ARM
    CPU --> RISCV
```

## WebGL

WebGL is a JavaScript API that is used to render 2D and 3D graphics in web browsers. It is a low-level API that is designed to be used with WebAssembly, and can be used to create interactive graphics in web browsers. So every call to the GPU is a call to the Javascript WebGL API. This can be problematic for WebAssembly, because it is an extraneus environment outsite the wasm, and it is a o common source of slowdowns and issues.

## WebGPU

WebGPU is a low-level graphics API that is designed to be used with WebAssembly, and can be used to create high-performance graphics in web browsers.

## Common Restrictions

- Memory
- Threads
- Network