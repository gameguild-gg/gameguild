import { beforeEach, describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({
  createServerClient: vi.fn(),
  getToken: vi.fn(),
  request: vi.fn(),
}));

vi.mock('@/auth', () => ({
  getToken: mocks.getToken,
}));

vi.mock('@game-guild/client', () => ({
  createServerClient: mocks.createServerClient,
}));

import {
  getCodingAssignmentPublic,
  getCodingAssignmentFull,
  putCodingAssignment,
  isStandardTest,
  isFunctionalTestGroup,
  isFunctionalTestCase,
  isTest,
  isTestFunctionData,
  isFunctionParameter,
  isFunctionParameterWithName,
  narrowCodingAssignmentContent,
  type CodingAssignmentContent,
  type StandardTest,
  type FunctionalTestGroup,
  type Test,
} from './client';

// ---------------------------------------------------------------------------
// Fixtures — mirror the wire shape (PascalCase fields + lowercase `kind`).
// ---------------------------------------------------------------------------

const standardTest: StandardTest = {
  kind: 'standard',
  Weight: 1.5,
  Name: 'greets the user',
  Stdin: '',
  Stdout: 'hello\n',
  ExitCode: 0,
};

const functionalTestGroup: FunctionalTestGroup = {
  kind: 'functional',
  Weight: 2.0,
  Function: {
    FunctionName: 'add',
    Parameters: [
      { Name: 'a', Type: 'integer' },
      { Name: 'b', Type: 'integer' },
    ],
    ReturnType: { Type: 'integer' },
  },
  Cases: [
    { Inputs: [{ Type: 'integer', Content: 2 }, { Type: 'integer', Content: 3 }], Expected: { Type: 'integer', Content: 5 } },
    { Inputs: [{ Type: 'integer', Content: 10 }, { Type: 'integer', Content: 20 }], Expected: { Type: 'integer', Content: 30 } },
  ],
};

const fullContent: CodingAssignmentContent = {
  Type: 'coding-assignment',
  Version: 1,
  Environment: {
    Language: 'cpp',
    Tools: 'clang',
    AllowStudentCreateFiles: true,
  },
  Data: {
    Files: {
      'main.cpp': {
        Content: 'int main() { return 0; }',
        Encoding: 'text',
        Visibility: 'Public',
        Modifiable: true,
      },
    },
  },
  Tests: {
    Public: [standardTest],
    Private: [functionalTestGroup],
  },
  Grading: { MaxScore: 100 },
};

// ---------------------------------------------------------------------------
// Wrappers — exercised against a mocked client.request.
// ---------------------------------------------------------------------------

describe('coding-assignment client wrappers', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mocks.getToken.mockResolvedValue('access-token');
    mocks.createServerClient.mockReturnValue({
      request: mocks.request,
    });
    // PINNED signature check: typeof each export matches the contract.
    expect(typeof getCodingAssignmentPublic).toBe('function');
    expect(typeof getCodingAssignmentFull).toBe('function');
    expect(typeof putCodingAssignment).toBe('function');
  });

  it('getCodingAssignmentPublic: returns narrowed content on HTTP 200', async () => {
    mocks.request.mockResolvedValue({ ok: true, data: fullContent });

    const result = await getCodingAssignmentPublic('prog-1', 'content-1');

    expect(mocks.request).toHaveBeenCalledWith({
      method: 'GET',
      path: '/v1.0/courses/prog-1/content/content-1/coding-assignment',
      requiresAuth: true,
    });
    expect(result).not.toBeNull();
    expect(result?.Type).toBe('coding-assignment');
    expect(result?.Tests.Public[0].kind).toBe('standard');
    expect(result?.Tests.Private[0].kind).toBe('functional');
  });

  it('getCodingAssignmentPublic: returns null on HTTP failure', async () => {
    mocks.request.mockResolvedValue({
      ok: false,
      error: { status: 404, message: 'Not Found' },
    });
    const result = await getCodingAssignmentPublic('prog-1', 'content-1');
    expect(result).toBeNull();
  });

  it('getCodingAssignmentFull: hits the /full route and narrows', async () => {
    mocks.request.mockResolvedValue({ ok: true, data: fullContent });
    const result = await getCodingAssignmentFull('prog-1', 'content-1');
    expect(mocks.request).toHaveBeenCalledWith({
      method: 'GET',
      path: '/v1.0/courses/prog-1/content/content-1/coding-assignment/full',
      requiresAuth: true,
    });
    expect(result?.Tests.Private).toHaveLength(1);
  });

  it('putCodingAssignment: returns {success:true} on HTTP 200', async () => {
    mocks.request.mockResolvedValue({ ok: true, data: fullContent });
    const result = await putCodingAssignment('prog-1', 'content-1', fullContent);
    expect(mocks.request).toHaveBeenCalledWith({
      method: 'PUT',
      path: '/v1.0/courses/prog-1/content/content-1/coding-assignment',
      body: fullContent,
      requiresAuth: true,
    });
    expect(result).toEqual({ success: true });
  });

  it('putCodingAssignment: returns {success:false,error} on validation failure, extracting the detail', async () => {
    mocks.request.mockResolvedValue({
      ok: false,
      error: {
        status: 400,
        code: 'VALIDATION_ERROR',
        detail: 'private_file_not_modifiable: Private files must not be modifiable',
      },
    });
    const result = await putCodingAssignment('prog-1', 'content-1', fullContent);
    expect(result).toEqual({
      success: false,
      error: 'private_file_not_modifiable: Private files must not be modifiable',
    });
  });

  it('putCodingAssignment: catches thrown errors', async () => {
    mocks.request.mockRejectedValue(new Error('network down'));
    const result = await putCodingAssignment('prog-1', 'content-1', fullContent);
    expect(result).toEqual({
      success: false,
      error: 'Unexpected error: network down',
    });
  });
});

