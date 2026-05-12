/**
 * IndexedDB-backed filesystem.
 *
 * Modes:
 *  - **persistent** (default): reads/writes go through IDB. An in-memory write
 *    cache ensures write-then-read consistency for recently written files.
 *    Reads are NOT cached in RAM — data is fetched from IDB on demand.
 *  - **persistent + versioned**: same as persistent, but clears all data when the
 *    supplied version string differs from the one stored in the DB (used for the
 *    overlay write-layer so stale toolchain writes are invalidated on manifest change).
 *  - **volatile** (`{ volatile: true }`): operates purely on an in-memory Map —
 *    no IndexedDB is opened at all.  Used for /tmp and other ephemeral paths.
 */

import type { FSStats, IFileSystem } from 'emception';

export interface IDBFSOptions {
  /** When true, skip all IndexedDB I/O — data lives only in writeCache. */
  volatile?: boolean;
  /**
   * Version tag for cache invalidation.  On `init()` the stored version is
   * compared to this value; if they differ, all entries are cleared.
   * Ignored when `volatile` is true.
   */
  version?: string;
}

interface StoredEntry {
  path: string;
  data: Uint8Array;
  dir: boolean;
  mtime: number;
}

const VERSION_KEY = '__idbfs_version__';

export class IDBFS implements IFileSystem {
  private dbName: string;
  private volatile: boolean;
  private version: string | undefined;
  private idb: IDBDatabase | null = null;
  // Write cache: holds recently-written entries for sync read-after-write consistency.
  // NOT a general read cache — IDB reads are not cached here.
  private writeCache = new Map<string, StoredEntry>();

  constructor(dbName: string, options?: IDBFSOptions) {
    this.dbName = dbName;
    this.volatile = options?.volatile ?? false;
    this.version = options?.version;
  }

  // ---------- lifecycle ----------

