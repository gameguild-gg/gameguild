import { readFileSync } from 'fs';
import { resolve } from 'path';
import { defineConfig } from 'tsup';

export default defineConfig({
    entry: {
        index: 'src/lib/orchestrator/index.ts',
        'worker-entry': 'src/lib/orchestrator/worker-entry.ts',
    },
    format: ['esm'],
    // DTS generated via separate tsc step (see build:lib script)
    // because rollup-plugin-dts can't resolve .py imports
    dts: false,
    splitting: true,
    sourcemap: true,
    clean: true,
    outDir: 'dist',
    target: 'es2020',
    platform: 'browser',
    esbuildPlugins: [
        {
            name: 'raw-py-loader',
            setup(build) {
                build.onResolve({ filter: /\.py$/ }, (args) => ({
                    path: resolve(args.resolveDir, args.path),
                    namespace: 'raw-py',
                }));
                build.onLoad({ filter: /.*/, namespace: 'raw-py' }, (args) => {
                    const content = readFileSync(args.path, 'utf-8');
                    return {
                        contents: `export default ${JSON.stringify(content)};`,
                        loader: 'js',
                    };
                });
            },
        },
    ],
    external: ['@xterm/xterm'],
});
