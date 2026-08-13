/**
 * CodingAssignmentContent (v1 backend DTO) → emception TestPlan mapper.
 *
 * Pure data transformation: no EmceptionAPI dependency, no IO. The caller
 * (Task 11 grader IDE) writes each {@link GeneratedFile} into the workspace
 * and feeds the {@link TestPlan} to `EmceptionAPI.runTests`.
 *
 * Wire shape mirrors the v1 backend DTOs under
 * `apps/api/.../Models/CodingAssignmentContent/` (PascalCase JSON field names
 * per Task 1 learnings — EXCEPT the lowercase `kind` discriminator and the
 * lowercase `FunctionParameterType` enum values serialized via
 * `CamelCaseEnumConverter`).
 *
 * The harness generator (`functional/harness.ts`, Task 6) consumes a
 * camelCase descriptor with PascalCase `type` enum values; this mapper
 * performs both transforms.
 */

import {
    generateDoctestHarness,
    type FunctionParameter as HarnessFunctionParameter,
    type FunctionalTestCase as HarnessFunctionalTestCase,
    type FunctionalTestSignature,
} from './functional/harness.js';
import type { TestCase, TestPlan } from '../types.js';

// --- Wire-shape types (mirror v1 backend DTOs) -------------------------------

/** File encoding wire values (Task 1: lowercase). */
export type FileEncoding = 'text' | 'base64';

/** File visibility wire values (Task 1: PascalCase, `"Solution"` rejected). */
export type FileVisibility = 'Public' | 'Private';

/** v1 `BundleFileMeta` wire shape. */
export interface BundleFileMeta {
    Content: string;
    Encoding?: FileEncoding;
    Visibility?: FileVisibility;
    Modifiable?: boolean;
}

/** v1 primitive parameter type wire values (Task 1: lowercase via CamelCaseEnumConverter). */
export type WireFunctionParameterType = 'string' | 'boolean' | 'integer' | 'float';

/** v1 `FunctionParameter` wire shape — `Content` is opaque to this mapper. */
export interface FunctionParameter {
    Type: WireFunctionParameterType;
    Content: unknown;
}

/** v1 `FunctionParameterWithName` wire shape. */
export interface FunctionParameterWithName extends FunctionParameter {
    Name: string;
}

/** v1 `TestFunctionData` wire shape (PascalCase field names). */
export interface TestFunctionData {
    FunctionName: string;
    Parameters?: FunctionParameterWithName[];
    ReturnType: FunctionParameter;
}

/**
 * v1 `FunctionalTestCase` wire shape — one case within a FunctionalTestGroup.
 * `Inputs[i]` aligns positionally with `TestFunctionData.Parameters[i]`.
 * PascalCase field names per the v1 wire convention; lowercase `Type` enum
 * values per CamelCaseEnumConverter.
 */
export interface FunctionalTestCase {
    Inputs: FunctionParameter[];
    Expected: FunctionParameter;
}

/** v1 polymorphic `Test` union, keyed by lowercase `kind`. */
export type Test =
    | {
        kind: 'standard';
        Weight?: number;
        Name?: string;
        Stdin?: string;
        Stdout: string;
        Stderr?: string;
        ExitCode?: number;
    }
    | {
        kind: 'functional';
        Weight?: number;
        Name?: string;
        Function: TestFunctionData;
        Cases: FunctionalTestCase[];
    };

/** v1 `TestSuite` wire shape. */
export interface TestSuite {
    Public?: Test[];
    Private?: Test[];
}

/** v1 `CodingAssignmentContent` wire shape — the mapper input. */
export interface CodingAssignmentContent {
    Type: 'coding-assignment';
    Version: 1;
    Environment: unknown;
    Data: { Files: Record<string, BundleFileMeta> };
    Tests: TestSuite;
    Grading: unknown;
}

// --- Output types ------------------------------------------------------------

/** File the caller must write into the emception workspace before runTests. */
export interface GeneratedFile {
    path: string;
    content: string;
}

/** Mode selects which test sets feed the plan. */
export type BuildTestPlanMode = 'public-only' | 'full';

export interface BuildTestPlanOptions {
    mode: BuildTestPlanMode;
}

export interface BuildTestPlanResult {
    plan: TestPlan;
    generatedFiles: GeneratedFile[];
}

// --- Internals ---------------------------------------------------------------

/** Emception workspace mount path (per Task 5 learnings). */
const WORKSPACE_MOUNT = '/home/user';

/**
 * Capitalize a lowercase wire enum value to match the harness's PascalCase
 * `FunctionParameterType` union. Single-purpose: do not generalize.
 */
