"use client"
import React from "react"
import { cn } from "@/lib/utils"
import {
  AlertCircle,
  Info,
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

export interface AdmonitionProps extends React.HTMLAttributes<HTMLDivElement> {
  title?: React.ReactNode
  content?: React.ReactNode
  type?: AdmonitionType
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
  note: <Info className="h-5 w-5 mr-2" />,
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

export function Admonition({ className, type = "note", title, content, ...props }: AdmonitionProps) {
  return (
    <div className={cn("rounded-md border p-4 text-sm", admonitionVariants[type], className)} {...props}>
      <div className="font-medium flex items-center gap-1.5">
        {typeToIcon[type]}
        {title || typeToLabel[type]}
      </div>
      {content && <div className="mt-2 bg-background rounded-md p-2 text-foreground">{content}</div>}
    </div>
  )
}
