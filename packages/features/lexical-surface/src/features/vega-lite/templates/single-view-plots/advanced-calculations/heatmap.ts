import { Target } from "lucide-react";

export default {
  id: "heatmap",
  type: "heatmap",
  title: "Heatmap",
  description: "2D heatmap visualization with color encoding",
  icon: Target,
  category: "single-view-plots",
  subcategory: "advanced-calculations",
  spec: {
    $schema: "https://vega.github.io/schema/vega-lite/v5.json",
    description: "A heatmap showing correlation between variables.",
    data: {
      values: [
        { x: "A", y: "1", z: 0.1 },
        { x: "A", y: "2", z: 0.2 },
        { x: "A", y: "3", z: 0.3 },
        { x: "B", y: "1", z: 0.4 },
        { x: "B", y: "2", z: 0.5 },
        { x: "B", y: "3", z: 0.6 },
        { x: "C", y: "1", z: 0.7 },
        { x: "C", y: "2", z: 0.8 },
        { x: "C", y: "3", z: 0.9 },
      ],
    },
    mark: "rect",
    encoding: {
      x: { field: "x", type: "nominal" },
      y: { field: "y", type: "nominal" },
      color: { field: "z", type: "quantitative", scale: { scheme: "viridis" } },
    },
  },
};
