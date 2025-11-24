"use client"
import React from "react"
import { cn } from "@/lib/utils"
import {
  AlertCircle,
  Info,
  Notebook,
  CheckCircle,
  AlertTriangle,
  Flame,
  HelpCircle,
  Skull,
  Bug,
  List,
  Quote,
  FileText,
  Zap,
  ShieldAlert,
  Bell,
  Lightbulb,
  Check,
  BookMarked,
} from "lucide-react"

export type AdmonitionType =
  | "note"
  | "abstract"
  | "info"
  | "tip"
  | "success"
  | "question"
  | "warning"
  | "failure"
  | "danger"
  | "bug"
  | "example"
  | "quote"
  | "important"
  | "caution"
  | "attention"
  | "hint"
  | "check"
  | "summary"

export interface AdmonitionProps extends Omit<React.HTMLAttributes<HTMLDivElement>, 'title' | 'content'> {
  title?: React.ReactNode
  content?: React.ReactNode
  type?: AdmonitionType
  customBorderColor?: string
  customTextColor?: string
  design?: "default" | "compact" | "bordered" | "vertical-bar"
}

const admonitionVariants = {
  note: "border-blue-500 bg-blue-900 text-blue-100",
  abstract: "border-sky-500 bg-sky-900 text-sky-100",
  info: "border-cyan-500 bg-cyan-900 text-cyan-100",
  tip: "border-lime-500 bg-lime-900 text-lime-100",
  success: "border-green-500 bg-green-900 text-green-100",
  question: "border-amber-500 bg-amber-900 text-amber-100",
  warning: "border-yellow-500 bg-yellow-900 text-yellow-100",
  failure: "border-red-500 bg-red-900 text-red-100",
  danger: "border-orange-500 bg-orange-900 text-orange-100",
  bug: "border-stone-500 bg-stone-900 text-stone-100",
  example: "border-teal-500 bg-teal-900 text-teal-100",
  quote: "border-pink-500 bg-pink-900 text-pink-100",
  important: "border-purple-500 bg-purple-900 text-purple-100",
  caution: "border-rose-500 bg-rose-900 text-rose-100",
  attention: "border-fuchsia-500 bg-fuchsia-900 text-fuchsia-100",
  hint: "border-emerald-500 bg-emerald-900 text-emerald-100",
  check: "border-indigo-500 bg-indigo-900 text-indigo-100",
  summary: "border-violet-500 bg-violet-900 text-violet-100",
}

const typeToLabel = {
  note: "Note",
  abstract: "Abstract",
  info: "Info",
  tip: "Tip",
  success: "Success",
  question: "Question",
  warning: "Warning",
  failure: "Failure",
  danger: "Danger",
  bug: "Bug",
  example: "Example",
  quote: "Quote",
  important: "Important",
  caution: "Caution",
  attention: "Attention",
  hint: "Hint",
  check: "Check",
  summary: "Summary",
}

const typeToIcon = {
  note: <Notebook className="h-5 w-5 mr-2" />,
  abstract: <FileText className="h-5 w-5 mr-2" />,
  info: <Info className="h-5 w-5 mr-2" />,
  tip: <Flame className="h-5 w-5 mr-2" />,
  success: <CheckCircle className="h-5 w-5 mr-2" />,
  question: <HelpCircle className="h-5 w-5 mr-2" />,
  warning: <AlertTriangle className="h-5 w-5 mr-2" />,
  failure: <AlertCircle className="h-5 w-5 mr-2" />,
  danger: <Skull className="h-5 w-5 mr-2" />,
  bug: <Bug className="h-5 w-5 mr-2" />,
  example: <List className="h-5 w-5 mr-2" />,
  quote: <Quote className="h-5 w-5 mr-2" />,
  important: <Zap className="h-5 w-5 mr-2" />,
  caution: <ShieldAlert className="h-5 w-5 mr-2" />,
  attention: <Bell className="h-5 w-5 mr-2" />,
  hint: <Lightbulb className="h-5 w-5 mr-2" />,
  check: <Check className="h-5 w-5 mr-2" />,
  summary: <BookMarked className="h-5 w-5 mr-2" />,
}

const typeToColor = {
  note: "#3b82f6",      // blue-500
  abstract: "#0ea5e9",  // sky-500
  info: "#06b6d4",      // cyan-500
  tip: "#84cc16",       // lime-500
  success: "#22c55e",   // green-500
  question: "#f59e0b",  // amber-500
  warning: "#eab308",   // yellow-500
  failure: "#ef4444",   // red-500
  danger: "#f97316",    // orange-500
  bug: "#78716c",       // stone-500
  example: "#14b8a6",   // teal-500
  quote: "#ec4899",     // pink-500
  important: "#a855f7", // purple-500
  caution: "#f43f5e",   // rose-500
  attention: "#d946ef", // fuchsia-500
  hint: "#10b981",      // emerald-500
  check: "#6366f1",     // indigo-500
  summary: "#8b5cf6",   // violet-500
}

