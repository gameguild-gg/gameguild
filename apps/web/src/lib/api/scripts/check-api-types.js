import { execSync } from 'child_process';
import { existsSync, mkdirSync, readFileSync, unlinkSync, writeFileSync } from 'fs';
import { createHash } from 'crypto';
import { join } from 'path';

// Dynamic import for node-fetch in ESM
const fetch = (...args) => import('node-fetch').then(({ default: fetch }) => fetch(...args));

const projectRoot = process.cwd();
const generatedDir = join(projectRoot, 'src', 'lib', 'api', 'generated');
const metadataFile = join(generatedDir, '.api-metadata.json');

// Configuration
const API_URL = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5001';
const SWAGGER_ENDPOINT = `${API_URL}/swagger/v1/swagger.json`;
const isDevelopment = process.env.NODE_ENV !== 'production';

console.log('🔍 Checking API types...');
console.log(`Environment: NODE_ENV=${process.env.NODE_ENV}`);
console.log(`API URL: ${API_URL}`);
console.log(`Development mode: ${isDevelopment}`);

if (!isDevelopment) {
  console.log('📦 Production mode - skipping API type check');
  process.exit(0);
}

async function fetchSwaggerSpec() {
  try {
    console.log(`📡 Fetching API specification from ${SWAGGER_ENDPOINT}`);
    const controller = new AbortController();
    const timeoutId = setTimeout(() => controller.abort(), 10000);

    const response = await fetch(SWAGGER_ENDPOINT, {
      signal: controller.signal,
      headers: { Accept: 'application/json' },
    });

    clearTimeout(timeoutId);

    if (!response.ok) {
      throw new Error(`HTTP ${response.status}: ${response.statusText}`);
    }

    const spec = await response.json();
    console.log('✅ Successfully fetched API specification');
    return spec;
  } catch (error) {
    if (error.name === 'AbortError') {
      throw new Error('Request timed out - make sure API is running');
    }
    throw error;
  }
}

function calculateHash(content) {
  return createHash('sha256')
    .update(JSON.stringify(content, null, 0))
    .digest('hex');
}

function loadMetadata() {
  if (!existsSync(metadataFile)) {
    return { hash: null, timestamp: null };
  }
  try {
    const metadata = JSON.parse(readFileSync(metadataFile, 'utf8'));
    return metadata;
  } catch {
    console.log('⚠️  Invalid metadata file, will regenerate');
    return { hash: null, timestamp: null };
  }
}

function saveMetadata(hash, apiVersion) {
  if (!existsSync(generatedDir)) {
    mkdirSync(generatedDir, { recursive: true });
  }
  const metadata = {
    hash,
    timestamp: new Date().toISOString(),
    apiUrl: API_URL,
    apiVersion: apiVersion || 'unknown',
    generator: '@hey-api/openapi-ts',
  };
  writeFileSync(metadataFile, JSON.stringify(metadata, null, 2));
}

function hasGeneratedTypes() {
  const typesFile = join(generatedDir, 'types.gen.ts');
  const servicesFile = join(generatedDir, 'services.gen.ts');
  const schemasFile = join(generatedDir, 'schemas.gen.ts');
  return existsSync(typesFile) || existsSync(servicesFile) || existsSync(schemasFile);
}

async function generateTypes(swaggerSpec) {
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
      console.warn('⚠️  ESLint fix failed (non-critical):', lintError.message);
    }
  } catch (error) {
    console.error('❌ Failed to generate types:', error.message);
    throw error;
  } finally {
    try {
      if (existsSync(tempSwaggerFile)) {
        unlinkSync(tempSwaggerFile);
      }
    } catch (cleanupError) {
      console.warn('⚠️  Failed to clean up temporary file:', cleanupError.message);
    }
  }
}

async function main() {
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
    console.error('❌ Type check failed:', error.message);
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
