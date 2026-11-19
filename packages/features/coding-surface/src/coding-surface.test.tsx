import { cleanup, fireEvent, render, screen, waitFor } from '@testing-library/react';
import { useEffect, type ReactNode } from 'react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { codingContractFixtureV2 } from '@game-guild/coding/testing';
import { CodingSurface } from './coding-surface';
import { getCodingSurfaceProfile } from './profile-registry';
import { codingWorkspaceStorageKey } from './storage';
import { workspaceConfigFromDefinition } from './workspace';

vi.mock('@gameguild/emception-ide', () => {
  const preset = {
      id: 'cpp', label: 'C++', compile: { tool: 'clang', args: [], output: 'main.wasm', sourceDetect: { extensions: ['.cpp'], entryPoint: 'main.cpp' } },
      run: { type: 'wasi-terminal', tool: 'wasi-run', args: [] }, features: { canvas: false, terminalInput: true, showTestButton: false }, files: {},
  };
  const controller = {
    api: {
      workspace: { readFile: vi.fn(), writeFile: vi.fn(), deleteFile: vi.fn() },
      runTests: vi.fn(async () => ({
        passed: 1,
        failed: 1,
        totalDurationMs: 3,
        cases: [
          { name: 'public-pass', passed: true, durationMs: 1 },
          { name: 'public-fail', passed: false, durationMs: 2, diagnostic: 'expected output' },
        ],
      })),
    },
    getFiles: vi.fn(async () => []),
    syncWorkspace: vi.fn(async () => undefined),
    setFilesReadOnly: vi.fn(),
  };
  return {
    Ide: ({ title, enableTerminal, onReady, extensions = [] }: {
      title?: string;
      enableTerminal?: boolean;
      onReady?: (value: typeof controller) => void;
      extensions?: readonly { id: string; toolbarEnd?: () => unknown; bottomPanel?: () => unknown }[];
    }) => {
      useEffect(() => { onReady?.(controller); }, [onReady]);
      return (
        <div data-testid="public-emception-ide" data-terminal={String(enableTerminal)}>
          {title}
          {extensions.map((extension) => (
            <div key={extension.id}>
              {extension.toolbarEnd?.() as ReactNode}
              {extension.bottomPanel?.() as ReactNode}
            </div>
          ))}
        </div>
      );
    },
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
    'standard-io': codingContractFixtureV2.authoring,
    function: codingContractFixtureV2.functionAuthoring,
  };
  const learnerByType = {
    'standard-io': codingContractFixtureV2.learnerDelivery,
    function: codingContractFixtureV2.functionLearnerDelivery,
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
    expect(screen.getByTestId('public-emception-ide')).toBeTruthy();
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
    expect(screen.getByTestId('public-emception-ide').getAttribute('data-terminal')).toBe(String(exerciseType === 'standard-io'));
    expect(screen.getByLabelText(`learner ${exerciseType} profile`).textContent).not.toContain('private');
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
      ...codingContractFixtureV2.authoring,
      toolchain: { ...codingContractFixtureV2.authoring.toolchain, key: 'c-emception' },
    });
    expect(workspace.id).toBe('gameguild-coding-c');
  });
});
