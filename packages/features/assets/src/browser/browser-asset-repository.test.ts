import "fake-indexeddb/auto";
import { beforeAll, describe, expect, it } from "vitest";
import { BrowserAssetRepository } from "./browser-asset-repository";
import { ASSET_DATABASE_NAME } from "./browser-storage-schema";
import { resetAssetDatabaseConnectionForTests } from "./metadata-database";
import { ComposedAssetRepository } from "../repository/composed-asset-repository";
import { RemoteAssetProviderRegistry } from "../providers/remote-provider-registry";
import { FakeRemoteAssetProvider } from "../testing/fake-remote-provider";
import { createAssetUri, parseAssetUri } from "../core/asset-uri";

async function uploadRemote(provider: FakeRemoteAssetProvider, value = "remote") {
  const uri = createAssetUri();
  const [record] = await provider.upload(
    [{
      id: parseAssetUri(uri)!.id,
      uri,
      blob: new Blob([value]),
      name: "remote.txt",
      mimeType: "text/plain",
    }],
    {},
  );
  return record!;
}

beforeAll(async () => {
  resetAssetDatabaseConnectionForTests();
  await new Promise<void>((resolve, reject) => {
    const request = indexedDB.deleteDatabase(ASSET_DATABASE_NAME);
    request.onsuccess = () => resolve();
    request.onerror = () => reject(request.error);
  });
  await new Promise<void>((resolve, reject) => {
    const request = indexedDB.open(ASSET_DATABASE_NAME, 1);
    request.onupgradeneeded = () => request.result.createObjectStore("prototype-data");
    request.onsuccess = () => {
      request.result.close();
      resolve();
    };
    request.onerror = () => reject(request.error);
  });
});

