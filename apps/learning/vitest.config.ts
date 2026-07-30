import react from '@vitejs/plugin-react';
import path from 'path';
import { defineConfig } from 'vitest/config';

export default defineConfig({
    plugins: [react()],
    test: {
        globals: true,
        environment: 'jsdom',
        setupFiles: ['./src/test/setup.ts'],
        testTimeout: 15_000,
        include: ['src/**/*.{test,spec}.{js,ts,jsx,tsx}'],
    },
    resolve: {
        alias: {
            '@': path.resolve(__dirname, './src'),
            '@game-guild/ui': path.resolve(__dirname, '../../packages/infrastructure/ui/src'),
            '@game-guild/client/react': path.resolve(
                __dirname,
                '../../packages/infrastructure/client/src/integrations/react/index.ts'
            ),
            '@game-guild/client': path.resolve(
                __dirname,
                '../../packages/infrastructure/client/src/index.ts'
            ),
        },
    },
});
