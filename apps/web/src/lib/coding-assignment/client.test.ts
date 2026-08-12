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
  isFunctionalTest,
  isTest,
  isTestFunctionData,
  isFunctionParameter,
  isFunctionParameterWithName,
  narrowCodingAssignmentContent,
  type CodingAssignmentContent,
  type StandardTest,
  type FunctionalTest,
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

const functionalTest: FunctionalTest = {
  kind: 'functional',
  Weight: 2.0,
  Function: {
    FunctionName: 'add',
    Parameters: [
      { Name: 'a', Type: 'integer', Content: 2 },
      { Name: 'b', Type: 'integer', Content: 3 },
    ],
    ReturnType: { Type: 'integer', Content: 0 },
  },
  Result: { Type: 'integer', Content: 5 },
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
    Private: [functionalTest],
  },
  Grading: { MaxScore: 100, PassingScore: 60 },
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
    expect(isStandardTest(functionalTest)).toBe(false);
    expect(isStandardTest({ kind: 'standard' })).toBe(false); // missing Stdout
    expect(isStandardTest({ kind: 'standard', Stdout: 42 })).toBe(false); // wrong type
    expect(isStandardTest(null)).toBe(false);
    expect(isStandardTest(undefined)).toBe(false);
  });

  it('isFunctionalTest accepts a well-formed FunctionalTest', () => {
    expect(isFunctionalTest(functionalTest)).toBe(true);
    expect(isFunctionalTest(standardTest)).toBe(false);
    expect(isFunctionalTest({ kind: 'functional' })).toBe(false);
  });

  it('isTest accepts any Test variant', () => {
    const t: Test = standardTest;
    expect(isTest(t)).toBe(true);
    expect(isTest(functionalTest)).toBe(true);
    expect(isTest({ Weight: 1 })).toBe(false);
  });

  it('isFunctionParameter / isFunctionParameterWithName narrow correctly', () => {
    expect(isFunctionParameter({ Type: 'string', Content: 'hi' })).toBe(true);
    expect(isFunctionParameter({ Type: 'integer', Content: 5 })).toBe(true);
    expect(isFunctionParameter({ Type: 'integer', Content: 'no' })).toBe(true); // guard trusts wire, not semantic
    expect(isFunctionParameter({ Type: 'array', Content: [] })).toBe(false); // array not in v1 set
    expect(isFunctionParameter({ Type: 'integer' })).toBe(false); // missing Content
    expect(isFunctionParameterWithName({ Name: 'x', Type: 'string', Content: 'hi' })).toBe(true);
    expect(isFunctionParameterWithName({ Type: 'string', Content: 'hi' })).toBe(false); // missing Name
  });

  it('isTestFunctionData validates function metadata', () => {
    expect(isTestFunctionData(functionalTest.Function)).toBe(true);
    expect(isTestFunctionData({ FunctionName: 'add' })).toBe(false); // missing Parameters + ReturnType
    expect(
      isTestFunctionData({
        FunctionName: 'add',
        Parameters: [{ Name: 'a', Type: 'integer', Content: 1 }],
        ReturnType: { Type: 'integer', Content: 0 },
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
