import { BarChart3 } from "lucide-react";

export default {
  id: "stacked-bar",
  type: "bar",
  title: "Stacked Bar Chart",
  description: "Stacked bar chart showing part-to-whole relationships",
  icon: BarChart3,
  category: "single-view-plots",
  subcategory: "bar-charts",
  spec: {
    $schema: "https://vega.github.io/schema/vega-lite/v5.json",
    description: "A stacked bar chart with multiple series.",
    data: {
      values: [
        { category: "A", group: "x", value: 28 },
        { category: "A", group: "y", value: 55 },
        { category: "B", group: "x", value: 43 },
        { category: "B", group: "y", value: 91 },
        { category: "C", group: "x", value: 81 },
        { category: "C", group: "y", value: 53 },
      ],
    },
    mark: "bar",
    encoding: {
      x: { field: "category", type: "nominal" },
      y: { aggregate: "sum", field: "value", type: "quantitative" },
      color: { field: "group", type: "nominal" },
    },
  },
};
