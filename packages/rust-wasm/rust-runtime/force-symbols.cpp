// Force linker to include Parse_Crate and other critical symbols
#include <string>
#include "../upstream/mrustc/src/ast/edition.hpp"
#include "../upstream/mrustc/src/ast/crate.hpp"

// External declarations
extern AST::Crate Parse_Crate(const ::std::string& mainfile, AST::Edition edition);

// This function will never be called, but forces the linker to include Parse_Crate
extern "C" __attribute__((used))
void __force_include_symbols() {
    // Reference Parse_Crate so linker must include it
    volatile auto ptr = &Parse_Crate;
    (void)ptr;
}
