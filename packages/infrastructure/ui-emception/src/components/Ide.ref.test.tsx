import { createRef } from 'react';
import { render, act } from '@testing-library/react';

// Polyfill TextEncoder/TextDecoder for jsdom (not available by default)
if (typeof globalThis.TextEncoder === 'undefined') {
  // eslint-disable-next-line @typescript-eslint/no-require-imports
  const { TextEncoder, TextDecoder } = require('util');
  globalThis.TextEncoder = TextEncoder;
  globalThis.TextDecoder = TextDecoder;
}

// ── Mock heavy deps ────────────────────────────────────────────────────────
// @gameguild/emception-browser is auto-mocked via src/__mocks__/@gameguild/emception-browser.ts

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

// ── Import Ide AFTER mocks ──────────────────────────────────────────────────
import Ide from './Ide';
import type { IdeHandle } from './Ide';

// ── Tests ──────────────────────────────────────────────────────────────────

describe('Ide ref / imperative handle', () => {
  it('ref.current exposes all 5 handle methods', async () => {
    const ref = createRef<IdeHandle>();

    await act(async () => {
      render(<Ide ref={ref} />);
    });

    // After render + boot, the ref should be populated
    expect(ref.current).not.toBeNull();
    expect(typeof ref.current!.runTests).toBe('function');
    expect(typeof ref.current!.compileAndRun).toBe('function');
    expect(typeof ref.current!.getFiles).toBe('function');
    expect(typeof ref.current!.setFiles).toBe('function');
    expect(typeof ref.current!.reset).toBe('function');
  });

  it('setFiles + getFiles round-trips file content', async () => {
    const ref = createRef<IdeHandle>();

    await act(async () => {
      render(<Ide ref={ref} />);
    });

    await act(async () => {
      await ref.current!.setFiles([
        { path: '/home/user/main.c', content: 'int x;' },
      ]);
    });

    const files = await ref.current!.getFiles();
    const mainC = files.find((f) => f.path === '/home/user/main.c');
    expect(mainC).toEqual({ path: '/home/user/main.c', content: 'int x;' });
  });

  it('runTests rejects cleanly when worker not booted', async () => {
    const { bootInWorker } = require('@gameguild/emception-browser');
    bootInWorker.mockReturnValue(new Promise(() => {}));

    const ref = createRef<IdeHandle>();

    await act(async () => {
      render(<Ide ref={ref} />);
    });

    expect(ref.current).not.toBeNull();
    await expect(ref.current!.runTests({ cases: [] })).rejects.toThrow('Worker not booted yet');
  });
});
