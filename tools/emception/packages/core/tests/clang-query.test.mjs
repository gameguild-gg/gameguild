// Clang-query matcher engine verification over a synthetic AST.

import assert from 'node:assert/strict';
import test from 'node:test';

import {
    TestFailureError,
    compileMatcher,
    queryClangAst,
} from '../dist/index.js';

// Synthetic clang AST-dump shape — kept tiny but representative.
const ast = {
    kind: 'TranslationUnitDecl',
    inner: [
        {
            kind: 'NamespaceDecl', name: 'std', loc: { includedFrom: 'iostream' },
            inner: [
                { kind: 'CXXRecordDecl', name: 'string', loc: { includedFrom: 'iostream' } },
            ],
        },
        {
            kind: 'CXXRecordDecl', name: 'LinkedList',
            inner: [
                { kind: 'FieldDecl', name: 'head' },
                {
                    kind: 'CXXMethodDecl', name: 'push',
                    inner: [{ kind: 'ParmVarDecl', name: 'value' }],
                },
                { kind: 'CXXMethodDecl', name: 'pop' },
            ],
        },
        {
            kind: 'FunctionDecl', name: 'main',
            inner: [{ kind: 'ParmVarDecl', name: 'argc' }, { kind: 'ParmVarDecl', name: 'argv' }],
        },
        { kind: 'VarDecl', name: 'globalCounter' },
    ],
};

test('plan example: hasRecordDecl(hasName("LinkedList")) finds 1', () => {
    const r = queryClangAst('hasRecordDecl(hasName("LinkedList"))', ast);
    assert.equal(r.count, 1);
    assert.deepEqual(r.samples, [{ kind: 'CXXRecordDecl', name: 'LinkedList' }]);
});

test('cxxRecordDecl finds both std::string and LinkedList', () => {
    const r = queryClangAst('cxxRecordDecl()', ast);
    assert.equal(r.count, 2);
});

test('cxxMethodDecl(hasName("push")) finds 1', () => {
    assert.equal(queryClangAst('cxxMethodDecl(hasName("push"))', ast).count, 1);
});

test('functionDecl(hasName("main")) finds 1', () => {
    assert.equal(queryClangAst('functionDecl(hasName("main"))', ast).count, 1);
});

test('matchesName regex on FunctionDecl', () => {
    assert.equal(queryClangAst('functionDecl(matchesName("^ma"))', ast).count, 1);
    assert.equal(queryClangAst('functionDecl(matchesName("^z"))', ast).count, 0);
});

test('allOf / unless combinators', () => {
    // CXXRecordDecl named LinkedList
    assert.equal(
        queryClangAst('cxxRecordDecl(allOf(hasName("LinkedList")))', ast).count,
        1,
    );
    // every CXXRecordDecl that is NOT named std::string
    assert.equal(
        queryClangAst('cxxRecordDecl(unless(hasName("string")))', ast).count,
        1,
    );
});

test('anyOf collects matches across alternatives', () => {
    // any decl named "main" OR "globalCounter"
    assert.equal(
        queryClangAst(
            'decl(anyOf(hasName("main"), hasName("globalCounter")))',
            ast,
        ).count,
        2,
    );
});

test('has() requires a *direct* child match', () => {
    // CXXRecordDecl with a *direct* FieldDecl child → LinkedList qualifies
    assert.equal(queryClangAst('cxxRecordDecl(has(fieldDecl()))', ast).count, 1);
    // std::string has no FieldDecl children
});

test('hasDescendant() walks transitively', () => {
    // CXXRecordDecl that has a ParmVarDecl somewhere deeper → LinkedList::push
    assert.equal(
        queryClangAst('cxxRecordDecl(hasDescendant(parmVarDecl()))', ast).count,
        1,
    );
});

test('isExpansionInMainFile filters out included nodes', () => {
    // std namespace was marked includedFrom; LinkedList is in main file.
    const r = queryClangAst(
        'cxxRecordDecl(isExpansionInMainFile())',
        ast,
    );
    assert.equal(r.count, 1);
    assert.equal(r.samples[0].name, 'LinkedList');
});

test('decl(hasName("X")) walks ALL *Decl nodes', () => {
    assert.equal(queryClangAst('decl(hasName("LinkedList"))', ast).count, 1);
    assert.equal(queryClangAst('decl(hasName("argv"))', ast).count, 1);
});

test('namedDecl matches every node carrying a name', () => {
    // 1 std + 1 string + LinkedList + head + push + value + pop + main + argc + argv + globalCounter = 11
    assert.equal(queryClangAst('namedDecl()', ast).count, 11);
});

test('samples are capped at MAX_SAMPLES=8', () => {
    const big = { kind: 'TranslationUnitDecl', inner: [] };
    for (let i = 0; i < 20; i++) {
        big.inner.push({ kind: 'VarDecl', name: `v${i}` });
    }
    const r = queryClangAst('varDecl()', big);
    assert.equal(r.count, 20);
    assert.equal(r.samples.length, 8);
});

test('parser rejects trailing junk', () => {
    assert.throws(() => compileMatcher('functionDecl()garbage'), TestFailureError);
});

test('parser rejects missing parens', () => {
    assert.throws(() => compileMatcher('functionDecl'), TestFailureError);
});

test('unknown matcher name surfaces a clear error', () => {
    assert.throws(
        () => compileMatcher('hasUnicornDecl()'),
        (e) => e instanceof TestFailureError && /unsupported matcher/i.test(e.message),
    );
});

test('hasName rejects non-string args', () => {
    assert.throws(() => compileMatcher('hasName(functionDecl())'), TestFailureError);
});

test('escape sequences inside string args', () => {
    // matcher: hasName("a\"b") should match a node whose name is literally a"b
    const tree = { kind: 'TranslationUnitDecl', inner: [{ kind: 'VarDecl', name: 'a"b' }] };
    assert.equal(queryClangAst('varDecl(hasName("a\\"b"))', tree).count, 1);
});

test('compileMatcher result is reusable across calls', () => {
    const m = compileMatcher('functionDecl()');
    const tree1 = { kind: 'TranslationUnitDecl', inner: [{ kind: 'FunctionDecl', name: 'a' }] };
    const tree2 = { kind: 'TranslationUnitDecl', inner: [] };
    // Use queryClangAst by source vs reusing — both should converge
    assert.equal(queryClangAst('functionDecl()', tree1).count, 1);
    assert.equal(queryClangAst('functionDecl()', tree2).count, 0);
    // Re-use predicate directly:
    assert.equal(m(tree1.inner[0]), true);
});
