#!/usr/bin/env node

import { exec } from 'child_process';
import { createHash } from 'crypto';
import { existsSync, mkdirSync, readFileSync, unlinkSync, writeFileSync } from 'fs';
import { join } from 'path';
import { promisify } from 'util';

const execAsync = promisify(exec);

interface SwaggerInfo {
    version?: string;
    title?: string;
    description?: string;
}

interface SwaggerSpec {
    info?: SwaggerInfo;
    openapi?: string;
    swagger?: string;
    [key: string]: unknown;
}

interface ApiMetadata {
    hash: string | null;
    timestamp: string | null;
    apiUrl?: string;
    apiVersion?: string;
    generator?: string;
}

const projectRoot: string = process.cwd();
const generatedDir: string = join(projectRoot, 'src', 'lib', 'api', 'generated');
const metadataFile: string = join(generatedDir, '.api-metadata.json');

// Configuration
const API_URL: string = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5000';
const SWAGGER_ENDPOINT: string = `${API_URL}/swagger/v1/swagger.json`;
const isDevelopment: boolean = process.env.NODE_ENV !== 'production';

console.log('🔄 Running API generation, formatting, and linting...');
console.log(`Environment: NODE_ENV=${process.env.NODE_ENV}`);
console.log(`API URL: ${API_URL}`);
console.log(`Development mode: ${isDevelopment}`);

if (!isDevelopment) {
    console.log('📦 Production mode - skipping API type check');
    process.exit(0);
}

async function fetchSwaggerSpec(): Promise<SwaggerSpec> {
    console.log(`📡 Fetching API specification from ${SWAGGER_ENDPOINT}`);

    const response = await fetch(SWAGGER_ENDPOINT, {
        headers: { Accept: 'application/json' },
    });

    if (!response.ok) throw new Error(`HTTP ${response.status}: ${response.statusText}`);

    const spec = (await response.json()) as SwaggerSpec;
    console.log('✅ Successfully fetched API specification');
    return spec;
}

function calculateHash(content: SwaggerSpec): string {
    return createHash('sha256')
        .update(JSON.stringify(content, null, 0))
        .digest('hex');
}

function loadMetadata(): ApiMetadata {
    if (!existsSync(metadataFile)) {
        return { hash: null, timestamp: null };
    }
    try {
        const metadata = JSON.parse(readFileSync(metadataFile, 'utf8')) as ApiMetadata;
        return metadata;
    } catch {
        console.log('⚠️  Invalid metadata file, will regenerate');
        return { hash: null, timestamp: null };
    }
}

function saveMetadata(hash: string, apiVersion?: string): void {
    if (!existsSync(generatedDir)) {
        mkdirSync(generatedDir, { recursive: true });
    }
    const metadata: ApiMetadata = {
        hash,
        timestamp: new Date().toISOString(),
        apiUrl: API_URL,
        apiVersion: apiVersion || 'unknown',
        generator: '@hey-api/openapi-ts',
    };
    writeFileSync(metadataFile, JSON.stringify(metadata, null, 2));
}

function hasGeneratedTypes(): boolean {
    const typesFile = join(generatedDir, 'types.gen.ts');
    const servicesFile = join(generatedDir, 'services.gen.ts');
    const schemasFile = join(generatedDir, 'schemas.gen.ts');
    return existsSync(typesFile) || existsSync(servicesFile) || existsSync(schemasFile);
}

// Status tracking for background operation
let statusInterval: NodeJS.Timeout | null = null;
let startTime: number = 0;

function startStatusTracking(operationName: string): void {
    startTime = Date.now();

    statusInterval = setInterval(() => {
        const elapsed = Math.floor((Date.now() - startTime) / 1000);
        const minutes = Math.floor(elapsed / 60);
        const seconds = elapsed % 60;
        const timeStr = minutes > 0 ? `${minutes}m ${seconds}s` : `${seconds}s`;

        console.log(`🔄 ${operationName} still working in background for ${timeStr}...`);
    }, 10000); // Every 10 seconds
}

function stopStatusTracking(): void {
    if (statusInterval) {
        clearInterval(statusInterval);
        statusInterval = null;
    }
}

