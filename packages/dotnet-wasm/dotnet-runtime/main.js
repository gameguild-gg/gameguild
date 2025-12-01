// This file is the entry point for the .NET WASM runtime
// It exposes the C# CompileAndRun function to JavaScript

import { dotnet } from './dotnet.js'

const { setModuleImports, getAssemblyExports, getConfig } = await dotnet
    .withDiagnosticTracing(false)
    .create();

const config = getConfig();
const exports = await getAssemblyExports(config.mainAssemblyName);

// Expose to global window
window.CSharpCompiler = {
    compileAndRun: exports.RoslynWrapper.Program.CompileAndRun
};

console.log('C# Compiler initialized and ready!');
