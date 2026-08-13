import { Type } from "lucide-react";

export default {
  id: "annotated-line",
  type: "layered",
  title: "Annotated Line Chart",
  description: "Line chart with text annotations for key points",
  icon: Type,
  category: "layered-plots",
  subcategory: "labeling-annotation",
  spec: {
    $schema: "https://vega.github.io/schema/vega-lite/v5.json",
    description: "A line chart with text annotations.",
    data: {
      values: [
        { year: "2018", value: 28 },
        { year: "2019", value: 55 },
        { year: "2020", value: 43, label: "COVID-19" },
        { year: "2021", value: 91 },
        { year: "2022", value: 81 },
        { year: "2023", value: 53 },
      ],
    },
    layer: [
      {
        mark: "line",
        encoding: {
          x: { field: "year", type: "temporal" },
          y: { field: "value", type: "quantitative" },
        },
      },
      {
        mark: {
          type: "text",
          align: "left",
          dx: 5,
          dy: -5,
        },
        encoding: {
          x: { field: "year", type: "temporal" },
          y: { field: "value", type: "quantitative" },
          text: { field: "label", type: "nominal" },
        },
      },
    ],
  },
};
