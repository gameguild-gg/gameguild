// Unit tests for `buildTestPlan` — CodingAssignmentContent v1 → emception
// TestPlan mapper. Pure data transformation; no EmceptionAPI dependency.
//
// Run: `node --test packages/core/src/testing/assignment-plan.test.ts`
// (Node 24 strips types via --experimental-strip-types).
//
// The SUT (`assignment-plan.ts`) has a cross-module value import from
// `./functional/harness.js`. Node's type-stripping does not rewrite `.js`
// specifiers to `.ts`, so we register a `.js → .ts` resolver hook before
// dynamically importing the SUT. Type-only imports are erased by the
// stripper and need no resolver.

import assert from 'node:assert/strict';
import { register } from 'node:module';
import test from 'node:test';

import type { CodingAssignmentContent, Test } from './assignment-plan.ts';

register(new URL('./_resolve-ts.mjs', import.meta.url));

const { buildTestPlan } = await import('./assignment-plan.ts');

// --- Fixtures ----------------------------------------------------------------

const stdPublic = {
    kind: 'standard',
    Weight: 2,
    Name: 'hello-stdout',
    Stdin: '',
    Stdout: 'hello\n',
} as const satisfies Test;

const stdPrivate = {
    kind: 'standard',
    Weight: 3,
    Name: 'private-edge',
    Stdin: '42',
    Stdout: 'answer\n',
    Stderr: '',
    ExitCode: 0,
} as const satisfies Test;

const funcPublic = {
    kind: 'functional',
    Weight: 5,
    Name: 'add-ints',
    Function: {
        FunctionName: 'add',
        Parameters: [
            { Name: 'a', Type: 'integer', Content: 0 },
            { Name: 'b', Type: 'integer', Content: 0 },
        ],
        ReturnType: { Type: 'integer', Content: 0 },
    },
    Cases: [
        { Inputs: [{ Type: 'integer', Content: 2 }, { Type: 'integer', Content: 3 }], Expected: { Type: 'integer', Content: 5 } },
    ],
} as const satisfies Test;

/** Assignment with 2 text files, 1 base64 file, 1 std + 1 func in Public, 1 std in Private. */
function sampleAssignment(): CodingAssignmentContent {
    return {
        Type: 'coding-assignment',
        Version: 1,
        Environment: { kind: 'native' },
        Data: {
            Files: {
                '/home/user/solution.cpp': { Content: 'int main(){}', Encoding: 'text' },
                '/home/user/helper.cpp': { Content: 'void h(){}' }, // default text
                '/home/user/asset.png': { Content: 'iVBORw0KG', Encoding: 'base64' },
            },
        },
        Tests: { Public: [stdPublic, funcPublic], Private: [stdPrivate] },
        Grading: { PassingScore: 60 },
    };
}

// --- Tests -------------------------------------------------------------------

test('buildTestPlan: public-only mode includes only Public tests', () => {
    const { plan } = buildTestPlan(sampleAssignment(), { mode: 'public-only' });
    assert.equal(plan.cases.length, 2, '1 std + 1 func in Public');
    assert.equal(plan.cases[0].kind, 'stdio');
    assert.equal(plan.cases[1].kind, 'doctest');
});

test('buildTestPlan: full mode includes Public + Private tests', () => {
    const { plan } = buildTestPlan(sampleAssignment(), { mode: 'full' });
    assert.equal(plan.cases.length, 3, 'Public(2) + Private(1)');
    assert.equal(plan.cases[0].kind, 'stdio');
    assert.equal(plan.cases[1].kind, 'doctest');
    assert.equal(plan.cases[2].kind, 'stdio');
});

