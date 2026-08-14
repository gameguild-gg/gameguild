import { FilePlus2 } from "lucide-react";
import type { VegaLiteTemplate } from "./template-loader";

export default {
  id: "blank-chart",
  type: "custom",
  title: "Blank Chart",
  description: "Start with an empty Vega-Lite specification",
  icon: FilePlus2,
  category: "starter",
  subcategory: "blank",
  spec: {},
} as VegaLiteTemplate;