// ---------------------------------------------------------------------------
// Type-guard smoke tests — verify the narrowing logic the wrappers rely on.
// ---------------------------------------------------------------------------

describe('coding-assignment type guards', () => {
  it('isStandardTest accepts a well-formed StandardTest and rejects Foreign shapes', () => {
    expect(isStandardTest(standardTest)).toBe(true);
    expect(isStandardTest(functionalTestGroup)).toBe(false);
    expect(isStandardTest({ kind: 'standard' })).toBe(false); // missing Stdout
    expect(isStandardTest({ kind: 'standard', Stdout: 42 })).toBe(false); // wrong type
    expect(isStandardTest(null)).toBe(false);
    expect(isStandardTest(undefined)).toBe(false);
  });

  it('isFunctionalTestGroup accepts a well-formed FunctionalTestGroup', () => {
    expect(isFunctionalTestGroup(functionalTestGroup)).toBe(true);
    expect(isFunctionalTestGroup(standardTest)).toBe(false);
    expect(isFunctionalTestGroup({ kind: 'functional' })).toBe(false);
    // ponytail: 0-case groups pass the guard — Array.every is vacuous on []. The
    // server's at_least_one_case validator rejects empty Cases; the FE guard only
    // narrows the wire shape and does not duplicate server-side rules.
    expect(isFunctionalTestGroup({ kind: 'functional', Function: functionalTestGroup.Function, Cases: [] })).toBe(true);
  });

  it('isFunctionalTestCase narrows each case shape', () => {
    expect(isFunctionalTestCase(functionalTestGroup.Cases[0])).toBe(true);
    expect(isFunctionalTestCase({ Inputs: [], Expected: { Type: 'integer', Content: 0 } })).toBe(true);
    expect(isFunctionalTestCase({ Inputs: [{ Type: 'integer', Content: 1 }] })).toBe(false); // missing Expected
    expect(isFunctionalTestCase({ Inputs: [{ Type: 'array', Content: [] }], Expected: { Type: 'integer', Content: 0 } })).toBe(false); // bad param type
  });

  it('isTest accepts any Test variant', () => {
    const t: Test = standardTest;
    expect(isTest(t)).toBe(true);
    expect(isTest(functionalTestGroup)).toBe(true);
    expect(isTest({ Weight: 1 })).toBe(false);
  });

  it('isFunctionParameter / isFunctionParameterWithName narrow correctly', () => {
    expect(isFunctionParameter({ Type: 'string', Content: 'hi' })).toBe(true);
    expect(isFunctionParameter({ Type: 'integer', Content: 5 })).toBe(true);
    expect(isFunctionParameter({ Type: 'integer', Content: 'no' })).toBe(true); // guard trusts wire, not semantic
    expect(isFunctionParameter({ Type: 'array', Content: [] })).toBe(false); // array not in v1 set
    expect(isFunctionParameter({ Type: 'integer' })).toBe(false); // missing Content
    expect(isFunctionParameterWithName({ Name: 'x', Type: 'string' })).toBe(true);
    expect(isFunctionParameterWithName({ Type: 'string' })).toBe(false); // missing Name
  });

  it('isTestFunctionData validates function metadata', () => {
    expect(isTestFunctionData(functionalTestGroup.Function)).toBe(true);
    expect(isTestFunctionData({ FunctionName: 'add' })).toBe(false); // missing Parameters + ReturnType
    expect(
      isTestFunctionData({
        FunctionName: 'add',
        Parameters: [{ Name: 'a', Type: 'integer' }],
        ReturnType: { Type: 'integer' },
      }),
    ).toBe(true);
  });

  it('narrowCodingAssignmentContent accepts a full payload and rejects malformed ones', () => {
    expect(narrowCodingAssignmentContent(fullContent)).not.toBeNull();
    // wrong Type discriminator
    expect(
      narrowCodingAssignmentContent({ ...fullContent, Type: 'quiz-assignment' }),
    ).toBeNull();
    // wrong Version
    expect(
      narrowCodingAssignmentContent({ ...fullContent, Version: 2 }),
    ).toBeNull();
    // missing Tests
    expect(
      narrowCodingAssignmentContent({ ...fullContent, Tests: undefined }),
    ).toBeNull();
    // unknown garbage
    expect(narrowCodingAssignmentContent(null)).toBeNull();
    expect(narrowCodingAssignmentContent(42)).toBeNull();
  });

  it('narrowCodingAssignmentContent drops test entries that fail the kind guard', () => {
    const malformed = {
      ...fullContent,
      Tests: {
        Public: [standardTest, { kind: 'mystery', Stdout: 'x' }],
        Private: [],
      },
    };
    const narrowed = narrowCodingAssignmentContent(malformed);
    expect(narrowed).not.toBeNull();
    expect(narrowed?.Tests.Public).toHaveLength(1);
    expect(narrowed?.Tests.Public[0].kind).toBe('standard');
  });
});

