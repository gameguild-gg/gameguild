/**
 * Lazy-loading filesystem from CDN with IndexedDB cache and hash invalidation.
 */

import type { FSManifest, FSStats, IFileSystem, ManifestEntry } from 'emception';

// Re-export the core manifest types under their legacy names so consumers of
// this module (LazyFS callers within @gameguild/emception-browser) keep working.
export type { FSManifest } from 'emception';
export type FileEntry = ManifestEntry;

export class LazyFS implements IFileSystem {
  private manifest: FSManifest;

  /**
   * Optional injected brotli decompressor.
   * When set, used instead of the bundled Emscripten-compiled Brotli WASM
   * module (brotli_wasm.js) which is loaded from the CDN at runtime.
   */
  static customBrotliDecompressor: ((data: Uint8Array) => Uint8Array) | null = null;
  private pendingFetches = new Map<string, Promise<Uint8Array>>();
  private pendingBundles = new Map<string, Promise<void>>();
  private blobUrls = new Map<string, string>();
  /** Track which bundles have been fully extracted to IDB. */
  private loadedBundles = new Set<string>();
  private idb: IDBDatabase | null = null;
  private dbName: string;

  /**
   * Pre-computed directory index: maps each directory path to the set of
   * immediate child names (files and subdirectories). Built once from the
   * manifest during construction. Enables O(1) readdir/exists lookups
   * without scanning all manifest keys.
   */
  private dirIndex = new Map<string, Set<string>>();

  private static readonly P = '[Emception:LazyFS]';
  private static fmtSize(n: number): string {
    if (n < 1024) return `${n}B`;
    if (n < 1024 * 1024) return `${(n / 1024).toFixed(1)}KB`;
    return `${(n / (1024 * 1024)).toFixed(1)}MB`;
  }

  // Cache name is versioned so we can invalidate previously persisted bundle
  // extractions when file integrity logic changes.
  constructor(manifest: FSManifest, dbName = 'lazyfs-cache-v3') {
    this.manifest = manifest;
    this.dbName = dbName;
    this.buildDirIndex();
  }

  /**
   * Build the directory index from manifest file paths.
   * For each file /a/b/c.txt, adds:
   *   dirIndex['/a/b'] = { 'c.txt' }
   *   dirIndex['/a']   = { 'b' }
   *   dirIndex['/']    = { 'a' }
   */
  private buildDirIndex(): void {
    for (const filePath of Object.keys(this.manifest.files)) {
      let dir = filePath.substring(0, filePath.lastIndexOf('/')) || '/';
      const name = filePath.substring(filePath.lastIndexOf('/') + 1);
      if (!name) continue;

      // Add file to its parent directory
      let children = this.dirIndex.get(dir);
      if (!children) {
        children = new Set();
        this.dirIndex.set(dir, children);
      }
      children.add(name);

      // Add intermediate directory entries up to root
      while (dir !== '/') {
        const parent = dir.substring(0, dir.lastIndexOf('/')) || '/';
        const dirName = dir.substring(dir.lastIndexOf('/') + 1);
        let parentChildren = this.dirIndex.get(parent);
        if (!parentChildren) {
          parentChildren = new Set();
          this.dirIndex.set(parent, parentChildren);
        }
        if (parentChildren.has(dirName)) break; // Already indexed ancestors
        parentChildren.add(dirName);
        dir = parent;
      }
    }
  }

  async init(): Promise<void> {
    const t0 = performance.now();
    const { P } = LazyFS;
    console.log(`${P} Initializing (dbName=${this.dbName})...`);

    this.idb = await this.openDatabase();
    console.log(`${P}   IDB opened`);

    await this.invalidateStaleCache();
    console.log(`${P}   Stale cache entries invalidated`);

    const criticalFiles = Object.entries(this.manifest.files)
      .filter(([, entry]) => entry.priority === 'critical')
      .map(([path]) => path);
    if (criticalFiles.length > 0) {
      console.log(`${P}   Preloading ${criticalFiles.length} critical file(s): ${criticalFiles.join(', ')}`);
      await this.preload(criticalFiles);
    }
    console.log(`${P} Init complete in ${(performance.now() - t0).toFixed(1)}ms (manifest: ${Object.keys(this.manifest.files).length} files)`);
  }

