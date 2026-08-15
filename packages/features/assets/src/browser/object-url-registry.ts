import type { ResolvedAssetUrl } from "../core/asset-contracts";

interface ObjectUrlEntry {
  url: string;
  leases: number;
}

export class ObjectUrlRegistry {
  private readonly entries = new Map<string, ObjectUrlEntry>();

  acquire(objectId: string, blob: Blob): ResolvedAssetUrl {
    let entry = this.entries.get(objectId);
    if (!entry) {
      entry = { url: URL.createObjectURL(blob), leases: 0 };
      this.entries.set(objectId, entry);
    }
    entry.leases += 1;
    let released = false;
    return {
      url: entry.url,
      release: () => {
        if (released) return;
        released = true;
        const current = this.entries.get(objectId);
        if (!current) return;
        current.leases -= 1;
        if (current.leases <= 0) {
          URL.revokeObjectURL(current.url);
          this.entries.delete(objectId);
        }
      },
    };
  }

  invalidate(objectId: string): void {
    const entry = this.entries.get(objectId);
    if (!entry) return;
    URL.revokeObjectURL(entry.url);
    this.entries.delete(objectId);
  }

  hasLease(objectId: string): boolean {
    return (this.entries.get(objectId)?.leases ?? 0) > 0;
  }

  clear(): void {
    for (const entry of this.entries.values()) URL.revokeObjectURL(entry.url);
    this.entries.clear();
  }
}
