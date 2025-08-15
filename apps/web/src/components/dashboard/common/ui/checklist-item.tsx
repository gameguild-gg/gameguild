import { CheckCircle2, AlertCircle } from "lucide-react"
import { cn } from "@/lib/utils"

export function ChecklistItem({ label, done }: { label: string; done: boolean }) {
  return (
    <div className="flex items-center gap-3 p-3 rounded-lg bg-muted/10 border border-border/30">
      {done ? (
        <CheckCircle2 className="h-5 w-5 text-emerald-400 flex-shrink-0" />
      ) : (
        <AlertCircle className="h-5 w-5 text-amber-400 flex-shrink-0" />
      )}
      <span className={cn("transition-colors", done ? "text-foreground" : "text-muted-foreground")}>{label}</span>
    </div>
  )
}
