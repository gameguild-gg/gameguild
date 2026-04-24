/**
 * `emception doctor` — environment diagnostics.
 *
 * Phase 1 sketch: detects whether the host environment can run the toolchain.
 * Browser-side checks (SAB / COOP / COEP / Worker availability) live in
 * `@emception/browser/coi` and are surfaced from the doctor command when the
 * CLI is run via a browser harness; here in the Node CLI we focus on the
 * Node-side prerequisites (Phase 7).
 */

import { promises as fsp } from 'node:fs';
import { createRequire } from 'node:module';
import * as os from 'node:os';
import * as path from 'node:path';

export interface DoctorCheck {
    name: string;
    ok: boolean;
    detail?: string;
}

export interface DoctorReport {
    runtime: 'node' | 'browser' | 'unknown';
    nodeVersion?: string;
    checks: DoctorCheck[];
    ok: boolean;
}

export interface DoctorOptions {
    /**
     * Optional filesystem path the caller intends to use as the
     * `FsWorkspaceManager` root. When provided, doctor verifies it's
     * read+write+createDir-able. When omitted, a tmpdir probe is used.
     */
    workspaceRoot?: string;
}

function detectRuntime(): DoctorReport['runtime'] {
    if (typeof process !== 'undefined' && process.versions?.node) return 'node';
    // @ts-expect-error -- intentional runtime probe
    if (typeof window !== 'undefined' || typeof self !== 'undefined') return 'browser';
    return 'unknown';
}

function checkNodeVersion(): DoctorCheck {
    const major = Number.parseInt(process.versions.node.split('.')[0] ?? '0', 10);
    return {
        name: 'node-version',
        ok: major >= 20,
        detail: `node ${process.versions.node} (need >= 20)`,
    };
}

function checkFetch(): DoctorCheck {
    return {
        name: 'fetch',
        ok: typeof fetch === 'function',
        detail: typeof fetch === 'function' ? 'global fetch available' : 'global fetch missing — upgrade Node or polyfill',
    };
}

function checkWorkerThreads(): DoctorCheck {
    try {
        // require resolution avoids importing the module synchronously.
        const req = createRequire(import.meta.url);
        req.resolve('node:worker_threads');
        return { name: 'worker-threads', ok: true, detail: 'node:worker_threads resolvable' };
    } catch (err) {
        return { name: 'worker-threads', ok: false, detail: String(err) };
    }
}

function checkSysrootResolvable(): DoctorCheck {
    try {
        const req = createRequire(import.meta.url);
        const resolved = req.resolve('@emception/sysroot/manifest.json');
        return { name: 'sysroot', ok: true, detail: resolved };
    } catch {
        return {
            name: 'sysroot',
            ok: false,
            detail: '@emception/sysroot/manifest.json not resolvable — install @emception/sysroot or pass manifestPath/manifestUrl explicitly',
        };
    }
}

/**
 * Verify the chosen workspace root is read+write+createDir-able. We always
 * write inside a uniquely-named subdirectory so existing data is untouched
 * even when the probe targets a real workspace store.
 */
async function checkWorkspaceWritable(root: string): Promise<DoctorCheck> {
    const probeDir = path.join(root, `.emception-doctor-${process.pid}-${Date.now()}`);
    try {
        await fsp.mkdir(probeDir, { recursive: true });
        const probeFile = path.join(probeDir, 'probe.txt');
        await fsp.writeFile(probeFile, 'ok', 'utf8');
        const back = await fsp.readFile(probeFile, 'utf8');
        if (back !== 'ok') {
            return { name: 'workspace-writable', ok: false, detail: `roundtrip mismatch under ${root}` };
        }
        return { name: 'workspace-writable', ok: true, detail: `${root} (read+write+createDir)` };
    } catch (err) {
        return {
            name: 'workspace-writable',
            ok: false,
            detail: `${root}: ${(err as Error).message}`,
        };
    } finally {
        await fsp.rm(probeDir, { recursive: true, force: true }).catch(() => undefined);
    }
}

export async function runDoctor(opts: DoctorOptions = {}): Promise<DoctorReport> {
    const runtime = detectRuntime();

    if (runtime !== 'node') {
        return {
            runtime,
            checks: [],
            ok: false,
        };
    }

    const workspaceRoot = opts.workspaceRoot ?? os.tmpdir();
    const checks: DoctorCheck[] = [
        checkNodeVersion(),
        checkFetch(),
        checkWorkerThreads(),
        checkSysrootResolvable(),
        await checkWorkspaceWritable(workspaceRoot),
    ];

    return {
        runtime,
        nodeVersion: process.versions.node,
        checks,
        ok: checks.every((c) => c.ok),
    };
}

export function formatReport(report: DoctorReport): string {
    const lines: string[] = [];
    lines.push(`emception doctor — runtime: ${report.runtime}`);
    if (report.nodeVersion) lines.push(`  node: ${report.nodeVersion}`);
    lines.push('');
    for (const c of report.checks) {
        const mark = c.ok ? 'ok ' : 'FAIL';
        lines.push(`  [${mark}] ${c.name}${c.detail ? ` — ${c.detail}` : ''}`);
    }
    lines.push('');
    lines.push(report.ok ? 'All checks passed.' : 'Some checks failed.');
    return lines.join('\n');
}
