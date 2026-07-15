import { createEmception } from '@gameguild/emception-browser';
import '@gameguild/emception-webcomponent';

const STARTER_SOURCE = `#include <stdio.h>
int main() {
  int x;
  scanf("%d", &x);
  printf("got %d\\n", x * 2);
  return 0;
}
`;

const log = document.getElementById('log') as HTMLPreElement;
const print = (msg: string, cls = '') => {
    const line = document.createElement('div');
    if (cls) line.className = cls;
    line.textContent = msg;
    log.appendChild(line);
    log.scrollTop = log.scrollHeight;
};

const el = document.getElementById('run') as HTMLElement & { api?: unknown };
el.setAttribute('source', STARTER_SOURCE);

for (const name of ['ready', 'exit'] as const) {
    el.addEventListener(`emception-${name}`, (ev) => {
        const detail = (ev as CustomEvent).detail;
        print(`[${name}] ${detail ? JSON.stringify(detail) : ''}`);
    });
}

const manifestUrl = `${import.meta.env.BASE_URL}cdn/manifest.json`;

void (async () => {
    try {
        print(`[boot] loading ${manifestUrl}`);
        const api = await createEmception({ manifestUrl, tty: 'none' });
        el.api = api;
        print('[boot] api attached');
    } catch (err) {
        print(`createEmception failed: ${err instanceof Error ? err.message : String(err)}`, 'err');
    }
})();

