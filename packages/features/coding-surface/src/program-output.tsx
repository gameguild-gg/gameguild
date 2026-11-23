import { useEffect, useRef, useState } from 'react';
import type { Terminal } from '@xterm/xterm';
import '@xterm/xterm/css/xterm.css';
import type { CodingProgramSessionState } from './program-session-types';

export function CodingProgramOutput({
  state,
  interactive,
  onInput,
  onEndInput,
}: {
  readonly state: CodingProgramSessionState;
  readonly interactive: boolean;
  readonly onInput: (text: string) => void;
  readonly onEndInput: () => void;
}) {
  const [channel, setChannel] = useState<'stdout' | 'stderr'>('stdout');
  const [line, setLine] = useState('');
  const [inputEnded, setInputEnded] = useState(false);
  const canInput = interactive && state.status === 'running' && !inputEnded;
  return (
    <section aria-label="Program execution" style={{ minWidth: 0 }}>
      <p role="status">
        Program: {state.status}
        {state.status === 'completed' ? ` (exit ${state.exitCode})` : state.status === 'failed' ? ` (${state.reason})` : ''}
      </p>
      <div role="tablist" aria-label="Program channels">
        <button type="button" role="tab" aria-selected={channel === 'stdout'} onClick={() => setChannel('stdout')}>
          Standard output
        </button>
        <button type="button" role="tab" aria-selected={channel === 'stderr'} onClick={() => setChannel('stderr')}>
          Standard error
        </button>
      </div>
      <CodingTerminal key={`${state.candidateKey}:${channel}`} output={state.output[channel]} channel={channel} />
      {state.output.truncated ? <p role="status">Visible output truncated.</p> : null}
      {interactive ? (
        <form
          onSubmit={(event) => {
            event.preventDefault();
            if (canInput) {
              onInput(`${line}\n`);
              setLine('');
            }
          }}
          style={{ display: 'flex', flexWrap: 'wrap', gap: 8 }}
        >
          <label style={{ flex: '1 1 180px', minWidth: 0 }}>
            Program input
            <input
              aria-label="Interactive input"
              disabled={!canInput}
              value={line}
              onChange={(event) => setLine(event.target.value)}
              style={{ width: '100%', boxSizing: 'border-box' }}
            />
          </label>
          <button type="submit" disabled={!canInput}>
            Send
          </button>
          <button
            type="button"
            disabled={!canInput}
            onClick={() => {
              onEndInput();
              setInputEnded(true);
            }}
          >
            End input
          </button>
        </form>
      ) : null}
    </section>
  );
}

function CodingTerminal({ output, channel }: { readonly output: string; readonly channel: string }) {
  const container = useRef<HTMLDivElement>(null);
  const terminal = useRef<Terminal | null>(null);
  const rendered = useRef('');
  const currentOutput = useRef(output);
  useEffect(() => {
    currentOutput.current = output;
  }, [output]);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let active = true;
    let release: (() => void) | undefined;
    void (async () => {
      const { Terminal: TerminalClass } = await import('@xterm/xterm');
      const { FitAddon } = await import('@xterm/addon-fit');
      if (!active || !container.current) return;
      const fit = new FitAddon();
      const observer = new ResizeObserver(() => {
        if (active) fit.fit();
      });
      const instance = new TerminalClass({
        convertEol: true,
        disableStdin: true,
        screenReaderMode: true,
        scrollback: 1000,
        fontSize: 13,
        theme: { background: '#09090b', foreground: '#f4f4f5' },
      });
      release = () => {
        observer.disconnect();
        terminal.current = null;
        instance.dispose();
      };
      instance.loadAddon(fit);
      instance.open(container.current);
      terminal.current = instance;
      instance.write(currentOutput.current);
      rendered.current = currentOutput.current;
      observer.observe(container.current);
      fit.fit();
    })().catch((cause: unknown) => {
      release?.();
      release = undefined;
      if (active) setError(cause instanceof Error ? cause.message : 'Terminal unavailable.');
    });
    return () => {
      active = false;
      release?.();
    };
  }, []);

  useEffect(() => {
    const instance = terminal.current;
    if (!instance) return;
    if (!output.startsWith(rendered.current)) {
      instance.reset();
      rendered.current = '';
    }
    instance.write(output.slice(rendered.current.length));
    rendered.current = output;
  }, [output]);

  return (
    <>
      {error ? <p role="alert">Terminal unavailable: {error}</p> : null}
      <div
        ref={container}
        aria-label={`Program ${channel}`}
        data-testid={`program-${channel}`}
        style={{ height: 160, minWidth: 0, overflow: 'hidden', margin: '8px 0' }}
      />
    </>
  );
}
