import { AreaChart } from "lucide-react";

export default {
  id: "area-chart",
  type: "area",
  title: "Area Chart",
  description: "Simple area chart for time series data",
  icon: AreaChart,
  category: "single-view-plots",
  subcategory: "area-streamgraphs",
  spec: {
    $schema: "https://vega.github.io/schema/vega-lite/v5.json",
    description: "An area chart showing values over time.",
    data: {
      values: [
        { year: "2018", value: 28 },
        { year: "2019", value: 55 },
        { year: "2020", value: 43 },
        { year: "2021", value: 91 },
        { year: "2022", value: 81 },
        { year: "2023", value: 53 },
      ],
    },
    mark: "area",
    encoding: {
      x: { field: "year", type: "temporal" },
      y: { field: "value", type: "quantitative" },
    },
  },
};
