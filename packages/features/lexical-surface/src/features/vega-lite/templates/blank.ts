import { FilePlus2 } from "lucide-react";
import type { VegaLiteTemplate } from "./template-loader";

export default {
  id: "blank-chart",
  type: "custom",
  title: "Blank Chart",
  initialTitle: "",
  description: "Start with an empty Vega-Lite specification",
  icon: FilePlus2,
  category: "starter",
  subcategory: "blank",
  spec: {
  "$schema": "https://vega.github.io/schema/vega-lite/v5.json",
  "description": "",
  "data": {
    "values": []
  },
  "mark": "bar",
  "encoding": {}
},
} as VegaLiteTemplate;
