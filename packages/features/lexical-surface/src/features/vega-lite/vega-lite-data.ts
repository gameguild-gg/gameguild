import type { AssetUri } from "@game-guild/assets";

export interface VegaDataAttachment {
  name: string;
  assetUri: AssetUri;
  mimeType: "text/csv" | "application/json";
  size: number;
}

export interface VegaLiteData {
  spec: string;
  title?: string;
  caption?: string;
  size?: number;
  theme?:
    | "default"
    | "excel"
    | "ggplot2"
    | "quartz"
    | "vox"
    | "fivethirtyeight"
    | "latimes"
    | "urbaninstitute"
    | "googlecharts"
    | "powerbi";
  themeMode?: "system" | "only-light" | "only-dark";
  layout?: "square" | "rectangular";
  attachments?: Record<string, VegaDataAttachment>;
}
