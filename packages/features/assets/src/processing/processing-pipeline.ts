import type {
  AssetProcessingContext,
  AssetProcessingInput,
  AssetProcessingResult,
  AssetProcessor,
} from "./asset-processor";

export async function runAssetProcessingPipeline(
  input: AssetProcessingInput,
  processors: readonly AssetProcessor[],
  context: AssetProcessingContext = {},
): Promise<AssetProcessingResult> {
  let current: AssetProcessingResult = input;
  const warnings: string[] = [];
  for (const processor of processors) {
    if (context.signal?.aborted) throw new DOMException("Aborted", "AbortError");
    if (!processor.supports(current)) continue;
    current = await processor.process(current, context);
    if (current.warnings) warnings.push(...current.warnings);
  }
  return warnings.length ? { ...current, warnings } : current;
}
