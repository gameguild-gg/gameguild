/**
 * Virtual filesystem interface used by the orchestrator.
 * All backends (LazyFS, MemFS, IDBFS, OverlayFS) implement this.
 */

export interface FSStats {
  type: 'file' | 'dir' | 'symlink';
  size: number;
  mode: number;
  mtimeNs: bigint;
  symlinkTarget?: string;
}

export interface IFileSystem {
  readFile(path: string): Promise<Uint8Array | null>;
  writeFile(path: string, data: Uint8Array): Promise<boolean>;
  exists(path: string): Promise<boolean>;
  stat(path: string): Promise<FSStats | null>;
  readdir(path: string): Promise<string[]>;
  deleteFile(path: string): Promise<boolean>;
  mkdir(path: string): Promise<boolean>;
}
