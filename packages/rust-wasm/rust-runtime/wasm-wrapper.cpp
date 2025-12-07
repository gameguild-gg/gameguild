/**
 * mrustc WASM Wrapper
 * 
 * This file wraps mrustc functionality to be callable from JavaScript
 * using Emscripten's ccall/cwrap
 */

#include <string>
#include <emscripten.h>
#include <emscripten/bind.h>

// Forward declarations for mrustc functions
// These would be from mrustc's actual implementation
namespace mrustc {
    std::string compile(const std::string& source_code, const std::string& options);
    std::string compile_multi(const std::string& files_json, const std::string& options);
}

extern "C" {

/**
 * Compile single Rust source file
 * 
 * @param source_code - Rust source code as string
 * @param options_json - JSON string with compilation options
 * @return Compilation result (SUCCESS\noutput or ERROR\nerror_message)
 */
EMSCRIPTEN_KEEPALIVE
const char* compile_rust(const char* source_code, const char* options_json) {
    try {
        std::string code(source_code);
        std::string options(options_json);
        
        // Call mrustc compiler
        std::string result = mrustc::compile(code, options);
        
        // Return result (memory managed by Emscripten)
        static std::string output;
        output = result;
        return output.c_str();
        
    } catch (const std::exception& e) {
        static std::string error_msg;
        error_msg = std::string("ERROR\n") + e.what();
        return error_msg.c_str();
    }
}

/**
 * Compile multiple Rust source files (multi-file project)
 * 
 * @param files_json - JSON string with {filename: content} pairs
 * @param options_json - JSON string with compilation options
 * @return Compilation result
 */
EMSCRIPTEN_KEEPALIVE
const char* compile_rust_multi(const char* files_json, const char* options_json) {
    try {
        std::string files(files_json);
        std::string options(options_json);
        
        // Call mrustc multi-file compiler
        std::string result = mrustc::compile_multi(files, options);
        
        static std::string output;
        output = result;
        return output.c_str();
        
    } catch (const std::exception& e) {
        static std::string error_msg;
        error_msg = std::string("ERROR\n") + e.what();
        return error_msg.c_str();
    }
}

} // extern "C"

// Emscripten bindings (alternative to ccall)
EMSCRIPTEN_BINDINGS(mrustc_module) {
    emscripten::function("compileRust", &compile_rust);
    emscripten::function("compileRustMulti", &compile_rust_multi);
}
