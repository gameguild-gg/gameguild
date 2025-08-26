import { Card, CardHeader, CardTitle, CardContent } from "@/components/ui/card"
import type { LucideIcon } from "lucide-react"
import * as React from "react"

export function StatCard({
  title,
  value,
  icon: Icon,
  subtext,
  trend,
}: {
  title: string
  value: string
  icon: LucideIcon
  subtext?: React.ReactNode
  trend?: string
}) {
  return (
    <Card className="dark-card">
      <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
        <CardTitle className="text-sm font-medium text-muted-foreground">{title}</CardTitle>
        <Icon className="h-4 w-4 text-muted-foreground" />
      </CardHeader>
      <CardContent>
        <div className="flex items-baseline gap-2">
          <div className="text-2xl font-bold capitalize text-foreground">{value}</div>
          {trend && <span className="text-xs text-emerald-400 font-medium">{trend}</span>}
        </div>
        {subtext && <div className="mt-2">{subtext}</div>}
      </CardContent>
    </Card>
  )
}
