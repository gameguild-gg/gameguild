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

  it('public mode: filters hidden cases before calling runTests', async () => {
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

    // Wire mock to return single-case report (only the public case ran)
    const { wrapWorkerClient } = require('@gameguild/emception-browser');
    const mockApi = wrapWorkerClient();
    mockApi.runTests.mockResolvedValue(SINGLE_CASE_REPORT);

    const btn = document.querySelector('[data-testid="run-tests-button"]') as HTMLButtonElement;
    expect(btn).not.toBeNull();

    await act(async () => {
      fireEvent.click(btn!);
    });

    // runTests should have been called with a plan containing ONLY the public case
    expect(mockApi.runTests).toHaveBeenCalledTimes(1);
    const calledPlan = mockApi.runTests.mock.calls[0][0];
    expect(calledPlan.cases).toHaveLength(1);
    expect(calledPlan.cases[0].name).toBe('stdio_public');
    expect(calledPlan.cases[0].hidden).toBeUndefined(); // hidden stripped

    // onTestReport should fire with the report (from imperative handle)
    expect(onTestReport).toHaveBeenCalledTimes(1);
    expect(onTestReport).toHaveBeenCalledWith(SINGLE_CASE_REPORT);
  });

  it('full mode: runs all cases including hidden', async () => {
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

    const { wrapWorkerClient } = require('@gameguild/emception-browser');
    const mockApi = wrapWorkerClient();
    mockApi.runTests.mockResolvedValue(PASSING_REPORT);

    const btn = document.querySelector('[data-testid="run-tests-button"]') as HTMLButtonElement;

    await act(async () => {
      fireEvent.click(btn!);
    });

    // runTests should have been called with ALL cases (both public + hidden)
    expect(mockApi.runTests).toHaveBeenCalledTimes(1);
    const calledPlan = mockApi.runTests.mock.calls[0][0];
    expect(calledPlan.cases).toHaveLength(2);
    expect(calledPlan.cases.map((c: any) => c.name)).toEqual(['stdio_public', 'doctest_hidden']);
  });

  it('score preview shows weighted computeScore result in TestResultsPanel', async () => {
    const ref = createRef<IdeHandle>();

    // Use a plan where only the first case passes: weight 2/(2+3) * 100 = 40
    const partialReport = {
      passed: 1,
      failed: 1,
      totalDurationMs: 100,
      cases: [
        { name: 'stdio_public', passed: true, durationMs: 40 },
        { name: 'doctest_hidden', passed: false, durationMs: 60, diagnostic: 'test failed' },
      ],
    };

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

    const { wrapWorkerClient } = require('@gameguild/emception-browser');
    const mockApi = wrapWorkerClient();
    mockApi.runTests.mockResolvedValue(partialReport);

    const btn = document.querySelector('[data-testid="run-tests-button"]') as HTMLButtonElement;

    await act(async () => {
      fireEvent.click(btn!);
    });

    // TestResultsPanel should render with score
    const panel = document.querySelector('[data-testid="test-results-panel"]');
    expect(panel).not.toBeNull();

    // Weighted score: weight 2/(2+3) * 100 = 40 (stdio passes, doctest fails)
    // Check the score text appears
    expect(panel!.textContent).toContain('Score: 40/100');
    expect(panel!.textContent).toContain('1 passed');
    expect(panel!.textContent).toContain('1 failed');
  });

  it('default testMode is "full" (all cases run when testMode omitted)', async () => {
    const ref = createRef<IdeHandle>();

    await act(async () => {
      render(<Ide ref={ref} testPlan={SAMPLE_PLAN} />);
    });

    const { wrapWorkerClient } = require('@gameguild/emception-browser');
    const mockApi = wrapWorkerClient();
    mockApi.runTests.mockResolvedValue(PASSING_REPORT);

    const btn = document.querySelector('[data-testid="run-tests-button"]') as HTMLButtonElement;

    await act(async () => {
      fireEvent.click(btn!);
    });

    // Default is "full" — all 2 cases passed
    const calledPlan = mockApi.runTests.mock.calls[0][0];
    expect(calledPlan.cases).toHaveLength(2);
  });

  it('engine error surfaces and button re-enables', async () => {
    const ref = createRef<IdeHandle>();

    await act(async () => {
      render(<Ide ref={ref} testPlan={SAMPLE_PLAN} testMode="full" />);
    });

    const { wrapWorkerClient } = require('@gameguild/emception-browser');
    const mockApi = wrapWorkerClient();
    mockApi.runTests.mockRejectedValue(new Error('Worker not booted yet'));

    const btn = document.querySelector('[data-testid="run-tests-button"]') as HTMLButtonElement;
    expect(btn).not.toBeNull();
    expect(btn.disabled).toBe(false);

    await act(async () => {
      fireEvent.click(btn!);
    });

    // Button should re-enable after error
    expect(btn.disabled).toBe(false);
  });
});
