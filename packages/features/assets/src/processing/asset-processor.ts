export interface AssetProcessingInput {
  blob: Blob;
  name: string;
  mimeType: string;
}

export interface AssetProcessingContext {
  signal?: AbortSignal;
}

export interface AssetProcessingResult extends AssetProcessingInput {
  warnings?: string[];
}

export interface AssetProcessor {
  readonly key: string;
  supports(input: AssetProcessingInput): boolean;
  process(
    input: AssetProcessingInput,
    context: AssetProcessingContext,
  ): Promise<AssetProcessingResult>;
}
