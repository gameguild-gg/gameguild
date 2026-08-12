/**
 * Clang-query matcher engine (runtime-agnostic half).
 *
 * Parses a tiny subset of the LibASTMatchers DSL and walks a clang AST-dump
 * JSON tree counting matches. The dump itself is produced by the adapter
 * via `clang -Xclang -ast-dump=json` (out of scope here); this module only
 * deals with the parsed JSON, so it has no DOM, no Node, and no clang
 * dependency at runtime.
 *
 * Supported matcher names (all that the plan's example needs and a small
 * useful neighborhood):
 *
 *   Kind matchers (each accepts zero-or-more inner matchers, ANDed):
 *     decl(...)              — any *Decl node
 *     namedDecl(...)         — any node with a `name` field
 *     recordDecl(...)        — RecordDecl
 *     cxxRecordDecl(...)     — CXXRecordDecl (C++ class/struct)
 *     functionDecl(...)      — FunctionDecl
 *     cxxMethodDecl(...)     — CXXMethodDecl
 *     varDecl(...)           — VarDecl
 *     fieldDecl(...)         — FieldDecl
 *     parmVarDecl(...)       — ParmVarDecl
 *     namespaceDecl(...)     — NamespaceDecl
 *     enumDecl(...)          — EnumDecl
 *
 *   Aliases (for the plan's exact example syntax):
 *     hasRecordDecl(...)     — alias for `recordDecl(...)`
 *
 *   Predicates:
 *     hasName("X")           — node.name === "X"
 *     matchesName("/regex/") — RegExp on node.name
 *     isExpansionInMainFile()— node.loc.includedFrom == null (best-effort
 *                              w/ clang JSON; treats missing loc as main)
 *
 *   Combinators:
 *     allOf(a, b, ...)       — every inner must match (default for kind args)
 *     anyOf(a, b, ...)       — at least one
 *     unless(a)              — negation
 *     has(inner)             — at least one *direct child* matches inner
 *     hasDescendant(inner)   — at least one descendant matches inner
 *
 * Unsupported names raise `TestFailureError` at parse time so misspelled
 * matchers surface immediately rather than silently matching nothing.
 */

import { TestFailureError } from '../../errors.js';

/** Minimal shape of a clang AST-dump JSON node we care about. */
export interface ClangAstNode {
  kind?: string;
  name?: string;
  inner?: ClangAstNode[];
  loc?: { includedFrom?: unknown };
  [k: string]: unknown;
}

/** A node-level predicate compiled from a matcher string. */
export type CompiledMatcher = (node: ClangAstNode) => boolean;

export interface MatchResult {
  /** Number of distinct nodes satisfying the matcher anywhere in the tree. */
  count: number;
  /** First N matched nodes' `kind`/`name` (capped, for diagnostics). */
  samples: Array<{ kind?: string; name?: string }>;
}

const MAX_SAMPLES = 8;

// ─────────────────────── DSL parser ───────────────────────

interface ParsedCall {
  kind: 'call';
  name: string;
  args: ParsedArg[];
}
type ParsedArg = ParsedCall | { kind: 'string'; value: string };

class Parser {
  private i = 0;
  constructor(private readonly src: string) {}

  parse(): ParsedCall {
    this.skipWs();
    const top = this.parseCall();
    this.skipWs();
    if (this.i !== this.src.length) {
      throw new TestFailureError(`clang-query matcher: trailing input at position ${this.i} in ${JSON.stringify(this.src)}`);
    }
    return top;
  }

  private parseCall(): ParsedCall {
    const name = this.parseIdent();
    this.skipWs();
    if (this.peek() !== '(') {
      throw new TestFailureError(`clang-query matcher: expected '(' after '${name}' at position ${this.i}`);
    }
    this.i++; // consume '('
    const args: ParsedArg[] = [];
    this.skipWs();
    if (this.peek() === ')') {
      this.i++;
      return { kind: 'call', name, args };
    }
    while (true) {
      this.skipWs();
      args.push(this.parseArg());
      this.skipWs();
      const c = this.peek();
      if (c === ',') {
        this.i++;
        continue;
      }
      if (c === ')') {
        this.i++;
        return { kind: 'call', name, args };
      }
      throw new TestFailureError(`clang-query matcher: expected ',' or ')' at position ${this.i}`);
    }
  }

  private parseArg(): ParsedArg {
    const c = this.peek();
    if (c === '"') return this.parseString();
    return this.parseCall();
  }

  private parseString(): { kind: 'string'; value: string } {
    if (this.peek() !== '"') {
      throw new TestFailureError(`clang-query matcher: expected '"' at position ${this.i}`);
    }
    this.i++;
    let out = '';
    while (this.i < this.src.length) {
      const ch = this.src[this.i];
      if (ch === '\\' && this.i + 1 < this.src.length) {
        out += this.src[this.i + 1];
        this.i += 2;
        continue;
      }
      if (ch === '"') {
        this.i++;
        return { kind: 'string', value: out };
      }
      out += ch;
      this.i++;
    }
    throw new TestFailureError(`clang-query matcher: unterminated string starting at position ${this.i}`);
  }

  private parseIdent(): string {
    const start = this.i;
    while (this.i < this.src.length && /[A-Za-z0-9_]/.test(this.src[this.i])) {
      this.i++;
    }
    if (start === this.i) {
      throw new TestFailureError(`clang-query matcher: expected identifier at position ${this.i}`);
    }
    return this.src.slice(start, this.i);
  }

  private peek(): string {
    return this.src[this.i] ?? '';
  }

  private skipWs(): void {
    while (this.i < this.src.length && /\s/.test(this.src[this.i])) this.i++;
  }
}

// ─────────────────────── matcher registry ───────────────────────