// ---------------------------------------------------------------------------
// Wire-casing normalization — the API returns camelCase JSON (AddJsonOptions
// web defaults); narrow must normalize to the PascalCase guard shape.
// ---------------------------------------------------------------------------

// Mirrors the proven live wire shape (curl against :8080).
const camelCaseWirePayload = {
  type: 'coding-assignment',
  version: 1,
  environment: {
    language: 'cpp',
    tools: 'clang',
    libBundle: null,
    allowStudentCreateFiles: false,
  },
  data: {
    files: {
      '/user/main.cpp': {
        content: '#include <iostream>\nint main() { std::string n; std::getline(std::cin, n); std::cout << "hello " << n; }',
        encoding: 'text',
        visibility: 'Public',
        modifiable: true,
      },
    },
  },
  tests: {
    public: [
      {
        kind: 'standard',
        weight: 1,
        name: 'greets the world',
        stdin: 'world',
        stdout: 'hello world',
        stderr: null,
        exitCode: 0,
      },
      {
        kind: 'functional',
        weight: 1,
        name: 'adds integers',
        function: {
          functionName: 'add',
          parameters: [
            { name: 'a', type: 'integer' },
            { name: 'b', type: 'integer' },
          ],
          returnType: { type: 'integer' },
        },
        cases: [
          {
            inputs: [
              { type: 'integer', content: 2 },
              { type: 'integer', content: 3 },
            ],
            expected: { type: 'integer', content: 5 },
          },
        ],
      },
    ],
    private: [],
  },
  grading: { maxScore: 100 },
};

