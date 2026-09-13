import { findAssetUris, type AssetPortabilityReport, type AssetRecord, type AssetScope, type AssetUri } from "@game-guild/assets";

export interface AuthoringAssetRepository {
  get(uri: AssetUri): Promise<AssetRecord | null>;
  uploadLocalAsset(
    uri: AssetUri,
    providerKey: string,
    options: { scope: AssetScope },
  ): Promise<AssetRecord>;
  checkPortability(uris: readonly AssetUri[]): Promise<AssetPortabilityReport>;
  reconcileUsage(
    scope: AssetScope,
    usages: ReadonlyArray<{ uri: AssetUri; consumerId: string; role?: string }>,
  ): Promise<void>;
}

/** Makes every embedded lesson asset remotely resolvable before persistence. */
export async function prepareAuthoringAssets(
  payload: unknown,
  repository: AuthoringAssetRepository,
  scope: AssetScope,
): Promise<void> {
  const uris = [...new Set(findAssetUris(payload))];
  for (const uri of uris) {
    const record = await repository.get(uri);
    if (record?.location.type === "local") {
      await repository.uploadLocalAsset(uri, "gameguild", { scope });
    }
  }

  const portability = await repository.checkPortability(uris);
  if (!portability.portable) {
    throw new Error(
      `Upload every lesson asset before saving (${portability.localOnly.length} local, ${portability.unavailable.length} unavailable).`,
    );
  }

  await repository.reconcileUsage(
    scope,
    uris.map((uri) => ({ uri, consumerId: "lesson-document", role: "embedded" })),
  );
}
