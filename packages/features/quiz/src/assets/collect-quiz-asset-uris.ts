import type { AssetUri } from "@game-guild/assets";
import { QuizEntryType, type QuizEntry } from "../questions/question-types";

export function collectQuizAssetUris(entry: QuizEntry): AssetUri[] {
  const uris = new Set<AssetUri>();
  for (const attachment of entry.attachments?.learnerVisible ?? []) {
    uris.add(attachment.assetUri);
  }
  for (const attachment of entry.attachments?.authorOnly ?? []) {
    uris.add(attachment.assetUri);
  }
  if (entry.type === QuizEntryType.Hotspot && entry.imageAssetUri) {
    uris.add(entry.imageAssetUri);
  }
  return Array.from(uris);
}