function capitalize(s: string): string {
    return s.charAt(0).toUpperCase() + s.slice(1);
}

/** Map a wire FunctionParameter → harness descriptor FunctionParameter. */
function toHarnessParam(wire: FunctionParameter): HarnessFunctionParameter {
    return {
        type: capitalize(wire.Type) as HarnessFunctionParameter['type'],
        content: wire.Content,
    };
}

/**
 * Convert one wire Test → one plan TestCase, recording any generated harness.
 *
 * `index` is the position of `test` within the concatenated test list — it
 * seeds the harness filename + doctest TEST_CASE name uniqueness.
 */
function mapTest(test: Test, index: number, generatedFiles: GeneratedFile[]): TestCase {
    switch (test.kind) {
        case 'standard':
            return {
                kind: 'stdio',
                stdin: test.Stdin ?? '',
                expectedStdout: test.Stdout,
                // undefined → engine's matcher skips stderr/exit checks.
                expectedStderr: test.Stderr === undefined ? undefined : test.Stderr,
                expectedExit: test.ExitCode,
                weight: test.Weight,
                name: test.Name,
            };
        case 'functional': {
            // Metis M5: 0-case group is invalid (avoids divide-by-zero below
            // AND matches the upstream DTO's `required` Cases). The harness
            // also enforces this at its own boundary.
            if (test.Cases.length === 0) {
                throw new Error('FunctionalTestGroup requires \u22651 case');
            }
            const fn = test.Function;
            const signature: FunctionalTestSignature = {
                functionName: fn.FunctionName,
                parameters: (fn.Parameters ?? []).map((p) => ({ ...toHarnessParam(p), name: p.Name })),
                returnType: toHarnessParam(fn.ReturnType),
            };
            const cases: HarnessFunctionalTestCase[] = test.Cases.map((c) => ({
                Inputs: c.Inputs.map(toHarnessParam),
                Expected: toHarnessParam(c.Expected),
            }));
            const harness = generateDoctestHarness(signature, cases, { index, name: test.Name });
            const path = `${WORKSPACE_MOUNT}/${harness.filename}`;
            generatedFiles.push({ path, content: harness.source });
            // ponytail: equal-split per case; per-case weight deferred.
            // Weight 0 is valid (compiles + runs, contributes 0 to score).
            const caseWeight = test.Weight === undefined ? undefined : test.Weight / test.Cases.length;
            return {
                kind: 'doctest',
                sourceFiles: [path],
                weight: caseWeight,
                name: test.Name,
            };
        }
        default: {
            // Exhaustiveness guard — runtime safety against hand-built input
            // smuggling a non-v1 kind past TS.
            const _exhaustive: never = test;
            throw new Error(`Unsupported test kind: ${String((_exhaustive as { kind?: unknown }).kind ?? _exhaustive)}`);
        }
    }
}

// --- Public API --------------------------------------------------------------

/**
 * Build an emception {@link TestPlan} from a v1 {@link CodingAssignmentContent}.
 *
 * - `'public-only'` mode uses only `Tests.Public[]` (student-visible grading).
 * - `'full'` mode concatenates `Tests.Public[] + Tests.Private[]` (full grading).
 *
 * For each `StandardTest` → one `'stdio'` case. For each `FunctionalTestGroup`
 * → one generated `.cpp` harness (collected in `generatedFiles`) with N CHECK
 * blocks + one `'doctest'` case referencing it. The group's `Weight` is split
 * equally across cases (lazy default; per-case weight deferred).
 *
 * `plan.build.sources` is set to ALL text-encoded `Data.Files` paths so the
 * engine's `doctest` handler compiles+links student code with each harness
 * via `compileAndRun`. Base64-encoded files are excluded (binary assets).
 *
 * Throws on unknown `kind` values — never silently drops a test.
 */
export function buildTestPlan(
    assignment: CodingAssignmentContent,
    options: BuildTestPlanOptions,
): BuildTestPlanResult {
    const publicTests = assignment.Tests.Public ?? [];
    const privateTests = options.mode === 'full' ? (assignment.Tests.Private ?? []) : [];
    const tests = [...publicTests, ...privateTests];

    const generatedFiles: GeneratedFile[] = [];
    const cases = tests.map((test, index) => mapTest(test, index, generatedFiles));

    const sources = Object.entries(assignment.Data.Files)
        .filter(([, meta]) => (meta.Encoding ?? 'text') === 'text')
        .map(([path]) => path);

    const plan: TestPlan = {
        build: { sources },
        cases,
    };

    return { plan, generatedFiles };
}
