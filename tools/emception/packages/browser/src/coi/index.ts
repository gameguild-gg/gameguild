/**
 * Cross-Origin Isolation (COI) preflight.
 *
 * SharedArrayBuffer + threaded WebAssembly requires the document to be
 * cross-origin isolated (COOP: same-origin + COEP: require-corp). This module
 * provides a small synchronous probe and an optional async fetch-based
 * verification that hosts can run before booting emception.
 */

export interface COICheck {
    name: string;
    ok: boolean;
    detail?: string;
}

export interface COIReport {
    crossOriginIsolated: boolean;
    sharedArrayBuffer: boolean;
    worker: boolean;
    offscreenCanvas: boolean;
    checks: COICheck[];
    ok: boolean;
}

/**
 * Synchronous COI probe. Safe to run in main thread, worker, or SSR (returns
 * a report with all flags false when DOM globals are missing).
 */
export function probeCOI(): COIReport {
    const g = globalThis as unknown as {
        crossOriginIsolated?: boolean;
        SharedArrayBuffer?: unknown;
        Worker?: unknown;
        OffscreenCanvas?: unknown;
    };

    const crossOriginIsolated = g.crossOriginIsolated === true;
    const sharedArrayBuffer = typeof g.SharedArrayBuffer === 'function';
    const worker = typeof g.Worker === 'function';
    const offscreenCanvas = typeof g.OffscreenCanvas === 'function';

    const checks: COICheck[] = [
        {
            name: 'crossOriginIsolated',
            ok: crossOriginIsolated,
            detail: crossOriginIsolated ? 'document is cross-origin isolated' : 'set COOP: same-origin and COEP: require-corp headers',
        },
        {
            name: 'SharedArrayBuffer',
            ok: sharedArrayBuffer,
            detail: sharedArrayBuffer ? 'available' : 'unavailable — required for threaded wasm',
        },
        {
            name: 'Worker',
            ok: worker,
            detail: worker ? 'available' : 'unavailable — required for the emception runtime',
        },
        {
            name: 'OffscreenCanvas',
            ok: offscreenCanvas,
            detail: offscreenCanvas ? 'available' : 'unavailable — needed for SDL/ImGui rendering off the main thread',
        },
    ];

    return {
        crossOriginIsolated,
        sharedArrayBuffer,
        worker,
        offscreenCanvas,
        checks,
        ok: checks.every((c) => c.ok),
    };
}

/**
 * Async verification: probes a same-origin URL and inspects the response
 * headers to confirm COOP/COEP are actually being served. Useful for catching
 * dev environments where headers are missing but `crossOriginIsolated` is
 * still true thanks to a service worker.
 */
export async function verifyHeaders(url: string = location.href): Promise<COICheck[]> {
    try {
        const res = await fetch(url, { method: 'HEAD', cache: 'no-store' });
        const coop = res.headers.get('cross-origin-opener-policy');
        const coep = res.headers.get('cross-origin-embedder-policy');
        return [
            {
                name: 'COOP-header',
                ok: coop === 'same-origin',
                detail: `Cross-Origin-Opener-Policy: ${coop ?? '(missing)'}`,
            },
            {
                name: 'COEP-header',
                ok: coep === 'require-corp' || coep === 'credentialless',
                detail: `Cross-Origin-Embedder-Policy: ${coep ?? '(missing)'}`,
            },
        ];
    } catch (err) {
        return [{ name: 'headers-fetch', ok: false, detail: String(err) }];
    }
}

export function formatCOIReport(report: COIReport): string {
    const lines: string[] = ['emception COI preflight'];
    lines.push('');
    for (const c of report.checks) {
        const mark = c.ok ? 'ok ' : 'FAIL';
        lines.push(`  [${mark}] ${c.name}${c.detail ? ` — ${c.detail}` : ''}`);
    }
    lines.push('');
    lines.push(report.ok ? 'COI preflight passed.' : 'COI preflight failed — emception will not run until fixed.');
    return lines.join('\n');
}
