import { render, act, screen, within, fireEvent } from '@testing-library/react';
import '@testing-library/jest-dom';
import { createRef } from 'react';

if (typeof globalThis.TextEncoder === 'undefined') {
  // eslint-disable-next-line @typescript-eslint/no-require-imports
  const { TextEncoder, TextDecoder } = require('util');
  globalThis.TextEncoder = TextEncoder;
  globalThis.TextDecoder = TextDecoder;
}

// Path of the file row that DEFAULT_PRESET (= CPP_SDL3_PRESET) always seeds.
const SEED_FILE = '/user/sdl-main.cpp';

// ── Mock heavy deps (keep FileExplorer REAL — that's the SUT) ───────────────
// @gameguild/emception-browser is auto-mocked via src/__mocks__/@gameguild/emception-browser.ts

jest.mock('@monaco-editor/react', () => {
  const { createElement } = require('react');
  return {
    __esModule: true,
    default: () => createElement('div', { 'data-testid': 'mock-monaco' }),
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

jest.mock('./TerminalPanel', () => {
  const { createElement } = require('react');
  // Do NOT invoke onBootTerminalReady — we don't want bootInWorker firing in these tests.
  return { __esModule: true, default: () => createElement('div', { 'data-testid': 'mock-terminal-panel' }) };
});

// Stub shadcn Select + Switch with native elements so jsdom can interact with them.
jest.mock('@game-guild/ui/components/select', () => {
  const React = require('react');
  const flattenItems = (children: any): any[] => {
    const out: any[] = [];
    for (const c of React.Children.toArray(children)) {
      if (!c || typeof c !== 'object' || !('props' in c)) continue;
      if (c.props?.value !== undefined) {
        out.push(c);
      } else if (c.props?.children) {
        out.push(...flattenItems(c.props.children));
      }
    }
    return out;
  };
  return {
    __esModule: true,
    Select: ({ value, onValueChange, children, ...rest }: any) => {
      const items = flattenItems(children);
      return React.createElement(
        'select',
        {
          value,
          onChange: (e: any) => onValueChange?.(e.target.value),
          ...rest,
        },
        items.map((c: any) =>
          React.createElement('option', { key: c.props.value, value: c.props.value }, c.props.children),
        ),
      );
    },
    SelectTrigger: ({ children, ...rest }: any) => React.createElement('span', rest, children),
    SelectValue: () => null,
    SelectContent: ({ children }: any) => React.createElement('span', null, children),
    SelectItem: ({ value, children }: any) => React.createElement('span', { 'data-value': value }, children),
  };
});

jest.mock('@game-guild/ui/components/switch', () => ({
  __esModule: true,
  Switch: ({ checked, onCheckedChange, ...rest }: any) =>
    require('react').createElement('input', {
      type: 'checkbox',
      checked: !!checked,
      onChange: (e: any) => onCheckedChange?.(e.target.checked),
      ...rest,
    }),
}));

import Ide from './Ide';
import type { IdeHandle } from './Ide';
import type { GradingPlan } from './ide-types';

// Smoke test the manual shadcn stubs to catch broken mock wiring early.
describe('shadcn mock smoke', () => {
  it('Select stub renders a <select> with options', () => {
    const { Select, SelectItem } = require('@game-guild/ui/components/select');
    const { container } = render(
      <Select value="a" onValueChange={() => {}}>
        <SelectItem value="a">A</SelectItem>
        <SelectItem value="b">B</SelectItem>
      </Select>,
    );
    expect(container.querySelector('select')).not.toBeNull();
    expect(container.querySelectorAll('option').length).toBe(2);
  });
});

describe('T7 in-IDE authoring regions', () => {
  it('(a) FileExplorer renders visibility + modifiable toggle buttons per file when fileMeta supplied', async () => {
    await act(async () => {
      render(
        <Ide
          fileMeta={{
            [SEED_FILE]: { visibility: 'Public', modifiable: true },
          }}
          onFileMetaChange={() => {}}
        />,
      );
    });

    // DEFAULT_PRESET seeds SEED_FILE — controls attach to that row.
    const visBtn = screen.queryByTestId(`file-visibility-${SEED_FILE}`);
    expect(visBtn).not.toBeNull();
    expect(visBtn!.textContent).toBe('👁');
    const modBtn = screen.queryByTestId(`file-modifiable-${SEED_FILE}`);
    expect(modBtn).not.toBeNull();
    expect(modBtn!.textContent).toBe('✏️');
  });

  it('(a-backcompat) FileExplorer renders NO meta controls when fileMeta prop omitted', async () => {
    await act(async () => {
      render(<Ide />);
    });
    expect(screen.queryByTestId(`file-visibility-${SEED_FILE}`)).toBeNull();
    expect(screen.queryByTestId(`file-modifiable-${SEED_FILE}`)).toBeNull();
  });

  it('(a-wiring) changing visibility fires onFileMetaChange with the new value', async () => {
    const onFileMetaChange = jest.fn();
    await act(async () => {
      render(
        <Ide
          fileMeta={{ [SEED_FILE]: { visibility: 'Public', modifiable: true } }}
          onFileMetaChange={onFileMetaChange}
        />,
      );
    });
    const visBtn = screen.getByTestId(`file-visibility-${SEED_FILE}`);
    await act(async () => {
      fireEvent.click(visBtn);
    });
    expect(onFileMetaChange).toHaveBeenCalledWith(SEED_FILE, { visibility: 'Private' });
  });

  it('(a-wiring-switch) toggling modifiable fires onFileMetaChange with the new value', async () => {
    const onFileMetaChange = jest.fn();
    await act(async () => {
      render(
        <Ide
          fileMeta={{ [SEED_FILE]: { visibility: 'Public', modifiable: true } }}
          onFileMetaChange={onFileMetaChange}
        />,
      );
    });
    const modBtn = screen.getByTestId(`file-modifiable-${SEED_FILE}`);
    await act(async () => {
      fireEvent.click(modBtn);
    });
    expect(onFileMetaChange).toHaveBeenCalledWith(SEED_FILE, { modifiable: false });
  });

  it('(b) testsPanelSlot renders in bottom-panel Tests tab when supplied; hidden otherwise', async () => {
    await act(async () => {
      render(
        <Ide
          testsPanelSlot={
            <div data-testid="slot-content">
              <span>tests editor goes here</span>
            </div>
          }
        />,
      );
    });

    // Default tab is Terminal — neither the slot wrapper nor its content should be in the DOM.
    expect(screen.queryByTestId('tests-panel-slot')).toBeNull();
    expect(screen.queryByTestId('slot-content')).toBeNull();

    // The Tests tab button is only rendered when the slot is supplied.
    const testsTab = screen.getByRole('button', { name: 'Tests' });
    await act(async () => {
      fireEvent.click(testsTab);
    });

    // After switching to the Tests tab, the slot content lives in the bottom panel.
    expect(screen.queryByTestId('tests-panel-slot')).not.toBeNull();
    expect(screen.queryByTestId('slot-content')).not.toBeNull();

    // Switching back to Terminal hides the slot again without unmounting the terminal.
    const terminalTab = screen.getByRole('button', { name: 'Terminal' });
    await act(async () => {
      fireEvent.click(terminalTab);
    });
    expect(screen.queryByTestId('tests-panel-slot')).toBeNull();
    expect(screen.queryByTestId('slot-content')).toBeNull();
  });

  it('(c) preset picker IS visible when presetOptions supplied (even with workspaceConfig)', async () => {
    const onPresetChange = jest.fn();
    const workspaceConfig = {
      id: 'custom-ws',
      label: 'Custom WS',
      compile: { tool: 'emcc', args: [], output: '/tmp/out.js' },
      run: { type: 'wasi-terminal' as const },
      features: {},
      layout: { activeFile: '/user/main.cpp', openTabs: [{ path: '/user/main.cpp', group: 'main' as const }] },
      files: {
        '/user/main.cpp': { encoding: 'text' as const, content: 'int main(){}' },
      },
    };
    await act(async () => {
      render(
        <Ide
          workspaceConfig={workspaceConfig}
          presetOptions={[
            { value: 'cpp-sdl3', label: 'C++ SDL3' },
            { value: 'python', label: 'Python' },
          ]}
          onPresetChange={onPresetChange}
        />,
      );
    });
    const picker = screen.queryByTestId('workspace-picker');
    expect(picker).not.toBeNull();
    // presetOptions supplied → options come from presetOptions, NOT from PRESET_IDS.
    const options = within(picker!).queryAllByRole('option');
    expect(options.map((o) => (o as HTMLOptionElement).value)).toEqual(['cpp-sdl3', 'python']);
  });

  it('(c-backcompat) preset picker hidden when workspaceConfig supplied and NO presetOptions', async () => {
    const workspaceConfig = {
      id: 'custom-ws',
      label: 'Custom WS',
      compile: { tool: 'emcc', args: [], output: '/tmp/out.js' },
      run: { type: 'wasi-terminal' as const },
      features: {},
      layout: { activeFile: '/user/main.cpp', openTabs: [{ path: '/user/main.cpp', group: 'main' as const }] },
      files: {
        '/user/main.cpp': { encoding: 'text' as const, content: 'int main(){}' },
      },
    };
    await act(async () => {
      render(<Ide workspaceConfig={workspaceConfig} />);
    });
    expect(screen.queryByTestId('workspace-picker')).toBeNull();
  });

  it('(c-onPresetChange) onChange fires onPresetChange and switchWorkspace for known preset ids', async () => {
    const onPresetChange = jest.fn();
    await act(async () => {
      render(<Ide presetOptions={[{ value: 'cpp-sdl3', label: 'C++ SDL3' }]} onPresetChange={onPresetChange} />);
    });
    const picker = screen.getByTestId('workspace-picker') as HTMLSelectElement;
    // First preset in PRESETS that's in our list — cpp-sdl3.
    await act(async () => {
      picker.value = 'cpp-sdl3';
      picker.dispatchEvent(new Event('change', { bubbles: true }));
    });
    expect(onPresetChange).toHaveBeenCalledWith('cpp-sdl3');
  });

  it('(image-upload) dropped image is stored as a file and returned by getFiles with base64 encoding', async () => {
    // Given: a synchronous FileReader stub that yields a base64 data-URI.
    const RealFileReader = globalThis.FileReader;
    const b64 = btoa('fake-png-bytes');
    class FakeFileReader {
      result: string | ArrayBuffer | null = null;
      onload: (() => void) | null = null;
      onerror: (() => void) | null = null;
      readAsDataURL(file: File) {
        this.result = `data:${file.type};base64,${b64}`;
        this.onload?.();
      }
    }
    (globalThis as unknown as { FileReader: unknown }).FileReader = FakeFileReader;

    const ref = createRef<IdeHandle>();
    window.localStorage.clear();
    await act(async () => {
      render(<Ide ref={ref} />);
    });

    // When: an image file is dropped onto the explorer.
    const aside = document.querySelector('aside')!;
    const png = new File(['fake-png-bytes'], 'pic.png', { type: 'image/png' });
    await act(async () => {
      fireEvent.drop(aside, { dataTransfer: { types: ['Files'], files: [png] } });
    });

    // Then: getFiles returns it with the data-URI prefix stripped + base64 encoding.
    const files = await ref.current!.getFiles();
    const img = files.find((f) => f.path === '/user/pic.png');
    expect(img).toEqual({ path: '/user/pic.png', content: b64, encoding: 'base64' });

    // And: a same-name drop again gets a -2 collision suffix.
    await act(async () => {
      fireEvent.drop(aside, { dataTransfer: { types: ['Files'], files: [png] } });
    });
    const filesAfterCollision = await ref.current!.getFiles();
    expect(filesAfterCollision.find((f) => f.path === '/user/pic-2.png')).toBeDefined();

    // And: image files are excluded from the localStorage persistence payload.
    const persisted = JSON.parse(
      window.localStorage.getItem('gameguild.emception.workspace.v1') ?? '{}',
    ) as { files?: Record<string, { type?: string }> };
    expect(persisted.files?.['/user/pic.png']).toBeUndefined();

    (globalThis as unknown as { FileReader: unknown }).FileReader = RealFileReader;
  });
});

describe('allowCreateFiles prop chain', () => {
  it('workspace toggle renders through Ide in authoring mode and fires the callback', async () => {
    const onAllowCreateFilesChange = jest.fn();
    await act(async () => {
      render(<Ide allowCreateFiles onAllowCreateFilesChange={onAllowCreateFilesChange} />);
    });
    const toggle = screen.getByTestId('allow-student-create');
    expect(toggle).toHaveAttribute('aria-pressed', 'true');
    await act(async () => {
      fireEvent.click(toggle);
    });
    expect(onAllowCreateFilesChange).toHaveBeenCalledWith(false);
  });

  it('runtime mode (no onAllowCreateFilesChange) renders NO workspace toggle', async () => {
    await act(async () => {
      render(<Ide />);
    });
    expect(screen.queryByTestId('allow-student-create')).toBeNull();
  });

  it('runtime mode with allowCreateFiles=false hides the create row — prompt can never fire', async () => {
    const promptSpy = jest.spyOn(window, 'prompt').mockReturnValue('/user/sneaky.cpp');
    await act(async () => {
      render(<Ide allowCreateFiles={false} />);
    });
    expect(screen.queryByTestId('explorer-new-file')).toBeNull();
    expect(screen.queryByTestId('explorer-upload')).toBeNull();
    expect(promptSpy).not.toHaveBeenCalled();
    promptSpy.mockRestore();
  });

  it('authoring mode keeps createFile live even when allowCreateFiles=false', async () => {
    const promptSpy = jest.spyOn(window, 'prompt').mockReturnValue(null);
    await act(async () => {
      render(<Ide allowCreateFiles={false} onAllowCreateFilesChange={() => {}} />);
    });
    await act(async () => {
      fireEvent.click(screen.getByTestId('explorer-new-file'));
    });
    expect(promptSpy).toHaveBeenCalledTimes(1);
    promptSpy.mockRestore();
  });
});

describe('read-only delete/rename guard', () => {
  it('Rename/Delete buttons disabled for a modifiable:false selected file', async () => {
    await act(async () => {
      render(<Ide fileMeta={{ [SEED_FILE]: { visibility: 'Public', modifiable: false } }} />);
    });
    expect(screen.getByText('Rename')).toBeDisabled();
    expect(screen.getByText('Delete')).toBeDisabled();
  });

  it('delete handler refuses a modifiable:false file even when invoked via the context menu', async () => {
    const confirmSpy = jest.spyOn(window, 'confirm').mockReturnValue(true);
    const ref = createRef<IdeHandle>();
    await act(async () => {
      render(<Ide ref={ref} fileMeta={{ [SEED_FILE]: { visibility: 'Public', modifiable: false } }} />);
    });
    // The ctx-menu entry bypasses footer-button disabling — exercises the handler itself.
    fireEvent.contextMenu(screen.getByTestId(`file-row-${SEED_FILE}`));
    await act(async () => {
      fireEvent.click(screen.getByText('🗑 Delete'));
    });
    expect(confirmSpy).not.toHaveBeenCalled();
    const files = await ref.current!.getFiles();
    expect(files.find((f) => f.path === SEED_FILE)).toBeDefined();
    confirmSpy.mockRestore();
  });

  it('rename handler refuses a modifiable:false file even when invoked via the context menu', async () => {
    const promptSpy = jest.spyOn(window, 'prompt').mockReturnValue('/user/renamed.cpp');
    const ref = createRef<IdeHandle>();
    await act(async () => {
      render(<Ide ref={ref} fileMeta={{ [SEED_FILE]: { visibility: 'Public', modifiable: false } }} />);
    });
    // The ctx-menu entry bypasses footer-button disabling — exercises the handler itself.
    fireEvent.contextMenu(screen.getByTestId(`file-row-${SEED_FILE}`));
    await act(async () => {
      fireEvent.click(screen.getByText('✏ Rename'));
    });
    expect(promptSpy).not.toHaveBeenCalled();
    const files = await ref.current!.getFiles();
    expect(files.find((f) => f.path === SEED_FILE)).toBeDefined();
    expect(files.find((f) => f.path === '/user/renamed.cpp')).toBeUndefined();
    promptSpy.mockRestore();
  });

  it('a modifiable file stays deletable', async () => {
    const confirmSpy = jest.spyOn(window, 'confirm').mockReturnValue(true);
    const ref = createRef<IdeHandle>();
    await act(async () => {
      render(<Ide ref={ref} fileMeta={{ [SEED_FILE]: { visibility: 'Public', modifiable: true } }} />);
    });
    const del = screen.getByText('Delete');
    expect(del).toBeEnabled();
    await act(async () => {
      fireEvent.click(del);
    });
    expect(confirmSpy).toHaveBeenCalledTimes(1);
    const files = await ref.current!.getFiles();
    expect(files.find((f) => f.path === SEED_FILE)).toBeUndefined();
    confirmSpy.mockRestore();
  });
});

describe('external workspaceConfig sync', () => {
  const makeConfig = (id: string, entry: string) => ({
    id,
    label: `WS ${id}`,
    compile: { tool: 'clang', args: [], output: '/home/user/main.wasm' },
    run: { type: 'wasi-terminal' as const },
    features: {},
    layout: { activeFile: entry, openTabs: [{ path: entry, group: 'main' as const }] },
    files: { [entry]: { encoding: 'text' as const, content: 'int main(){}' } },
  });
  const pickerOptions = [
    { value: 'ws-a', label: 'Workspace A' },
    { value: 'ws-b', label: 'Workspace B' },
  ];

  it('applies a workspaceConfig prop whose id differs from the active preset (dropdown follows)', async () => {
    const view = render(<Ide workspaceConfig={makeConfig('ws-a', '/user/main.cpp')} presetOptions={pickerOptions} />);
    expect((screen.getByTestId('workspace-picker') as HTMLSelectElement).value).toBe('ws-a');

    await act(async () => {
      view.rerender(<Ide workspaceConfig={makeConfig('ws-b', '/user/other.c')} presetOptions={pickerOptions} />);
    });

    expect((screen.getByTestId('workspace-picker') as HTMLSelectElement).value).toBe('ws-b');
  });

  it('does not re-apply when the prop echoes back with the same id', async () => {
    const onPresetChange = jest.fn();
    const view = render(
      <Ide workspaceConfig={makeConfig('ws-a', '/user/main.cpp')} presetOptions={pickerOptions} onPresetChange={onPresetChange} />,
    );
    // In-IDE pick of a non-PRESETS id: only onPresetChange fires, active
    // preset stays ws-a (the parent is expected to re-seed with a ws-b config).
    const picker = screen.getByTestId('workspace-picker') as HTMLSelectElement;
    await act(async () => {
      picker.value = 'ws-b';
      picker.dispatchEvent(new Event('change', { bubbles: true }));
    });
    expect(onPresetChange).toHaveBeenCalledWith('ws-b');
    // Same-id echo must not apply anything — internal state is still ws-a.
    await act(async () => {
      view.rerender(<Ide workspaceConfig={makeConfig('ws-a', '/user/main.cpp')} presetOptions={pickerOptions} onPresetChange={onPresetChange} />);
    });
    expect((screen.getByTestId('workspace-picker') as HTMLSelectElement).value).toBe('ws-a');
  });

  it('skips the initial persistence write so a restorable draft survives mount', async () => {
    const setItem = jest.spyOn(Storage.prototype, 'setItem');
    try {
      const view = render(
        <Ide assignmentToken="tok-1" workspaceConfig={makeConfig('ws-a', '/user/main.cpp')} presetOptions={pickerOptions} />,
      );
      // The unedited initial state is not persisted: writing it on mount
      // would clobber a restorable draft before the restore effect reads it.
      expect(setItem).not.toHaveBeenCalledWith(
        'gameguild.emception.workspace.tok-1.ws-a.v2',
        expect.stringContaining('/user/main.cpp'),
      );

      await act(async () => {
        view.rerender(
          <Ide assignmentToken="tok-1" workspaceConfig={makeConfig('ws-b', '/user/other.c')} presetOptions={pickerOptions} />,
        );
      });
      // A real state change (workspace apply) still persists.
      expect(setItem).toHaveBeenCalledWith(
        'gameguild.emception.workspace.tok-1.ws-b.v2',
        expect.stringContaining('/user/other.c'),
      );
    } finally {
      setItem.mockRestore();
      window.localStorage.clear();
    }
  });
});

describe('legacy storage key fallback (read migration)', () => {
  const wsKey = (token: string, ws: string) => `gameguild.emception.workspace.${token}.${ws}.v2`;
  const wsConfig = {
    id: 'ws-a',
    label: 'WS A',
    compile: { tool: 'clang', args: [], output: '/home/user/main.wasm' },
    run: { type: 'wasi-terminal' as const },
    features: {},
    layout: { activeFile: '/user/main.cpp', openTabs: [{ path: '/user/main.cpp', group: 'main' as const }] },
    files: { '/user/main.cpp': { encoding: 'text' as const, content: 'int main(){}' } },
  };
  const persisted = (content: string) => JSON.stringify({
    files: { '/user/main.cpp': { path: '/user/main.cpp', type: 'text', content } },
    selectedPath: '/user/main.cpp',
    expandedDirs: [],
    openTabs: [{ id: 'tab:/user/main.cpp', path: '/user/main.cpp', type: 'text', group: 'main' }],
    activeTabId: 'tab:/user/main.cpp',
  });

  beforeEach(() => {
    window.localStorage.clear();
  });

  afterEach(() => {
    window.localStorage.clear();
  });

  it('restores from the legacy post-colon-suffix key when the new key is absent', async () => {
    window.localStorage.setItem(wsKey('assess-1', 'ws-a'), persisted('legacy-draft'));
    const ref = createRef<IdeHandle>();
    await act(async () => {
      render(<Ide ref={ref} assignmentToken="user-1:assess-1" workspaceConfig={wsConfig} />);
    });

    const files = await ref.current!.getFiles();
    expect(files.find((f) => f.path === '/user/main.cpp')?.content).toBe('legacy-draft');

    // Read migration: the legacy entry is left in place.
    expect(window.localStorage.getItem(wsKey('assess-1', 'ws-a'))).not.toBeNull();
  });

  it('writes restored state back under the NEW key, never the legacy key', async () => {
    window.localStorage.setItem(wsKey('assess-1', 'ws-a'), persisted('legacy-draft'));
    const setItem = jest.spyOn(Storage.prototype, 'setItem');
    try {
      const ref = createRef<IdeHandle>();
      await act(async () => {
        render(<Ide ref={ref} assignmentToken="user-1:assess-1" workspaceConfig={wsConfig} />);
      });

      expect(setItem).toHaveBeenCalledWith(
        wsKey('user-1:assess-1', 'ws-a'),
        expect.stringContaining('legacy-draft'),
      );
      expect(setItem).not.toHaveBeenCalledWith(
        wsKey('assess-1', 'ws-a'),
        expect.anything(),
      );
    } finally {
      setItem.mockRestore();
    }
  });

  it('prefers the new key when both new and legacy entries exist', async () => {
    window.localStorage.setItem(wsKey('user-1:assess-1', 'ws-a'), persisted('new-draft'));
    window.localStorage.setItem(wsKey('assess-1', 'ws-a'), persisted('legacy-draft'));
    const ref = createRef<IdeHandle>();
    await act(async () => {
      render(<Ide ref={ref} assignmentToken="user-1:assess-1" workspaceConfig={wsConfig} />);
    });

    const files = await ref.current!.getFiles();
    expect(files.find((f) => f.path === '/user/main.cpp')?.content).toBe('new-draft');
  });

  it('restore is a no-op without crashing when both keys hold corrupt JSON', async () => {
    window.localStorage.setItem(wsKey('user-1:assess-1', 'ws-a'), '{not json');
    window.localStorage.setItem(wsKey('assess-1', 'ws-a'), 'also not json');
    const ref = createRef<IdeHandle>();
    await act(async () => {
      render(<Ide ref={ref} assignmentToken="user-1:assess-1" workspaceConfig={wsConfig} />);
    });

    // Config-seeded content is untouched by the failed restore.
    const files = await ref.current!.getFiles();
    expect(files.find((f) => f.path === '/user/main.cpp')?.content).toBe('int main(){}');
  });

  it('reads the legacy key only while the new key is absent (no repeated legacy reads after migration write)', async () => {
    window.localStorage.setItem(wsKey('assess-1', 'ws-a'), persisted('legacy-draft'));
    const getItem = jest.spyOn(Storage.prototype, 'getItem');
    try {
      const ref = createRef<IdeHandle>();
      let view: ReturnType<typeof render>;
      await act(async () => {
        view = render(<Ide ref={ref} assignmentToken="user-1:assess-1" workspaceConfig={wsConfig} />);
      });

      expect(getItem.mock.calls.filter(([k]) => k === wsKey('assess-1', 'ws-a'))).toHaveLength(1);
      // The mount persistence effect migrated the draft to the new key.
      expect(window.localStorage.getItem(wsKey('user-1:assess-1', 'ws-a'))).not.toBeNull();

      getItem.mockClear();
      view!.unmount();
      const ref2 = createRef<IdeHandle>();
      await act(async () => {
        render(<Ide ref={ref2} assignmentToken="user-1:assess-1" workspaceConfig={wsConfig} />);
      });
      expect(getItem.mock.calls.filter(([k]) => k === wsKey('assess-1', 'ws-a'))).toHaveLength(0);
    } finally {
      getItem.mockRestore();
    }
  });
});

// ── Bottom tabs: Test Cases + Test Results (student runtime mode) ──────────

// Mixed plan: named/unnamed, empty stdin, RegExp expectedStdout, default
// weight, and a hidden case (stripped in public mode).
const MIXED_PLAN: GradingPlan = {
  cases: [
    { kind: 'stdio', name: 'greeter', stdin: 'world', expectedStdout: 'hello world', weight: 2 },
    { kind: 'stdio', expectedStdout: /^h[aeiou]llo$/ },
    { kind: 'stdio', name: 'secret-case', expectedStdout: 'answer', hidden: true },
  ],
  build: {},
};

describe('bottom tabs: Test Cases + Test Results', () => {
  it('(gate) student mode (testPlan, no slot) renders Test Cases + Test Results tabs, no authoring Tests tab', async () => {
    await act(async () => {
      render(<Ide testPlan={MIXED_PLAN} />);
    });
    expect(screen.getByRole('button', { name: 'Terminal' })).not.toBeNull();
    expect(screen.getByRole('button', { name: 'Test Cases' })).not.toBeNull();
    expect(screen.getByRole('button', { name: 'Test Results' })).not.toBeNull();
    expect(screen.queryByRole('button', { name: 'Tests' })).toBeNull();
  });

  it('(gate) instructor mode (testPlan + testsPanelSlot) keeps only the authoring Tests tab', async () => {
    await act(async () => {
      render(
        <Ide
          testPlan={MIXED_PLAN}
          testsPanelSlot={<div data-testid="slot-content">authoring</div>}
        />,
      );
    });
    expect(screen.getByRole('button', { name: 'Tests' })).not.toBeNull();
    expect(screen.queryByRole('button', { name: 'Test Cases' })).toBeNull();
    expect(screen.queryByRole('button', { name: 'Test Results' })).toBeNull();
  });

  it('(gate) no testPlan renders neither new tab', async () => {
    await act(async () => {
      render(<Ide />);
    });
    expect(screen.queryByRole('button', { name: 'Test Cases' })).toBeNull();
    expect(screen.queryByRole('button', { name: 'Test Results' })).toBeNull();
  });

  it('(table) Test Cases tab renders one read-only row per plan case with fallbacks', async () => {
    await act(async () => {
      render(<Ide testPlan={MIXED_PLAN} />);
    });
    await act(async () => {
      fireEvent.click(screen.getByRole('button', { name: 'Test Cases' }));
    });

    const panel = screen.getByTestId('test-cases-panel');
    // Default testMode is 'full' — all 3 cases shown.
    const rows = within(panel).getAllByTestId('test-case-row');
    expect(rows).toHaveLength(3);

    // Row 0: named, explicit stdin/stdout/weight.
    expect(within(rows[0]).getByText('greeter')).not.toBeNull();
    expect(within(rows[0]).getByText('world')).not.toBeNull();
    expect(within(rows[0]).getByText('hello world')).not.toBeNull();
    expect(within(rows[0]).getByText('2')).not.toBeNull();

    // Row 1: unnamed → '(unnamed)', no stdin → '(empty)', RegExp → String(v), weight default 1.
    expect(within(rows[1]).getByText('(unnamed)')).not.toBeNull();
    expect(within(rows[1]).getByText('(empty)')).not.toBeNull();
    expect(within(rows[1]).getByText(String(/^h[aeiou]llo$/))).not.toBeNull();
    expect(within(rows[1]).getByText('1')).not.toBeNull();
  });

  it('(table) public mode strips hidden cases from the Test Cases table', async () => {
    await act(async () => {
      render(<Ide testPlan={MIXED_PLAN} testMode="public" />);
    });
    await act(async () => {
      fireEvent.click(screen.getByRole('button', { name: 'Test Cases' }));
    });
    const rows = within(screen.getByTestId('test-cases-panel')).getAllByTestId('test-case-row');
    expect(rows).toHaveLength(2);
    expect(screen.queryByText('secret-case')).toBeNull();
  });

  it('(kept-mounted) Test Results slot stays in the DOM with display:none when switching away', async () => {
    await act(async () => {
      render(<Ide testPlan={MIXED_PLAN} />);
    });

    // Switch to Test Results: slot visible, no panel content yet (no run).
    await act(async () => {
      fireEvent.click(screen.getByRole('button', { name: 'Test Results' }));
    });
    const slot = screen.getByTestId('test-results-slot');
    expect(slot.style.display).not.toBe('none');
    expect(screen.queryByTestId('test-results-panel')).toBeNull();

    // Switch back to Terminal: slot unmounts nothing — just hides.
    await act(async () => {
      fireEvent.click(screen.getByRole('button', { name: 'Terminal' }));
    });
    expect(screen.getByTestId('test-results-slot').style.display).toBe('none');
  });
});
