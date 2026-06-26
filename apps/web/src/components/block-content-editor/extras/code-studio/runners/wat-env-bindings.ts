/**
 * WebAssembly Environment Bindings for WAT Runner
 * 
 * This file provides a comprehensive set of environment imports that WebAssembly
 * modules (compiled from WAT, AssemblyScript, C, Rust, etc.) can use.
 * 
 * Organized into logical namespaces matching common WebAssembly import conventions.
 * 
 * @deprecated Use 'wat-env-bindings/index' instead. This file is kept for backward compatibility.
 */

export { createWatEnvironment, createMemoryHelpers, createDOMBindings } from './wat-env-bindings/index'
