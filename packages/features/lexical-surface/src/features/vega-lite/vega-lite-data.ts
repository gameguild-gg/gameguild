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
  data?: Record<string, string>;
}
