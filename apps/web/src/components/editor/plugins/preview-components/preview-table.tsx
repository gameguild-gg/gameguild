"use client"

import { Table, TableHeader, TableBody, TableHead, TableRow, TableCell } from "@/components/ui/table"
import { cn } from "@/lib/utils"
import type { TableData, TableStyle } from "@/components/editor/nodes/table-node"

interface PreviewTableProps {
  node: {
    type: "table"
    data: TableData
  }
}

export function PreviewTable({ node }: PreviewTableProps) {
  const data = node.data

  // Ensure data has valid cells
  const safeData = {
    ...data,
    cells: data.cells && data.cells.length > 0 ? data.cells : (() => {
      const cells = []
      for (let i = 0; i < data.rows; i++) {
        const row = []
        for (let j = 0; j < data.columns; j++) {
          row.push({
            content: i === 0 && data.showHeader ? `Header ${j + 1}` : `Cell ${i + 1}-${j + 1}`,
            isHeader: i === 0 && data.showHeader,
          })
        }
        cells.push(row)
      }
      return cells
    })()
  }

  const getTableStyleClasses = (style: TableStyle) => {
    switch (style) {
      case "striped":
        return cn(
          "border-collapse border border-gray-300 dark:border-gray-600",
          "[&_tr:nth-child(even)]:bg-gray-50 dark:[&_tr:nth-child(even)]:bg-gray-800/30",
          "[&_tr:nth-child(odd)]:bg-white dark:[&_tr:nth-child(odd)]:bg-gray-900",
          "[&_th]:bg-gray-100 dark:[&_th]:bg-gray-700 [&_th]:font-semibold [&_th]:text-gray-900 dark:[&_th]:text-gray-100",
          "[&_th]:border-b-2 [&_th]:border-gray-400 dark:[&_th]:border-gray-500",
          "[&_td]:border-b [&_td]:border-gray-200 dark:[&_td]:border-gray-700",
          "shadow-sm"
        )
      case "bordered":
        return cn(
          "border-collapse border-2 border-gray-400 dark:border-gray-500",
          "[&_th]:border-2 [&_th]:border-gray-400 dark:[&_th]:border-gray-500",
          "[&_td]:border [&_td]:border-gray-300 dark:[&_td]:border-gray-600",
          "[&_th]:bg-gray-200 dark:[&_th]:bg-gray-700 [&_th]:font-bold [&_th]:text-gray-900 dark:[&_th]:text-gray-100",
          "[&_td]:bg-white dark:[&_td]:bg-gray-900",
          "shadow-lg"
        )
      case "minimal":
        return cn(
          "border-collapse",
          "[&_th]:border-b-2 [&_th]:border-gray-300 dark:[&_th]:border-gray-600",
          "[&_th]:font-semibold [&_th]:text-gray-800 dark:[&_th]:text-gray-200",
          "[&_td]:border-b [&_td]:border-gray-100 dark:[&_td]:border-gray-800",
          "[&_tr:hover]:bg-gray-50 dark:[&_tr:hover]:bg-gray-800/50",
          "text-gray-700 dark:text-gray-300"
        )
      case "modern":
        return cn(
          "border-collapse shadow-lg rounded-lg overflow-hidden",
          "[&_th]:bg-gradient-to-r [&_th]:from-blue-600 [&_th]:to-blue-700 dark:[&_th]:from-blue-700 dark:[&_th]:to-blue-800",
          "[&_th]:text-white [&_th]:font-bold [&_th]:text-sm [&_th]:uppercase [&_th]:tracking-wider",
          "[&_th]:px-6 [&_th]:py-4",
          "[&_td]:bg-white dark:[&_td]:bg-gray-900 [&_td]:px-6 [&_td]:py-4",
          "[&_tr:nth-child(even)]:bg-gray-50 dark:[&_tr:nth-child(even)]:bg-gray-800/50",
          "[&_tr:hover]:bg-blue-50 dark:[&_tr:hover]:bg-blue-900/20 [&_tr:hover]:transition-colors",
          "text-gray-700 dark:text-gray-300"
        )
      case "grid":
        return cn(
          "border-collapse border border-gray-400 dark:border-gray-500",
          "[&_th]:border [&_th]:border-gray-400 dark:[&_th]:border-gray-500",
          "[&_td]:border [&_td]:border-gray-400 dark:[&_td]:border-gray-500",
          "[&_th]:bg-gray-300 dark:[&_th]:bg-gray-600 [&_th]:font-bold [&_th]:text-gray-900 dark:[&_th]:text-gray-100",
          "[&_td]:bg-white dark:[&_td]:bg-gray-900",
          "[&_th]:px-4 [&_th]:py-3 [&_td]:px-4 [&_td]:py-3",
          "shadow-md"
        )
      case "accent":
        return cn(
          "border-collapse shadow-md rounded-lg overflow-hidden",
          "[&_th]:bg-gradient-to-r [&_th]:from-emerald-500 [&_th]:to-emerald-600 dark:[&_th]:from-emerald-600 dark:[&_th]:to-emerald-700",
          "[&_th]:text-white [&_th]:font-semibold",
          "[&_th]:px-4 [&_th]:py-3",
          "[&_td]:bg-white dark:[&_td]:bg-gray-900 [&_td]:px-4 [&_td]:py-3",
          "[&_tr:nth-child(even)]:bg-emerald-50 dark:[&_tr:nth-child(even)]:bg-emerald-900/10",
          "[&_tr:hover]:bg-emerald-100 dark:[&_tr:hover]:bg-emerald-900/20",
          "[&_td]:border-b [&_td]:border-emerald-100 dark:[&_td]:border-emerald-800",
          "text-gray-700 dark:text-gray-300"
        )
      case "dark":
        return cn(
          "border-collapse shadow-xl rounded-lg overflow-hidden",
          "[&_th]:bg-gray-800 dark:[&_th]:bg-gray-900",
          "[&_th]:text-gray-100 [&_th]:font-bold [&_th]:text-sm [&_th]:uppercase [&_th]:tracking-wide",
          "[&_th]:px-6 [&_th]:py-4 [&_th]:border-b [&_th]:border-gray-600",
          "[&_td]:bg-gray-700 dark:[&_td]:bg-gray-800 [&_td]:text-gray-100",
          "[&_td]:px-6 [&_td]:py-4 [&_td]:border-b [&_td]:border-gray-600 dark:[&_td]:border-gray-700",
          "[&_tr:hover]:bg-gray-600 dark:[&_tr:hover]:bg-gray-700"
        )
      case "colorful":
        return cn(
          "border-collapse shadow-lg rounded-lg overflow-hidden",
          "[&_th]:bg-gradient-to-r [&_th]:from-purple-500 [&_th]:via-pink-500 [&_th]:to-red-500",
          "[&_th]:text-white [&_th]:font-bold [&_th]:text-center",
          "[&_th]:px-4 [&_th]:py-3",
          "[&_td]:px-4 [&_td]:py-3",
          "[&_tbody_tr:nth-child(odd)]:bg-red-50 dark:[&_tbody_tr:nth-child(odd)]:bg-red-900/10",
          "[&_tbody_tr:nth-child(even)]:bg-blue-50 dark:[&_tbody_tr:nth-child(even)]:bg-blue-900/10", 
          "[&_tr:hover]:bg-purple-50 dark:[&_tr:hover]:bg-purple-900/20",
          "text-gray-700 dark:text-gray-300"
        )
      case "professional":
        return cn(
          "border-collapse shadow-md",
          "[&_th]:bg-slate-700 dark:[&_th]:bg-slate-800",
          "[&_th]:text-white [&_th]:font-semibold [&_th]:text-sm [&_th]:tracking-wide",
          "[&_th]:px-6 [&_th]:py-4 [&_th]:text-left",
          "[&_th]:border-b-2 [&_th]:border-slate-500",
          "[&_td]:bg-white dark:[&_td]:bg-gray-900 [&_td]:px-6 [&_td]:py-4",
          "[&_td]:border-b [&_td]:border-slate-200 dark:[&_td]:border-slate-700",
          "[&_tr:hover]:bg-slate-50 dark:[&_tr:hover]:bg-slate-800/30",
          "text-slate-700 dark:text-slate-300",
          "rounded-lg overflow-hidden"
        )
      default:
        return cn(
          "border-collapse",
          "[&_th]:border-b [&_th]:border-gray-200 dark:[&_th]:border-gray-700",
          "[&_th]:font-medium [&_th]:text-left",
          "[&_td]:border-b [&_td]:border-gray-200 dark:[&_td]:border-gray-700",
          "text-gray-900 dark:text-gray-100"
        )
    }
  }

  return (
    <div className="table-node my-4 max-w-full">
      {safeData.caption && (
        <div className="mb-2">
          <span className="text-sm text-muted-foreground">- {safeData.caption}</span>
        </div>
      )}
      
      <div className="overflow-x-auto">
        <Table className={cn(
          getTableStyleClasses(safeData.style),
          !safeData.showBorders && "[&_th]:border-0 [&_td]:border-0",
          "w-full"
        )}>
          {safeData.caption && <caption className="text-sm text-muted-foreground mb-2">{safeData.caption}</caption>}
          {safeData.showHeader && (
            <TableHeader>
              <TableRow>
                {safeData.cells[0]?.map((cell, colIndex) => (
                  <TableHead key={colIndex}>
                    {cell.content}
                  </TableHead>
                ))}
              </TableRow>
            </TableHeader>
          )}
          <TableBody>
            {safeData.cells.slice(safeData.showHeader ? 1 : 0).map((row, rowIndex) => (
              <TableRow key={rowIndex}>
                {row.map((cell, colIndex) => (
                  <TableCell key={colIndex}>
                    {cell.content}
                  </TableCell>
                ))}
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </div>
    </div>
  )
}