  private async invalidateStaleCache(): Promise<void> {
    if (!this.idb) return;
    const tx = this.idb.transaction('files', 'readwrite');
    const store = tx.objectStore('files');
    const request = store.openCursor();
    await new Promise<void>((resolve) => {
      request.onsuccess = () => {
        const cursor = request.result;
        if (!cursor) {
          resolve();
          return;
        }
        const { path, hash } = cursor.value as { path: string; hash: string };
        const manifestEntry = this.manifest.files[path];
        if (!manifestEntry || manifestEntry.hash !== hash) {
          cursor.delete();
        }
        cursor.continue();
      };
      request.onerror = () => resolve();
    });
  }

  private openDatabase(): Promise<IDBDatabase> {
    return new Promise((resolve, reject) => {
      const request = indexedDB.open(this.dbName, 1);
      request.onerror = () => reject(request.error);
      request.onsuccess = () => resolve(request.result);
      request.onupgradeneeded = (event) => {
        const db = (event.target as IDBOpenDBRequest).result;
        if (!db.objectStoreNames.contains('files')) {
          db.createObjectStore('files', { keyPath: 'path' });
        }
      };
    });
  }

  async readFile(path: string): Promise<Uint8Array | null> {
    const { P, fmtSize } = LazyFS;
    const normalized = this.normalizePath(path);
    const entry = this.manifest.files[normalized];
    if (!entry) {
      //console.log(`${P} readFile: ${normalized} — NOT IN MANIFEST`);
      return null;
    }
    if (entry.symlink) {
      const target = this.resolveSymlink(normalized, entry.symlink);
      //console.log(`${P} readFile: ${normalized} — symlink → ${target}`);
      return this.readFile(target);
    }
    const idbCached = await this.idbGet(normalized);
    if (idbCached && idbCached.hash === entry.hash) {
      //console.log(`${P} readFile: ${normalized} — IDB CACHE HIT (${fmtSize(idbCached.data.length)})`);
      return idbCached.data;
    }
    if (this.pendingFetches.has(normalized)) {
      //console.log(`${P} readFile: ${normalized} — COALESCING with pending fetch`);
      return this.pendingFetches.get(normalized)!;
    }
    //console.log(`${P} readFile: ${normalized} — CDN FETCH (expected ${fmtSize(entry.size)})`);
    const t0 = performance.now();
    const fetchPromise = this.fetchFile(normalized, entry);
    this.pendingFetches.set(normalized, fetchPromise);
    try {
      const data = await fetchPromise;
      //console.log(`${P} readFile: ${normalized} — fetched ${fmtSize(data.length)} in ${(performance.now() - t0).toFixed(1)}ms`);
      return data;
    } finally {
      this.pendingFetches.delete(normalized);
    }
  }

  /** Create a blob URL for a file (used for .wasm/.mjs/.js that need URL-based loading). */
  private createBlobUrl(path: string, data: Uint8Array): void {
    if (this.blobUrls.has(path)) return;
    let mime = 'application/octet-stream';
    if (path.endsWith('.wasm')) mime = 'application/wasm';
    else if (path.endsWith('.mjs') || path.endsWith('.js')) mime = 'text/javascript';
    const blob = new Blob([data.buffer as ArrayBuffer], { type: mime });
    this.blobUrls.set(path, URL.createObjectURL(blob));
  }

  private async fetchFile(path: string, entry: FileEntry): Promise<Uint8Array> {
    const { P } = LazyFS;
    if (!entry.bundle) {
      // Every file should be in a bundle. If not, the manifest is stale.
      console.error(`${P}   fetchFile: ${path} has no bundle assignment — regenerate bundles`);
      throw new Error(`File ${path} is not assigned to any bundle. Run build:bundles to fix.`);
    }
    console.log(`${P}   fetchFile: ${path} — loading from bundle "${entry.bundle}"`);
    await this.loadBundle(entry.bundle, path);
    // Files are now in IDB after bundle extraction
    const idbEntry = await this.idbGet(path);
    if (idbEntry) return idbEntry.data;
    throw new Error(`File ${path} not found in IDB after loading bundle ${entry.bundle}`);
  }

  private async loadBundle(bundleName: string, triggeredBy?: string): Promise<void> {
    const { P, fmtSize } = LazyFS;
    const bundle = this.manifest.bundles[bundleName];
    if (!bundle) throw new Error(`Bundle not found: ${bundleName}`);
    if (this.loadedBundles.has(bundleName)) {
      return;
    }
    // Coalesce concurrent requests for the same bundle
    if (this.pendingBundles.has(bundleName)) {
      console.log(`${P}   loadBundle: "${bundleName}" — COALESCING with pending bundle fetch`);
      return this.pendingBundles.get(bundleName)!;
    }
    const promise = this.loadBundleImpl(bundleName, bundle, triggeredBy);
    this.pendingBundles.set(bundleName, promise);
    try {
      await promise;
    } finally {
      this.pendingBundles.delete(bundleName);
    }
  }

