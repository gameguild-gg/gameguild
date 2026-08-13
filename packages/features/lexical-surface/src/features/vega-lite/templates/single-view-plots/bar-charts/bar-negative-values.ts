import { BarChart3 } from "lucide-react";

export default {
  id: "bar-negative-values",
  type: "bar",
  title: "Bar Chart with Negative Values and a Zero-Baseline",
  description: "Bars for both positive and negative values",
  icon: BarChart3,
  category: "single-view-plots",
  subcategory: "bar-charts",
  spec: {
    $schema: "https://vega.github.io/schema/vega-lite/v5.json",
    description: "Bar chart with negative values.",
    data: {
      values: [
        { category: "A", value: 28 },
        { category: "B", value: -15 },
        { category: "C", value: 43 },
        { category: "D", value: -25 },
        { category: "E", value: 81 },
      ],
    },
    mark: "bar",
    encoding: {
      x: { field: "category", type: "nominal" },
      y: { field: "value", type: "quantitative", scale: { zero: true } },
      color: {
        condition: { test: "datum.value > 0", value: "steelblue" },
        value: "coral",
      },
    },
  },
};
