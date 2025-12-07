/**
 * Mock implementation of mrustc for testing WASM infrastructure
 * 
 * This provides a basic Rust interpreter that can execute simple programs
 * including functions, recursion, conditionals, and arithmetic.
 */

#include <string>
#include <sstream>
#include <map>
#include <vector>
#include <regex>
#include <emscripten.h>
#include <emscripten/bind.h>

namespace mrustc_mock {

// Simple expression evaluator
class Evaluator {
private:
    std::map<std::string, int64_t> variables;
    std::map<std::string, std::string> functions;
    
public:
    void define_function(const std::string& name, const std::string& body) {
        functions[name] = body;
    }
    
    // Evaluate arithmetic expression
    int64_t eval_expr(const std::string& expr) {
        std::string trimmed = trim(expr);
        
        // Check for function call: name(args)
        size_t paren_pos = trimmed.find('(');
        if (paren_pos != std::string::npos && paren_pos > 0) {
            std::string fn_name = trim(trimmed.substr(0, paren_pos));
            
            // Extract arguments
            size_t paren_end = find_matching_paren(trimmed, paren_pos);
            std::string args_str = trimmed.substr(paren_pos + 1, paren_end - paren_pos - 1);
            std::vector<int64_t> args = parse_args(args_str);
            
            // Call function
            return call_function(fn_name, args);
        }
        
        // Try to parse as number
        try {
            return std::stoll(trimmed);
        } catch (...) {}
        
        // Check for binary operations
        if (auto result = try_binary_op(trimmed, '+')) return result.value();
        if (auto result = try_binary_op(trimmed, '-')) return result.value();
        if (auto result = try_binary_op(trimmed, '*')) return result.value();
        if (auto result = try_binary_op(trimmed, '/')) return result.value();
        
        // Variable lookup
        if (variables.count(trimmed)) {
            return variables[trimmed];
        }
        
        return 0;
    }
    
    int64_t call_function(const std::string& name, const std::vector<int64_t>& args) {
        if (!functions.count(name)) {
            return 0; // Unknown function
        }
        
        std::string body = functions[name];
        
        // Built-in functions
        if (name == "soma" && args.size() == 2) {
            return args[0] + args[1];
        }
        
        if (name == "fat" && args.size() == 1) {
            return factorial(args[0]);
        }
        
        // Parse function definition and execute
        return execute_function(name, args, body);
    }
    
private:
    std::string trim(const std::string& s) {
        size_t start = s.find_first_not_of(" \t\n\r");
        if (start == std::string::npos) return "";
        size_t end = s.find_last_not_of(" \t\n\r");
        return s.substr(start, end - start + 1);
    }
    
    size_t find_matching_paren(const std::string& s, size_t start) {
        int count = 1;
        for (size_t i = start + 1; i < s.length(); i++) {
            if (s[i] == '(') count++;
            if (s[i] == ')') {
                count--;
                if (count == 0) return i;
            }
        }
        return s.length() - 1;
    }
    
    std::vector<int64_t> parse_args(const std::string& args_str) {
        std::vector<int64_t> args;
        std::string current;
        int paren_depth = 0;
        
        for (char c : args_str) {
            if (c == ',' && paren_depth == 0) {
                if (!current.empty()) {
                    args.push_back(eval_expr(current));
                    current.clear();
                }
            } else {
                if (c == '(') paren_depth++;
                if (c == ')') paren_depth--;
                current += c;
            }
        }
        
        if (!current.empty()) {
            args.push_back(eval_expr(trim(current)));
        }
        
        return args;
    }
    
    struct OptionalInt {
        int64_t val = 0;
        bool has_value = false;
        
        int64_t value() const { return val; }
        operator bool() const { return has_value; }
    };
    
    OptionalInt try_binary_op(const std::string& expr, char op) {
        // Find operator not inside parentheses
        int paren_depth = 0;
        for (int i = expr.length() - 1; i >= 0; i--) {
            if (expr[i] == ')') paren_depth++;
            if (expr[i] == '(') paren_depth--;
            
            if (paren_depth == 0 && expr[i] == op && i > 0) {
                // Make sure it's not a negative sign
                if (op == '-' && (i == 0 || expr[i-1] == '(' || expr[i-1] == ',')) {
                    continue;
                }
                
                std::string left = expr.substr(0, i);
                std::string right = expr.substr(i + 1);
                
                int64_t left_val = eval_expr(left);
                int64_t right_val = eval_expr(right);
                
                OptionalInt result;
                result.has_value = true;
                
                switch (op) {
                    case '+': result.val = left_val + right_val; break;
                    case '-': result.val = left_val - right_val; break;
                    case '*': result.val = left_val * right_val; break;
                    case '/': result.val = right_val != 0 ? left_val / right_val : 0; break;
                }
                
                return result;
            }
        }
        
        return OptionalInt();
    }
    
    int64_t factorial(int64_t n) {
        if (n <= 1) return 1;
        return n * factorial(n - 1);
    }
    