  async init(): Promise<void> {
    if (this.volatile) return; // nothing to open

    this.idb = await new Promise((resolve, reject) => {
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

    // Version-based invalidation
    if (this.version !== undefined) {
      const stored = await this.dbGet(VERSION_KEY);
      const storedVersion = stored ? new TextDecoder().decode(stored.data) : null;

      if (storedVersion !== this.version) {
        console.log(
          `[IDBFS:${this.dbName}] Version changed (${storedVersion} → ${this.version}), clearing store`,
        );
        await this.dbClear();
        // Write the new version marker
        await this.dbPut({
          path: VERSION_KEY,
          data: new TextEncoder().encode(this.version),
          dir: false,
          mtime: Date.now(),
        });
      }
    }
  }

  /**
   * @deprecated No longer preloads IDB into RAM. Use async readFile() instead.
   */
  async preloadAll(): Promise<void> {
    // Intentionally empty — we no longer bulk-load IDB entries into RAM.
    // Async reads via Asyncify hooks handle on-demand loading from IDB.
  }

  // ---------- path helpers ----------

  private normalizePath(path: string): string {
    const parts = path.split('/').filter((p) => p && p !== '.');
    const result: string[] = [];
    for (const part of parts) {
      if (part === '..') result.pop();
      else result.push(part);
    }
    return '/' + result.join('/');
  }

  /**
   * Synchronously creates all ancestor directory entries in writeCache.
   * Mirrors MemFS.ensureParentDir — works for both volatile and persistent
   * modes (persistent will persist dirs via the normal writeFile/mkdir path
   * later; this just ensures the cache is coherent for fast lookups).
   */
  private ensureParentDirInCache(path: string): void {
    const dir = path.substring(0, path.lastIndexOf('/')) || '/';
    if (dir !== '/' && !this.writeCache.has(dir)) {
      this.ensureParentDirInCache(dir);
      this.writeCache.set(dir, {
        path: dir,
        data: new Uint8Array(0),
        dir: true,
        mtime: Date.now(),
      });
    }
  }

  // ---------- IDB helpers ----------

  private async dbGet(path: string): Promise<StoredEntry | null> {
    if (!this.idb) return null;
    return new Promise((resolve) => {
      const tx = this.idb!.transaction('files', 'readonly');
      const request = tx.objectStore('files').get(path);
      request.onsuccess = () => resolve(request.result ?? null);
      request.onerror = () => resolve(null);
    });
  }

  private async dbPut(entry: StoredEntry): Promise<void> {
    if (!this.idb) return;
    return new Promise((resolve, reject) => {
      const tx = this.idb!.transaction('files', 'readwrite');
      const request = tx.objectStore('files').put(entry);
      request.onsuccess = () => resolve();
      request.onerror = () => reject(request.error);
    });
  }

  private async dbDelete(path: string): Promise<void> {
    if (!this.idb) return;
    return new Promise((resolve, reject) => {
      const tx = this.idb!.transaction('files', 'readwrite');
      const request = tx.objectStore('files').delete(path);
      request.onsuccess = () => resolve();
      request.onerror = () => reject(request.error);
    });
  }

  /** Clear every entry in the object store. */
  private async dbClear(): Promise<void> {
    if (!this.idb) return;
    return new Promise((resolve, reject) => {
      const tx = this.idb!.transaction('files', 'readwrite');
      const request = tx.objectStore('files').clear();
      request.onsuccess = () => {
        this.writeCache.clear();
        resolve();
      };
      request.onerror = () => reject(request.error);
    });
  }

  private async dbKeys(prefix: string): Promise<string[]> {
    if (!this.idb) return [];
    return new Promise((resolve) => {
      const tx = this.idb!.transaction('files', 'readonly');
      const request = tx.objectStore('files').openKeyCursor();
      const keys: string[] = [];
      const dirPrefix = prefix.endsWith('/') ? prefix : prefix + '/';

      request.onsuccess = () => {
        const cursor = request.result;
        if (!cursor) {
          resolve(keys);
          return;
        }
        const key = cursor.primaryKey as string;
        if (key === prefix || key.startsWith(dirPrefix)) {
          keys.push(key);
        }
        cursor.continue();
      };
      request.onerror = () => resolve([]);
    });
  }

  /** Collect keys from writeCache matching a prefix (for volatile readdir). */
  private memKeys(prefix: string): string[] {
    const keys: string[] = [];
    const dirPrefix = prefix.endsWith('/') ? prefix : prefix + '/';
    for (const key of this.writeCache.keys()) {
      if (key === prefix || key.startsWith(dirPrefix)) {
        keys.push(key);
      }
    }
    return keys;
  }

  // ---------- IFileSystem ----------

  async readFile(path: string): Promise<Uint8Array | null> {
    const normalized = this.normalizePath(path);
    // Check write cache first (recently written files not yet confirmed in IDB)
    const cached = this.writeCache.get(normalized);
    if (cached) return cached.dir ? null : cached.data;
    if (this.volatile) return null;
    // Read directly from IDB — no in-memory caching of reads
    const entry = await this.dbGet(normalized);
    if (!entry || entry.dir) return null;
    return entry.data;
  }

  // ---------- Sync accessors (writeCache for recently-written files) ----------

  /** Synchronous read from writeCache. Returns null on miss. */
  readFileSync(path: string): Uint8Array | null {
    const entry = this.writeCache.get(this.normalizePath(path));
    return entry && !entry.dir ? entry.data : null;
  }

  /** Synchronous write: updates writeCache immediately, fires async IDB persist. */
  writeFileSync(path: string, data: Uint8Array): void {
    const normalized = this.normalizePath(path);
    this.ensureParentDirInCache(normalized);
    const entry: StoredEntry = { path: normalized, data, dir: false, mtime: Date.now() };
    this.writeCache.set(normalized, entry);
    if (!this.volatile && this.idb) {
      this.dbPut(entry).catch(() => { /* fire-and-forget */ });
    }
  }

  /** Synchronous stat from writeCache. */
  statSync(path: string): FSStats | null {
    const entry = this.writeCache.get(this.normalizePath(path));
    if (!entry) return null;
    return {
      type: entry.dir ? 'dir' : 'file',
      size: entry.data?.length ?? 0,
      mode: entry.dir ? 0o755 : 0o644,
      mtimeNs: BigInt(entry.mtime) * 1_000_000n,
    };
  }

  /** Synchronous readdir from writeCache keys. */
  readdirSync(path: string): string[] {
    const normalized = this.normalizePath(path);
    const prefix = normalized === '/' ? '/' : normalized + '/';
    const entries = new Set<string>();
    for (const key of this.writeCache.keys()) {
      if (key.startsWith(prefix)) {
        const rel = key.slice(prefix.length);
        const first = rel.split('/')[0];
        if (first) entries.add(first);
      }
    }
    return [...entries];
  }

  /** Synchronous existence check from writeCache. */
  existsSync(path: string): boolean {
    return this.writeCache.has(this.normalizePath(path));
  }

  /** Synchronous mkdir: creates directory entry in writeCache. */
  mkdirSync(path: string): void {
    const normalized = this.normalizePath(path);
    if (this.writeCache.has(normalized)) return;
    this.ensureParentDirInCache(normalized);
    const entry: StoredEntry = { path: normalized, data: new Uint8Array(0), dir: true, mtime: Date.now() };
    this.writeCache.set(normalized, entry);
    if (!this.volatile && this.idb) {
      this.dbPut(entry).catch(() => { /* fire-and-forget */ });
    }
  }

  /** Synchronous delete from writeCache. */
  deleteFileSync(path: string): boolean {
    const normalized = this.normalizePath(path);
    const entry = this.writeCache.get(normalized);
    if (!entry || entry.dir) return false;
    this.writeCache.delete(normalized);
    if (!this.volatile && this.idb) {
      this.dbDelete(normalized).catch(() => { /* fire-and-forget */ });
    }
    return true;
  }

  async writeFile(path: string, data: Uint8Array): Promise<boolean> {
    const normalized = this.normalizePath(path);
    this.ensureParentDirInCache(normalized);
    const entry: StoredEntry = {
      path: normalized,
      data,
      dir: false,
      mtime: Date.now(),
    };
    this.writeCache.set(normalized, entry);

    if (!this.volatile) {
      // Persist parent dirs that only exist in cache
      const dir = normalized.substring(0, normalized.lastIndexOf('/')) || '/';
      if (dir !== '/') {
        const dirEntry = this.writeCache.get(dir);
        if (dirEntry && !(await this.dbGet(dir))) {
          await this.persistDirChain(dir);
        }
      }
      await this.dbPut(entry);
    }
    return true;
  }

  /** Persist a directory and all its ancestors to IDB. */
  private async persistDirChain(dirPath: string): Promise<void> {
    const parent = dirPath.substring(0, dirPath.lastIndexOf('/')) || '/';
    if (parent !== '/') {
      const parentEntry = this.writeCache.get(parent);
      if (parentEntry && !(await this.dbGet(parent))) {
        await this.persistDirChain(parent);
      }
    }
    const entry = this.writeCache.get(dirPath);
    if (entry) {
      await this.dbPut(entry);
    }
  }

  async exists(path: string): Promise<boolean> {
    const normalized = this.normalizePath(path);
    if (this.writeCache.has(normalized)) return true;
    if (this.volatile) return false;
    const entry = await this.dbGet(normalized);
    if (entry) this.writeCache.set(normalized, entry);
    return entry !== null;
  }

  async stat(path: string): Promise<FSStats | null> {
    const normalized = this.normalizePath(path);
    let entry = this.writeCache.get(normalized);
    if (!entry && !this.volatile) {
      entry = (await this.dbGet(normalized)) ?? undefined;
      if (entry) this.writeCache.set(normalized, entry);
    }
    if (!entry) return null;
    return {
      type: entry.dir ? 'dir' : 'file',
      size: entry.data?.length ?? 0,
      mode: entry.dir ? 0o755 : 0o644,
      mtimeNs: BigInt(entry.mtime) * 1_000_000n,
    };
  }

  async readdir(path: string): Promise<string[]> {
    const normalized = this.normalizePath(path);
    const prefix = normalized === '/' ? '/' : normalized + '/';
    const keys = this.volatile ? this.memKeys(prefix) : await this.dbKeys(prefix);

    // In persistent mode, also merge writeCache keys (may have dirs not yet flushed)
    if (!this.volatile) {
      const memOnlyKeys = this.memKeys(prefix);
      for (const k of memOnlyKeys) {
        if (!keys.includes(k)) keys.push(k);
      }
    }

    const entries = new Set<string>();
    for (const key of keys) {
      if (key.startsWith(prefix)) {
        const rel = key.slice(prefix.length);
        const first = rel.split('/')[0];
        if (first) entries.add(first);
      }
    }
    return [...entries];
  }

  async deleteFile(path: string): Promise<boolean> {
    const normalized = this.normalizePath(path);
    const cached = this.writeCache.get(normalized);
    if (cached) {
      if (cached.dir) return false;
      this.writeCache.delete(normalized);
      if (!this.volatile) await this.dbDelete(normalized);
      return true;
    }
    if (this.volatile) return false;
    const entry = await this.dbGet(normalized);
    if (!entry || entry.dir) return false;
    await this.dbDelete(normalized);
    return true;
  }

  async mkdir(path: string): Promise<boolean> {
    const normalized = this.normalizePath(path);
    if (this.writeCache.has(normalized)) return false;
    if (!this.volatile && (await this.dbGet(normalized))) return false;

    this.ensureParentDirInCache(normalized);
    const entry: StoredEntry = {
      path: normalized,
      data: new Uint8Array(0),
      dir: true,
      mtime: Date.now(),
    };
    this.writeCache.set(normalized, entry);

    if (!this.volatile) {
      await this.persistDirChain(normalized);
      await this.dbPut(entry);
    }
    return true;
  }

  /** Clear all entries (writeCache + IDB). Re-creates the root dir entry. */
  async clear(): Promise<void> {
    this.writeCache.clear();
    if (!this.volatile && this.idb) {
      await this.dbClear();
    }
  }
}
