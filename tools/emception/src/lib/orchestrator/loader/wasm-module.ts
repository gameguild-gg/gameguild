/**
 * WASM module loader with cache. Used by the tool runner to load Emscripten
 * JS glue (and optionally pre-compiled WebAssembly.Module) without re-fetching.
 */

const moduleFactoryCache = new Map<string, (config: Record<string, unknown>) => Promise<unknown>>();

const P = '[Emception:WasmLoader]';

export interface WasmModuleLoaderOptions {
  /** Resolve WASM path to full URL for the JS glue (e.g. .mjs). */
  getGlueUrl: (wasmPath: string) => string;
  /** Optional: custom import for dynamic loading (e.g. for Workers). */
  importFn?: (url: string) => Promise<unknown>;
}

/**
 * Load the Emscripten module factory for a given WASM path. Caches by path.
 * The factory is the default export of the Emscripten-generated .mjs glue.
 */
export async function loadModuleFactory(
  wasmPath: string,
  options: WasmModuleLoaderOptions
): Promise<(config: Record<string, unknown>) => Promise<unknown>> {
  const cached = moduleFactoryCache.get(wasmPath);
  if (cached) {
    console.log(`${P} loadModuleFactory: ${wasmPath} — CACHE HIT`);
    return cached;
  }

  const t0 = performance.now();
  const importFn = options.importFn ?? ((url: string) => import(/* webpackIgnore: true */ url));
  const glueUrl = options.getGlueUrl(wasmPath);
  console.log(`${P} loadModuleFactory: ${wasmPath} — importing glue from ${glueUrl}...`);

  let factory: (config: Record<string, unknown>) => Promise<unknown>;
  try {
    const tImport = performance.now();
    const mod = await importFn(glueUrl);
    console.log(`${P}   Glue JS imported in ${(performance.now() - tImport).toFixed(1)}ms`);
    const fn = (mod as { default?: unknown; createModule?: unknown }).default
      ?? (mod as { createModule?: unknown }).createModule;
    if (typeof fn !== 'function') {
      const keys = Object.keys(mod as object).join(', ') || '(none)';
      console.error(`${P}   Glue at ${glueUrl} did not export a factory. Found keys: ${keys}`);
      throw new Error(
        `WASM glue at ${glueUrl} did not export a factory function. ` +
        `Found keys: ${keys}`
      );
    }
    factory = fn as (config: Record<string, unknown>) => Promise<unknown>;
  } catch (err) {
    console.error(`${P} ❌ Failed to load WASM module factory for ${wasmPath}:`, err);
    throw new Error(
      `Could not load WASM module at ${glueUrl}: ${err instanceof Error ? err.message : String(err)}`
    );
  }

  moduleFactoryCache.set(wasmPath, factory);
  console.log(`${P} loadModuleFactory: ${wasmPath} — ready in ${(performance.now() - t0).toFixed(1)}ms`);
  return factory;
}

/** Clear the module factory cache (e.g. for tests or manifest updates). */
export function clearModuleCache(): void {
  moduleFactoryCache.clear();
}
