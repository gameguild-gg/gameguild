import { spawnSync } from 'child_process';
import fs from 'fs';
import path from 'path';

export type GitHubSource = {
    readonly repository: string;
    readonly version: string;
    readonly destination: string;
    readonly keyFile?: string;
};

export class GitHubSourceError extends Error {
    readonly repository: string;
    readonly version: string;
    readonly stage: 'download' | 'extract';

    constructor(source: GitHubSource, stage: 'download' | 'extract') {
        super(`Failed to ${stage} ${source.repository} @ ${source.version}`);
        this.name = 'GitHubSourceError';
        this.repository = source.repository;
        this.version = source.version;
        this.stage = stage;
    }
}

export function ensureGitHubSource(source: GitHubSource): string {
    const keyFile = source.keyFile ?? 'CMakeLists.txt';
    if (fs.existsSync(path.join(source.destination, keyFile))) {
        console.log(`Using existing source: ${path.basename(source.destination)}`);
        return source.destination;
    }

    fs.rmSync(source.destination, { recursive: true, force: true });
    fs.mkdirSync(source.destination, { recursive: true });
    const tarball = `${source.destination}.tar.gz`;
    const urls = [
        `https://github.com/${source.repository}/archive/refs/tags/${source.version}.tar.gz`,
        `https://codeload.github.com/${source.repository}/tar.gz/refs/tags/${source.version}`,
        `https://github.com/${source.repository}/archive/refs/heads/${source.version}.tar.gz`,
        `https://codeload.github.com/${source.repository}/tar.gz/refs/heads/${source.version}`,
    ] as const;
    const downloaded = urls.some((url) =>
        spawnSync(
            'curl',
            ['-fSL', '--http1.1', '--retry', '8', '--retry-all-errors', '--retry-delay', '2', '-o', tarball, url],
            { stdio: 'ignore' },
        ).status === 0,
    );

    if (!downloaded) {
        throw new GitHubSourceError(source, 'download');
    }

    const extraction = spawnSync(
        'tar',
        ['xzf', tarball, '--strip-components=1', '-C', source.destination],
        { stdio: 'inherit' },
    );
    if (extraction.status !== 0) {
        throw new GitHubSourceError(source, 'extract');
    }

    fs.rmSync(tarball, { force: true });
    return source.destination;
}
