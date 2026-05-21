"use client"

import { useState } from "react"
import { Check, Plus, Minus, Grid3X3, Save } from "lucide-react"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Table, TableHeader, TableBody, TableHead, TableRow, TableCell } from "@/components/ui/table"
import { Label } from "@/components/ui/label"
import { Switch } from "@/components/ui/switch"
import { cn } from "@/lib/utils"
import { useEditorSettings } from "@/components/block-content-editor/extras/settings-menu"
import { BlockEditorShell } from "@/components/block-content-editor/extras/block-editor-shell"
import type { TableData, TableStyle, TableCellData } from "@/components/block-content-editor/nodes/table-node"

interface TableEditorProps {
  initialData?: TableData
  onSave: (data: TableData) => void
  onCancel: () => void
}

function ensureCells(data: { rows: number; columns: number; showHeader: boolean; cells?: TableCellData[][] }): TableCellData[][] {
  if (data.cells && data.cells.length > 0) return data.cells
  const cells: TableCellData[][] = []
  for (let i = 0; i < data.rows; i++) {
    const row: TableCellData[] = []
    for (let j = 0; j < data.columns; j++) {
      row.push({
        content: i === 0 && data.showHeader ? `Header ${j + 1}` : `Cell ${i + 1}-${j + 1}`,
        isHeader: i === 0 && data.showHeader,
      })
    }
    cells.push(row)
  }
  return cells
}

