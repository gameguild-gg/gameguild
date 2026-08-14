import { createRef } from 'react';
import { render, act, fireEvent } from '@testing-library/react';

// Polyfill TextEncoder/TextDecoder for jsdom
if (typeof globalThis.TextEncoder === 'undefined') {
  // eslint-disable-next-line @typescript-eslint/no-require-imports
  const { TextEncoder, TextDecoder } = require('util');
  globalThis.TextEncoder = TextEncoder;
  globalThis.TextDecoder = TextDecoder;
}

// ── Mock heavy deps ────────────────────────────────────────────────────────
jest.mock('@monaco-editor/react', () => {
  const { createElement, useEffect } = require('react');
  return {
    __esModule: true,
    default: ({ onMount }: any) => {
      useEffect(() => {
        if (onMount) {
          onMount(
            { focus: jest.fn(), getModel: jest.fn() },
            { editor: { getModels: jest.fn().mockReturnValue([]) }, Uri: { file: jest.fn() } },
          );
        }
      }, []);
      return createElement('div', { 'data-testid': 'mock-monaco' });
    },
  };
});

jest.mock('@xterm/xterm', () => ({
  Terminal: jest.fn().mockImplementation(() => ({
    writeln: jest.fn(),
    write: jest.fn(),
    clear: jest.fn(),
    onData: jest.fn().mockReturnValue({ dispose: jest.fn() }),
    open: jest.fn(),
  })),
}));

jest.mock('react-resizable-panels', () => {
  const { createElement } = require('react');
  return {
    PanelGroup: ({ children }: any) => createElement('div', null, children),
    Panel: ({ children }: any) => createElement('div', null, children),
    PanelResizeHandle: () => createElement('div', null),
  };
});

jest.mock('./DockGroup', () => {
  const { createElement } = require('react');
  return { __esModule: true, default: () => createElement('div', { 'data-testid': 'mock-dock-group' }) };
});

jest.mock('./FileExplorer', () => {
  const { createElement } = require('react');
  return { __esModule: true, default: () => createElement('div', { 'data-testid': 'mock-file-explorer' }) };
});

jest.mock('./TerminalPanel', () => {
  const { createElement, useEffect } = require('react');
  return {
    __esModule: true,
    default: ({ onBootTerminalReady }: any) => {
      useEffect(() => {
        if (onBootTerminalReady) {
          onBootTerminalReady({
            writeln: jest.fn(),
            write: jest.fn(),
            clear: jest.fn(),
            onData: jest.fn().mockReturnValue({ dispose: jest.fn() }),
          });
        }
      }, []);
      return createElement('div', { 'data-testid': 'mock-terminal-panel' });
    },
  };
});

// ── Import AFTER mocks ─────────────────────────────────────────────────────
import Ide from './Ide';
import type { IdeHandle } from './Ide';
import type { GradingPlan } from './ide-types';

// ── Sample grading plan with 1 public + 1 hidden case ──────────────────────
const SAMPLE_PLAN: GradingPlan = {
  cases: [
    { kind: 'stdio', name: 'stdio_public', stdin: '', expectedStdout: 'hello', weight: 2, hidden: false },
    { kind: 'doctest', name: 'doctest_hidden', sourceFiles: ['/user/test.cpp'], weight: 3, hidden: true },
  ],
  build: {},
};

const PASSING_REPORT = {
  passed: 2,
  failed: 0,
  totalDurationMs: 100,
  cases: [
    { name: 'stdio_public', passed: true, durationMs: 40 },
    { name: 'doctest_hidden', passed: true, durationMs: 60 },
  ],
};

const SINGLE_CASE_REPORT = {
  passed: 1,
  failed: 0,
  totalDurationMs: 40,
  cases: [{ name: 'stdio_public', passed: true, durationMs: 40 }],
};

// ── Tests ──────────────────────────────────────────────────────────────────

