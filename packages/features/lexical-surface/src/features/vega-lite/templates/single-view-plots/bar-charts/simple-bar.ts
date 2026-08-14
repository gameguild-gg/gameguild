import { BarChart3 } from "lucide-react";

export default {
  id: "bar-chart",
  type: "bar",
  title: "Simple Bar Chart",
  description: "Simple vertical bar chart with embedded data",
  icon: BarChart3,
  category: "single-view-plots",
  subcategory: "bar-charts",
  previewImage: "simple-bar.png",
  spec: {
    $schema: "https://vega.github.io/schema/vega-lite/v5.json",
    description: "A simple bar chart with embedded data.",
    data: {
      values: [
        { a: "A", b: 28 },
        { a: "B", b: 55 },
        { a: "C", b: 43 },
        { a: "D", b: 91 },
        { a: "E", b: 81 },
        { a: "F", b: 53 },
        { a: "G", b: 19 },
        { a: "H", b: 87 },
        { a: "I", b: 52 },
      ],
    },
    mark: "bar",
    encoding: {
      x: { field: "a", type: "nominal", axis: { labelAngle: 0 } },
      y: { field: "b", type: "quantitative" },
    },
  },
};
