import { describe, expect, it } from "vitest";
import { AssetError } from "../core/asset-errors";
import { MemoryAssetRepository } from "./memory-asset-repository";

describe("MemoryAssetRepository", () => {
  it("imports, deduplicates, reads, renames, and removes assets", async () => {
    const repository = new MemoryAssetRepository();
    const first = await repository.importBlob(new Blob(["a,b\n1,2"], { type: "text/csv" }), {
      name: "one.csv",
    });
    const second = await repository.importBlob(new Blob(["a,b\n1,2"], { type: "text/csv" }), {
      name: "two.csv",
    });

    expect(first.uri).not.toBe(second.uri);
    expect(first.contentHash).toBe(second.contentHash);
    expect((await repository.getStorageStatus()).localBytes).toBe(7);
    expect(await repository.readText(first.uri)).toBe("a,b\n1,2");
    expect((await repository.rename(first.uri, "renamed.csv")).name).toBe("renamed.csv");

    await repository.remove(first.uri);
    expect(await repository.readText(second.uri)).toBe("a,b\n1,2");
  });

  it("protects referenced assets", async () => {
    const repository = new MemoryAssetRepository();
    const record = await repository.importBlob(new Blob(["data"]), { name: "data.bin" });
    await repository.reconcileUsage(
      { type: "document", id: "one" },
      [{ uri: record.uri, consumerId: "node-one" }],
    );

    await expect(repository.remove(record.uri)).rejects.toMatchObject({
      code: "invalid",
    } satisfies Partial<AssetError>);
    await repository.remove(record.uri, { force: true });
    expect(await repository.get(record.uri)).toBeNull();
  });
});
