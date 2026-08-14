import { BarChart3 } from "lucide-react";

export default {
  id: "diverging-stacked-bar",
  type: "bar",
  title: "Diverging Stacked Bar Chart (Population Pyramid)",
  description: "Population pyramid style diverging stacked bars",
  icon: BarChart3,
  category: "single-view-plots",
  subcategory: "bar-charts",
  spec: {
    $schema: "https://vega.github.io/schema/vega-lite/v5.json",
    description: "Diverging stacked bar chart.",
    data: {
      values: [
        { age: "0-19", male: -40, female: 35 },
        { age: "20-39", male: -45, female: 40 },
        { age: "40-59", male: -35, female: 32 },
        { age: "60+", male: -20, female: 18 },
      ],
    },
    mark: "bar",
    encoding: {
      y: { field: "age", type: "nominal" },
      x: { field: "male", type: "quantitative" },
      color: { value: "steelblue" },
    },
  },
};
