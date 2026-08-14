"use client";

import { useEffect, useRef, useState } from "react";
import {
  applyThemeOverrides,
  DARK_THEME_OVERRIDES,
  LIGHT_THEME_OVERRIDES,
} from "./vega-theme-overrides";

const THEME_MAP: Record<string, string> = {
  default: "default",
  dark: "dark",
  excel: "excel",
  ggplot2: "ggplot2",
  quartz: "quartz",
  vox: "vox",
  fivethirtyeight: "fivethirtyeight",
  latimes: "latimes",
  urbaninstitute: "urbaninstitute",
  googlecharts: "googlecharts",
  powerbi: "powerbi",
  "excel-dark": "excel",
  "ggplot2-dark": "ggplot2",
  "quartz-dark": "quartz",
  "vox-dark": "vox",
  "fivethirtyeight-dark": "fivethirtyeight",
  "latimes-dark": "latimes",
  "urbaninstitute-dark": "urbaninstitute",
  "googlecharts-dark": "googlecharts",
  "powerbi-dark": "powerbi",
};

const renderGenerations = new WeakMap<HTMLElement, number>();

function nextRenderGeneration(container: HTMLElement): number {
  const generation = (renderGenerations.get(container) ?? 0) + 1;
  renderGenerations.set(container, generation);
  return generation;
}

function createDarkTheme(baseTheme: Record<string, any>) {
  return {
    ...baseTheme,
    background: "#1a1a1a",
    view: { ...baseTheme.view, fill: "#1a1a1a", stroke: "#404040" },
    axis: {
      ...baseTheme.axis,
      domainColor: "#666666",
      gridColor: "#333333",
      tickColor: "#666666",
      labelColor: "#cccccc",
      titleColor: "#ffffff",
    },
    legend: {
      ...baseTheme.legend,
      labelColor: "#cccccc",
      titleColor: "#ffffff",
    },
    title: { ...baseTheme.title, color: "#ffffff" },
    text: { ...baseTheme.text, fill: "#cccccc" },
  };
}

interface VegaChartData {
  parsedSpec: Record<string, any> | null;
  isLoading: boolean;
  error: string;
  vegaRef: React.RefObject<HTMLDivElement | null>;
  fullscreenVegaRef: React.RefObject<HTMLDivElement | null>;
}

export function useVegaLiteChart({
  spec,
  layout = "rectangular",
}: {
  spec: string;
  layout?: "square" | "rectangular";
  theme?: string;
}): VegaChartData {
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState("");
  const [parsedSpec, setParsedSpec] = useState<Record<string, any> | null>(
    null,
  );
  const vegaRef = useRef<HTMLDivElement>(null);
  const fullscreenVegaRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (!spec) {
      setParsedSpec(null);
      setError("");
      return;
    }

    setIsLoading(true);
    try {
      const parsed = JSON.parse(spec) as Record<string, any>;
      if (!parsed || typeof parsed !== "object" || Array.isArray(parsed)) {
        throw new Error("Specification must be a JSON object");
      }
      if (!parsed.data && !parsed.datasets) {
        throw new Error("Vega-Lite spec missing data field");
      }
      if (
        !parsed.mark &&
        !parsed.layer &&
        !parsed.concat &&
        !parsed.hconcat &&
        !parsed.vconcat &&
        !parsed.facet &&
        !parsed.repeat
      ) {
        throw new Error("Vega-Lite spec missing a mark or composite view");
      }

      setParsedSpec({
        ...parsed,
        width: layout === "square" ? 400 : 800,
        height: layout === "square" ? 400 : 300,
      });
      setError("");
    } catch (parseError) {
      setParsedSpec(null);
      setError(
        parseError instanceof Error
          ? parseError.message
          : "Invalid Vega-Lite specification",
      );
    } finally {
      setIsLoading(false);
    }
  }, [layout, spec]);

  return { parsedSpec, isLoading, error, vegaRef, fullscreenVegaRef };
}

export async function renderVegaChart(
  container: HTMLElement,
  parsedSpec: Record<string, any>,
  layout: "square" | "rectangular" = "rectangular",
  theme = "default",
): Promise<() => void> {
  const generation = nextRenderGeneration(container);
  const isCurrent = () => renderGenerations.get(container) === generation;
  const [vegaLite, vega, vegaThemes] = await Promise.all([
    import("vega-lite"),
    import("vega"),
    import("vega-themes"),
  ]);

  if (!isCurrent()) return () => undefined;

  let specWithTheme = { ...parsedSpec };
  if (theme !== "default" && THEME_MAP[theme]) {
    const isDarkTheme = theme.endsWith("-dark");
    const baseThemeName = isDarkTheme ? theme.replace("-dark", "") : theme;
    const themeConfig = (vegaThemes as unknown as Record<string, any>)[
      THEME_MAP[theme]
    ];
    if (themeConfig) {
      let finalThemeConfig =
        isDarkTheme && baseThemeName !== "dark"
          ? createDarkTheme(themeConfig)
          : themeConfig;
      const overrides = isDarkTheme
        ? DARK_THEME_OVERRIDES[theme]
        : LIGHT_THEME_OVERRIDES[theme];
      if (overrides) {
        finalThemeConfig = applyThemeOverrides(finalThemeConfig, overrides);
      }
      specWithTheme = {
        ...specWithTheme,
        config: { ...specWithTheme.config, ...finalThemeConfig },
      };
    }
  }

  const vegaSpec = vegaLite.compile(
    specWithTheme as Parameters<typeof vegaLite.compile>[0],
  ).spec;
  if (!isCurrent()) return () => undefined;

  container.replaceChildren();
  const view = new vega.View(vega.parse(vegaSpec), { renderer: "svg" });
  view.initialize(container);

  try {
    await view.runAsync();
    if (!isCurrent()) {
      view.finalize();
      return () => undefined;
    }

    const rendered = container.firstElementChild as HTMLElement | null;
    if (rendered) {
      rendered.style.display = "block";
      rendered.style.maxWidth = "100%";
      rendered.style.height = "auto";
      if (layout === "square") rendered.style.margin = "0 auto";
    }
  } catch (error) {
    view.finalize();
    if (isCurrent()) container.replaceChildren();
    throw error;
  }

  return () => {
    view.finalize();
    if (isCurrent()) {
      renderGenerations.delete(container);
      container.replaceChildren();
    }
  };
}
