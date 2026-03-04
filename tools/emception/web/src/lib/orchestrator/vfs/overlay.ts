/**
 * Overlay filesystem: routes reads/writes to mounted backends with write-through.
 */

import { FSStats, IFileSystem } from './interface';

export class OverlayFS implements IFileSystem {
  private mounts = new Map<string, IFileSystem>();
  private writeLayer: IFileSystem;
  private defaultFs: IFileSystem;

  constructor(defaultFs: IFileSystem, writeLayer: IFileSystem) {
    this.defaultFs = defaultFs;
    this.writeLayer = writeLayer;
  }

  mount(path: string, fs: IFileSystem): void {
    this.mounts.set(this.normalizePath(path), fs);
  }

  private getFs(path: string): { fs: IFileSystem; relativePath: string } {
    const normalized = this.normalizePath(path);
    let bestMatch = '';
    let bestFs = this.defaultFs;
    for (const [mountPoint, fs] of this.mounts) {
      if (
        (normalized === mountPoint || normalized.startsWith(mountPoint + '/')) &&
        mountPoint.length > bestMatch.length
      ) {
        bestMatch = mountPoint;
        bestFs = fs;
      }
    }
    const relativePath = bestMatch ? normalized.slice(bestMatch.length) || '/' : normalized;
    return { fs: bestFs, relativePath };
  }

  async readFile(path: string): Promise<Uint8Array | null> {
    const writeResult = await this.writeLayer.readFile(path);
    if (writeResult) return writeResult;
    const { fs, relativePath } = this.getFs(path);
    return fs.readFile(relativePath);
  }

  async writeFile(path: string, data: Uint8Array): Promise<boolean> {
    const { fs, relativePath } = this.getFs(path);
    const wrote = await fs.writeFile(relativePath, data);
    if (!wrote) {
      return this.writeLayer.writeFile(path, data);
    }
    return true;
  }

  async exists(path: string): Promise<boolean> {
    if (await this.writeLayer.exists(path)) return true;
    const { fs, relativePath } = this.getFs(path);
    return fs.exists(relativePath);
  }

  async stat(path: string): Promise<FSStats | null> {
    const writeStat = await this.writeLayer.stat(path);
    if (writeStat) return writeStat;
    const { fs, relativePath } = this.getFs(path);
    return fs.stat(relativePath);
  }

  async readdir(path: string): Promise<string[]> {
    const normalized = this.normalizePath(path);
    const { fs, relativePath } = this.getFs(path);
    const entries = new Set(await fs.readdir(relativePath));
    const writeEntries = await this.writeLayer.readdir(normalized);
    for (const e of writeEntries) entries.add(e);
    const prefix = normalized === '/' ? '/' : normalized + '/';
    for (const mountPoint of this.mounts.keys()) {
      if (mountPoint.startsWith(prefix) && mountPoint !== normalized) {
        const relative = mountPoint.slice(prefix.length);
        if (!relative.includes('/')) entries.add(relative);
      }
    }
    return [...entries];
  }

  async deleteFile(path: string): Promise<boolean> {
    const { fs, relativePath } = this.getFs(path);
    return fs.deleteFile(relativePath);
  }

  async mkdir(path: string): Promise<boolean> {
    const { fs, relativePath } = this.getFs(path);
    const ok = await fs.mkdir(relativePath);
    if (!ok) return this.writeLayer.mkdir(path);
    return true;
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
}