function getTableStyleClasses(style: TableStyle) {
  const baseClasses = "[&_th]:truncate [&_td]:truncate [&_th]:max-w-0 [&_td]:max-w-0"
  switch (style) {
    case "striped":
      return cn(baseClasses, "border-collapse border border-gray-300 dark:border-gray-600", "[&_tr:nth-child(even)]:bg-gray-50 dark:[&_tr:nth-child(even)]:bg-gray-800/30", "[&_tr:nth-child(odd)]:bg-white dark:[&_tr:nth-child(odd)]:bg-gray-900", "[&_th]:bg-gray-100 dark:[&_th]:bg-gray-700 [&_th]:font-semibold [&_th]:text-gray-900 dark:[&_th]:text-gray-100", "[&_th]:border-b-2 [&_th]:border-gray-400 dark:[&_th]:border-gray-500", "[&_td]:border-b [&_td]:border-gray-200 dark:[&_td]:border-gray-700", "shadow-sm")
    case "bordered":
      return cn(baseClasses, "border-collapse border-2 border-gray-400 dark:border-gray-500", "[&_th]:border-2 [&_th]:border-gray-400 dark:[&_th]:border-gray-500", "[&_td]:border [&_td]:border-gray-300 dark:[&_td]:border-gray-600", "[&_th]:bg-gray-200 dark:[&_th]:bg-gray-700 [&_th]:font-bold [&_th]:text-gray-900 dark:[&_th]:text-gray-100", "[&_td]:bg-white dark:[&_td]:bg-gray-900", "shadow-lg")
    case "minimal":
      return cn(baseClasses, "border-collapse", "[&_th]:border-b-2 [&_th]:border-gray-300 dark:[&_th]:border-gray-600", "[&_th]:font-semibold [&_th]:text-gray-800 dark:[&_th]:text-gray-200", "[&_td]:border-b [&_td]:border-gray-100 dark:[&_td]:border-gray-800", "[&_tr:hover]:bg-gray-50 dark:[&_tr:hover]:bg-gray-800/50", "text-gray-700 dark:text-gray-300")
    case "modern":
      return cn(baseClasses, "border-collapse shadow-lg rounded-lg overflow-hidden", "[&_th]:bg-gradient-to-r [&_th]:from-blue-600 [&_th]:to-blue-700 dark:[&_th]:from-blue-700 dark:[&_th]:to-blue-800", "[&_th]:text-white [&_th]:font-bold [&_th]:text-sm [&_th]:uppercase [&_th]:tracking-wider", "[&_th]:px-6 [&_th]:py-4", "[&_td]:bg-white dark:[&_td]:bg-gray-900 [&_td]:px-6 [&_td]:py-4", "[&_tr:nth-child(even)]:bg-gray-50 dark:[&_tr:nth-child(even)]:bg-gray-800/50", "[&_tr:hover]:bg-blue-50 dark:[&_tr:hover]:bg-blue-900/20 [&_tr:hover]:transition-colors", "text-gray-700 dark:text-gray-300")
    case "grid":
      return cn(baseClasses, "border-collapse border border-gray-400 dark:border-gray-500", "[&_th]:border [&_th]:border-gray-400 dark:[&_th]:border-gray-500", "[&_td]:border [&_td]:border-gray-400 dark:[&_td]:border-gray-500", "[&_th]:bg-gray-300 dark:[&_th]:bg-gray-600 [&_th]:font-bold [&_th]:text-gray-900 dark:[&_th]:text-gray-100", "[&_td]:bg-white dark:[&_td]:bg-gray-900", "[&_th]:px-4 [&_th]:py-3 [&_td]:px-4 [&_td]:py-3", "shadow-md")
    case "accent":
      return cn(baseClasses, "border-collapse shadow-md rounded-lg overflow-hidden", "[&_th]:bg-gradient-to-r [&_th]:from-emerald-500 [&_th]:to-emerald-600 dark:[&_th]:from-emerald-600 dark:[&_th]:to-emerald-700", "[&_th]:text-white [&_th]:font-semibold", "[&_th]:px-4 [&_th]:py-3", "[&_td]:bg-white dark:[&_td]:bg-gray-900 [&_td]:px-4 [&_td]:py-3", "[&_tr:nth-child(even)]:bg-emerald-50 dark:[&_tr:nth-child(even)]:bg-emerald-900/10", "[&_tr:hover]:bg-emerald-100 dark:[&_tr:hover]:bg-emerald-900/20", "[&_td]:border-b [&_td]:border-emerald-100 dark:[&_td]:border-emerald-800", "text-gray-700 dark:text-gray-300")
    case "dark":
      return cn(baseClasses, "border-collapse shadow-xl rounded-lg overflow-hidden", "[&_th]:bg-gray-800 dark:[&_th]:bg-gray-900", "[&_th]:text-gray-100 [&_th]:font-bold [&_th]:text-sm [&_th]:uppercase [&_th]:tracking-wide", "[&_th]:px-6 [&_th]:py-4 [&_th]:border-b [&_th]:border-gray-600", "[&_td]:bg-gray-700 dark:[&_td]:bg-gray-800 [&_td]:text-gray-100", "[&_td]:px-6 [&_td]:py-4 [&_td]:border-b [&_td]:border-gray-600 dark:[&_td]:border-gray-700", "[&_tr:hover]:bg-gray-600 dark:[&_tr:hover]:bg-gray-700")
    case "colorful":
      return cn(baseClasses, "border-collapse shadow-lg rounded-lg overflow-hidden", "[&_th]:bg-gradient-to-r [&_th]:from-purple-500 [&_th]:via-pink-500 [&_th]:to-red-500", "[&_th]:text-white [&_th]:font-bold [&_th]:text-center", "[&_th]:px-4 [&_th]:py-3", "[&_td]:px-4 [&_td]:py-3", "[&_tbody_tr:nth-child(odd)]:bg-red-50 dark:[&_tbody_tr:nth-child(odd)]:bg-red-900/10", "[&_tbody_tr:nth-child(even)]:bg-blue-50 dark:[&_tbody_tr:nth-child(even)]:bg-blue-900/10", "[&_tr:hover]:bg-purple-50 dark:[&_tr:hover]:bg-purple-900/20", "text-gray-700 dark:text-gray-300")
    case "professional":
      return cn(baseClasses, "border-collapse shadow-md", "[&_th]:bg-slate-700 dark:[&_th]:bg-slate-800", "[&_th]:text-white [&_th]:font-semibold [&_th]:text-sm [&_th]:tracking-wide", "[&_th]:px-6 [&_th]:py-4 [&_th]:text-left", "[&_th]:border-b-2 [&_th]:border-slate-500", "[&_td]:bg-white dark:[&_td]:bg-gray-900 [&_td]:px-6 [&_td]:py-4", "[&_td]:border-b [&_td]:border-slate-200 dark:[&_td]:border-slate-700", "[&_tr:hover]:bg-slate-50 dark:[&_tr:hover]:bg-slate-800/30", "text-slate-700 dark:text-slate-300", "rounded-lg overflow-hidden")
    default:
      return cn(baseClasses, "border-collapse", "[&_th]:border-b [&_th]:border-gray-200 dark:[&_th]:border-gray-700", "[&_th]:font-medium [&_th]:text-left", "[&_td]:border-b [&_td]:border-gray-200 dark:[&_td]:border-gray-700", "text-gray-900 dark:text-gray-100")
  }
}

