import { Workflow } from "lucide-react";
import type { MermaidTemplate } from "./template-loader";

export default {
  id: "sankey-basic",
  type: "sankey",
  title: "Sankey Diagram",
  description: "Show flow quantities between stages",
  icon: Workflow,
  category: "flows",
  preview: "Source → Flow → Destination",
  code: `sankey-beta
    Agricultural Waste, Bio-conversion, 124.729
    Bio-conversion, Liquid, 0.597
    Bio-conversion, Losses, 26.862
    Bio-conversion, Solid, 280.322
    Bio-conversion, Gas, 81.144
    Biofuel Imports, Liquid, 35
    Biomass Imports, Solid, 35
    Coal Imports, Coal, 11.606
    Coal Reserves, Coal, 63.965
    Coal, Solid, 75.571
    District Heating, Industry, 10.639
    District Heating, Heating and Cooling - Commercial, 22.505
    District Heating, Heating and Cooling - Homes, 46.184`,
} as MermaidTemplate;
