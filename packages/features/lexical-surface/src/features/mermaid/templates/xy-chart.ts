import { BarChart3 } from "lucide-react";
import type { MermaidTemplate } from "./template-loader";

export default {
  id: "xy-chart-basic",
  type: "xyChart",
  title: "XY Chart",
  description: "Create scatter plots and line charts",
  icon: BarChart3,
  category: "charts",
  preview: "X-axis → Y-axis data points",
  code: `xychart-beta
    title "Monthly Sales Revenue"
    x-axis [Jan, Feb, Mar, Apr, May, Jun, Jul, Aug, Sep, Oct, Nov, Dec]
    y-axis "Revenue (in $)" 4000 --> 11000
    bar [5000, 6000, 7500, 8200, 9500, 10500, 11000, 10200, 9200, 8500, 7000, 6000]
    line [5000, 6000, 7500, 8200, 9500, 10500, 11000, 10200, 9200, 8500, 7000, 6000]`,
} as MermaidTemplate;
