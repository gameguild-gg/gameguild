import { Activity } from "lucide-react";

export default {
  id: "histogram",
  type: "histogram",
  title: "Histogram",
  description: "Distribution histogram with binning",
  icon: Activity,
  category: "single-view-plots",
  subcategory: "histograms-density-plots",
  spec: {
    $schema: "https://vega.github.io/schema/vega-lite/v5.json",
    description: "A histogram of a distribution.",
    data: {
      values: [
        { value: 1 },
        { value: 2 },
        { value: 3 },
        { value: 2 },
        { value: 4 },
        { value: 3 },
        { value: 5 },
        { value: 4 },
        { value: 6 },
        { value: 5 },
        { value: 7 },
        { value: 6 },
        { value: 8 },
        { value: 7 },
        { value: 9 },
      ],
    },
    mark: "bar",
    encoding: {
      x: { bin: true, field: "value", type: "quantitative" },
      y: { aggregate: "count", type: "quantitative" },
    },
  },
};
