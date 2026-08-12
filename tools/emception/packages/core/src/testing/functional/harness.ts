/**
 * FunctionalTest C/C++ doctest harness generator.
 *
 * v1 supports four primitive parameter types (String, Boolean, Integer,
 * Float). Array/Dictionary are rejected at serialization time — the upstream
 * validator already rejects them with `functional_param_type_not_supported_v1`,
 * but we re-throw here so a hand-built descriptor cannot slip a non-v1 type
 * past the boundary.
 *
 * The generated harness is a self-contained .cpp file:
 *
 *   - `#include "doctest.h"` — single-include ship from the sysroot.
 *   - `#include <string>` — needed whenever `std::string` is in the signature.
 *   - `extern "C"` forward declaration — bounds C++ name mangling so a
 *     student-defined global-scope function with the identical signature
 *     links against the harness. The FunctionName regex
 *     `^[A-Za-z_][A-Za-z0-9_]*$` enforced upstream guarantees the student
 *     definition lives at global scope (no namespaces, no class members).
 *   - `TEST_CASE("<index>:<functionName>")` — `<index>` guarantees test-case
 *     name uniqueness across multiple FunctionalTests in the same binary.
 *   - `CHECK(<functionName>(<arg literals>) == <result literal>);` — the
 *     single assertion that defines pass/fail.
 *
 * Student solution files are NOT referenced here — they are linked at grade
 * time via `plan.build.sources` (set by the Task 7 mapper). The engine's
 * `doctest` handler at `engine.ts:185-226` compiles `plan.build.sources` +
 * `test.sourceFiles` together and runs the resulting binary.
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
 * Shape consumed by the harness generator: the `FunctionalTest` wire shape
 * minus `weight` and `name` (those flow into the TestPlan, not the harness).
 *
 * `returnType` matches the draft's `TestFunctionData.ReturnType` (PascalCase
 * on the wire; we keep PascalCase here to mirror the source of truth).
 */
export interface FunctionalTestDescriptor {
    functionName: string;
    parameters: FunctionParameterWithName[];
    returnType: FunctionParameter;
    result: FunctionParameter;
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
 * Generate a doctest harness `.cpp` source for a single FunctionalTest.
 *
 * `index` is assigned by the caller (the Task 7 mapper) and is used only to
 * guarantee test-case name uniqueness when multiple FunctionalTests compile
 * into the same binary — it carries no semantic meaning.
 *
 * Returns `{ filename, source }`. The caller writes `source` to `filename`
 * inside the emception workspace and lists the path in `test.sourceFiles`.
 */
export function generateDoctestHarness(
    test: FunctionalTestDescriptor,
    index: number,
): { filename: string; source: string } {
    const paramTypes = test.parameters.map((p) => mapCppType(p.type)).join(', ');
    const argLiterals = test.parameters.map((p) => serializeCppLiteral(p)).join(', ');
    const retType = mapCppType(test.returnType.type);
    const resultLiteral = serializeCppLiteral(test.result);

    const lines: string[] = [
        '#include "doctest.h"',
        '#include <string>',
        '',
        `extern "C" ${retType} ${test.functionName}(${paramTypes});`,
        `TEST_CASE("${index}:${test.functionName}") {`,
        `    CHECK(${test.functionName}(${argLiterals}) == ${resultLiteral});`,
        '}',
        '',
    ];

    return {
        filename: `functional_${index}_test.cpp`,
        source: lines.join('\n'),
    };
}
