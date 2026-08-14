import { AreaChart } from "lucide-react";

export default {
  id: "stacked-area",
  type: "area",
  title: "Stacked Area Chart",
  description: "Stacked area chart showing multiple series over time",
  icon: AreaChart,
  category: "single-view-plots",
  subcategory: "area-streamgraphs",
  spec: {
    $schema: "https://vega.github.io/schema/vega-lite/v5.json",
    description: "A stacked area chart.",
    data: {
      values: [
        { year: "2018", category: "A", value: 28 },
        { year: "2018", category: "B", value: 55 },
        { year: "2019", category: "A", value: 43 },
        { year: "2019", category: "B", value: 61 },
        { year: "2020", category: "A", value: 81 },
        { year: "2020", category: "B", value: 73 },
        { year: "2021", category: "A", value: 53 },
        { year: "2021", category: "B", value: 89 },
        { year: "2022", category: "A", value: 19 },
        { year: "2022", category: "B", value: 87 },
      ],
    },
    mark: "area",
    encoding: {
      x: { field: "year", type: "temporal" },
      y: { field: "value", type: "quantitative", stack: "zero" },
      color: { field: "category", type: "nominal" },
    },
  },
};
