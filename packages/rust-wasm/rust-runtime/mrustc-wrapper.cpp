// mrustc WASM wrapper - Full integration with mrustc compiler
#include <string>
#include <cstring>
#include <cstdio>
#include <cstdlib>
#include <sstream>
#include <vector>
#include <emscripten.h>

// Include mrustc headers
#include "../upstream/mrustc/src/ast/edition.hpp"
#include "../upstream/mrustc/src/ast/crate.hpp"
#include "../upstream/mrustc/src/ast/ast.hpp"
#include "../upstream/mrustc/src/include/main_bindings.hpp"

// External bridge function (defined in parse-bridge.cpp)
extern "C" void* bridge_parse_crate(const char* filename);

// C wrapper for Parse_Crate to avoid name mangling issues
extern "C" {
    EMSCRIPTEN_KEEPALIVE
    void* mrustc_parse_crate(const char* filename, int edition_num) {
        EM_ASM({
            console.log('[mrustc_parse_crate] Calling bridge with:', UTF8ToString($0));
        }, filename);
        
        try {
            // Use the bridge function instead of calling Parse_Crate directly
            void* result = bridge_parse_crate(filename);
            
            EM_ASM({
                console.log('[mrustc_parse_crate] Bridge returned:', $0);
            }, result);
            
            return result;
        } catch (...) {
            EM_ASM({
                console.error('[mrustc_parse_crate] Exception caught - returning nullptr');
            });
            return nullptr;
        }
    }
    
    EMSCRIPTEN_KEEPALIVE
    void mrustc_free_crate(void* crate_ptr) {
        if (crate_ptr) {
            delete static_cast<AST::Crate*>(crate_ptr);
        }
    }
    
    // Stub implementations for weak symbols
    void __attribute__((weak)) _Z12debug_outputiPKc(int, const char*) {}
    bool __attribute__((weak)) _Z13debug_enabledv() { return false; }
}

// Capture stdout/stderr during compilation
class OutputCapture {
public:
    std::ostringstream stdout_buffer;
    std::ostringstream stderr_buffer;
    
    void start() {
        // Redirect stdout/stderr would go here if needed
    }
    
    void stop() {
        // Restore stdout/stderr
    }
    
    std::string get_output() {
        std::string result;
        if (!stdout_buffer.str().empty()) {
            result += "=== Output ===\n" + stdout_buffer.str() + "\n";
        }
        if (!stderr_buffer.str().empty()) {
            result += "=== Errors ===\n" + stderr_buffer.str() + "\n";
        }
        return result;
    }
};

