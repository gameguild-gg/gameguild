/**
 * In-memory filesystem for volatile storage (/tmp).
 * All data is lost on page reload.
 */

import { FSStats, IFileSystem } from './interface';

interface MemEntry {
  data?: Uint8Array;
  dir: boolean;
  mtime: number;
}

export class MemFS implements IFileSystem {
  private store = new Map<string, MemEntry>();

  private normalizePath(path: string): string {
    const parts = path.split('/').filter((p) => p && p !== '.');
    const result: string[] = [];
    for (const part of parts) {
      if (part === '..') result.pop();
      else result.push(part);
    }
    return '/' + result.join('/');
  }

  private ensureParentDir(path: string): void {
    const dir = path.substring(0, path.lastIndexOf('/')) || '/';
    if (dir !== '/' && !this.store.has(dir)) {
      this.ensureParentDir(dir);
      this.store.set(dir, { dir: true, mtime: Date.now() });
    }
  }

  async readFile(path: string): Promise<Uint8Array | null> {
    const normalized = this.normalizePath(path);
    const entry = this.store.get(normalized);
    if (!entry || entry.dir) return null;
    return entry.data ?? null;
  }

  async writeFile(path: string, data: Uint8Array): Promise<boolean> {
    const normalized = this.normalizePath(path);
    this.ensureParentDir(normalized);
    this.store.set(normalized, { data, dir: false, mtime: Date.now() });
    return true;
  }

  async exists(path: string): Promise<boolean> {
    const normalized = this.normalizePath(path);
    return this.store.has(normalized);
  }

  async stat(path: string): Promise<FSStats | null> {
    const normalized = this.normalizePath(path);
    const entry = this.store.get(normalized);
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
    const entries = new Set<string>();
    for (const key of this.store.keys()) {
      if (key.startsWith(prefix) && key.length > prefix.length) {
        const rel = key.slice(prefix.length);
        const first = rel.split('/')[0];
        if (first) entries.add(first);
      }
    }
    return [...entries];
  }

  async deleteFile(path: string): Promise<boolean> {
    const normalized = this.normalizePath(path);
    const entry = this.store.get(normalized);
    if (!entry || entry.dir) return false;
    this.store.delete(normalized);
    return true;
  }

  async mkdir(path: string): Promise<boolean> {
    const normalized = this.normalizePath(path);
    if (this.store.has(normalized)) return false;
    this.ensureParentDir(normalized);
    this.store.set(normalized, { dir: true, mtime: Date.now() });
    return true;
  }
}
