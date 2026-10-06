"use client";

import {
  getVegaLiteThemePair as getThemePair,
  type VegaLiteThemeMode,
  type VegaThemeBase,
  VegaLiteViewer,
} from "@game-guild/lexical-surface";

interface PreviewVegaLiteProps {
  node: {
    data: {
      spec: string;
      title?: string;
      caption?: string;
      theme?: string;
      themeMode?: string;
      layout?: "square" | "rectangular";
      size?: number;
    };
  };
}

export function PreviewVegaLite({ node }: PreviewVegaLiteProps) {
  const { spec, title, caption, layout, size } = node.data;
  const theme = (node.data.theme || "default") as VegaThemeBase;
  const themeMode = (node.data.themeMode || "system") as VegaLiteThemeMode;
  const themePair = getThemePair(theme, themeMode);

  return (
    <VegaLiteViewer
      spec={spec}
      layout={layout}
      themeLight={themePair.themeLight}
      themeDark={themePair.themeDark}
      title={title}
      caption={caption}
      size={size}
      showControls={true}
      allowFullscreen={true}
    />
  );
}
