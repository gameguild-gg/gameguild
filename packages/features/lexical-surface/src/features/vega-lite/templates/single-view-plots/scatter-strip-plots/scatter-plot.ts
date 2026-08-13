import { ScatterChart } from "lucide-react";

export default {
  id: "scatter-plot",
  type: "scatter",
  title: "Scatter Plot",
  description: "Scatter plot with correlation",
  icon: ScatterChart,
  category: "single-view-plots",
  subcategory: "scatter-strip-plots",
  spec: {
    $schema: "https://vega.github.io/schema/vega-lite/v5.json",
    description: "A scatterplot showing correlation between variables.",
    data: {
      values: [
        { x: 10, y: 20 },
        { x: 20, y: 35 },
        { x: 30, y: 45 },
        { x: 40, y: 55 },
        { x: 50, y: 25 },
        { x: 60, y: 65 },
        { x: 70, y: 75 },
        { x: 80, y: 85 },
        { x: 90, y: 95 },
      ],
    },
    mark: "circle",
    encoding: {
      x: { field: "x", type: "quantitative", scale: { zero: false } },
      y: { field: "y", type: "quantitative", scale: { zero: false } },
      size: { value: 100 },
    },
  },
};
