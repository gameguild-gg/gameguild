import { describe, expect, it } from "vitest";
import type { AssetRepository } from "../repository/asset-repository";
import type { RemoteAssetProvider } from "../providers/remote-asset-provider";

export function describeAssetRepositoryContract(
  name: string,
  createRepository: () => AssetRepository,
): void {
  describe(`${name} asset repository contract`, () => {
    it("imports, reads, queries, renames, tracks usage, and removes", async () => {
      const repository = createRepository();
      const record = await repository.importBlob(
        new Blob(["name,value\na,1"], { type: "text/csv" }),
        { name: "sample.csv", scope: { type: "test", id: "one" } },
      );

      expect(await repository.readText(record.uri)).toBe("name,value\na,1");
      expect(await new Response(await repository.readStream(record.uri)).text()).toBe("name,value\na,1");
      expect((await repository.list({ search: "sample" })).items).toHaveLength(1);
      expect((await repository.rename(record.uri, "renamed.csv")).name).toBe("renamed.csv");

      const scope = { type: "document", id: "one" };
      await repository.reconcileUsage(scope, [
        { uri: record.uri, consumerId: "attachment", role: "source" },
      ]);
      expect(await repository.listUsedByScope(scope)).toHaveLength(1);
      expect((await repository.checkPortability([record.uri])).localOnly).toEqual([record.uri]);
      await expect(repository.remove(record.uri)).rejects.toBeTruthy();
      await repository.remove(record.uri, { force: true });
      expect(await repository.get(record.uri)).toBeNull();
    });

    it("deduplicates physical content while preserving logical identities", async () => {
      const repository = createRepository();
      const blob = new Blob(["same"]);
      const first = await repository.importBlob(blob, { name: "first.txt" });
      const second = await repository.importBlob(blob, { name: "second.txt" });
      expect(first.uri).not.toBe(second.uri);
      expect(first.contentHash).toBe(second.contentHash);
      expect((await repository.getStorageStatus()).localBytes).toBe(blob.size);
    });
  });
}

export function describeRemoteAssetProviderContract(
  name: string,
  createProvider: () => RemoteAssetProvider,
): void {
  describe(`${name} remote asset provider contract`, () => {
    it("uploads, lists, downloads, resolves, and deletes", async () => {
      const provider = createProvider();
      const blob = new Blob(["remote body"], { type: "text/plain" });
      const [record] = await provider.upload(
        [{ blob, name: "remote.txt", mimeType: blob.type }],
        { scope: { type: "test", id: "remote" } },
      );
      expect(record).toBeDefined();
      expect((await provider.get(record!.uri, {}))?.name).toBe("remote.txt");
      expect((await provider.list({ search: "remote" }, {})).items).toHaveLength(1);
      expect(await (await provider.download(record!.uri, {})).blob.text()).toBe("remote body");
      await provider.delete?.(record!.uri, {});
      expect(await provider.get(record!.uri, {})).toBeNull();
    });

    it("honors an already-aborted upload", async () => {
      const provider = createProvider();
      const controller = new AbortController();
      controller.abort();
      await expect(provider.upload(
        [{ blob: new Blob(["x"]), name: "x.txt", mimeType: "text/plain" }],
        { signal: controller.signal },
      )).rejects.toMatchObject({ name: "AbortError" });
    });
  });
}