describe('narrowCodingAssignmentContent wire-casing normalization', () => {
  it('accepts the live camelCase payload and returns PascalCase internals', () => {
    const result = narrowCodingAssignmentContent(camelCaseWirePayload);
    expect(result).not.toBeNull();
    expect(result?.Type).toBe('coding-assignment');
    expect(result?.Version).toBe(1);
    expect(result?.Environment.Language).toBe('cpp');
    expect(result?.Environment.Tools).toBe('clang');
    expect(result?.Environment.AllowStudentCreateFiles).toBe(false);
    expect(result?.Grading.MaxScore).toBe(100);
    expect(result?.Tests.Public).toHaveLength(2);
    const [standard, functional] = result?.Tests.Public ?? [];
    expect(standard?.kind).toBe('standard');
    expect(standard?.Stdin).toBe('world');
    expect(standard?.Stdout).toBe('hello world');
    expect(standard?.ExitCode).toBe(0);
    expect(functional?.kind).toBe('functional');
    expect(functional?.Function.FunctionName).toBe('add');
    expect(functional?.Function.Parameters[0]?.Name).toBe('a');
    expect(functional?.Function.ReturnType.Type).toBe('integer');
    expect(functional?.Cases[0]?.Expected.Content).toBe(5);
  });

  it('still accepts PascalCase payloads (no regression)', () => {
    const result = narrowCodingAssignmentContent(fullContent);
    expect(result).not.toBeNull();
    expect(result?.Data.Files['main.cpp']?.Content).toBe('int main() { return 0; }');
    expect(result?.Tests.Public[0]?.Stdout).toBe('hello\n');
  });

  it('preserves file-path keys inside data.files unchanged', () => {
    const result = narrowCodingAssignmentContent(camelCaseWirePayload);
    expect(Object.keys(result?.Data.Files ?? {})).toEqual(['/user/main.cpp']);
    expect(result?.Data.Files['/user/main.cpp']?.Modifiable).toBe(true);
    expect(result?.Data.Files['/user/main.cpp']?.Visibility).toBe('Public');
  });

  it('normalizes PascalCase parameter types (e.g. Type: "Integer") in functional tests', () => {
    const payloadWithPascalEnums = {
      type: 'coding-assignment',
      version: 1,
      environment: {
        language: 'cpp',
        tools: 'clang',
        allowStudentCreateFiles: false,
      },
      data: {
        files: {
          '/user/main.cpp': {
            content: 'int add(int a, int b) { return a + b; }',
            encoding: 'text',
            visibility: 'Public',
            modifiable: true,
          },
        },
      },
      tests: {
        public: [
          {
            kind: 'functional',
            name: 'add-fn',
            weight: 1,
            function: {
              functionName: 'add',
              parameters: [
                { name: 'a', type: 'Integer' },
                { name: 'b', type: 'Integer' },
              ],
              returnType: { type: 'Integer' },
            },
            cases: [
              {
                inputs: [
                  { type: 'Integer', content: 2 },
                  { type: 'Integer', content: 3 },
                ],
                expected: { type: 'Integer', content: 5 },
              },
            ],
          },
        ],
        private: [],
      },
      grading: { maxScore: 100 },
    };

    const result = narrowCodingAssignmentContent(payloadWithPascalEnums);
    expect(result).not.toBeNull();
    const [fnTest] = result?.Tests.Public ?? [];
    expect(fnTest?.kind).toBe('functional');
    if (fnTest?.kind === 'functional') {
      expect(fnTest.Function.Parameters[0]?.Type).toBe('integer');
      expect(fnTest.Function.ReturnType.Type).toBe('integer');
      expect(fnTest.Cases[0]?.Inputs[0]?.Type).toBe('integer');
      expect(fnTest.Cases[0]?.Expected.Type).toBe('integer');
    }
  });
});

// ---------------------------------------------------------------------------
// Compile-time check: pinned signatures are enforced by TS at the import site.
// If the wrapper exports drift, this file fails to typecheck.
// ---------------------------------------------------------------------------

describe('pinned signatures (compile-time)', () => {
  it('getCodingAssignmentPublic: (programId, contentId) => Promise<CodingAssignmentContent | null>', async () => {
    const fn: (
      programId: string,
      contentId: string,
    ) => Promise<CodingAssignmentContent | null> = getCodingAssignmentPublic;
    expect(typeof fn).toBe('function');
  });

  it('getCodingAssignmentFull: (programId, contentId) => Promise<CodingAssignmentContent | null>', async () => {
    const fn: (
      programId: string,
      contentId: string,
    ) => Promise<CodingAssignmentContent | null> = getCodingAssignmentFull;
    expect(typeof fn).toBe('function');
  });

  it('putCodingAssignment: (programId, contentId, content) => Promise<{success:true} | {success:false;error}>', async () => {
    const fn: (
      programId: string,
      contentId: string,
      content: CodingAssignmentContent,
    ) => Promise<{ success: true } | { success: false; error: string }> =
      putCodingAssignment;
    expect(typeof fn).toBe('function');
  });
});