test('buildTestPlan: StandardTest → stdio case 1:1 with all fields', () => {
    const { plan } = buildTestPlan(
        {
            ...sampleAssignment(),
            Tests: {
                Public: [
                    {
                        kind: 'standard',
                        Weight: 7,
                        Name: 'echo',
                        Stdin: 'in',
                        Stdout: 'out',
                        Stderr: 'err',
                        ExitCode: 0,
                    },
                ],
                Private: [],
            },
        },
        { mode: 'public-only' },
    );

    assert.equal(plan.cases.length, 1);
    const c = plan.cases[0];
    assert.equal(c.kind, 'stdio');
    if (c.kind !== 'stdio') throw new Error('unreachable');
    assert.equal(c.stdin, 'in');
    assert.equal(c.expectedStdout, 'out');
    assert.equal(c.expectedStderr, 'err');
    assert.equal(c.expectedExit, 0);
    assert.equal(c.weight, 7);
    assert.equal(c.name, 'echo');
});

test('buildTestPlan: StandardTest with omitted Stdin/Stderr/ExitCode → undefined skips matcher', () => {
    const { plan } = buildTestPlan(
        {
            ...sampleAssignment(),
            Tests: {
                Public: [{ kind: 'standard', Stdout: 'just-out' }],
                Private: [],
            },
        },
        { mode: 'public-only' },
    );

    const c = plan.cases[0];
    assert.equal(c.kind, 'stdio');
    if (c.kind !== 'stdio') throw new Error('unreachable');
    assert.equal(c.stdin, '', 'omitted Stdin defaults to empty string');
    assert.equal(c.expectedStderr, undefined, 'omitted Stderr → undefined (matcher skips)');
    assert.equal(c.expectedExit, undefined, 'omitted ExitCode → undefined (matcher skips)');
});

test('buildTestPlan: FunctionalTest → doctest case + 1 generated harness .cpp', () => {
    const { plan, generatedFiles } = buildTestPlan(
        {
            ...sampleAssignment(),
            Tests: { Public: [funcPublic], Private: [] },
        },
        { mode: 'public-only' },
    );

    assert.equal(plan.cases.length, 1);
    const c = plan.cases[0];
    assert.equal(c.kind, 'doctest');

    assert.equal(generatedFiles.length, 1);
    const gf = generatedFiles[0];
    assert.match(gf.path, /^\/home\/user\/functional_0_test\.cpp$/, 'workspace mount path + index filename');
    // TEST_CASE label falls back to options.name (group Name), not functionName.
    assert.match(gf.content, /TEST_CASE\("0:add-ints"\)/, 'TEST_CASE label uses the group Name');
    assert.match(gf.content, /extern "C" int add\(int, int\);/, 'lowercase wire Type → PascalCase C++ type');
    assert.match(gf.content, /CHECK\(add\(2, 3\) == 5\)/, 'argument + expected literals');
});

test('buildTestPlan: FunctionalTest in doctest case references generated harness path', () => {
    const { plan, generatedFiles } = buildTestPlan(
        {
            ...sampleAssignment(),
            Tests: { Public: [funcPublic], Private: [] },
        },
        { mode: 'public-only' },
    );

    const c = plan.cases[0];
    if (c.kind !== 'doctest') throw new Error('expected doctest');
    assert.deepEqual(c.sourceFiles, [generatedFiles[0].path]);
});

test('buildTestPlan: weight + name flow through for both kinds', () => {
    const { plan } = buildTestPlan(sampleAssignment(), { mode: 'full' });

    // Public[0] = stdPublic: Weight 2, Name 'hello-stdout'
    const a = plan.cases[0];
    assert.equal(a.kind, 'stdio');
    assert.equal(a.weight, 2);
    assert.equal(a.name, 'hello-stdout');

    // Public[1] = funcPublic: Weight 5, Name 'add-ints'
    const b = plan.cases[1];
    assert.equal(b.kind, 'doctest');
    assert.equal(b.weight, 5);
    assert.equal(b.name, 'add-ints');

    // Private[0] = stdPrivate: Weight 3, Name 'private-edge'
    const c = plan.cases[2];
    assert.equal(c.kind, 'stdio');
    assert.equal(c.weight, 3);
    assert.equal(c.name, 'private-edge');
});