describe('Ide runTests button (testPlan/testMode props)', () => {
  it('renders "Run Tests" button when testPlan is provided', async () => {
    const ref = createRef<IdeHandle>();

    await act(async () => {
      render(<Ide ref={ref} testPlan={SAMPLE_PLAN} />);
    });

    const btn = document.querySelector('[data-testid="run-tests-button"]') as HTMLButtonElement | null;
    expect(btn).not.toBeNull();
    expect(btn!.textContent).toContain('Run Tests');
  });

  it('does NOT render "Run Tests" button when testPlan is absent', async () => {
    const ref = createRef<IdeHandle>();

    await act(async () => {
      render(<Ide ref={ref} />);
    });

    const btn = document.querySelector('[data-testid="run-tests-button"]');
    expect(btn).toBeNull();
  });

  it('public mode: filters hidden cases before running', async () => {
    const onTestReport = jest.fn();
    const ref = createRef<IdeHandle>();

    await act(async () => {
      render(
        <Ide
          ref={ref}
          testPlan={SAMPLE_PLAN}
          testMode="public"
          maxScore={100}
          passingScore={60}
          onTestReport={onTestReport}
        />,
      );
    });

    const { stubClient } = require('@gameguild/emception-browser');
    stubClient.run.mockResolvedValue({ exitCode: 0, stdout: 'hello', stderr: '' });

    const btn = document.querySelector('[data-testid="run-tests-button"]') as HTMLButtonElement;
    expect(btn).not.toBeNull();

    await act(async () => {
      fireEvent.click(btn!);
    });

    // Public mode runs ONLY the stdio_public case (doctest_hidden is filtered).
    // Pipeline: 1 clang + 1 wasm-ld + 1 wasi-run = 3 client.run calls.
    const tools = stubClient.run.mock.calls.map((c: string[]) => c[0]);
    expect(tools).toEqual(['clang', 'wasm-ld', 'wasi-run']);

    // Report fires with exactly 1 case (the public stdio case, which passes).
    expect(onTestReport).toHaveBeenCalledTimes(1);
    const report = onTestReport.mock.calls[0][0];
    expect(report.cases).toHaveLength(1);
    expect(report.cases[0].name).toBe('stdio_public');
    expect(report.cases[0].passed).toBe(true);
  });

  it('full mode: runs stdio cases and skips non-stdio cases with a diagnostic', async () => {
    const onTestReport = jest.fn();
    const ref = createRef<IdeHandle>();

    await act(async () => {
      render(
        <Ide
          ref={ref}
          testPlan={SAMPLE_PLAN}
          testMode="full"
          maxScore={100}
          passingScore={60}
          onTestReport={onTestReport}
        />,
      );
    });

    const { stubClient } = require('@gameguild/emception-browser');
    stubClient.run.mockResolvedValue({ exitCode: 0, stdout: 'hello', stderr: '' });

    const btn = document.querySelector('[data-testid="run-tests-button"]') as HTMLButtonElement;

    await act(async () => {
      fireEvent.click(btn!);
    });

    // Only the stdio case triggers wasi-run; doctest is skipped before execution.
    const tools = stubClient.run.mock.calls.map((c: string[]) => c[0]);
    expect(tools.filter((t: string) => t === 'wasi-run')).toHaveLength(1);

    // Report fires with all 2 cases: stdio passes, doctest flagged unsupported.
    expect(onTestReport).toHaveBeenCalledTimes(1);
    const report = onTestReport.mock.calls[0][0];
    expect(report.cases).toHaveLength(2);
    expect(report.cases.map((c: any) => c.name)).toEqual(['stdio_public', 'doctest_hidden']);
    expect(report.cases[0].passed).toBe(true);
    expect(report.cases[1].passed).toBe(false);
    expect(report.cases[1].diagnostic).toMatch(/not yet supported/);
  });

  it('score preview shows weighted computeScore result in TestResultsPanel', async () => {
    const ref = createRef<IdeHandle>();

    await act(async () => {
      render(
        <Ide
          ref={ref}
          testPlan={SAMPLE_PLAN}
          testMode="full"
          maxScore={100}
          passingScore={60}
        />,
      );
    });

    const { stubClient } = require('@gameguild/emception-browser');
    // Make stdio_public pass (expectedStdout='hello'); doctest is unsupported.
    stubClient.run.mockResolvedValue({ exitCode: 0, stdout: 'hello', stderr: '' });

    const btn = document.querySelector('[data-testid="run-tests-button"]') as HTMLButtonElement;

    await act(async () => {
      fireEvent.click(btn!);
    });

    const panel = document.querySelector('[data-testid="test-results-panel"]');
    expect(panel).not.toBeNull();

    // Weighted score: weight 2/(2+3) * 100 = 40 (stdio passes, doctest unsupported)
    expect(panel!.textContent).toContain('Score: 40/100');
    expect(panel!.textContent).toContain('1 passed');
    expect(panel!.textContent).toContain('1 failed');
  });

  it('default testMode is "full" (all cases run when testMode omitted)', async () => {
    const onTestReport = jest.fn();
    const ref = createRef<IdeHandle>();

    await act(async () => {
      render(<Ide ref={ref} testPlan={SAMPLE_PLAN} onTestReport={onTestReport} />);
    });

    const { stubClient } = require('@gameguild/emception-browser');
    stubClient.run.mockResolvedValue({ exitCode: 0, stdout: 'hello', stderr: '' });

    const btn = document.querySelector('[data-testid="run-tests-button"]') as HTMLButtonElement;

    await act(async () => {
      fireEvent.click(btn!);
    });

    // Default is "full" — both cases appear in the report.
    expect(onTestReport).toHaveBeenCalledTimes(1);
    const report = onTestReport.mock.calls[0][0];
    expect(report.cases).toHaveLength(2);
  });

  it('compile error surfaces and button re-enables', async () => {
    const ref = createRef<IdeHandle>();

    await act(async () => {
      render(<Ide ref={ref} testPlan={SAMPLE_PLAN} testMode="full" />);
    });

    const { stubClient } = require('@gameguild/emception-browser');
    // clang fails → handleRunTests throws and surfaces a single-case failure report.
    stubClient.run.mockResolvedValue({ exitCode: 1, stdout: '', stderr: 'boom' });

    const btn = document.querySelector('[data-testid="run-tests-button"]') as HTMLButtonElement;
    expect(btn).not.toBeNull();
    expect(btn.disabled).toBe(false);

    await act(async () => {
      fireEvent.click(btn!);
    });

    // Button re-enables after the error path.
    expect(btn.disabled).toBe(false);
  });
});
