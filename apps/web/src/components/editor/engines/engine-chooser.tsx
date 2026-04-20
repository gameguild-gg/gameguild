"use client"

import { FileText, LayoutGrid } from "lucide-react"
import { ENGINE_TYPES, type EngineType } from "@/lib/storage/editor/project-types"

interface EngineOption {
  engine: EngineType
  name: string
  description: string
  features: string[]
  icon: React.ReactNode
}

const ENGINE_OPTIONS: EngineOption[] = [
  {
    engine: ENGINE_TYPES.LEXICAL,
    name: "Rich Text",
    description: "A powerful rich-text editor for free-form content creation.",
    features: ["Text formatting & styles", "Media embeds & images", "Tables & code blocks", "Free-form layout"],
    icon: <FileText className="w-8 h-8" />,
  },
  {
    engine: ENGINE_TYPES.BLOCKS,
    name: "Blocks",
    description: "A structured block-based editor for modular content.",
    features: ["Drag & drop blocks", "Modular structure", "Custom block types", "Template support"],
    icon: <LayoutGrid className="w-8 h-8" />,
  },
]

interface EngineChooserProps {
  engines: EngineType[]
  onChoose: (engine: EngineType) => void
}

export function EngineChooser({ engines, onChoose }: EngineChooserProps) {
  const options = ENGINE_OPTIONS.filter((o) => engines.includes(o.engine))

  return (
    <div className="flex flex-col items-center justify-center min-h-[420px] gap-8 p-8">
      <div className="flex flex-col items-center gap-2">
        <h2 className="text-2xl font-bold tracking-tight text-foreground">
          Choose an editor engine
        </h2>
        <p className="text-sm text-muted-foreground text-center max-w-lg">
          Select how you want to create your content. You can switch later by creating a new project.
        </p>
      </div>

      <div className="grid grid-cols-1 sm:grid-cols-2 gap-6 w-full max-w-2xl">
        {options.map((option) => (
          <button
            key={option.engine}
            onClick={() => onChoose(option.engine)}
            className="group relative flex flex-col items-start gap-4 p-6 rounded-2xl border border-border bg-card text-left transition-all duration-200 hover:border-primary hover:shadow-lg hover:shadow-primary/5 hover:-translate-y-0.5 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
          >
            <div className="flex items-center gap-3 w-full">
              <div className="flex items-center justify-center w-12 h-12 rounded-xl bg-muted text-muted-foreground group-hover:bg-primary/10 group-hover:text-primary transition-colors">
                {option.icon}
              </div>
              <div className="flex-1">
                <h3 className="text-lg font-semibold text-foreground">
                  {option.name}
                </h3>
                <p className="text-xs text-muted-foreground">
                  {option.description}
                </p>
              </div>
            </div>

            <ul className="flex flex-col gap-1.5 w-full pl-1">
              {option.features.map((feature) => (
                <li key={feature} className="flex items-center gap-2 text-xs text-muted-foreground">
                  <span className="w-1 h-1 rounded-full bg-muted-foreground/50 shrink-0" />
                  {feature}
                </li>
              ))}
            </ul>

            <div className="mt-auto pt-2 w-full">
              <span className="inline-flex items-center justify-center w-full rounded-lg border border-border bg-muted/50 px-4 py-2 text-sm font-medium text-muted-foreground group-hover:bg-primary group-hover:text-primary-foreground group-hover:border-primary transition-colors">
                Get started
              </span>
            </div>
          </button>
        ))}
      </div>
    </div>
  )
}
