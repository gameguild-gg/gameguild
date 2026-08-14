import "fake-indexeddb/auto";
import { beforeAll, describe, expect, it } from "vitest";
import { BrowserAssetRepository } from "./browser-asset-repository";
import { ASSET_DATABASE_NAME } from "./browser-storage-schema";
import { resetAssetDatabaseConnectionForTests } from "./metadata-database";
import { ComposedAssetRepository } from "../repository/composed-asset-repository";
import { RemoteAssetProviderRegistry } from "../providers/remote-provider-registry";
import { FakeRemoteAssetProvider } from "../testing/fake-remote-provider";

beforeAll(async () => {
  resetAssetDatabaseConnectionForTests();
  await new Promise<void>((resolve, reject) => {
    const request = indexedDB.deleteDatabase(ASSET_DATABASE_NAME);
    request.onsuccess = () => resolve();
    request.onerror = () => reject(request.error);
  });
});

describe("BrowserAssetRepository IndexedDB fallback", () => {
  it("persists raw Blob bytes across repository instances", async () => {
    const writer = new BrowserAssetRepository();
    const bytes = new Uint8Array([0, 1, 2, 255]);
    const record = await writer.importBlob(
      new Blob([bytes], { type: "application/octet-stream" }),
      { name: "binary.dat" },
    );

    const reader = new BrowserAssetRepository();
    expect((await reader.getStorageStatus()).backend).toBe("indexeddb");
    expect(await (await reader.readBlob(record.uri)).arrayBuffer()).toEqual(bytes.buffer);
    expect((await reader.get(record.uri))?.availability).toBe("local-only");

    writer.dispose();
    reader.dispose();
  });

  it("downloads remote assets once and evicts only unleased cache entries", async () => {
    const browser = new BrowserAssetRepository();
    const providers = new RemoteAssetProviderRegistry();
    const provider = new FakeRemoteAssetProvider();
    providers.register(provider);
    const [remote] = await provider.upload(
      [{ blob: new Blob(["remote"]), name: "remote.txt", mimeType: "text/plain" }],
      {},
    );
    const repository = new ComposedAssetRepository(browser, providers);

    const local = await repository.importBlob(new Blob(["upload"]), { name: "upload.txt" });
    const mapping = await repository.uploadLocalAsset(local.uri, "fake");
    expect(await repository.getRemoteMapping(local.uri)).toBe(mapping.remote.uri);
    await repository.remove(mapping.remote.uri);
    expect(await repository.getRemoteMapping(local.uri)).toBeNull();

    expect(await repository.readText(remote!.uri)).toBe("remote");
    expect((await repository.get(remote!.uri))?.availability).toBe("cached-remote");
    const lease = await repository.createObjectUrl(remote!.uri);
    expect(await repository.evictRemoteCache({ maxBytes: 0, maxEntries: 0 })).toEqual({
      entriesRemoved: 0,
      bytesRemoved: 0,
    });
    lease.release();
    expect((await repository.evictRemoteCache({ maxBytes: 0, maxEntries: 0 })).entriesRemoved).toBe(1);
    expect((await browser.getStorageStatus()).localBytes).toBe(10);
    browser.dispose();
  });
});
