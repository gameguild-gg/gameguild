import { cleanup, fireEvent, render, screen, waitFor } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { codingContractFixture } from '@game-guild/coding/testing';
import { CODING_TOOLCHAIN } from '@game-guild/coding';
import { createEmception } from '@gameguild/emception-browser';
import { CodingSurface } from './coding-surface';
import { getCodingSurfaceProfile } from './profile-registry';
import { codingWorkspaceStorageKey } from './storage';
import { workspaceConfigFromDefinition } from './workspace';
import type { CodingEditorController } from './types';

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

vi.mock('./preview-runner', () => ({
  runCodingPreview: vi.fn(async () => ({
    scope: 'public', academic: false, report: await runtime.runTests(),
    score: { score: 100, passed: false, feedback: 'Local evidence' },
  })),
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
    const definition = executableDelivery();
    render(<CodingSurface mode="learner" definition={definition} maxScore={200} />);
    const runButton = await screen.findByRole('button', { name: 'Run public tests' });
    await waitFor(() => expect((runButton as HTMLButtonElement).disabled).toBe(false));
    fireEvent.click(runButton);
    expect((await screen.findByTestId('test-results-panel')).textContent).toContain('1 passed');
    expect(screen.getByTestId('test-case-0').textContent).toContain('public-pass');
    expect(screen.getByTestId('test-case-1').textContent).toContain('public-fail');
    fireEvent.click(screen.getByTestId('test-case-1'));
    expect(screen.getByTestId('test-case-diagnostic-1').textContent).toContain('expected output');
    expect(screen.getByTestId('test-results-panel').textContent).toContain('not an official grade');
    fireEvent.change(screen.getByTestId('monaco-editor'), { target: { value: 'edited after run' } });
    expect(await screen.findByText(/Outdated result/)).toBeTruthy();
  });

  it('makes the editor and submission available without booting a runtime', async () => {
    vi.mocked(createEmception).mockClear();
    const onReady = vi.fn<(editor: CodingEditorController) => void>();
    const onSubmit = vi.fn();
    render(<CodingSurface mode="learner" definition={executableDelivery()} onReady={onReady} onSubmit={onSubmit} />);
    await waitFor(() => expect(onReady).toHaveBeenCalled());
    expect(onReady.mock.calls[0]?.[0]).not.toHaveProperty('api');
    fireEvent.change(screen.getByTestId('monaco-editor'), { target: { value: 'offline edited code' } });
    fireEvent.click(screen.getByRole('button', { name: 'Submit solution' }));
    await waitFor(() => expect(onSubmit).toHaveBeenCalledWith(expect.objectContaining({ files: expect.arrayContaining([expect.objectContaining({ content: 'offline edited code' })]) })));
    expect(createEmception).not.toHaveBeenCalled();
  });

  it('starts a clean workspace when the authenticated context changes', async () => {
    const definition = executableDelivery();
    const { rerender } = render(<CodingSurface mode="learner" contextKey="attempt-a" definition={definition} />);
    fireEvent.change(screen.getByTestId('monaco-editor'), { target: { value: 'attempt-a code' } });
    rerender(<CodingSurface mode="learner" contextKey="attempt-b" definition={definition} />);
    expect(screen.getByDisplayValue(definition.files[0].content)).toBeTruthy();
    expect(screen.queryByDisplayValue('attempt-a code')).toBeNull();
  });

  it('does not persist private author buffers as learner drafts', async () => {
    const key = 'coding-private-author-test';
    window.localStorage.removeItem(key);
    render(<CodingSurface mode="author" definition={authoringByType['standard-io']} workspaceStorageKey={key} />);
    fireEvent.change(screen.getByTestId('monaco-editor'), { target: { value: 'author draft' } });
    expect(window.localStorage.getItem(key)).toBeNull();
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

  it('presents the selected source as Initial file without requiring main in it', async () => {
    const definition = structuredClone(authoringByType['standard-io']);
    const initial = definition.files.find((file) => file.visibility === 'public' && file.encoding === 'text');
    if (!initial) throw new Error('Fixture requires a public text source.');
    initial.path = '/home/user/helpers.cpp';
    initial.content = 'int helper(){return 7;}';
    definition.entryPoint.path = initial.path;
    definition.files.push({ id: 'program-main', path: '/home/user/program.cpp', encoding: 'text', visibility: 'public',
      mutable: true, content: 'int helper(); int main(){return helper() == 7 ? 0 : 1;}' });
    render(<CodingSurface mode="author" definition={definition} />);
    expect(screen.getByText('Initial file: /home/user/helpers.cpp')).toBeTruthy();
    expect(screen.getByRole('tab', { name: 'helpers.cpp' }).getAttribute('aria-selected')).toBe('true');
    expect(screen.getByRole('tab', { name: 'program.cpp' })).toBeTruthy();
    expect(screen.getByTestId('monaco-editor').getAttribute('readonly')).toBeNull();
    expect(await screen.findByDisplayValue('int helper(){return 7;}')).toBeTruthy();
  });
});

function executableDelivery() {
  const definition = structuredClone(codingContractFixture.learnerDelivery);
  definition.environment = { language: 'cpp', runtime: 'wasi' };
  definition.toolchain = { key: 'cpp-emception', ...CODING_TOOLCHAIN };
  definition.limits = { timeLimitMs: 30_000, outputLimitBytes: 2_000_000, maxFileCount: 16 };
  return definition;
}
