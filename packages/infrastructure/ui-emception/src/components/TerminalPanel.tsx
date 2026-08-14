import { FitAddon } from '@xterm/addon-fit';
import { Terminal } from '@xterm/xterm';
import '@xterm/xterm/css/xterm.css';
import { useCallback, useEffect, useRef } from 'react';
import type { TerminalTab } from './ide-types';
import { TERMINAL_THEME } from './ide-types';

// ─── TerminalInstance ────────────────────────────────────────────────────────
// Each terminal tab mounts one of these. It creates and owns its xterm.

interface TerminalInstanceProps {
    tabId: string;
    isActive: boolean;
    isBoot: boolean;
    onReady: (tabId: string, term: Terminal) => void;
}

function TerminalInstance({ tabId, isActive, isBoot, onReady }: TerminalInstanceProps) {
    const divRef = useRef<HTMLDivElement>(null);
    const instanceRef = useRef<{ term: Terminal; fit: FitAddon } | null>(null);

    // Mount xterm once
    useEffect(() => {
        if (!divRef.current || instanceRef.current) return;

        const term = new Terminal({
            cursorBlink: true,
            scrollback: 10000,
            theme: TERMINAL_THEME,
            fontFamily: 'Menlo, Monaco, "Courier New", monospace',
            fontSize: 14,
        });
        const fit = new FitAddon();
        term.loadAddon(fit);
        term.open(divRef.current);
        setTimeout(() => {
            try {
                fit.fit();
            } catch { }
        }, 100);

        instanceRef.current = { term, fit };

        if (!isBoot) {
            // Scratch terminal: local echo
            term.writeln('\x1b[36m── local terminal (scratch pad) ──\x1b[0m');
            term.onData((data) => {
                // echo printable chars + handle CR
                for (const ch of data) {
                    const code = ch.charCodeAt(0);
                    if (code === 13) {
                        term.write('\r\n');
                    } else if (code === 127) {
                        term.write('\b \b');
                    } else if (code >= 32) {
                        term.write(ch);
                    }
                }
            });
        }

        onReady(tabId, term);

        return () => {
            term.dispose();
            instanceRef.current = null;
        };
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, []);

    // Fit when becoming active or on window resize
    useEffect(() => {
        if (!isActive) return;
        const doFit = () => {
            try {
                instanceRef.current?.fit.fit();
            } catch { }
        };
        doFit();
        const timer = setTimeout(doFit, 80);
        window.addEventListener('resize', doFit);
        return () => {
            clearTimeout(timer);
            window.removeEventListener('resize', doFit);
        };
    }, [isActive]);

    return (
        <div
            ref={divRef}
            data-terminal-id={tabId}
            style={{
                height: '100%',
                display: isActive ? 'block' : 'none',
                padding: '0.45rem',
                background: '#181825',
                overflow: 'hidden',
                boxSizing: 'border-box',
            }}
        />
    );
}

// ─── TerminalPanel ────────────────────────────────────────────────────────────

interface TerminalPanelProps {
    terminalTabs: TerminalTab[];
    activeTerminalId: string;
    onSetActiveTerminal: (id: string) => void;
    onNewTerminal: () => void;
    onCloseTerminal: (id: string) => void;
    /** Called once when terminal-1 (the boot/system terminal) is created */
    onBootTerminalReady: (term: Terminal) => void;
}

const iconBtnStyle: React.CSSProperties = {
    border: 'none',
    background: 'transparent',
    color: 'inherit',
    cursor: 'pointer',
    fontSize: 'inherit',
    padding: 0,
};

export default function TerminalPanel({
    terminalTabs,
    activeTerminalId,
    onSetActiveTerminal,
    onNewTerminal,
    onCloseTerminal,
    onBootTerminalReady,
}: TerminalPanelProps) {
    const handleReady = useCallback(
        (tabId: string, term: Terminal) => {
            if (tabId === 'terminal-1') {
                onBootTerminalReady(term);
            }
        },
        [onBootTerminalReady],
    );

    return (
        <div style={{ height: '100%', background: '#11111b', display: 'flex', flexDirection: 'column', overflow: 'hidden', borderTop: '1px solid #313244' }}>
            {/* Terminal tab strip + actions */}
            <div
                style={{
                    display: 'flex',
                    alignItems: 'center',
                    justifyContent: 'space-between',
                    gap: '0.1rem',
                    padding: '0.15rem 0.35rem',
                    borderBottom: '1px solid #313244',
                    background: '#181825',
                    overflowX: 'auto',
                    flexShrink: 0,
                }}
            >
                <div style={{ display: 'flex', alignItems: 'center', gap: '0.1rem', overflowX: 'auto' }}>
                    {terminalTabs.map((tab) => {
                        const isActive = tab.id === activeTerminalId;
                        const isBoot = tab.id === 'terminal-1';
                        return (
                            <div
                                key={tab.id}
                                onClick={() => onSetActiveTerminal(tab.id)}
                                style={{
                                    display: 'flex',
                                    alignItems: 'center',
                                    gap: '0.35rem',
                                    padding: '0.2rem 0.5rem',
                                    borderRadius: 3,
                                    background: isActive ? '#313244' : 'transparent',
                                    color: isActive ? '#cdd6f4' : '#9399b2',
                                    fontSize: '0.73rem',
                                    cursor: 'pointer',
                                    border: isActive ? '1px solid #45475a' : '1px solid transparent',
                                    userSelect: 'none',
                                    whiteSpace: 'nowrap',
                                }}
                                title={tab.title}
                            >
                                <span style={{ fontSize: '0.5rem', color: isActive ? (isBoot ? '#a6e3a1' : '#89b4fa') : '#6c7086' }}>●</span>
                                <span>{tab.title}</span>
                                {terminalTabs.length > 1 && (
                                    <button
                                        onClick={(e) => {
                                            e.stopPropagation();
                                            onCloseTerminal(tab.id);
                                        }}
                                        style={{ border: 'none', background: 'transparent', color: '#9399b2', cursor: 'pointer', padding: 0, fontSize: '0.72rem' }}
                                        aria-label={`Close ${tab.title}`}
                                    >
                                        ×
                                    </button>
                                )}
                            </div>
                        );
                    })}
                </div>
                <div style={{ display: 'flex', alignItems: 'center', gap: '0.1rem', padding: '0 0.5rem', color: '#9399b2', fontSize: '0.85rem' }}>
                    <button onClick={onNewTerminal} style={iconBtnStyle} title="New terminal">
                        ＋
                    </button>
                    <button onClick={() => onCloseTerminal(activeTerminalId)} style={iconBtnStyle} title="Kill terminal">
                        🗙
                    </button>
                </div>
            </div>

            {/* Terminal instances — all mounted; only active is visible */}
            <div style={{ flex: 1, overflow: 'hidden', background: '#1e1e2e' }}>
                {terminalTabs.map((tab) => (
                    <TerminalInstance key={tab.id} tabId={tab.id} isActive={tab.id === activeTerminalId} isBoot={tab.id === 'terminal-1'} onReady={handleReady} />
                ))}
            </div>
        </div>
    );
}