  private async loadBundleImpl(bundleName: string, bundle: FSManifest['bundles'][string], triggeredBy?: string): Promise<void> {
    const { P, fmtSize } = LazyFS;
    const t0 = performance.now();
    console.log(
      `${P}   loadBundle: fetching "${bundleName}" (${bundle.files.length} files, expected ${fmtSize(bundle.size)}) — triggered by: ${triggeredBy ?? 'unknown'}`,
    );
    const response = await fetch(bundle.url);
    if (!response.ok) throw new Error(`Failed to fetch bundle ${bundleName}: HTTP ${response.status}`);
    let data = new Uint8Array(await response.arrayBuffer());
    const rawSize = data.length;
    console.log(`${P}   loadBundle: received ${fmtSize(rawSize)}`);

    // Verify bundle hash (on compressed data)
    const hash = await this.computeHash(data);
    if (hash !== bundle.hash) {
      console.error(`${P}   loadBundle: HASH MISMATCH for "${bundleName}": expected ${bundle.hash.slice(0, 12)}..., got ${hash.slice(0, 12)}...`);
      throw new Error(`Bundle hash mismatch for ${bundleName}`);
    }

    // Decompress if the URL ends with .br (brotli-compressed tar)
    if (bundle.url.endsWith('.tar.br') || bundle.url.endsWith('.br')) {
      const tDecomp = performance.now();
      data = new Uint8Array(await this.decompressBrotli(data));
      console.log(`${P}   loadBundle: brotli decompressed ${fmtSize(rawSize)} → ${fmtSize(data.length)} in ${(performance.now() - tDecomp).toFixed(1)}ms`);
    } else if (bundle.url.endsWith('.tar.gz') || bundle.url.endsWith('.gz')) {
      const tDecomp = performance.now();
      data = new Uint8Array(await this.decompressGzip(data));
      console.log(`${P}   loadBundle: gzip decompressed ${fmtSize(rawSize)} → ${fmtSize(data.length)} in ${(performance.now() - tDecomp).toFixed(1)}ms`);
    }

    // Extract tar entries into IDB (no in-memory cache)
    await this.extractTar(data);
    this.loadedBundles.add(bundleName);
    console.log(`${P}   loadBundle: "${bundleName}" extracted ${bundle.files.length} files in ${(performance.now() - t0).toFixed(1)}ms`);
  }

  /**
   * Batch-write multiple entries to IDB in a single transaction.
   * Much faster than one transaction per file for large bundles.
   */
  private async idbPutBatch(entries: Array<{ path: string; data: Uint8Array; hash: string }>): Promise<void> {
    if (!this.idb || entries.length === 0) return;
    return new Promise((resolve, reject) => {
      const tx = this.idb!.transaction('files', 'readwrite');
      const store = tx.objectStore('files');
      for (const entry of entries) {
        store.put({ path: entry.path, data: entry.data, hash: entry.hash });
      }
      tx.oncomplete = () => resolve();
      tx.onerror = () => reject(tx.error);
    });
  }

  private async extractTar(data: Uint8Array): Promise<void> {
    const idbEntries: Array<{ path: string; data: Uint8Array; hash: string }> = [];
    let offset = 0;
    while (offset < data.length) {
      const header = data.slice(offset, offset + 512);
      offset += 512;
      if (header.every((b) => b === 0)) break;
      const name = '/' + this.readTarString(header, 0, 100).replace(/\0+$/, '');
      const size = parseInt(this.readTarString(header, 124, 12), 8) || 0;
      const typeFlag = header[156];
      const content = data.slice(offset, offset + size);
      offset += Math.ceil(size / 512) * 512;
      if (typeFlag === 48 || typeFlag === 0) {
        const fileData = new Uint8Array(content);
        const entry = this.manifest.files[name];
        if (entry?.hash) {
          idbEntries.push({ path: name, data: fileData, hash: entry.hash });
        }
        // Create blob URLs for module files (.wasm, .mjs, .js) so that
        // import() and fetch() work for bundled tool binaries.
        if (name.endsWith('.wasm') || name.endsWith('.mjs') || name.endsWith('.js')) {
          this.createBlobUrl(name, fileData);
        }
      }
    }
    // Batch-write all extracted files to IDB in a single transaction
    await this.idbPutBatch(idbEntries);
  }

  async exists(path: string): Promise<boolean> {
    const normalized = this.normalizePath(path);
    return this.existsSync(normalized);
  }

