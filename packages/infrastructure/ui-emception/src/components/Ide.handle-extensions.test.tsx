import { createRef } from 'react';
import { render, act } from '@testing-library/react';

if (typeof globalThis.TextEncoder === 'undefined') {
  // eslint-disable-next-line @typescript-eslint/no-require-imports
  const { TextEncoder, TextDecoder } = require('util');
  globalThis.TextEncoder = TextEncoder;
  globalThis.TextDecoder = TextDecoder;
}

// Captured editor instance so tests can assert per-file readOnly behavior.
let capturedEditor: { updateOptions: jest.Mock; focus: jest.Mock } | null = null;
type MockModel = {
  updateOptions: jest.Mock;
  getValue: jest.Mock;
  setValue: jest.Mock;
  dispose: jest.Mock;
  uri: { scheme: string; path: string };
};
const modelsByPath = new Map<string, MockModel>();
(globalThis as unknown as { __emceptionTestModels: Map<string, MockModel> }).__emceptionTestModels = modelsByPath;

jest.mock('@monaco-editor/react', () => {
  const { createElement, useEffect } = require('react');
  return {
    __esModule: true,
    default: ({ onMount }: any) => {
      useEffect(() => {
        if (onMount) {
          const editor = {
            focus: jest.fn(),
            updateOptions: jest.fn(),
            getModel: jest.fn(),
          };
          // eslint-disable-next-line @typescript-eslint/no-explicit-any
          (globalThis as any).__emceptionCapturedEditor = editor;
          const models = (globalThis as any).__emceptionTestModels as Map<string, MockModel>;
          onMount(editor, {
            editor: {
              getModels: jest.fn().mockReturnValue([]),
              getModel: jest.fn().mockImplementation((uri: { path: string } | undefined) =>
                uri?.path ? (models.get(uri.path) ?? null) : null,
              ),
              createModel: jest.fn().mockImplementation((content: string, _lang: string, uri: { path: string } | undefined) => {
                const path = uri?.path ?? '';
                const existing = models.get(path);
                if (existing) return existing;
                const m: MockModel = {
                  getValue: jest.fn().mockReturnValue(content),
                  setValue: jest.fn(),
                  updateOptions: jest.fn(),
                  dispose: jest.fn(),
                  uri: { scheme: 'file', path },
                };
                models.set(path, m);
                return m;
              }),
            },
            Uri: { file: (p: string) => ({ scheme: 'file', path: p }) },
          });
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

jest.mock('./FileExplorer', () => {
  const { createElement } = require('react');
  return {
    __esModule: true,
    default: () => createElement('div', { 'data-testid': 'mock-file-explorer' }),
    tabIcon: () => ({ icon: '📄', color: '#cdd6f4' }),
  };
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

import Ide from './Ide';
import type { IdeHandle } from './Ide';

async function renderIde() {
  const ref = createRef<IdeHandle>();
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  (globalThis as any).__emceptionCapturedEditor = null;
  modelsByPath.clear();
  await act(async () => {
    render(<Ide ref={ref} />);
  });
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  capturedEditor = (globalThis as any).__emceptionCapturedEditor;
  return ref;
}

function findModel(path: string) {
  return modelsByPath.get(path);
}

describe('IdeHandle extensions: addFile / removeFile / setFileMeta / getModifiedFiles', () => {
  it('addFile then getFiles round-trips the new file', async () => {
    const ref = await renderIde();

    await act(async () => {
      await ref.current!.addFile('/user/added.c', 'int main(){return 0;}');
    });

    const files = await ref.current!.getFiles();
    const added = files.find((f) => f.path === '/user/added.c');
    expect(added).toEqual({ path: '/user/added.c', content: 'int main(){return 0;}', encoding: 'text' });
  });

  it('removeFile removes the file from getFiles result', async () => {
    const ref = await renderIde();

    await act(async () => {
      await ref.current!.addFile('/user/temp.c', 'temp');
      await ref.current!.removeFile('/user/temp.c');
    });

    const files = await ref.current!.getFiles();
    expect(files.find((f) => f.path === '/user/temp.c')).toBeUndefined();
  });

  it('setFileMeta marks modifiable=false and per-file readOnly propagates to Monaco model.updateOptions', async () => {
    const ref = await renderIde();

    await act(async () => {
      await ref.current!.setFiles([{ path: '/user/main.cpp', content: 'int main(){}' }]);
    });
    await act(async () => {
      await ref.current!.setFileMeta('/user/main.cpp', { modifiable: false });
    });

    const model = findModel('/user/main.cpp');
    expect(model).toBeDefined();
    const readOnlyTrueCalls = model!.updateOptions.mock.calls.filter(
      (call: any[]) => call[0]?.readOnly === true,
    );
    expect(readOnlyTrueCalls.length).toBeGreaterThan(0);
  });

  it('setFileMeta with visibility:Private does not throw and applies alongside modifiable', async () => {
    const ref = await renderIde();

    await act(async () => {
      await ref.current!.setFiles([{ path: '/user/main.cpp', content: 'x' }]);
    });
    await act(async () => {
      await ref.current!.setFileMeta('/user/main.cpp', { visibility: 'Private', modifiable: false });
    });

    const model = findModel('/user/main.cpp');
    expect(model).toBeDefined();
    expect(
      model!.updateOptions.mock.calls.some((call: any[]) => call[0]?.readOnly === true),
    ).toBe(true);
  });

  it('getModifiedFiles returns ONLY edited + student-created files, not unchanged', async () => {
    const ref = await renderIde();

    await act(async () => {
      await ref.current!.setFiles([
        { path: '/user/A.c', content: 'A-original' },
        { path: '/user/B.c', content: 'B-original' },
        { path: '/user/C.c', content: 'C-original' },
      ]);
    });

    await act(async () => {
      await ref.current!.addFile('/user/A.c', 'A-edited');
      await ref.current!.addFile('/user/D.c', 'D-created');
    });

    const modified = await ref.current!.getModifiedFiles();
    const paths = modified.map((m) => m.path).sort();
    expect(paths).toEqual(['/user/A.c', '/user/D.c']);
    expect(modified.every((m) => m.encoding === 'text')).toBe(true);
    const aEntry = modified.find((m) => m.path === '/user/A.c');
    expect(aEntry?.content).toBe('A-edited');
  });

  it('setFiles replaces the workspace (not merges) and reseeds the snapshot', async () => {
    const ref = await renderIde();

    await act(async () => {
      await ref.current!.setFiles([{ path: '/user/first.c', content: 'first' }]);
    });
    await act(async () => {
      await ref.current!.setFiles([{ path: '/user/second.c', content: 'second' }]);
    });

    const files = await ref.current!.getFiles();
    expect(files.map((f) => f.path)).toEqual(['/user/second.c']);
    const modified = await ref.current!.getModifiedFiles();
    expect(modified).toHaveLength(0);
  });

  it('per-file readOnly: a file with modifiable === false renders Monaco model with readOnly: true', async () => {
    const ref = await renderIde();

    await act(async () => {
      await ref.current!.setFiles([{ path: '/user/locked.c', content: 'locked' }]);
      await ref.current!.setFileMeta('/user/locked.c', { modifiable: false });
    });

    const model = findModel('/user/locked.c');
    expect(model).toBeDefined();
    const readOnlyTrueCalls = model!.updateOptions.mock.calls.filter(
      (call: any[]) => call[0]?.readOnly === true,
    );
    expect(readOnlyTrueCalls.length).toBeGreaterThan(0);

    await act(async () => {
      await ref.current!.setFileMeta('/user/locked.c', { modifiable: true });
    });
    const readOnlyFalseCalls = model!.updateOptions.mock.calls.filter(
      (call: any[]) => call[0]?.readOnly === false,
    );
    expect(readOnlyFalseCalls.length).toBeGreaterThan(0);
  });
});
