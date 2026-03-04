/**
 * Lazy-loading filesystem from CDN with IndexedDB cache and hash invalidation.
 */

import { FSStats, IFileSystem } from './interface';

export interface FSManifest {
  version: number;
  generated: string;
  baseUrl: string;
  files: {
    [path: string]: FileEntry;
  };
  bundles: {
    [name: string]: {
      files: string[];
      url: string;
      size: number;
      hash: string;
    };
  };
}

export interface FileEntry {
  size: number;
  hash: string;
  compressed?: 'br' | 'gz';
  executable?: boolean;
  symlink?: string;
  bundle?: string;
  priority?: 'critical' | 'high' | 'normal' | 'low';
}

export class LazyFS implements IFileSystem {
  private manifest: FSManifest;
  private memCache = new Map<string, Uint8Array>();
  private memCacheSize = 0;
  private readonly MAX_MEM_CACHE_BYTES = 128 * 1024 * 1024;
  private pendingFetches = new Map<string, Promise<Uint8Array>>();
  private idb: IDBDatabase | null = null;
  private dbName: string;

  private static readonly P = '[Emception:LazyFS]';
  private static fmtSize(n: number): string {
    if (n < 1024) return `${n}B`;
    if (n < 1024 * 1024) return `${(n / 1024).toFixed(1)}KB`;
    return `${(n / (1024 * 1024)).toFixed(1)}MB`;
  }

  constructor(manifest: FSManifest, dbName = 'lazyfs-cache') {
    this.manifest = manifest;
    this.dbName = dbName;
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
    const cached = this.memCache.get(normalized);
    if (cached) {
      console.log(`${P} readFile: ${normalized} — MEM CACHE HIT (${fmtSize(cached.length)})`);
      return cached;
    }
    const entry = this.manifest.files[normalized];
    if (!entry) {
      console.log(`${P} readFile: ${normalized} — NOT IN MANIFEST`);
      return null;
    }
    if (entry.symlink) {
      const target = this.resolveSymlink(normalized, entry.symlink);
      console.log(`${P} readFile: ${normalized} — symlink → ${target}`);
      return this.readFile(target);
    }
    const idbCached = await this.idbGet(normalized);
    if (idbCached && idbCached.hash === entry.hash) {
      console.log(`${P} readFile: ${normalized} — IDB CACHE HIT (${fmtSize(idbCached.data.length)})`);
      this.addToMemCache(normalized, idbCached.data);
      return idbCached.data;
    }
    if (this.pendingFetches.has(normalized)) {
      console.log(`${P} readFile: ${normalized} — COALESCING with pending fetch`);
      return this.pendingFetches.get(normalized)!;
    }
    console.log(`${P} readFile: ${normalized} — CDN FETCH (expected ${fmtSize(entry.size)})`);
    const t0 = performance.now();
    const fetchPromise = this.fetchFile(normalized, entry);
    this.pendingFetches.set(normalized, fetchPromise);
    try {
      const data = await fetchPromise;
      console.log(`${P} readFile: ${normalized} — fetched ${fmtSize(data.length)} in ${(performance.now() - t0).toFixed(1)}ms`);
      this.addToMemCache(normalized, data);
      await this.idbPut(normalized, data, entry.hash);
      return data;
    } finally {
      this.pendingFetches.delete(normalized);
    }
  }

  private addToMemCache(path: string, data: Uint8Array): void {
    while (
      this.memCacheSize + data.length > this.MAX_MEM_CACHE_BYTES &&
      this.memCache.size > 0
    ) {
      const oldest = this.memCache.keys().next().value!;
      const oldData = this.memCache.get(oldest)!;
      this.memCacheSize -= oldData.length;
      this.memCache.delete(oldest);
    }
    this.memCache.set(path, data);
    this.memCacheSize += data.length;
  }

