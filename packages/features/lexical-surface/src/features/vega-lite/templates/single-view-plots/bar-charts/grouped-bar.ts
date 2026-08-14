import { Layers } from "lucide-react";

export default {
  id: "grouped-bar",
  type: "grouped-bar",
  title: "Grouped Bar Chart",
  description: "Multi-series grouped bar chart",
  icon: Layers,
  category: "single-view-plots",
  subcategory: "bar-charts",
  spec: {
    $schema: "https://vega.github.io/schema/vega-lite/v5.json",
    description: "A grouped bar chart with multiple series.",
    data: {
      values: [
        { category: "A", group: "x", value: 0.1 },
        { category: "A", group: "y", value: 0.6 },
        { category: "A", group: "z", value: 0.9 },
        { category: "B", group: "x", value: 0.7 },
        { category: "B", group: "y", value: 0.2 },
        { category: "B", group: "z", value: 1.1 },
        { category: "C", group: "x", value: 0.6 },
        { category: "C", group: "y", value: 0.1 },
        { category: "C", group: "z", value: 0.2 },
      ],
    },
    mark: "bar",
    encoding: {
      x: { field: "category", type: "nominal" },
      y: { field: "value", type: "quantitative" },
      color: { field: "group", type: "nominal" },
      xOffset: { field: "group", type: "nominal" },
    },
  },
};