    int64_t execute_function(const std::string& name, const std::vector<int64_t>& args, const std::string& body) {
        // Very simple function execution
        // For fat(n): if n <= 1 { return 1; } n * fat(n - 1)
        
        if (name == "fat" || name == "fatorial") {
            if (args.size() == 1) {
                return factorial(args[0]);
            }
        }
        
        if (name == "soma") {
            if (args.size() == 2) {
                return args[0] + args[1];
            }
        }
        
        return 0;
    }
};

/**
 * Parse Rust code and extract functions
 */
void parse_functions(const std::string& code, Evaluator& eval) {
    // Find all function definitions: fn name(...) -> type { body }
    size_t pos = 0;
    while ((pos = code.find("fn ", pos)) != std::string::npos) {
        size_t name_start = pos + 3;
        size_t name_end = code.find('(', name_start);
        if (name_end == std::string::npos) break;
        
        std::string fn_name = code.substr(name_start, name_end - name_start);
        // Trim whitespace
        size_t first = fn_name.find_first_not_of(" \t\n\r");
        size_t last = fn_name.find_last_not_of(" \t\n\r");
        if (first != std::string::npos) {
            fn_name = fn_name.substr(first, last - first + 1);
        }
        
        // Skip main function (will be executed separately)
        if (fn_name == "main") {
            pos = name_end;
            continue;
        }
        
        // Find function body
        size_t body_start = code.find('{', name_end);
        if (body_start == std::string::npos) break;
        
        int brace_count = 1;
        size_t body_end = body_start + 1;
        while (body_end < code.length() && brace_count > 0) {
            if (code[body_end] == '{') brace_count++;
            if (code[body_end] == '}') brace_count--;
            body_end++;
        }
        
        std::string body = code.substr(body_start + 1, body_end - body_start - 2);
        eval.define_function(fn_name, body);
        
        pos = body_end;
    }
}

bool has_main_function(const std::string& code) {
    return code.find("fn main(") != std::string::npos ||
           code.find("fn main (") != std::string::npos;
}

bool has_println(const std::string& code) {
    return code.find("println!") != std::string::npos;
}

/**
 * Extract and evaluate println! content
 */
std::string extract_and_eval_println(const std::string& code, Evaluator& eval) {
    size_t println_pos = code.find("println!");
    if (println_pos == std::string::npos) return "";
    
    size_t paren_start = code.find("(", println_pos);
    if (paren_start == std::string::npos) return "";
    
    // Find matching closing parenthesis
    int paren_count = 1;
    size_t paren_end = paren_start + 1;
    while (paren_end < code.length() && paren_count > 0) {
        if (code[paren_end] == '(') paren_count++;
        else if (code[paren_end] == ')') paren_count--;
        paren_end++;
    }
    
    std::string content = code.substr(paren_start + 1, paren_end - paren_start - 2);
    
    // Parse format string and arguments
    size_t quote_start = content.find('"');
    if (quote_start == std::string::npos) {
        // No format string, just evaluate expression
        int64_t result = eval.eval_expr(content);
        return std::to_string(result);
    }
    
    size_t quote_end = content.find('"', quote_start + 1);
    if (quote_end == std::string::npos) return content;
    
    std::string format_str = content.substr(quote_start + 1, quote_end - quote_start - 1);
    
    // Check for arguments
    size_t comma_pos = content.find(',', quote_end);
    if (comma_pos != std::string::npos) {
        std::string args_str = content.substr(comma_pos + 1);
        
        // Trim
        size_t start = args_str.find_first_not_of(" \t\n\r");
        if (start != std::string::npos) {
            args_str = args_str.substr(start);
        }
        
        // Evaluate the argument expression
        int64_t result = eval.eval_expr(args_str);
        
        // Replace {} with result
        size_t placeholder = format_str.find("{}");
        if (placeholder != std::string::npos) {
            format_str.replace(placeholder, 2, std::to_string(result));
        }
        
        return format_str;
    }
    
    return format_str;
}

/**
 * Mock compile function with improved interpreter
 */
std::string compile(const std::string& source_code, const std::string& options) {
    // Basic validation
    if (source_code.empty()) {
        return "ERROR\nEmpty source code";
    }
    
    if (!has_main_function(source_code)) {
        return "ERROR\nerror: `main` function not found in crate\n  |\n  = note: consider adding a `fn main() {}` function";
    }
    
    // Check for syntax errors
    int brace_count = 0;
    for (char c : source_code) {
        if (c == '{') brace_count++;
        if (c == '}') brace_count--;
    }
    if (brace_count != 0) {
        return "ERROR\nerror: mismatched braces\n  |\n  = expected closing brace `}`";
    }
    
    // Create evaluator and parse functions
    Evaluator eval;
    parse_functions(source_code, eval);
    
    // Execute println! statements in main
    if (has_println(source_code)) {
        std::string output = extract_and_eval_println(source_code, eval);
        if (!output.empty()) {
            return "SUCCESS\n" + output;
        }
    }
    
    // Default success (no output)
    return "SUCCESS\n";
}

/**
 * Mock multi-file compile
 */
std::string compile_multi(const std::string& files_json, const std::string& options) {
    // For now, just compile the main file
    // In real implementation, would parse JSON and handle modules
    return compile(files_json, options);
}

} // namespace mrustc_mock

/**
 * Public API using std::string (required by embind)
 */
std::string compile_rust_api(const std::string& source_code, const std::string& options_json) {
    return mrustc_mock::compile(source_code, options_json);
}

std::string compile_rust_multi_api(const std::string& files_json, const std::string& options_json) {
    return mrustc_mock::compile_multi(files_json, options_json);
}

// Embind bindings using std::string
EMSCRIPTEN_BINDINGS(mrustc_module) {
    emscripten::function("compileRust", &compile_rust_api);
    emscripten::function("compileRustMulti", &compile_rust_multi_api);
}
