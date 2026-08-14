import { describe, expect, it } from "vitest";
import { MemoryAssetRepository } from "@game-guild/assets/testing";
import { resolveVegaAttachments } from "./vega-asset-loader";

describe("resolveVegaAttachments", () => {
  it("resolves valid JSON without embedding it in the attachment contract", async () => {
    const repository = new MemoryAssetRepository();
    const record = await repository.importBlob(
      new Blob(['[{"x":1}]'], { type: "application/json" }),
      { name: "values.json" },
    );

    await expect(resolveVegaAttachments(repository, {
      "values.json": {
        name: record.name,
        assetUri: record.uri,
        mimeType: "application/json",
        size: record.size,
      },
    })).resolves.toEqual({ "values.json": '[{"x":1}]' });
  });

  it("reports missing attachments by virtual filename", async () => {
    const repository = new MemoryAssetRepository();
    const record = await repository.importBlob(new Blob(["x,y\n1,2"], { type: "text/csv" }), {
      name: "missing.csv",
    });
    await repository.remove(record.uri);

    await expect(resolveVegaAttachments(repository, {
      "missing.csv": {
        name: record.name,
        assetUri: record.uri,
        mimeType: "text/csv",
        size: record.size,
      },
    })).rejects.toThrow("Dataset is unavailable: missing.csv");
  });
});