type MatcherFactory = (args: CompiledMatcher[], rawArgs: ParsedArg[]) => CompiledMatcher;

/**
 * Map matcher name → set of clang AST `kind` strings it matches.
 * Use null to mean "any *Decl" (suffix check); use the sentinel
 * `'__hasName__'` for `namedDecl` (any node with a string `name`).
 */
const KIND_MATCHERS: Record<string, ReadonlyArray<string> | '__anyDecl__' | '__hasName__'> = {
  decl: '__anyDecl__',
  namedDecl: '__hasName__',
  // Clang's RecordDecl matcher catches both RecordDecl AND CXXRecordDecl
  // (the latter is a subclass). Mirror that here so the plan's example
  // matches a C++ class without forcing callers to know the exact kind.
  recordDecl: ['RecordDecl', 'CXXRecordDecl'],
  hasRecordDecl: ['RecordDecl', 'CXXRecordDecl'], // plan-doc alias
  cxxRecordDecl: ['CXXRecordDecl'],
  functionDecl: ['FunctionDecl', 'CXXMethodDecl'],
  cxxMethodDecl: ['CXXMethodDecl'],
  varDecl: ['VarDecl', 'ParmVarDecl'],
  fieldDecl: ['FieldDecl'],
  parmVarDecl: ['ParmVarDecl'],
  namespaceDecl: ['NamespaceDecl'],
  enumDecl: ['EnumDecl'],
};

function kindFactory(spec: ReadonlyArray<string> | '__anyDecl__' | '__hasName__'): MatcherFactory {
  return (innerMatchers) => {
    const kindOk = (n: ClangAstNode): boolean => {
      if (spec === '__anyDecl__') return typeof n.kind === 'string' && n.kind.endsWith('Decl');
      if (spec === '__hasName__') return typeof n.name === 'string';
      return typeof n.kind === 'string' && spec.includes(n.kind);
    };
    if (innerMatchers.length === 0) return kindOk;
    return (n) => kindOk(n) && innerMatchers.every((m) => m(n));
  };
}

const FACTORIES: Record<string, MatcherFactory> = {
  ...Object.fromEntries(Object.entries(KIND_MATCHERS).map(([name, spec]) => [name, kindFactory(spec)])),

  hasName: (_inner, raw) => {
    const arg = raw[0];
    if (!arg || arg.kind !== 'string' || raw.length !== 1) {
      throw new TestFailureError('hasName(...) takes exactly one string argument.');
    }
    const want = arg.value;
    return (n) => n.name === want;
  },

  matchesName: (_inner, raw) => {
    const arg = raw[0];
    if (!arg || arg.kind !== 'string' || raw.length !== 1) {
      throw new TestFailureError('matchesName(...) takes exactly one string argument.');
    }
    const re = new RegExp(arg.value);
    return (n) => typeof n.name === 'string' && re.test(n.name);
  },

  isExpansionInMainFile: (_inner, raw) => {
    if (raw.length !== 0) throw new TestFailureError('isExpansionInMainFile() takes no arguments.');
    return (n) => !n.loc || n.loc.includedFrom == null;
  },

  allOf: (inner) => {
    if (inner.length === 0) return () => true;
    return (n) => inner.every((m) => m(n));
  },

  anyOf: (inner) => {
    if (inner.length === 0) return () => false;
    return (n) => inner.some((m) => m(n));
  },

  unless: (inner) => {
    if (inner.length !== 1) throw new TestFailureError('unless(...) takes exactly one inner matcher.');
    const m = inner[0];
    return (n) => !m(n);
  },

  has: (inner) => {
    if (inner.length !== 1) throw new TestFailureError('has(...) takes exactly one inner matcher.');
    const m = inner[0];
    return (n) => Array.isArray(n.inner) && n.inner.some(m);
  },

  hasDescendant: (inner) => {
    if (inner.length !== 1) throw new TestFailureError('hasDescendant(...) takes exactly one inner matcher.');
    const m = inner[0];
    const search = (node: ClangAstNode): boolean => {
      const kids = Array.isArray(node.inner) ? node.inner : [];
      for (const k of kids) {
        if (m(k)) return true;
        if (search(k)) return true;
      }
      return false;
    };
    return search;
  },
};

// ─────────────────────── compile + run ───────────────────────

function compileCall(call: ParsedCall): CompiledMatcher {
  const factory = FACTORIES[call.name];
  if (!factory) {
    throw new TestFailureError(`clang-query matcher: unsupported matcher '${call.name}'. ` + `Supported: ${Object.keys(FACTORIES).sort().join(', ')}.`);
  }
  const inner = call.args.filter((a): a is ParsedCall => a.kind === 'call').map(compileCall);
  return factory(inner, call.args);
}

/** Compile a matcher string into a reusable `CompiledMatcher`. */
export function compileMatcher(source: string): CompiledMatcher {
  return compileCall(new Parser(source).parse());
}

/**
 * Walk every node in `root` (DFS), counting nodes that satisfy `matcher`.
 * Returns a `MatchResult` with the total count and a capped sample for
 * diagnostics. Does not mutate the tree.
 */
export function runMatcher(matcher: CompiledMatcher, root: ClangAstNode): MatchResult {
  let count = 0;
  const samples: MatchResult['samples'] = [];
  const visit = (node: ClangAstNode): void => {
    if (matcher(node)) {
      count += 1;
      if (samples.length < MAX_SAMPLES) {
        samples.push({ kind: node.kind, name: node.name });
      }
    }
    const kids = Array.isArray(node.inner) ? node.inner : [];
    for (const k of kids) visit(k);
  };
  visit(root);
  return { count, samples };
}

/** Convenience: parse + run in one call. */
export function queryClangAst(source: string, root: ClangAstNode): MatchResult {
  return runMatcher(compileMatcher(source), root);
}
