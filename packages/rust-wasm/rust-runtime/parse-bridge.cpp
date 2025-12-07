// Direct bridge to Parse_Crate - compiled separately to ensure linking
#include <string>
#include <exception>
#include "../upstream/mrustc/src/ast/edition.hpp"
#include "../upstream/mrustc/src/ast/crate.hpp"
#include <emscripten.h>

// External declaration matching the ACTUAL signature in the object file (by value, not const ref!)
extern AST::Crate Parse_Crate(::std::string mainfile, AST::Edition edition);

extern "C" {
    // Bridge function that directly calls Parse_Crate
    EMSCRIPTEN_KEEPALIVE
    void* bridge_parse_crate(const char* filename) {
        EM_ASM({
            console.log('[bridge] Calling Parse_Crate with file:', UTF8ToString($0));
        }, filename);
        
        try {
            std::string fname(filename);
            EM_ASM({ console.log('[bridge] Creating Crate object...'); });
            AST::Crate* crate = new AST::Crate(Parse_Crate(fname, AST::Edition::Rust2021));
            EM_ASM({ console.log('[bridge] Parse_Crate succeeded!'); });
            return crate;
        } catch (const std::exception& e) {
            EM_ASM({
                console.error('[bridge] std::exception:', UTF8ToString($0));
            }, e.what());
            return nullptr;
        } catch (...) {
            EM_ASM({ console.error('[bridge] Exception caught - returning nullptr'); });
            return nullptr;
        }
    }
}
