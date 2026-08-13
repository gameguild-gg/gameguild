export interface MermaidData {
  code: string
  type:
    | "flowchart"
    | "class"
    | "sequence"
    | "xyChart"
    | "radar"
    | "quadrant"
    | "sankey"
    | "state"
    | "c4context"
    | "architecture"
    | "er"
    | "gantt"
    | "pie"
    | "gitgraph"
    | "mindmap"
    | "journey"
    | "timeline"
    | "quadrantChart"
    | "requirement"
    | "c4Context"
    | "c4Container"
    | "c4Component"
    | "c4Dynamic"
    | "c4Deployment"
    | "treemap-beta"
    | "kanban"
  direction?: "TD" | "TB" | "BT" | "RL"
  theme?:
    | "default"
    | "dark"
    | "forest"
    | "neutral"
    | "base"
    | "default-dark"
    | "forest-dark"
    | "neutral-dark"
    | "base-dark"
  themeMode?: "system" | "light" | "dark" | "both"
  fontFamily?: string
  title?: string
  caption?: string
  size?: number
}
