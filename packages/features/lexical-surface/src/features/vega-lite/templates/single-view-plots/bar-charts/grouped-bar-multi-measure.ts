import { Layers } from "lucide-react";

export default {
  id: "grouped-bar-multi-measure",
  type: "grouped-bar",
  title: "Grouped Bar Chart (Multiple Measure with Repeat)",
  description: "Multiple measures shown as grouped bars",
  icon: Layers,
  category: "single-view-plots",
  subcategory: "bar-charts",
  spec: {
    $schema: "https://vega.github.io/schema/vega-lite/v5.json",
    description: "Grouped bar chart with multiple measures.",
    data: {
      values: [
        { category: "A", measure1: 28, measure2: 35 },
        { category: "B", measure1: 55, measure2: 48 },
        { category: "C", measure1: 43, measure2: 51 },
        { category: "D", measure1: 91, measure2: 78 },
      ],
    },
    transform: [{ fold: ["measure1", "measure2"], as: ["measure", "value"] }],
    mark: "bar",
    encoding: {
      x: { field: "category", type: "nominal" },
      y: { field: "value", type: "quantitative" },
      color: { field: "measure", type: "nominal" },
      xOffset: { field: "measure" },
    },
  },
};