  /** Synchronous existence check using manifest + dirIndex (O(1)). */
  existsSync(path: string): boolean {
    const normalized = this.normalizePath(path);
    if (normalized in this.manifest.files) return true;
    return this.dirIndex.has(normalized);
  }

  async stat(path: string): Promise<FSStats | null> {
    return this.statSync(this.normalizePath(path));
  }

  /** Synchronous stat from manifest metadata + dirIndex (O(1)). */
  statSync(path: string): FSStats | null {
    const normalized = this.normalizePath(path);
    const entry = this.manifest.files[normalized];
    if (entry) {
      return {
        type: entry.symlink ? 'symlink' : 'file',
        size: entry.size ?? 0,
        mode: entry.executable ? 0o755 : 0o644,
        mtimeNs: BigInt(Date.parse(this.manifest.generated)) * 1_000_000n,
        ...(entry.symlink ? { symlinkTarget: entry.symlink } : {}),
      };
    }
    // Check dirIndex for directory status (O(1))
    if (this.dirIndex.has(normalized)) {
      return {
        type: 'dir',
        size: 0,
        mode: 0o755,
        mtimeNs: BigInt(Date.parse(this.manifest.generated)) * 1_000_000n,
      };
    }
    return null;
  }

  async readdir(path: string): Promise<string[]> {
    return this.readdirSync(this.normalizePath(path));
  }

  /** Synchronous readdir from pre-computed dirIndex (O(1)). */
  readdirSync(path: string): string[] {
    const normalized = this.normalizePath(path);
    const children = this.dirIndex.get(normalized);
    return children ? [...children] : [];
  }

  /**
   * Synchronous file read — always returns null.
   * File data is no longer cached in RAM; use the async readFile() path
   * (via Asyncify hooks) to load from IDB/CDN on demand.
   */
  readFileSync(_path: string): Uint8Array | null {
    return null;
  }

  async preload(paths: string[]): Promise<void> {
    await Promise.all(paths.map((p) => this.readFile(p).catch(() => null)));
  }

  /** Preload a specific bundle by name. Downloads and extracts all files into IDB. */
  async preloadBundle(bundleName: string): Promise<void> {
    return this.loadBundle(bundleName);
  }

  /** Preload all bundles in parallel. */
  async preloadAllBundles(): Promise<void> {
    const bundleNames = Object.keys(this.manifest.bundles);
    if (bundleNames.length === 0) return;
    const { P } = LazyFS;
    console.log(`${P} preloadAllBundles: loading ${bundleNames.length} bundles...`);
    const t0 = performance.now();
    await Promise.all(bundleNames.map((name) => this.loadBundle(name)));
    console.log(`${P} preloadAllBundles: done in ${(performance.now() - t0).toFixed(1)}ms`);
  }

  /**
   * Preload all files under a directory path into IDB.
   * Uses the dirIndex for efficient path enumeration instead of
   * scanning all manifest keys.
   */
  async preloadDir(dirPath: string): Promise<void> {
    const normalized = this.normalizePath(dirPath);
    const files: string[] = [];
    this.collectFilesInDir(normalized, files);
    if (files.length > 0) {
      const { P } = LazyFS;
      console.log(`${P} preloadDir: ${normalized} → ${files.length} files`);
      await this.preload(files);
    }
  }

  /**
   * Recursively collect all file paths under a directory using the dirIndex.
   */
  private collectFilesInDir(dir: string, out: string[]): void {
    const children = this.dirIndex.get(dir);
    if (!children) return;
    for (const name of children) {
      const fullPath = dir === '/' ? `/${name}` : `${dir}/${name}`;
      if (this.manifest.files[fullPath]) {
        out.push(fullPath);
      }
      // Recurse into subdirectories
      if (this.dirIndex.has(fullPath)) {
        this.collectFilesInDir(fullPath, out);
      }
    }
  }

  /** Get the bundle name a file belongs to, or undefined. */
  getBundleForFile(path: string): string | undefined {
    const normalized = this.normalizePath(path);
    const entry = this.manifest.files[normalized];
    return entry?.bundle;
  }

  /** Get all file paths belonging to a bundle. */
  getBundleFilePaths(bundleName: string): string[] {
    const bundle = this.manifest.bundles[bundleName];
    return bundle?.files ?? [];
  }

  /** Async version of getUrl: ensures the file's bundle is loaded first, then returns blob URL. */
  async getUrlAsync(path: string): Promise<string> {
    const normalized = this.normalizePath(path);
    const entry = this.manifest.files[normalized];
    if (!entry) return '';
    if (entry.bundle) {
      await this.loadBundle(entry.bundle);
    }
    return this.getUrl(path);
  }

