/**
 * Minimal self-contained doctest.h replacement for the IDE's direct-compile
 * test runner. The emception sysroot ships no doctest.h, so the authoring
 * IDE writes this file to `/home/user/doctest.h` before compiling a
 * combined doctest translation unit.
 *
 * Output format mirrors the doctest console reporter parsed by
 * `tools/emception/packages/core/src/testing/doctest/parse.ts`:
 *   [doctest] test cases:      N |      P passed |      F failed | 0 skipped
 *   [doctest] assertions:      M |      X passed |      Y failed |
 *   [doctest] Status: SUCCESS! / FAILURE!
 * plus `TEST CASE:  <name>` markers and
 * `<file>:<line>: ERROR: CHECK( expr ) is NOT correct!` failure lines.
 */

export const MINI_DOCTEST_H = `#pragma once
#include <cstdio>
#include <vector>

namespace mini_doctest {
struct Case { const char* name; void (*fn)(); };
inline std::vector<Case>& all_cases() { static std::vector<Case> v; return v; }
inline int& assert_total() { static int n = 0; return n; }
inline int& assert_failed() { static int n = 0; return n; }
struct Registrar {
    Registrar(const char* name, void(*fn)()) { all_cases().push_back({name, fn}); }
};
}

#define GG_CAT2(a, b) a##b
#define GG_CAT(a, b) GG_CAT2(a, b)

#define TEST_CASE(name)                                                        \\
    static void GG_CAT(gg_test_fn_, __LINE__)();                              \\
    static ::mini_doctest::Registrar GG_CAT(gg_test_reg_, __LINE__)(name,     \\
        &GG_CAT(gg_test_fn_, __LINE__));                                      \\
    void GG_CAT(gg_test_fn_, __LINE__)()

#define CHECK(expr)                                                            \\
    do {                                                                       \\
        ::mini_doctest::assert_total()++;                                      \\
        if (expr) {                                                            \\
        } else {                                                               \\
            ::mini_doctest::assert_failed()++;                                 \\
            std::printf("%s:%d: ERROR: CHECK( %s ) is NOT correct!\\n",         \\
                __FILE__, __LINE__, #expr);                                    \\
        }                                                                      \\
    } while (0)

int main() {
    int cases_failed = 0;
    for (const auto& c : ::mini_doctest::all_cases()) {
        int before = ::mini_doctest::assert_failed();
        std::printf("TEST CASE:  %s\\n", c.name);
        c.fn();
        if (::mini_doctest::assert_failed() > before) cases_failed++;
    }
    const int total = static_cast<int>(::mini_doctest::all_cases().size());
    const int passed = total - cases_failed;
    const int at = ::mini_doctest::assert_total();
    const int af = ::mini_doctest::assert_failed();
    std::printf("===============================================================================\\n");
    std::printf("[doctest] test cases:      %d |      %d passed |      %d failed | 0 skipped\\n", total, passed, cases_failed);
    std::printf("[doctest] assertions:      %d |      %d passed |      %d failed |\\n", at, at - af, af);
    std::printf("[doctest] Status: %s!\\n", cases_failed == 0 ? "SUCCESS" : "FAILURE");
    return cases_failed == 0 ? 0 : 1;
}
`;

/** Parsed mini-doctest stdout — mirrors the fields handleRunTests needs. */
export interface MiniDoctestResult {
    status: 'success' | 'failure' | 'crash';
    casesFailed: number;
    failures: string[];
}

/**
 * Parse mini-doctest stdout into a pass/fail verdict. `crash` means no
 * `[doctest] Status:` summary was printed (binary died before finishing).
 *
 * The wasi-run tool runner joins stdout chunks with '\n' at arbitrary
 * fd_write boundaries, so a line like `Status: SUCCESS!` can arrive as
 * `Status: SUCCESS\n!`. All matching runs against whitespace-flattened
 * text to be immune to those splits.
 */
export function parseMiniDoctest(stdout: string): MiniDoctestResult {
    const flat = stdout.replace(/\s+/g, ' ');
    const statusMatch = flat.match(/\[doctest\] Status: (SUCCESS|FAILURE)\s*!/i);
    if (!statusMatch) return { status: 'crash', casesFailed: -1, failures: [stdout.slice(0, 500)] };
    const failureLines = flat.match(/\S+:\d+: ERROR: CHECK\(.*?\) is NOT correct!/g) ?? [];
    const casesMatch = flat.match(/\[doctest\] test cases: *(\d+) \| *(\d+) passed \| *(\d+) failed/);
    return {
        status: statusMatch[1].toUpperCase() === 'SUCCESS' ? 'success' : 'failure',
        casesFailed: casesMatch ? Number(casesMatch[3]) : 0,
        failures: failureLines,
    };
}
