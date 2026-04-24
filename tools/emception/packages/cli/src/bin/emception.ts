#!/usr/bin/env node
// @emception/cli bin entry.

import { formatExportResult, runCdnExport } from '../cdn-export.js';
import { formatReport, runDoctor } from '../doctor.js';

const cmd = process.argv[2] ?? 'help';

function parseFlag(name: string): string | undefined {
    const eq = `--${name}=`;
    for (let i = 3; i < process.argv.length; i++) {
        const a = process.argv[i];
        if (a === `--${name}`) return process.argv[i + 1];
        if (a.startsWith(eq)) return a.slice(eq.length);
    }
    return undefined;
}

async function main(): Promise<number> {
    if (cmd === 'doctor') {
        const workspaceRoot = parseFlag('workspace-root');
        const report = await runDoctor({ workspaceRoot });
        console.log(formatReport(report));
        return report.ok ? 0 : 1;
    }
    if (cmd === 'cdn-export') {
        const positional = process.argv[3] && !process.argv[3].startsWith('--') ? process.argv[3] : undefined;
        const toDir = positional ?? parseFlag('to');
        const fromUrl = parseFlag('from');
        if (!toDir) {
            console.error('Usage: emception cdn-export <dir> [--from <manifest-url>]');
            return 2;
        }
        const result = await runCdnExport({
            toDir,
            fromUrl,
            onProgress: ({ asset, index, total, bytes }) => {
                const kb = (bytes / 1024).toFixed(1);
                console.log(`  [${index}/${total}] ${asset} (${kb} KiB)`);
            },
        });
        console.log(formatExportResult(result));
        return 0;
    }
    if (cmd === 'help' || cmd === '--help' || cmd === '-h') {
        console.log('emception CLI');
        console.log('Commands:');
        console.log('  doctor [--workspace-root D]  — check environment for emception prerequisites');
        console.log('  cdn-export <dir> [--from U]  — fetch sysroot manifest+bundles into <dir>');
        console.log('  run                          — Phase 9 (pending)');
        console.log('  test                         — Phase 9 (pending)');
        return 0;
    }
    console.error(`Unknown command: ${cmd}`);
    console.error(`Run 'emception help' for available commands.`);
    return 1;
}

main().then(
    (code) => process.exit(code),
    (err) => {
        console.error('emception: unhandled error:', err);
        process.exit(2);
    },
);