test('buildTestPlan: plan.build.sources contains all text-encoded files, base64 excluded', () => {
    const { plan } = buildTestPlan(sampleAssignment(), { mode: 'full' });

    assert.ok(plan.build, 'plan.build is set');
    const sources = plan.build!.sources ?? [];
    assert.equal(sources.length, 2, '2 text files only (1 explicit + 1 default-encoded)');
    assert.ok(sources.includes('/home/user/solution.cpp'));
    assert.ok(sources.includes('/home/user/helper.cpp'));
    assert.ok(!sources.includes('/home/user/asset.png'), 'base64 file excluded');
});

test('buildTestPlan: indices are unique per functional test within one call', () => {
    // Concatenated list: [StdA, FuncB, StdC, FuncD] → functional indices 1 and 3.
    const func2: Test = {
        kind: 'functional',
        Weight: 1,
        Function: {
            FunctionName: 'square',
            Parameters: [{ Name: 'x', Type: 'integer', Content: 0 }],
            ReturnType: { Type: 'integer', Content: 0 },
        },
        Cases: [
            { Inputs: [{ Type: 'integer', Content: 4 }], Expected: { Type: 'integer', Content: 16 } },
        ],
    };
    const { plan, generatedFiles } = buildTestPlan(
        {
            ...sampleAssignment(),
            Tests: {
                Public: [stdPublic, funcPublic, stdPrivate],
                Private: [func2],
            },
        },
        { mode: 'full' },
    );

    assert.equal(plan.cases.length, 4);
    assert.equal(generatedFiles.length, 2);
    assert.deepEqual(
        generatedFiles.map((g) => g.path),
        ['/home/user/functional_1_test.cpp', '/home/user/functional_3_test.cpp'],
        'indices are array positions in the concatenated list',
    );
});

test('buildTestPlan: empty Tests → empty plan.cases, still returns sources', () => {
    const { plan, generatedFiles } = buildTestPlan(
        { ...sampleAssignment(), Tests: { Public: [], Private: [] } },
        { mode: 'full' },
    );

    assert.equal(plan.cases.length, 0);
    assert.equal(generatedFiles.length, 0);
    assert.equal(plan.build!.sources!.length, 2, 'sources gathered independent of tests');
});

test('buildTestPlan: throws on unknown kind (does not silently drop)', () => {
    // TS rules this out at compile time; verify runtime guard bites anyway.
    const rogue = { kind: 'mutant', Weight: 1 } as unknown as Test;
    const assignment: CodingAssignmentContent = {
        ...sampleAssignment(),
        Tests: { Public: [rogue], Private: [] },
    };

    assert.throws(
        () => buildTestPlan(assignment, { mode: 'public-only' }),
        /Unsupported test kind: mutant/,
        'hand-built input smuggling a non-v1 kind is rejected',
    );
});