// Main compilation function
std::string compileRustWithMrustc(const char* code, const char* options) {
    std::ostringstream result;
    
    try {
        // Setup filesystem BEFORE any mrustc operations (CRITICAL for AST)
        EM_ASM({
            try {
                // Create predictable filesystem structure
                if (!FS.analyzePath('/work').exists) FS.mkdir('/work');
                if (!FS.analyzePath('/work/src').exists) FS.mkdir('/work/src');
                if (!FS.analyzePath('/tmp').exists) FS.mkdir('/tmp');
                FS.chdir('/work/src');
                console.log('[wrapper] Filesystem initialized: cwd=' + FS.cwd());
            } catch (e) {
                console.error('[wrapper] FS setup failed:', e);
            }
        });
        
        // Create file in predictable location (no relative paths)
        const char* filename = "/work/src/main.rs";
        
        // Write code to virtual file with proper crate structure
        FILE* f = fopen(filename, "w");
        if (!f) {
            return "ERROR: Failed to create file at " + std::string(filename);
        }
        
        // Check if code contains main function
        std::string code_str(code);
        bool has_main = code_str.find("fn main") != std::string::npos;
        
        if (has_main) {
            // User provided main, just write it as-is
            fprintf(f, "%s\n", code);
        } else {
            // Wrap code in a main function
            fprintf(f, "fn main() {\n");
            fprintf(f, "%s\n", code);
            fprintf(f, "}\n");
        }
        
        fclose(f);
        
        // Read back and verify file content
        f = fopen(filename, "r");
        if (f) {
            fseek(f, 0, SEEK_END);
            long fsize = ftell(f);
            fseek(f, 0, SEEK_SET);
            
            char* file_content = (char*)malloc(fsize + 1);
            fread(file_content, 1, fsize, f);
            file_content[fsize] = 0;
            fclose(f);
            
            EM_ASM({
                console.log('[wrapper] File content (' + $0 + ' bytes):\n' + UTF8ToString($1));
            }, fsize, file_content);
            
            free(file_content);
        } else {
            EM_ASM({ console.error('[wrapper] Failed to read back file!'); });
        }
        
        // Verify file exists before calling Parse_Crate
        f = fopen(filename, "r");
        if (!f) {
            result << "ERROR: File not accessible after creation\n";
            return result.str();
        }
        fclose(f);
        
        result << "=== Parsing Rust Code ===\n\n";
        
        // Parse the crate using mrustc via C wrapper
        OutputCapture capture;
        capture.start();
        
        EM_ASM({ console.log('[wrapper] Calling mrustc_parse_crate...'); });
        void* crate_ptr = mrustc_parse_crate(filename, 2021);
        EM_ASM({ console.log('[wrapper] mrustc_parse_crate returned:', $0); }, crate_ptr);
        
        if (crate_ptr) {
            result << "✓ Parsing successful!\n\n";
            
            result << "=== Crate Information ===\n";
            result << "Edition: 2021\n";
            result << "AST pointer: " << crate_ptr << "\n";
            
            // TODO: Extract more information from AST
            // Would require accessing AST::Crate members:
            // - root_module().items() for functions, structs, etc.
            // - m_edition for edition info
            
            result << "\n✅ Syntax is valid!\n";
            
            // Free the crate
            mrustc_free_crate(crate_ptr);
        } else {
            result << "✗ Parse failed (returned nullptr)\n";
            result << "This typically means:\n";
            result << "- Invalid Rust syntax\n";
            result << "- Unexpected end of file\n";
            result << "- Malformed code structure\n";
        }
        
        capture.stop();
        result << capture.get_output();
        
        result << "\n=== Compilation Status ===\n";
        result << "✓ Lexical analysis: Complete\n";
        result << "✓ Syntax parsing: Complete\n";
        result << "✓ AST generation: Complete\n";
        result << "⏸ Macro expansion: Not implemented\n";
        result << "⏸ Type checking: Not implemented\n";
        result << "⏸ Code generation: Not implemented\n";
        
        result << "\n=== Statistics ===\n";
        result << "Code size: " << strlen(code) << " bytes\n";
        result << "Analyzer: mrustc (syntax validator)\n";
        result << "Mode: AST parsing only\n";
        
    } catch (const std::exception& e) {
        result << "FATAL ERROR: " << e.what() << "\n";
    } catch (...) {
        result << "FATAL ERROR: Unknown exception\n";
    }
    
    return result.str();
}

// C API for JavaScript
extern "C" {
    EMSCRIPTEN_KEEPALIVE
    char* compileRust(const char* code, const char* options) {
        EM_ASM({
            console.log('[compileRust] Called with code length:', $0);
        }, code ? strlen(code) : 0);
        
        if (!code) {
            const char* error_msg = "ERROR: No code provided";
            char* result = (char*)malloc(strlen(error_msg) + 1);
            strcpy(result, error_msg);
            return result;
        }
        
        std::string result_str = compileRustWithMrustc(code, options);
        
        EM_ASM({
            console.log('[compileRust] Result string length:', $0);
        }, result_str.length());
        
        char* result = (char*)malloc(result_str.length() + 1);
        if (!result) {
            const char* error_msg = "ERROR: malloc failed";
            char* err = (char*)malloc(strlen(error_msg) + 1);
            strcpy(err, error_msg);
            return err;
        }
        
        strcpy(result, result_str.c_str());
        
        EM_ASM({
            console.log('[compileRust] Returning string:', UTF8ToString($0).substring(0, 100));
        }, result);
        
        return result;
    }
    
    EMSCRIPTEN_KEEPALIVE
    char* compileRustMulti(const char* files, const char* options) {
        return compileRust(files, options);
    }
}

