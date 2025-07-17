import { execSync } from 'child_process';
import { existsSync, mkdirSync, readFileSync, unlinkSync, writeFileSync } from 'fs';
import { createHash } from 'crypto';
import { join } from 'path';

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
const API_URL: string = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5001';
const SWAGGER_ENDPOINT: string = `${API_URL}/swagger/v1/swagger.json`;
const isDevelopment: boolean = process.env.NODE_ENV !== 'production';

console.log('🔍 Checking API types...');
console.log(`Environment: NODE_ENV=${process.env.NODE_ENV}`);
console.log(`API URL: ${API_URL}`);
console.log(`Development mode: ${isDevelopment}`);

if (!isDevelopment) {
  console.log('📦 Production mode - skipping API type check');
  process.exit(0);
}

async function fetchSwaggerSpec(): Promise<SwaggerSpec> {
  try {
    console.log(`📡 Fetching API specification from ${SWAGGER_ENDPOINT}`);
    const controller = new AbortController();
    const timeoutId = setTimeout(() => controller.abort(), 10000);

    const response = await fetch(SWAGGER_ENDPOINT, {
      signal: controller.signal,
      headers: { Accept: 'application/json' },
    });

    clearTimeout(timeoutId);

    if (!response.ok) throw new Error(`HTTP ${response.status}: ${response.statusText}`);

    const spec = (await response.json()) as SwaggerSpec;
    console.log('✅ Successfully fetched API specification');
    return spec;
  } catch (error) {
    if (error instanceof Error && error.name === 'AbortError') throw new Error('Request timed out - make sure API is running');
    throw error;
  }
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

async function generateTypes(swaggerSpec: SwaggerSpec): Promise<void> {
  console.log('🔄 Generating TypeScript types...');
  if (!existsSync(generatedDir)) {
    mkdirSync(generatedDir, { recursive: true });
  }
  const tempSwaggerFile = join(generatedDir, 'swagger.json');
  writeFileSync(tempSwaggerFile, JSON.stringify(swaggerSpec, null, 2));
  try {
    const command = `npx @hey-api/openapi-ts -i "${tempSwaggerFile}" -o "${generatedDir}" --client @hey-api/client-next`;
    console.log('Running:', command);
    execSync(command, {
      cwd: projectRoot,
      stdio: 'inherit',
      timeout: 60000,
    });
    console.log('✅ Types generated successfully');
    // Run ESLint fix on the generated folder
    console.log('🔧 Running ESLint fix on generated files...');
    try {
      const lintCommand = `npx eslint "src/lib/api/generated/**/*.{ts,js}" --fix`;
      execSync(lintCommand, {
        cwd: projectRoot,
        stdio: 'inherit',
        timeout: 30000,
      });
      console.log('✅ ESLint fix completed successfully');
    } catch (lintError) {
      const errorMessage = lintError instanceof Error ? lintError.message : String(lintError);
      console.warn('⚠️  ESLint fix failed (non-critical):', errorMessage);
    }
  } catch (error) {
    const errorMessage = error instanceof Error ? error.message : String(error);
    console.error('❌ Failed to generate types:', errorMessage);
    throw error;
  } finally {
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
  } catch (error) {
    const errorMessage = error instanceof Error ? error.message : String(error);
    console.error('❌ Type check failed:', errorMessage);
    if (hasGeneratedTypes()) {
      console.log('⚠️  Using existing types, but they may be outdated');
      console.log('💡 To fix this, ensure API is running: cd apps/api && dotnet run');
      return;
    }
    console.log('💡 Please start the API server: cd apps/api && dotnet run');
    process.exit(1);
  }
}

process.on('SIGINT', () => {
  console.log('\n🛑 Process interrupted');
  process.exit(1);
});

process.on('SIGTERM', () => {
  console.log('\n🛑 Process terminated');
  process.exit(1);
});

main().catch((error) => {
  console.error('💥 Unexpected error:', error);
  process.exit(1);
});
