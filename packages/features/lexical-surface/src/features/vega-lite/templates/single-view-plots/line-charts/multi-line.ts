import { LineChart } from "lucide-react";

export default {
  id: "multi-line",
  type: "line",
  title: "Multi-Line Chart",
  description: "Multiple time series lines on the same chart",
  icon: LineChart,
  category: "single-view-plots",
  subcategory: "line-charts",
  spec: {
    $schema: "https://vega.github.io/schema/vega-lite/v5.json",
    description: "A multi-series line chart.",
    data: {
      values: [
        { year: "2018", series: "A", value: 28 },
        { year: "2018", series: "B", value: 55 },
        { year: "2019", series: "A", value: 43 },
        { year: "2019", series: "B", value: 91 },
        { year: "2020", series: "A", value: 81 },
        { year: "2020", series: "B", value: 53 },
        { year: "2021", series: "A", value: 19 },
        { year: "2021", series: "B", value: 87 },
        { year: "2022", series: "A", value: 52 },
        { year: "2022", series: "B", value: 48 },
      ],
    },
    mark: "line",
    encoding: {
      x: { field: "year", type: "temporal" },
      y: { field: "value", type: "quantitative" },
      color: { field: "series", type: "nominal" },
    },
  },
};