  private async fetchFile(path: string, entry: FileEntry): Promise<Uint8Array> {
    const { P, fmtSize } = LazyFS;
    if (entry.bundle) {
      console.log(`${P}   fetchFile: ${path} — loading from bundle "${entry.bundle}"`);
      await this.loadBundle(entry.bundle);
      const bundled = this.memCache.get(path);
      if (bundled) return bundled;
      throw new Error(`File ${path} not found in bundle ${entry.bundle}`);
    }
    const ext = entry.compressed ? '.' + entry.compressed : '';
    const url = `${this.manifest.baseUrl}${path}${ext}`;
    const t0 = performance.now();
    console.log(`${P}   fetchFile: GET ${url}`);
    const response = await fetch(url);
    if (!response.ok) {
      console.error(`${P}   fetchFile: HTTP ${response.status} for ${url}`);
      throw new Error(`Failed to fetch ${path}: ${response.status}`);
    }
    let data: Uint8Array = new Uint8Array(await response.arrayBuffer());
    const rawSize = data.length;
    console.log(`${P}   fetchFile: received ${fmtSize(rawSize)} in ${(performance.now() - t0).toFixed(1)}ms`);

    if (entry.compressed === 'gz') {
      const tDecomp = performance.now();
      data = new Uint8Array(await this.decompressGzip(data));
      console.log(`${P}   fetchFile: gzip decompressed ${fmtSize(rawSize)} → ${fmtSize(data.length)} in ${(performance.now() - tDecomp).toFixed(1)}ms`);
    } else if (entry.compressed === 'br') {
      const tDecomp = performance.now();
      data = await this.decompressBrotli(data);
      console.log(`${P}   fetchFile: brotli decompressed ${fmtSize(rawSize)} → ${fmtSize(data.length)} in ${(performance.now() - tDecomp).toFixed(1)}ms`);
    }
    const tHash = performance.now();
    const hash = await this.computeHash(new Uint8Array(data));
    if (hash !== entry.hash) {
      console.error(`${P}   fetchFile: HASH MISMATCH for ${path}: expected ${entry.hash.slice(0, 12)}..., got ${hash.slice(0, 12)}...`);
      throw new Error(`Hash mismatch for ${path}: expected ${entry.hash}, got ${hash}`);
    }
    console.log(`${P}   fetchFile: hash verified in ${(performance.now() - tHash).toFixed(1)}ms (total fetch+process: ${(performance.now() - t0).toFixed(1)}ms)`);
    return new Uint8Array(data);
  }

  private async loadBundle(bundleName: string): Promise<void> {
    const { P, fmtSize } = LazyFS;
    const bundle = this.manifest.bundles[bundleName];
    if (!bundle) throw new Error(`Bundle not found: ${bundleName}`);
    if (bundle.files.every((f) => this.memCache.has(f))) {
      console.log(`${P}   loadBundle: "${bundleName}" — all ${bundle.files.length} files already cached`);
      return;
    }
    const t0 = performance.now();
    console.log(`${P}   loadBundle: fetching "${bundleName}" (${bundle.files.length} files, expected ${fmtSize(bundle.size)})...`);
    const response = await fetch(bundle.url);
    if (!response.ok) throw new Error(`Failed to fetch bundle ${bundleName}`);
    const tarData = new Uint8Array(await response.arrayBuffer());
    console.log(`${P}   loadBundle: received ${fmtSize(tarData.length)}, extracting...`);
    await this.extractTar(tarData);
    console.log(`${P}   loadBundle: "${bundleName}" extracted in ${(performance.now() - t0).toFixed(1)}ms`);
  }

  private async extractTar(data: Uint8Array): Promise<void> {
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
        this.addToMemCache(name, new Uint8Array(content));
        const entry = this.manifest.files[name];
        if (entry) {
          await this.idbPut(name, content, entry.hash);
        }
      }
    }
  }

  async exists(path: string): Promise<boolean> {
    const normalized = this.normalizePath(path);
    if (normalized in this.manifest.files) return true;
    // Check if path is a directory prefix
    const prefix = normalized === '/' ? '/' : normalized + '/';
    for (const filePath of Object.keys(this.manifest.files)) {
      if (filePath.startsWith(prefix)) return true;
    }
    return false;
  }

  async stat(path: string): Promise<FSStats | null> {
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
    // Check if path is a directory (any manifest entry starts with path + '/')
    const prefix = normalized === '/' ? '/' : normalized + '/';
    for (const filePath of Object.keys(this.manifest.files)) {
      if (filePath.startsWith(prefix)) {
        return {
          type: 'dir',
          size: 0,
          mode: 0o755,
          mtimeNs: BigInt(Date.parse(this.manifest.generated)) * 1_000_000n,
        };
      }
    }
    return null;
  }

  async readdir(path: string): Promise<string[]> {
    const normalized = this.normalizePath(path);
    const prefix = normalized === '/' ? '/' : normalized + '/';
    const entries = new Set<string>();
    for (const filePath of Object.keys(this.manifest.files)) {
      if (filePath.startsWith(prefix)) {
        const relative = filePath.slice(prefix.length);
        const firstPart = relative.split('/')[0];
        if (firstPart) entries.add(firstPart);
      }
    }
    return [...entries];
  }

  async preload(paths: string[]): Promise<void> {
    await Promise.all(paths.map((p) => this.readFile(p).catch(() => null)));
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
    const entry = this.manifest.files[this.normalizePath(path)];
    if (!entry) return '';
    const ext = entry.compressed ? '.' + entry.compressed : '';
    return `${this.manifest.baseUrl}${path}${ext}`;
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
    return [...new Uint8Array(hashBuffer)]
      .map((b) => b.toString(16).padStart(2, '0'))
      .join('');
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
    // Use DecompressionStream with 'deflate-raw' is not brotli;
    // 'br' support in DecompressionStream is not yet widely available.
    // Try native DecompressionStream('br') first, fall back to returning raw data.
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
      throw new Error(
        'Brotli decompression failed: DecompressionStream("br") is not supported in this browser. ' +
        'Rebuild the manifest with SKIP_BROTLI=1 or use a browser that supports brotli decompression.'
      );
    }
  }
}
