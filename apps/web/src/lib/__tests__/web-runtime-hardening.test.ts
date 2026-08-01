import { readFileSync } from 'node:fs';
import { readdir, readFile } from 'node:fs/promises';
import { resolve } from 'node:path';
import { describe, expect, it } from 'vitest';

const repoRoot = resolve(process.cwd(), '../..');

function readRepoFile(relativePath: string): string {
  return readFileSync(resolve(repoRoot, relativePath), 'utf8');
}

async function readSourceFiles(relativePath: string): Promise<Array<{ file: string; source: string }>> {
  const absolutePath = resolve(repoRoot, relativePath);
  const entries = await readdir(absolutePath, { withFileTypes: true });
  const sources = await Promise.all(entries.map(async (entry) => {
    const filePath = resolve(absolutePath, entry.name);
    const repoRelativePath = `${relativePath}/${entry.name}`;

    if (entry.isDirectory()) {
      if (entry.name === 'docs') return [];
      return readSourceFiles(repoRelativePath);
    }

    if (!/\.[cm]?[jt]sx?$/.test(entry.name)) return [];
    return [{ file: repoRelativePath, source: await readFile(filePath, 'utf8') }];
  }));

  return sources.flat();
}

describe('web runtime hardening', () => {
  it('keeps React Compiler enabled while giving the Docker build enough heap', () => {
    const nextConfig = readRepoFile('apps/web/next.config.ts');
    const economyGate = readRepoFile('scripts/ci/verify-economy.sh');

    expect(nextConfig).toContain('reactCompiler: true');
    expect(nextConfig).toContain('outputFileTracingRoot: path.resolve(__dirname, "../..")');
    expect(nextConfig).toContain('process.env.GAMEGUILD_DISABLE_WEBPACK_CACHE === "1"');
    expect(economyGate).toContain('GAMEGUILD_DISABLE_WEBPACK_CACHE=1');
    expect(readRepoFile('apps/web/Dockerfile')).toContain('NODE_OPTIONS=--max-old-space-size=4096');
  });

  it('uses lightweight container readiness and keeps the web health endpoint available', () => {
    const compose = readRepoFile('compose.coolify.yaml');

    expect(compose).toContain("fetch('http://127.0.0.1:3000/manifest.webmanifest')");
    expect(readRepoFile('apps/web/src/app/api/health/route.ts')).toContain("status: 'healthy'");
  });

  it('logs web requests and route handler failures with structured runtime events', () => {
    expect(readRepoFile('apps/web/src/proxy.ts')).toContain('logWebRequest');
    expect(readRepoFile('apps/web/src/app/api/health/route.ts')).toContain('logWebRequest');
    expect(readRepoFile('apps/web/src/app/api/static-viewer/folder/[folderName]/route.ts')).toContain('logWebRequest');
    expect(readRepoFile('apps/web/src/app/api/static-viewer/file/[...path]/route.ts')).toContain('logWebRequest');
  });

  it('does not depend on the internal Next loadable alias', async () => {
    expect(readRepoFile('apps/web/next.config.ts')).not.toContain(
      'next/dist/server/route-modules/app-page/vendored/contexts/loadable',
    );

    const dynamicImports = (await readSourceFiles('apps/web/src/components/block-content-editor')).filter(({ source }) =>
      source.includes('next/dynamic'),
    );

    expect(dynamicImports.map(({ file }) => file)).toEqual([]);
  });
});
