import { Table } from "lucide-react";

export default {
  id: "text-table",
  type: "text",
  title: "Text Table",
  description: "Display data in a text-based table format",
  icon: Table,
  category: "single-view-plots",
  subcategory: "table-plots",
  spec: {
    $schema: "https://vega.github.io/schema/vega-lite/v5.json",
    description: "A text table showing data values.",
    data: {
      values: [
        { name: "Product A", sales: 28, profit: 12 },
        { name: "Product B", sales: 55, profit: 23 },
        { name: "Product C", sales: 43, profit: 18 },
        { name: "Product D", sales: 91, profit: 38 },
        { name: "Product E", sales: 81, profit: 32 },
      ],
    },
    mark: "text",
    encoding: {
      y: { field: "name", type: "nominal", axis: { title: "" } },
      x: {
        field: "sales",
        type: "quantitative",
        axis: { orient: "top" },
      },
      text: { field: "sales", type: "quantitative" },
    },
  },
};