export function Admonition({ 
  className, 
  type = "note", 
  title, 
  content, 
  customBorderColor, 
  customTextColor,
  design = "default",
  ...props 
}: AdmonitionProps) {
  const baseStyle = customBorderColor 
    ? "" 
    : admonitionVariants[type]
  
  // Design "default" - estilo atual com fundo colorido
  if (design === "default") {
    return (
      <div 
        className={cn("rounded-md border p-4 text-sm", baseStyle, className)} 
        style={customBorderColor ? { 
          borderColor: customBorderColor,
          backgroundColor: `${customBorderColor}15`,
        } : undefined}
        {...props}
      >
        <div 
          className="font-medium flex items-center gap-1.5"
          style={customTextColor ? { color: customTextColor } : undefined}
        >
          {typeToIcon[type]}
          {title || typeToLabel[type]}
        </div>
        {content && <div className="mt-2 bg-background rounded-md p-2 text-foreground">{content}</div>}
      </div>
    )
  }
  
  // Design "compact" - estilo compacto com borda lateral (primeiro exemplo da imagem)
  if (design === "compact") {
    const color = customBorderColor || typeToColor[type]
    return (
      <div 
        className={cn("rounded-md border-l-4 p-3 text-sm", className)}
        style={{ 
          borderLeftColor: color,
          backgroundColor: `${color}40`, // 40% opacity for light background
        }}
        {...props}
      >
        <div 
          className="font-semibold flex items-center gap-2 mb-1"
        >
          <div style={{ color }}>
            {typeToIcon[type]}
          </div>
          <span 
            className={!customTextColor ? "text-gray-900 dark:text-white" : ""}
            style={customTextColor ? { color: customTextColor } : undefined}
          >
            {title || typeToLabel[type]}
          </span>
        </div>
        {content && (
          <div 
            className={!customTextColor ? "text-gray-800/90 dark:text-white/90 ml-7" : "ml-7"}
            style={customTextColor ? { color: customTextColor } : undefined}
          >
            {content}
          </div>
        )}
      </div>
    )
  }
  
  // Design "bordered" - estilo com borda completa (segundo exemplo da imagem)
  if (design === "bordered") {
    const color = customBorderColor || typeToColor[type]
    return (
      <div 
        className={cn(
          "rounded-md border-2 bg-background p-4 text-sm",
          className
        )}
        style={{ 
          borderColor: `${color}4d`, // 30% opacity
          backgroundColor: customBorderColor ? `${customBorderColor}0d` : undefined, // 5% opacity
        }}
        {...props}
      >
        <div 
          className="font-semibold flex items-center gap-2 mb-2"
          style={{ color: customTextColor || color }}
        >
          <div style={{ color }}>
            {typeToIcon[type]}
          </div>
          <span>{title || typeToLabel[type]}</span>
        </div>
        {content && <div className="text-foreground/90">{content}</div>}
      </div>
    )
  }
  
  // Design "vertical-bar" - estilo com barra vertical no lado esquerdo (terceiro exemplo)
  if (design === "vertical-bar") {
    const color = customBorderColor || typeToColor[type]
    return (
      <div 
        className={cn(
          "relative bg-muted/40 dark:bg-muted/20 pl-4 pr-4 py-3 text-sm overflow-hidden",
          className
        )}
        {...props}
      >
        <div 
          className="absolute left-0 top-0 bottom-0 w-1"
          style={{ backgroundColor: color }}
        />
        <div 
          className="font-semibold flex items-center gap-2 mb-1.5"
          style={{ color: customTextColor || color }}
        >
          <div style={{ color }}>
            {typeToIcon[type]}
          </div>
          <span>{title || typeToLabel[type]}</span>
        </div>
        {content && <div className="text-foreground/80">{content}</div>}
      </div>
    )
  }
    
  // Fallback para default
  return (
    <div 
      className={cn("rounded-md border p-4 text-sm", baseStyle, className)} 
      style={customBorderColor ? { 
        borderColor: customBorderColor,
        backgroundColor: `${customBorderColor}15`,
      } : undefined}
      {...props}
    >
      <div 
        className="font-medium flex items-center gap-1.5"
        style={customTextColor ? { color: customTextColor } : undefined}
      >
        {typeToIcon[type]}
        {title || typeToLabel[type]}
      </div>
      {content && <div className="mt-2 bg-background rounded-md p-2 text-foreground">{content}</div>}
    </div>
  )
}