  async writeFile(): Promise<boolean> {
    return false;
  }
  async deleteFile(): Promise<boolean> {
    return false;
  }
  async mkdir(): Promise<boolean> {
    return false;
  }

  getUrl(path: string): string {
    const normalized = this.normalizePath(path);
    const entry = this.manifest.files[normalized];
    if (!entry) return '';

    // Serve via blob URL for bundled .wasm/.mjs/.js files (created during extraction)
    const existing = this.blobUrls.get(normalized);
    if (existing) return existing;

    return `${this.manifest.baseUrl}${path}`;
  }

  private normalizePath(path: string): string {
    const parts = path.split('/').filter((p) => p && p !== '.');
    const result: string[] = [];
    for (const part of parts) {
      if (part === '..') result.pop();
      else result.push(part);
    }
    return '/' + result.join('/');
  }

  private resolveSymlink(from: string, target: string): string {
    if (target.startsWith('/')) return target;
    const dir = from.substring(0, from.lastIndexOf('/'));
    return this.normalizePath(dir + '/' + target);
  }

  private async idbGet(path: string): Promise<{ data: Uint8Array; hash: string } | null> {
    if (!this.idb) return null;
    return new Promise((resolve) => {
      const tx = this.idb!.transaction('files', 'readonly');
      const request = tx.objectStore('files').get(path);
      request.onsuccess = () => resolve(request.result ?? null);
      request.onerror = () => resolve(null);
    });
  }

  private async idbPut(path: string, data: Uint8Array, hash: string): Promise<void> {
    if (!this.idb) return;
    return new Promise((resolve, reject) => {
      const tx = this.idb!.transaction('files', 'readwrite');
      const request = tx.objectStore('files').put({ path, data, hash });
      request.onsuccess = () => resolve();
      request.onerror = () => reject(request.error);
    });
  }

  private readTarString(buf: Uint8Array, offset: number, len: number): string {
    const slice = buf.slice(offset, offset + len);
    const end = slice.indexOf(0);
    return new TextDecoder().decode(end >= 0 ? slice.slice(0, end) : slice);
  }

  private async computeHash(data: Uint8Array): Promise<string> {
    const hashBuffer = await crypto.subtle.digest('SHA-256', data.buffer as ArrayBuffer);
    return [...new Uint8Array(hashBuffer)].map((b) => b.toString(16).padStart(2, '0')).join('');
  }

  private async decompressGzip(data: Uint8Array): Promise<Uint8Array> {
    const ds = new DecompressionStream('gzip');
    const writer = ds.writable.getWriter();
    writer.write(new Uint8Array(data));
    writer.close();
    const reader = ds.readable.getReader();
    const chunks: Uint8Array[] = [];
    while (true) {
      const { done, value } = await reader.read();
      if (done) break;
      chunks.push(value);
    }
    const totalLength = chunks.reduce((sum, c) => sum + c.length, 0);
    const result = new Uint8Array(totalLength);
    let off = 0;
    for (const chunk of chunks) {
      result.set(new Uint8Array(chunk), off);
      off += chunk.length;
    }
    return new Uint8Array(result);
  }

  private async decompressBrotli(data: Uint8Array): Promise<Uint8Array> {
    const { P, fmtSize } = LazyFS;
    // Try native DecompressionStream('br') first
    try {
      const ds = new DecompressionStream('br' as unknown as CompressionFormat);
      const writer = ds.writable.getWriter();
      writer.write(new Uint8Array(data));
      writer.close();
      const reader = ds.readable.getReader();
      const chunks: Uint8Array[] = [];
      while (true) {
        const { done, value } = await reader.read();
        if (done) break;
        chunks.push(value);
      }
      const totalLength = chunks.reduce((sum, c) => sum + c.length, 0);
      const result = new Uint8Array(totalLength);
      let off = 0;
      for (const chunk of chunks) {
        result.set(new Uint8Array(chunk), off);
        off += chunk.length;
      }
      return result;
    } catch {
      // Native brotli not supported — fall back to injected or bundled WASM
      console.warn(`${P} DecompressionStream("br") not supported, falling back to bundled WASM (${fmtSize(data.length)})`);

      // Use injected decompressor if available (avoids dynamic import issues in Workers)
      if (LazyFS.customBrotliDecompressor) {
        return new Uint8Array(LazyFS.customBrotliDecompressor(data));
      }

      throw new Error(
        `Brotli decompression failed: DecompressionStream("br") is not supported and ` +
        `no customBrotliDecompressor was injected. Use a browser that supports Brotli natively.`,
      );
    }
  }
}
