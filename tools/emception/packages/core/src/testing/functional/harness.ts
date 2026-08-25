/**
 * FunctionalTestGroup C/C++ doctest harness generator.
 *
 * v1 supports four primitive parameter types (String, Boolean, Integer,
 * Float). Array/Dictionary are rejected at serialization time — the upstream
 * validator already rejects them with `functional_param_type_not_supported_v1`,
 * but we re-throw here so a hand-built descriptor cannot slip a non-v1 type
 * past the boundary.
 *
 * A FunctionalTestGroup carries ONE function signature + N cases. The
 * generated harness is a single self-contained .cpp file:
 *
 *   - `#include "doctest.h"` — single-include ship from the sysroot.
 *   - `#include <string>` — needed whenever `std::string` is in the signature.
 *   - `extern "C"` forward declaration (emitted ONCE) — bounds C++ name
 *     mangling so a student-defined global-scope function with the identical
 *     signature links against the harness. The FunctionName regex
 *     `^[A-Za-z_][A-Za-z0-9_]*$` enforced upstream guarantees the student
 *     definition lives at global scope (no namespaces, no class members).
 *   - `TEST_CASE("<index>:<label>")` — `<index>` guarantees test-case name
 *     uniqueness across multiple FunctionalTestGroups in the same binary.
 *   - N `CHECK(<functionName>(<arg literals>) == <expected literal>);` lines
 *     inside the single TEST_CASE block — one per case. Doctest reports
 *     the group as one pass/fail result; the grader splits the group's
 *     weight equally across cases (lazy default).
 *
 * Student solution files are NOT referenced here — they are linked at grade
 * time via `plan.build.sources` (set by the host's TestPlan mapper). The
 * engine's `doctest` handler at `engine.ts:186-227` compiles
 * `plan.build.sources` + `test.sourceFiles` together and runs the resulting
 * binary.
 */

/** v1 primitive types + the deferred v2 aggregate types. */
export type FunctionParameterType =
    | 'String'
    | 'Boolean'
    | 'Integer'
    | 'Float'
    | 'Array'
    | 'Dictionary';

/**
 * One parameter value. `content` is `unknown` because the boundary parser
 * (Pydantic v2 on the server) is responsible for narrowing per `type`; the
 * harness generator consumes already-narrowed values.
 */
export interface FunctionParameter {
    type: FunctionParameterType;
    content: unknown;
}

/** Named parameter — used in function signatures. */
export interface FunctionParameterWithName extends FunctionParameter {
    name: string;
}

/**
 * Function signature consumed by the harness generator: the
 * `FunctionalTestGroup.Function` wire shape minus `Weight`/`Name` (those
 * flow into the TestPlan, not the harness).
 *
 * `parameters[i].content` is unused by the harness (only `name` + `type`
 * drive the `extern "C"` decl) but kept to satisfy the existing
 * `FunctionParameter` shape that carries values elsewhere.
 */
export interface FunctionalTestSignature {
    functionName: string;
    parameters: FunctionParameterWithName[];
    returnType: FunctionParameter;
}

/**
 * One case within a functional test group. The PascalCase field names are a
 * generic compatibility format for host-side test-plan mappers.
 *
 * `Inputs[i]` aligns positionally with `FunctionalTestSignature.parameters[i]`.
 */
export interface FunctionalTestCase {
    Inputs: FunctionParameter[];
    Expected: FunctionParameter;
}

/** Map a v1 FunctionParameterType to its C++ type spelling. */
export function mapCppType(type: FunctionParameterType): string {
    switch (type) {
        case 'Integer':
            return 'int';
        case 'Float':
            return 'double';
        case 'Boolean':
            return 'bool';
        case 'String':
            return 'std::string';
        case 'Array':
        case 'Dictionary':
            throw new Error('Array/Dictionary parameter types not supported in v1');
        default: {
            // Exhaustiveness guard — if FunctionParameterType grows a new
            // variant, this throws at compile-time via the never assignment
            // AND at runtime for any value smuggled past TS.
            const _exhaustive: never = type;
            throw new Error(`Unsupported parameter type: ${_exhaustive as string}`);
        }
    }
}

/** Escape `"`, `\`, newline → `\n`, tab → `\t` for embedding in a C++ string literal. */
export function escapeCppString(s: string): string {
    return s
        .replace(/\\/g, '\\\\')
        .replace(/"/g, '\\"')
        .replace(/\n/g, '\\n')
        .replace(/\t/g, '\\t');
}

/**
 * Serialize a FunctionParameter value to its C++ literal spelling.
 *
 * Throws on Array/Dictionary — those types are deferred past v1.
 */
export function serializeCppLiteral(param: FunctionParameter): string {
    switch (param.type) {
        case 'String':
            return `"${escapeCppString(String(param.content))}"`;
        case 'Boolean':
            return param.content ? 'true' : 'false';
        case 'Integer':
            return String(param.content);
        case 'Float':
            return String(param.content);
        case 'Array':
        case 'Dictionary':
            throw new Error('Array/Dictionary parameter types not supported in v1');
        default: {
            const _exhaustive: never = param.type;
            throw new Error(`Unsupported parameter type: ${_exhaustive as string}`);
        }
    }
}

/**
 * Generate a doctest harness `.cpp` source for one FunctionalTestGroup:
 * ONE `extern "C"` forward decl + ONE TEST_CASE block containing N CHECK
 * lines (one per case).
 *
 * `options.index` is assigned by the caller (the host's TestPlan mapper) and
 * seeds the harness filename + doctest TEST_CASE name uniqueness across
 * multiple groups compiled into the same binary. Defaults to 0.
 * `options.name` overrides the TEST_CASE label (defaults to
 * `signature.functionName`).
 *
 * Throws if `cases.length === 0` — the upstream DTO marks `Cases` required,
 * but a hand-built descriptor could smuggle an empty array past the type
 * system. The mapper also enforces this; the harness re-checks at its own
 * public boundary.
 *
 * Returns `{ filename, source }`. The caller writes `source` to `filename`
 * inside the emception workspace and lists the path in `test.sourceFiles`.
 */
export function generateDoctestHarness(
    signature: FunctionalTestSignature,
    cases: FunctionalTestCase[],
    options?: { index?: number; name?: string },
): { filename: string; source: string } {
    if (cases.length === 0) {
        throw new Error('FunctionalTestGroup requires \u22651 case');
    }
    const paramTypes = signature.parameters.map((p) => mapCppType(p.type)).join(', ');
    const retType = mapCppType(signature.returnType.type);
    const index = options?.index ?? 0;
    const label = options?.name ?? signature.functionName;

    const checks = cases.map((c) => {
        const argLiterals = c.Inputs.map((p) => serializeCppLiteral(p)).join(', ');
        const expectedLiteral = serializeCppLiteral(c.Expected);
        return `    CHECK(${signature.functionName}(${argLiterals}) == ${expectedLiteral});`;
    });

    const lines: string[] = [
        '#include "doctest.h"',
        '#include <string>',
        '',
        `extern "C" ${retType} ${signature.functionName}(${paramTypes});`,
        `TEST_CASE("${index}:${label}") {`,
        ...checks,
        '}',
        '',
    ];

    return {
        filename: `functional_${index}_test.cpp`,
        source: lines.join('\n'),
    };
}