const STYLE_OPTIONS: { value: TableStyle; name: string; preview: string }[] = [
  { value: "default", name: "Plain Table", preview: "Simple clean design" },
  { value: "striped", name: "Table Striped", preview: "Alternating row colors" },
  { value: "bordered", name: "Table Grid", preview: "Complete border grid" },
  { value: "minimal", name: "Table List", preview: "Minimal lines design" },
  { value: "modern", name: "Table Modern", preview: "Blue professional style" },
  { value: "grid", name: "Table Grid Lines", preview: "Bold grid pattern" },
  { value: "accent", name: "Table Green", preview: "Green accent theme" },
  { value: "dark", name: "Table Dark", preview: "Dark mode design" },
  { value: "colorful", name: "Table Colorful", preview: "Vibrant row colors" },
  { value: "professional", name: "Table Professional", preview: "Corporate style" },
]

export function TableEditor({ initialData, onSave, onCancel }: TableEditorProps) {
  const [tempData, setTempData] = useState<TableData>(() => {
    const base: TableData = initialData ?? {
      rows: 3,
      columns: 3,
      style: "default",
      showHeader: true,
      showBorders: true,
      cells: [],
      caption: "",
    }
    return { ...base, cells: ensureCells(base) }
  })
  const settings = useEditorSettings("table")

  const handleSave = () => {
    const newCells: TableCellData[][] = []
    for (let i = 0; i < tempData.rows; i++) {
      const row: TableCellData[] = []
      for (let j = 0; j < tempData.columns; j++) {
        if (tempData.cells[i]?.[j]) {
          row.push({ content: tempData.cells[i]![j]!.content, isHeader: i === 0 && tempData.showHeader })
        } else {
          row.push({ content: i === 0 && tempData.showHeader ? `Header ${j + 1}` : `Cell ${i + 1}-${j + 1}`, isHeader: i === 0 && tempData.showHeader })
        }
      }
      newCells.push(row)
    }
    onSave({ ...tempData, cells: newCells, isNew: false })
  }

  const updateCellContent = (rowIndex: number, colIndex: number, content: string) => {
    const newCells = [...tempData.cells]
    if (!newCells[rowIndex]) newCells[rowIndex] = []
    newCells[rowIndex]![colIndex] = { content, isHeader: newCells[rowIndex]![colIndex]?.isHeader || false }
    setTempData({ ...tempData, cells: newCells })
  }

  const addRow = () => {
    const newRow: TableCellData[] = []
    for (let j = 0; j < tempData.columns; j++) {
      newRow.push({ content: `Cell ${tempData.rows + 1}-${j + 1}`, isHeader: false })
    }
    setTempData({ ...tempData, rows: tempData.rows + 1, cells: [...tempData.cells, newRow] })
  }

  const removeRow = () => {
    if (tempData.rows > 1) {
      setTempData({ ...tempData, rows: tempData.rows - 1, cells: tempData.cells.slice(0, -1) })
    }
  }

  const addColumn = () => {
    const newCells = tempData.cells.map((row, i) => [
      ...row,
      { content: i === 0 && tempData.showHeader ? `Header ${tempData.columns + 1}` : `Cell ${i + 1}-${tempData.columns + 1}`, isHeader: i === 0 && tempData.showHeader },
    ])
    setTempData({ ...tempData, columns: tempData.columns + 1, cells: newCells })
  }

  const removeColumn = () => {
    if (tempData.columns > 1) {
      setTempData({ ...tempData, columns: tempData.columns - 1, cells: tempData.cells.map((row) => row.slice(0, -1)) })
    }
  }

  const safeCells = ensureCells(tempData)

  return (
    <BlockEditorShell
      settings={settings}
      includeMonacoTheme={false}
      onClose={onCancel}
      icon={<Grid3X3 className="h-5 w-5 text-blue-600 dark:text-blue-400" />}
      title="Table Editor"
      footer={
        <div className="flex gap-2 justify-end">
          <Button variant="outline" onClick={onCancel} className="border-gray-300 dark:border-gray-600 hover:bg-gray-50 dark:hover:bg-gray-800 bg-transparent">
            Cancel
          </Button>
          <Button onClick={handleSave} className="flex items-center gap-2 bg-blue-600 hover:bg-blue-700 dark:bg-blue-500 dark:hover:bg-blue-600">
            <Save className="h-4 w-4" />
            Save Table
          </Button>
        </div>
      }
    >
      {/* Main Content */}
      <div className="flex-1 overflow-hidden flex">
          {/* Left Panel - Configuration */}
          <div className="w-80 shrink-0 border-r border-gray-200 dark:border-gray-800 bg-white dark:bg-gray-900">
            <div className="h-full overflow-y-auto p-6 space-y-6 bg-white dark:bg-gray-950">
              <div className="space-y-4">
                <div className="flex items-center gap-2 pb-2 border-b border-gray-200 dark:border-gray-700">
                  <Grid3X3 className="h-4 w-4 text-blue-600 dark:text-blue-400" />
                  <h3 className="text-sm font-medium text-gray-800 dark:text-gray-200 uppercase tracking-wide">Structure</h3>
                </div>
                <div className="grid grid-cols-2 gap-4">
                  <div>
                    <Label className="text-sm font-medium text-gray-700 dark:text-gray-300">Rows</Label>
                    <div className="flex items-center gap-2 mt-1">
                      <Button variant="outline" size="sm" onClick={removeRow} disabled={tempData.rows <= 2} className="h-8 w-8 p-0 border-gray-300 dark:border-gray-600 hover:bg-gray-50 dark:hover:bg-gray-800 bg-transparent">
                        <Minus className="h-4 w-4" />
                      </Button>
                      <div className="w-16 text-center h-8 bg-white dark:bg-gray-800 border border-gray-300 dark:border-gray-600 rounded-md flex items-center justify-center text-sm font-medium">
                        {tempData.rows}
                      </div>
                      <Button variant="outline" size="sm" onClick={addRow} disabled={tempData.rows >= 50} className="h-8 w-8 p-0 border-gray-300 dark:border-gray-600 hover:bg-gray-50 dark:hover:bg-gray-800 bg-transparent">
                        <Plus className="h-4 w-4" />
                      </Button>
                    </div>
                  </div>
                  <div>
                    <Label className="text-sm font-medium text-gray-700 dark:text-gray-300">Columns</Label>
                    <div className="flex items-center gap-2 mt-1">
                      <Button variant="outline" size="sm" onClick={removeColumn} disabled={tempData.columns <= 2} className="h-8 w-8 p-0 border-gray-300 dark:border-gray-600 hover:bg-gray-50 dark:hover:bg-gray-800 bg-transparent">
                        <Minus className="h-4 w-4" />
                      </Button>
                      <div className="w-16 text-center h-8 bg-white dark:bg-gray-800 border border-gray-300 dark:border-gray-600 rounded-md flex items-center justify-center text-sm font-medium">
                        {tempData.columns}
                      </div>
                      <Button variant="outline" size="sm" onClick={addColumn} disabled={tempData.columns >= 10} className="h-8 w-8 p-0 border-gray-300 dark:border-gray-600 hover:bg-gray-50 dark:hover:bg-gray-800 bg-transparent">
                        <Plus className="h-4 w-4" />
                      </Button>
                    </div>
                  </div>
                </div>
              </div>

              <div className="space-y-4">
                <div className="space-y-3">
                  <Label className="text-sm font-medium text-gray-700 dark:text-gray-300">Table Style</Label>
                  <div className="grid grid-cols-1 gap-2 p-3 border rounded-lg bg-gray-50 dark:bg-gray-800/50 border-gray-200 dark:border-gray-700 max-h-80 overflow-y-auto">
                    {STYLE_OPTIONS.map((style) => (
                      <button
                        key={style.value}
                        type="button"
                        onClick={() => setTempData({ ...tempData, style: style.value })}
                        className={cn(
                          "flex items-center gap-3 p-3 rounded-lg border-2 transition-all hover:shadow-md",
                          tempData.style === style.value
                            ? "border-blue-500 dark:border-blue-400 bg-blue-50 dark:bg-blue-900/20 shadow-lg ring-2 ring-blue-200 dark:ring-blue-800"
                            : "border-gray-200 dark:border-gray-600 hover:border-gray-300 dark:hover:border-gray-500 hover:bg-gray-50 dark:hover:bg-gray-700/30"
                        )}
                      >
                        <div className="shrink-0 w-20 h-14 border border-gray-300 dark:border-gray-600 rounded overflow-hidden bg-white dark:bg-gray-900">
                          <div className={cn("w-full h-full scale-[1] origin-top-left", getTableStyleClasses(style.value))}>
                            <table className="w-full h-full text-[8px]">
                              <thead><tr><th className="px-1 py-0.5">Header 1</th><th className="px-1 py-0.5">Header 2</th></tr></thead>
                              <tbody>
                                <tr><td className="px-1 py-0.5">Data 1</td><td className="px-1 py-0.5">Data 2</td></tr>
                                <tr><td className="px-1 py-0.5">Row 2</td><td className="px-1 py-0.5">Cell</td></tr>
                                <tr><td className="px-1 py-0.5">Row 3</td><td className="px-1 py-0.5">Info</td></tr>
                              </tbody>
                            </table>
                          </div>
                        </div>
                        <div className="flex-1 text-left">
                          <div className="text-sm font-medium text-gray-900 dark:text-gray-100">{style.name}</div>
                          <div className="text-xs text-gray-500 dark:text-gray-400 mt-1">{style.preview}</div>
                        </div>
                        {tempData.style === style.value && (
                          <div className="w-5 h-5 rounded-full bg-blue-500 flex items-center justify-center">
                            <Check className="w-3 h-3 text-white" />
                          </div>
                        )}
                      </button>
                    ))}
                  </div>
                </div>

                <div className="space-y-3">
                  <div className="flex items-center space-x-3 p-3 bg-gray-50 dark:bg-gray-800/50 rounded-lg border border-gray-200 dark:border-gray-700">
                    <Switch
                      id="show-header"
                      checked={tempData.showHeader}
                      onCheckedChange={(checked: boolean) => {
                        const newCells = [...tempData.cells]
                        if (newCells[0]) {
                          newCells[0] = newCells[0].map((cell, j) => ({
                            ...cell,
                            content: checked ? (cell.content.startsWith("Header") ? cell.content : `Header ${j + 1}`) : cell.content,
                            isHeader: checked,
                          }))
                        }
                        setTempData({ ...tempData, showHeader: checked, cells: newCells })
                      }}
                    />
                    <Label htmlFor="show-header" className="text-sm font-medium text-gray-700 dark:text-gray-300">Show Header Row</Label>
                  </div>
                  <div className="flex items-center space-x-3 p-3 bg-gray-50 dark:bg-gray-800/50 rounded-lg border border-gray-200 dark:border-gray-700">
                    <Switch
                      id="show-borders"
                      checked={tempData.showBorders}
                      onCheckedChange={(checked: boolean) => setTempData({ ...tempData, showBorders: checked })}
                    />
                    <Label htmlFor="show-borders" className="text-sm font-medium text-gray-700 dark:text-gray-300">Show Table Borders</Label>
                  </div>
                </div>

                <div>
                  <Label className="text-xs text-gray-600 dark:text-gray-400">Caption (Optional)</Label>
                  <Input
                    placeholder="Table caption..."
                    value={tempData.caption || ""}
                    onChange={(e) => setTempData({ ...tempData, caption: e.target.value })}
                    className="mt-1 bg-white dark:bg-gray-800 border-gray-300 dark:border-gray-600 focus:border-blue-500 dark:focus:border-blue-400"
                  />
                </div>
              </div>
            </div>
          </div>

          {/* Right Panel - Table Preview and Editing */}
          <div className="flex-1 flex flex-col">
            <div className="p-4 border-b border-gray-200 dark:border-gray-800 bg-gray-50 dark:bg-gray-900">
              <h3 className="text-sm font-medium text-gray-800 dark:text-gray-200 uppercase tracking-wide">Live Preview</h3>
            </div>
            <div className="flex-1 overflow-auto p-6 bg-white dark:bg-gray-950">
              <div className="min-h-full">
                <Table className={cn(
                  getTableStyleClasses(tempData.style),
                  !tempData.showBorders && "[&_th]:border-0 [&_td]:border-0",
                  "w-full table-fixed"
                )}>
                  {tempData.caption && <caption className="text-sm text-gray-600 dark:text-gray-400 mb-4 font-medium">{tempData.caption}</caption>}
                  {tempData.showHeader && (
                    <TableHeader>
                      <TableRow>
                        {safeCells[0]?.map((cell, colIndex) => (
                          <TableHead key={colIndex}>
                            <Input
                              value={cell.content}
                              onChange={(e) => updateCellContent(0, colIndex, e.target.value)}
                              className="w-full h-9 text-sm font-medium border-0 bg-transparent focus:bg-white dark:focus:bg-gray-800 focus:ring-2 focus:ring-blue-500 rounded px-2"
                              placeholder={`Header ${colIndex + 1}`}
                            />
                          </TableHead>
                        ))}
                      </TableRow>
                    </TableHeader>
                  )}
                  <TableBody>
                    {safeCells.slice(tempData.showHeader ? 1 : 0).map((row, rowIndex) => (
                      <TableRow key={rowIndex}>
                        {row.map((cell, colIndex) => (
                          <TableCell key={colIndex}>
                            <Input
                              value={cell.content}
                              onChange={(e) => updateCellContent(rowIndex + (tempData.showHeader ? 1 : 0), colIndex, e.target.value)}
                              className="w-full h-9 text-sm border-0 bg-transparent focus:bg-white dark:focus:bg-gray-800 focus:ring-2 focus:ring-blue-500 rounded px-2"
                              placeholder={`Cell ${rowIndex + 1}-${colIndex + 1}`}
                            />
                          </TableCell>
                        ))}
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              </div>
            </div>
          </div>
        </div>
    </BlockEditorShell>
  )
}