async function formatGeneratedFiles(): Promise<void> {
    console.log('🎨 Formatting generated files...');
    try {
        // Run Prettier on the generated directory
        const prettierCommand = `npx prettier --write "${generatedDir}/**/*.{ts,json}"`;
        console.log('Running:', prettierCommand);
        await execAsync(prettierCommand, {
            cwd: projectRoot,
        });
        console.log('✅ Generated files formatted successfully');
    } catch (error) {
        const errorMessage = error instanceof Error ? error.message : String(error);
        console.error('❌ Failed to format generated files:', errorMessage);
        // Don't throw here - formatting failure shouldn't break the build
        console.warn('⚠️  Continuing without formatting...');
    }
}

async function lintFixGeneratedFiles(): Promise<void> {
    console.log('🔧 Linting and fixing generated files...');
    try {
        // Run ESLint with --fix on the generated directory, using a simpler config
        const eslintCommand = `npx eslint --fix --config eslint.config.mjs "${generatedDir}/**/*.{ts,json}"`;
        console.log('Running:', eslintCommand);
        await execAsync(eslintCommand, {
            cwd: projectRoot,
        });
        console.log('✅ Generated files linted and fixed successfully');
    } catch (error) {
        const errorMessage = error instanceof Error ? error.message : String(error);
        console.error('❌ Failed to lint generated files:', errorMessage);
        // Don't throw here - linting failure shouldn't break the build
        console.warn('⚠️  Continuing without linting...');
    }
}

async function generateTypes(swaggerSpec: SwaggerSpec): Promise<void> {
    console.log('🔄 Generating TypeScript types...');
    startStatusTracking('Type generation');

    if (!existsSync(generatedDir)) {
        mkdirSync(generatedDir, { recursive: true });
    }
    const tempSwaggerFile = join(generatedDir, 'swagger.json');
    writeFileSync(tempSwaggerFile, JSON.stringify(swaggerSpec, null, 2));
    try {
        const command = `npx @hey-api/openapi-ts -i "${tempSwaggerFile}" -o "${generatedDir}" --client @hey-api/client-next`;
        console.log('Running:', command);
        await execAsync(command, {
            cwd: projectRoot,
        });
        console.log('✅ Types generated successfully');
    } catch (error) {
        const errorMessage = error instanceof Error ? error.message : String(error);
        console.error('❌ Failed to generate types:', errorMessage);
        throw error;
    } finally {
        stopStatusTracking();
        try {
            if (existsSync(tempSwaggerFile)) {
                unlinkSync(tempSwaggerFile);
            }
        } catch (cleanupError) {
            const errorMessage = cleanupError instanceof Error ? cleanupError.message : String(cleanupError);
            console.warn('⚠️  Failed to clean up temporary file:', errorMessage);
        }
    }
}

async function main(): Promise<void> {
    try {
        console.log('🚀 Starting API types check in background...');

        const currentSpec = await fetchSwaggerSpec();
        const currentHash = calculateHash(currentSpec);
        const metadata = loadMetadata();
        const needsRegeneration = !hasGeneratedTypes() || metadata.hash !== currentHash;

        if (needsRegeneration) {
            if (metadata.hash === null) {
                console.log('🆕 No previous API types found, generating...');
            } else {
                console.log('🔄 API changes detected, regenerating types...');
            }
            await generateTypes(currentSpec);
            saveMetadata(currentHash, currentSpec.info?.version);
            console.log('✅ API types are up to date');
        } else {
            console.log('✅ API types are already up to date');
        }

        // Always format and lint the generated files
        await formatGeneratedFiles();
        await lintFixGeneratedFiles();

        console.log('🎉 API generation and formatting completed');
    } catch (error) {
        stopStatusTracking();
        const errorMessage = error instanceof Error ? error.message : String(error);
        console.error('❌ Type check failed:', errorMessage);
        if (hasGeneratedTypes()) {
            console.log('⚠️  Using existing types, but they may be outdated');
            console.log('💡 To fix this, ensure API is running: cd apps/api && dotnet run');
            // Still try to format and lint existing files
            await formatGeneratedFiles();
            await lintFixGeneratedFiles();
            return;
        }
        console.log('💡 Please start the API server: cd apps/api && dotnet run');
        // Don't exit with error code to avoid breaking builds
        process.exit(0);
    }
}

// Handle process termination
process.on('SIGINT', () => {
    console.log('\n🛑 Process interrupted');
    stopStatusTracking();
    process.exit(0);
});

process.on('SIGTERM', () => {
    console.log('\n🛑 Process terminated');
    stopStatusTracking();
    process.exit(0);
});

// Run main function
main().catch((error) => {
    console.error('💥 Unexpected error:', error);
    stopStatusTracking();
    // Always exit with success to not break builds
    process.exit(0);
}); 