test('buildTestPlan: FunctionalTestGroup with N cases → ONE harness file with N CHECK blocks', () => {
    const group: Test = {
        kind: 'functional',
        Weight: 4,
        Name: 'add-multi',
        Function: {
            FunctionName: 'add',
            Parameters: [
                { Name: 'a', Type: 'integer', Content: 0 },
                { Name: 'b', Type: 'integer', Content: 0 },
            ],
            ReturnType: { Type: 'integer', Content: 0 },
        },
        Cases: [
            { Inputs: [{ Type: 'integer', Content: 2 }, { Type: 'integer', Content: 3 }], Expected: { Type: 'integer', Content: 5 } },
            { Inputs: [{ Type: 'integer', Content: 10 }, { Type: 'integer', Content: 20 }], Expected: { Type: 'integer', Content: 30 } },
        ],
    };
    const { plan, generatedFiles } = buildTestPlan(
        {
            ...sampleAssignment(),
            Tests: { Public: [group], Private: [] },
        },
        { mode: 'public-only' },
    );

    // ONE plan case + ONE generated file for the whole group (not per case).
    assert.equal(plan.cases.length, 1);
    assert.equal(generatedFiles.length, 1);

    const gf = generatedFiles[0];
    assert.match(gf.path, /^\/home\/user\/functional_0_test\.cpp$/);

    // ONE extern "C" decl, ONE TEST_CASE block, N CHECK lines.
    const declCount = (gf.content.match(/^extern "C" int add\(int, int\);$/gm) ?? []).length;
    assert.equal(declCount, 1, 'extern "C" decl emitted exactly once for the group');

    const testCaseCount = (gf.content.match(/^TEST_CASE\("0:add-multi"\) \{$/gm) ?? []).length;
    assert.equal(testCaseCount, 1, 'TEST_CASE block emitted exactly once');

    assert.match(gf.content, /^    CHECK\(add\(2, 3\) == 5\);$/m);
    assert.match(gf.content, /^    CHECK\(add\(10, 20\) == 30\);$/m);
    const checkCount = (gf.content.match(/^    CHECK\(add\(/gm) ?? []).length;
    assert.equal(checkCount, 2, 'one CHECK per case');
});

test('buildTestPlan: FunctionalTestGroup case weight = Weight / Cases.length (lazy equal-split)', () => {
    const group: Test = {
        kind: 'functional',
        Weight: 6,
        Name: 'add-split',
        Function: {
            FunctionName: 'add',
            Parameters: [
                { Name: 'a', Type: 'integer', Content: 0 },
                { Name: 'b', Type: 'integer', Content: 0 },
            ],
            ReturnType: { Type: 'integer', Content: 0 },
        },
        Cases: [
            { Inputs: [{ Type: 'integer', Content: 1 }, { Type: 'integer', Content: 1 }], Expected: { Type: 'integer', Content: 2 } },
            { Inputs: [{ Type: 'integer', Content: 2 }, { Type: 'integer', Content: 2 }], Expected: { Type: 'integer', Content: 4 } },
            { Inputs: [{ Type: 'integer', Content: 3 }, { Type: 'integer', Content: 3 }], Expected: { Type: 'integer', Content: 6 } },
        ],
    };
    const { plan } = buildTestPlan(
        {
            ...sampleAssignment(),
            Tests: { Public: [group], Private: [] },
        },
        { mode: 'public-only' },
    );

    // 6 / 3 cases = 2 per case-weight (one plan TestCase carries 2).
    assert.equal(plan.cases.length, 1);
    const c = plan.cases[0];
    if (c.kind !== 'doctest') throw new Error('expected doctest');
    assert.equal(c.weight, 2, 'Weight 6 split equally across 3 cases → 2');
});

test('buildTestPlan: Weight 0 is valid for FunctionalTestGroup (compiles, contributes 0)', () => {
    const group: Test = {
        kind: 'functional',
        Weight: 0,
        Function: {
            FunctionName: 'add',
            Parameters: [{ Name: 'a', Type: 'integer', Content: 0 }],
            ReturnType: { Type: 'integer', Content: 0 },
        },
        Cases: [
            { Inputs: [{ Type: 'integer', Content: 1 }], Expected: { Type: 'integer', Content: 1 } },
        ],
    };
    const { plan } = buildTestPlan(
        {
            ...sampleAssignment(),
            Tests: { Public: [group], Private: [] },
        },
        { mode: 'public-only' },
    );

    assert.equal(plan.cases.length, 1, 'Weight 0 group still emits a case');
    const c = plan.cases[0];
    if (c.kind !== 'doctest') throw new Error('expected doctest');
    assert.equal(c.weight, 0, 'Weight 0 → 0/N = 0 (valid, runs but does not contribute)');
});

test('buildTestPlan: FunctionalTestGroup with 0 Cases throws (Metis M5)', () => {
    const emptyGroup = {
        kind: 'functional',
        Weight: 5,
        Function: {
            FunctionName: 'add',
            Parameters: [],
            ReturnType: { Type: 'integer', Content: 0 },
        },
        Cases: [],
    } as unknown as Test;
    const assignment: CodingAssignmentContent = {
        ...sampleAssignment(),
        Tests: { Public: [emptyGroup], Private: [] },
    };

    assert.throws(
        () => buildTestPlan(assignment, { mode: 'public-only' }),
        { message: 'FunctionalTestGroup requires \u22651 case' },
        'explicit throw beats JS divide-by-zero returning Infinity',
    );
});
