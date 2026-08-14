import type { AssetRepository } from "@game-guild/assets";
import type { VegaDataAttachment } from "../vega-lite-data";

const MAX_VEGA_ATTACHMENT_BYTES = 25 * 1024 * 1024;

export async function resolveVegaAttachments(
  repository: AssetRepository,
  attachments: Record<string, VegaDataAttachment> = {},
  signal?: AbortSignal,
): Promise<Record<string, string>> {
  const entries = await Promise.all(
    Object.entries(attachments).map(async ([filename, attachment]) => {
      const record = await repository.get(attachment.assetUri);
      if (!record) throw new Error(`Dataset is unavailable: ${filename}`);
      if (record.size > MAX_VEGA_ATTACHMENT_BYTES) {
        throw new Error(`Dataset exceeds the 25 MB limit: ${filename}`);
      }
      const isJson = record.mimeType === "application/json" || record.name.endsWith(".json");
      const isCsv = record.mimeType === "text/csv" || record.name.endsWith(".csv");
      if (!isJson && !isCsv) throw new Error(`Unsupported dataset type: ${filename}`);
      const content = await repository.readText(attachment.assetUri, { signal });
      if (isJson) JSON.parse(content);
      return [filename, content] as const;
    }),
  );
  return Object.fromEntries(entries);
}
