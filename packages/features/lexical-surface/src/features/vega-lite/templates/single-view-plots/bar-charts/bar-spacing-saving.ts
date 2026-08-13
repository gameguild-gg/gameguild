import { BarChart3 } from "lucide-react";

export default {
  id: "bar-spacing-saving",
  type: "bar",
  title: "Bar Chart with a Spacing-Saving Y-Axis",
  description: "Bar chart optimized for limited vertical space",
  icon: BarChart3,
  category: "single-view-plots",
  subcategory: "bar-charts",
  spec: {
    $schema: "https://vega.github.io/schema/vega-lite/v5.json",
    description: "Bar chart with spacing-saving y-axis.",
    data: {
      values: [
        { category: "A", value: 28 },
        { category: "B", value: 55 },
        { category: "C", value: 43 },
        { category: "D", value: 91 },
        { category: "E", value: 81 },
        { category: "F", value: 53 },
      ],
    },
    mark: "bar",
    encoding: {
      y: { field: "category", type: "nominal" },
      x: { field: "value", type: "quantitative" },
      height: { value: 20 },
    },
  },
};