describe("BrowserAssetRepository IndexedDB storage", () => {
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

  it("enforces the configured local storage limit before persistence", async () => {
    const repository = new BrowserAssetRepository({ maxLocalBytes: 3 });
    await expect(
      repository.importBlob(new Blob(["four"]), { name: "large.txt" }),
    ).rejects.toMatchObject({ code: "unsupported" });
    expect((await repository.list({ search: "large.txt" })).items).toHaveLength(0);
    repository.dispose();
  });

  it("keeps deduplication reference counts correct during concurrent imports", async () => {
    const repository = new BrowserAssetRepository();
    const records = await Promise.all(
      Array.from({ length: 8 }, (_, index) =>
        repository.importBlob(new Blob(["concurrent"]), { name: `copy-${index}.txt` }),
      ),
    );

    expect(new Set(records.map((record) => record.contentHash)).size).toBe(1);
    for (const record of records.slice(0, -1)) await repository.remove(record.uri, { force: true });
    expect(await repository.readText(records.at(-1)!.uri)).toBe("concurrent");
    await repository.remove(records.at(-1)!.uri, { force: true });
    repository.dispose();
  });

  it("preserves each logical asset MIME type when bytes are deduplicated", async () => {
    const repository = new BrowserAssetRepository();
    const bytes = new Blob(["<svg></svg>"]);
    const text = await repository.importBlob(bytes, {
      name: "source.txt",
      mimeType: "text/plain",
    });
    const image = await repository.importBlob(bytes, {
      name: "image.svg",
      mimeType: "image/svg+xml",
    });

    expect((await repository.readBlob(text.uri)).type).toBe("text/plain");
    expect((await repository.readBlob(image.uri)).type).toBe("image/svg+xml");
    repository.dispose();
  });

  it("keeps an acquired object URL alive until its lease is released", async () => {
    const repository = new BrowserAssetRepository();
    const record = await repository.importBlob(new Blob(["leased"]), { name: "leased.txt" });
    const resolved = await repository.createObjectUrl(record.uri);
    await repository.remove(record.uri, { force: true });

    expect(await (await fetch(resolved.url)).text()).toBe("leased");
    resolved.release();
    repository.dispose();
  });

  it("resolves remote URLs without downloading or caching their bytes", async () => {
    const browser = new BrowserAssetRepository();
    const providers = new RemoteAssetProviderRegistry();
    const provider = new FakeRemoteAssetProvider();
    providers.register(provider);
    const remote = await uploadRemote(provider);
    const repository = new ComposedAssetRepository(browser, providers);

    const resolved = await repository.createObjectUrl(remote.uri);
    expect(await (await fetch(resolved.url)).text()).toBe("remote");
    expect(provider.resolveUrlCalls).toBe(1);
    expect(provider.downloadCalls).toBe(0);
    expect((await browser.get(remote.uri))?.location).toEqual(remote.location);
    resolved.release();

    expect(await repository.readText(remote.uri)).toBe("remote");
    expect(provider.downloadCalls).toBe(1);
    expect((await browser.get(remote.uri))?.location).toEqual(remote.location);
    browser.dispose();
  });

  it("protects referenced remote assets from deletion", async () => {
    const browser = new BrowserAssetRepository();
    const providers = new RemoteAssetProviderRegistry();
    const provider = new FakeRemoteAssetProvider();
    providers.register(provider);
    const remote = await uploadRemote(provider);
    const repository = new ComposedAssetRepository(browser, providers);
    const scope = { type: "document", id: "one" };
    await repository.reconcileUsage(scope, [{ uri: remote.uri, consumerId: "media" }]);

    await expect(repository.remove(remote.uri)).rejects.toMatchObject({ code: "invalid" });
    expect(await repository.get(remote.uri)).not.toBeNull();
    await repository.remove(remote.uri, { force: true });
    expect(await repository.get(remote.uri)).toBeNull();
    expect(await browser.hasUsage(remote.uri)).toBe(false);
    browser.dispose();
  });

  it("rejects remote downloads that conflict with their content hash", async () => {
    const browser = new BrowserAssetRepository();
    const providers = new RemoteAssetProviderRegistry();
    const provider = new FakeRemoteAssetProvider();
    providers.register(provider);
    const remote = await uploadRemote(provider);
    provider.seed(remote.uri, {
      blob: new Blob(["remote"]),
      record: { ...remote, contentHash: "sha256:invalid" },
    });
    const repository = new ComposedAssetRepository(browser, providers);

    await expect(repository.readBlob(remote.uri)).rejects.toMatchObject({ code: "corrupt" });
    browser.dispose();
  });

  it("keeps the logical URI stable when a local asset is uploaded", async () => {
    const browser = new BrowserAssetRepository();
    const providers = new RemoteAssetProviderRegistry();
    const provider = new FakeRemoteAssetProvider();
    providers.register(provider);
    const repository = new ComposedAssetRepository(browser, providers);
    const local = await repository.importBlob(new Blob(["publish me"]), {
      name: "publish.txt",
    });

    const uploaded = await repository.uploadLocalAsset(local.uri, provider.key);

    expect(uploaded.uri).toBe(local.uri);
    expect((await browser.get(local.uri))?.location).toEqual({
      type: "provider",
      providerKey: "fake",
      providerAssetId: local.id,
    });
    expect(await repository.readText(local.uri)).toBe("publish me");
    expect(provider.downloadCalls).toBe(0);
    browser.dispose();
  });

  it("drops retained bytes when provider metadata can no longer verify their hash", async () => {
    const browser = new BrowserAssetRepository();
    const providers = new RemoteAssetProviderRegistry();
    const provider = new FakeRemoteAssetProvider();
    providers.register(provider);
    const repository = new ComposedAssetRepository(browser, providers);
    const local = await repository.importBlob(new Blob(["publish me"]), {
      name: "publish.txt",
    });
    const remote = await repository.uploadLocalAsset(local.uri, provider.key);
    provider.seed(remote.uri, {
      blob: new Blob(["publish me"]),
      record: { ...remote, contentHash: undefined },
    });

    await repository.list({ includeRemote: true });
    expect(await repository.readText(remote.uri)).toBe("publish me");
    expect(provider.downloadCalls).toBe(1);
    browser.dispose();
  });

  it("stores external references as metadata behind a logical URI", async () => {
    const repository = new BrowserAssetRepository();
    const external = await repository.importExternal({
      name: "Video lesson",
      providerKey: "youtube",
      reference: "https://www.youtube.com/watch?v=example",
      kind: "video",
      mimeType: "text/uri-list",
    });

    expect(external.uri).toMatch(/^asset:\/\/[0-9a-f-]+$/);
    expect(external.location).toEqual({
      type: "external",
      providerKey: "youtube",
      reference: "https://www.youtube.com/watch?v=example",
    });
    expect((await repository.get(external.uri))?.location).toEqual(external.location);
    repository.dispose();
  });

  it("keeps scope identities collision-free", async () => {
    const repository = new BrowserAssetRepository();
    const first = await repository.importBlob(new Blob(["first"]), { name: "first.txt" });
    const second = await repository.importBlob(new Blob(["second"]), { name: "second.txt" });
    await repository.reconcileUsage({ type: "a:b", id: "c" }, [
      { uri: first.uri, consumerId: "one" },
    ]);
    await repository.reconcileUsage({ type: "a", id: "b:c" }, [
      { uri: second.uri, consumerId: "two" },
    ]);

    expect(await repository.listUsedByScope({ type: "a:b", id: "c" })).toEqual([first]);
    expect(await repository.listUsedByScope({ type: "a", id: "b:c" })).toEqual([second]);
    repository.dispose();
  });
});
