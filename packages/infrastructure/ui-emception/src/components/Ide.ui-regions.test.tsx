import { render, act, screen, within, fireEvent } from '@testing-library/react';

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
});
