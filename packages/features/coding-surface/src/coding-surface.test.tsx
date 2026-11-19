import { cleanup, fireEvent, render, screen, waitFor } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { codingContractFixture } from '@game-guild/coding/testing';
import { CodingSurface } from './coding-surface';
import { getCodingSurfaceProfile } from './profile-registry';
import { codingWorkspaceStorageKey } from './storage';
import { workspaceConfigFromDefinition } from './workspace';

const { runtime } = vi.hoisted(() => ({ runtime: {
  workspace: {
    readFile: vi.fn(async () => null),
    writeFile: vi.fn(async () => undefined),
    deleteFile: vi.fn(async () => undefined),
    setBuild: vi.fn(async () => undefined),
  },
  runTests: vi.fn(async () => ({
    passed: 1,
    failed: 1,
    totalDurationMs: 3,
    cases: [
      { name: 'public-pass', passed: true, durationMs: 1 },
      { name: 'public-fail', passed: false, durationMs: 2, diagnostic: 'expected output' },
    ],
  })),
  dispose: vi.fn(),
} }));

vi.mock('@gameguild/emception-browser', () => ({
  createEmception: vi.fn(async () => runtime),
}));

vi.mock('@monaco-editor/react', () => ({
  default: ({ value, onChange, options }: {
    value?: string;
    onChange?: (value: string) => void;
    options?: { readOnly?: boolean };
  }) => (
    <textarea
      data-testid="monaco-editor"
      value={value}
      readOnly={options?.readOnly}
      onChange={(event) => onChange?.(event.target.value)}
    />
  ),
}));

vi.mock('@gameguild/emception-ide', () => {
  const preset = {
      id: 'cpp', label: 'C++', compile: { tool: 'clang', toolchain: 'cpp', args: [], output: 'main.wasm', sourceDetect: { extensions: ['.cpp'], entryPoint: 'main.cpp' } },
      run: { type: 'wasi-terminal', tool: 'wasi-run', args: [] }, features: { canvas: false, terminalInput: true, showTestButton: false }, files: {},
  };
  return {
    PRESETS: Object.fromEntries([
      'cpp-terminal',
      'c-terminal',
      'cpp-sdl3',
      'cpp-raylib',
      'cpp-allegro',
    ].map((key) => [key, { ...preset, id: key }])),
  };
});

afterEach(cleanup);

describe('CodingSurface', () => {
  const authoringByType = {
    'standard-io': codingContractFixture.authoring,
    function: codingContractFixture.functionAuthoring,
  };
  const learnerByType = {
    'standard-io': codingContractFixture.learnerDelivery,
    function: codingContractFixture.functionLearnerDelivery,
  };

  it.each([
    ['standard-io', 'author'],
    ['standard-io', 'author-test'],
    ['standard-io', 'grader'],
    ['function', 'author'],
    ['function', 'author-test'],
    ['function', 'grader'],
  ] as const)('renders the %s profile in %s mode from a full definition', (exerciseType, mode) => {
    render(<CodingSurface mode={mode} definition={authoringByType[exerciseType]} />);
    expect(screen.getByTestId('coding-workbench')).toBeTruthy();
    expect(screen.queryByTestId('public-emception-ide')).toBeNull();
    expect(document.querySelector('[data-coding-mode]')?.getAttribute('data-coding-mode')).toBe(mode);
    expect(document.querySelector('[data-exercise-type]')?.getAttribute('data-exercise-type')).toBe(exerciseType);
    expect(screen.getByLabelText(`${mode} ${exerciseType} profile`)).toBeTruthy();
    if (exerciseType === 'function') expect(screen.getByText('sum(left: integer, right: integer): integer')).toBeTruthy();
  });

  it.each(['standard-io', 'function'] as const)('renders %s learner delivery without serializing private material', (exerciseType) => {
    const { container } = render(<CodingSurface mode="learner" definition={learnerByType[exerciseType]} />);
    expect(container.innerHTML).not.toContain('private-harness');
    expect(container.innerHTML).not.toContain('private-function');
    expect(container.querySelector('[data-exercise-type]')?.getAttribute('data-exercise-type')).toBe(exerciseType);
    expect(screen.getByLabelText(`learner ${exerciseType} profile`).textContent).not.toContain('private');
    expect(screen.queryByText('Render Canvas')).toBeNull();
  });

  it('renders per-case evidence after a learner preview', async () => {
    render(<CodingSurface mode="learner" definition={learnerByType['standard-io']} maxScore={200} />);
    const runButton = await screen.findByRole('button', { name: 'Run public tests' });
    await waitFor(() => expect((runButton as HTMLButtonElement).disabled).toBe(false));
    fireEvent.click(runButton);
    expect((await screen.findByTestId('test-results-panel')).textContent).toContain('1 passed');
    expect(screen.getByTestId('test-case-0').textContent).toContain('public-pass');
    expect(screen.getByTestId('test-case-1').textContent).toContain('public-fail');
    fireEvent.click(screen.getByTestId('test-case-1'));
    expect(screen.getByTestId('test-case-diagnostic-1').textContent).toContain('expected output');
    expect(screen.getByTestId('test-results-panel').textContent).toContain('not an official grade');
  });

  it('uses the complete-test command in grader mode', async () => {
    render(<CodingSurface mode="grader" definition={authoringByType.function} />);
    expect(await screen.findByRole('button', { name: 'Run full tests' })).toBeTruthy();
  });

  it('fails explicitly for an unregistered type', () => {
    expect(() => getCodingSurfaceProfile('visual' as never)).toThrow(/no registered surface/);
  });

  it('requires the complete academic namespace for local workspace storage', () => {
    expect(codingWorkspaceStorageKey({ tenantId: 't', assessmentRevisionId: 'r', actorId: 'a', attemptId: 'x' })).toContain('t.r.a.x');
    expect(() => codingWorkspaceStorageKey({ tenantId: '', assessmentRevisionId: 'r', actorId: 'a', attemptId: 'x' })).toThrow(/complete/);
  });

  it('selects the C workspace from its canonical toolchain key', () => {
    const workspace = workspaceConfigFromDefinition({
      ...codingContractFixture.authoring,
      toolchain: { ...codingContractFixture.authoring.toolchain, key: 'c-emception' },
    });
    expect(workspace.id).toBe('gameguild-coding-c');
  });
});
