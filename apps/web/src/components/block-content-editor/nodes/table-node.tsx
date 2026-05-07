"use client"

import { useState, useEffect, useContext, createContext } from "react"
import { DecoratorNode, type SerializedLexicalNode } from "lexical"
import { $getNodeByKey } from "lexical"
import { useLexicalComposerContext } from "@lexical/react/LexicalComposerContext"
import { Pencil, Check, Plus, Minus, Grid3X3, X, Save } from "lucide-react"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Table, TableHeader, TableBody, TableHead, TableRow, TableCell } from "@/components/ui/table"
import { Label } from "@/components/ui/label"
import { Switch } from "@/components/ui/switch"
import { cn } from "@/lib/utils"
import type { JSX } from "react/jsx-runtime"
import { ContentEditMenu } from "@/components/block-content-editor/extras/content-edit-menu"

// Create a local context for editor loading state
const EditorLoadingContext = createContext<boolean>(false)

export type TableStyle = "default" | "striped" | "bordered" | "minimal" | "modern" | "grid" | "accent" | "dark" | "colorful" | "professional"

export interface TableCellData {
  content: string
  isHeader: boolean
}

export interface TableData {
  rows: number
  columns: number
  style: TableStyle
  showHeader: boolean
  showBorders: boolean
  cells: TableCellData[][]
  caption?: string
  isNew?: boolean
}

export interface SerializedTableNode extends SerializedLexicalNode {
  type: "table"
  data: TableData
  version: 1
}

export class TableNode extends DecoratorNode<JSX.Element> {
  __data: TableData

  static getType(): string {
    return "table"
  }

  static clone(node: TableNode): TableNode {
    return new TableNode(node.__data, node.__key)
  }

  constructor(data: TableData, key?: string) {
    super(key)
    const rows = data.rows || 3
    const columns = data.columns || 3
    const showHeader = data.showHeader !== false
    
    this.__data = {
      rows,
      columns,
      style: data.style || "default",
      showHeader,
      showBorders: data.showBorders !== false,
      cells: data.cells && data.cells.length > 0 ? data.cells : this.createEmptyTable(rows, columns, showHeader),
      caption: data.caption || "",
      isNew: data.isNew || false,
    }
  }

  private createEmptyTable(rows: number, columns: number, showHeader: boolean): TableCellData[][] {
    const cells: TableCellData[][] = []
    for (let i = 0; i < rows; i++) {
      const row: TableCellData[] = []
      for (let j = 0; j < columns; j++) {
        row.push({
          content: i === 0 && showHeader ? `Header ${j + 1}` : `Cell ${i + 1}-${j + 1}`,
          isHeader: i === 0 && showHeader,
        })
      }
      cells.push(row)
    }
    return cells
  }

  static importJSON(serializedNode: SerializedTableNode): TableNode {
    const { data } = serializedNode
    return new TableNode(data)
  }

  exportJSON(): SerializedTableNode {
    return {
      type: "table",
      data: this.__data,
      version: 1,
    }
  }

  createDOM(): HTMLElement {
    const div = document.createElement("div")
    div.className = "table-node-container"
    return div
  }

  updateDOM(): false {
    return false
  }

  setData(data: Partial<TableData>): void {
    const writableNode = this.getWritable()
    writableNode.__data = { ...this.__data, ...data }
  }

  getData(): TableData {
    return this.__data
  }

  decorate(): JSX.Element {
    return <TableComponent node={this} />
  }
}

function TableComponent({ node }: { node: TableNode }) {
  const [editor] = useLexicalComposerContext()
  const [showEditor, setShowEditor] = useState(false)
  const [currentData, setCurrentData] = useState<TableData>(() => {
    const initialData = node.getData()
    // Ensure we have valid cells data
    if (!initialData.cells || initialData.cells.length === 0) {
      const cells = []
      for (let i = 0; i < initialData.rows; i++) {
        const row = []
        for (let j = 0; j < initialData.columns; j++) {
          row.push({
            content: i === 0 && initialData.showHeader ? `Header ${j + 1}` : `Cell ${i + 1}-${j + 1}`,
            isHeader: i === 0 && initialData.showHeader,
          })
        }
        cells.push(row)
      }
      return { ...initialData, cells }
    }
    return initialData
  })
  const [tempData, setTempData] = useState<TableData>(currentData)
  const isLoading = useContext(EditorLoadingContext) ?? false

  // Block body scroll and pointer events when modal is open
  useEffect(() => {
    if (showEditor) {
      document.body.style.overflow = 'hidden'
      document.body.style.pointerEvents = 'none'
      
      return () => {
        document.body.style.overflow = ''
        document.body.style.pointerEvents = ''
      }
    }
  }, [showEditor])

  useEffect(() => {
    const data = node.getData()
    // Ensure we have valid cells data
    let validData = data
    if (!data.cells || data.cells.length === 0) {
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
      validData = { ...data, cells }
    }
    
    if (validData.isNew) {
      setShowEditor(true)
    }
    setCurrentData(validData)
    setTempData(validData)
  }, [node])

  const handleSave = () => {
    editor.update(() => {
      const nodeKey = node.getKey()
      const currentNode = $getNodeByKey(nodeKey) as TableNode
      if (currentNode) {
        // Ensure cells array matches the new dimensions
        const newCells = []
        for (let i = 0; i < tempData.rows; i++) {
          const row = []
          for (let j = 0; j < tempData.columns; j++) {
            if (tempData.cells[i]?.[j]) {
              row.push({
                content: tempData.cells[i]![j]!.content,
                isHeader: i === 0 && tempData.showHeader,
              })
            } else {
              row.push({
                content: i === 0 && tempData.showHeader ? `Header ${j + 1}` : `Cell ${i + 1}-${j + 1}`,
                isHeader: i === 0 && tempData.showHeader,
              })
            }
          }
          newCells.push(row)
        }

        currentNode.setData({
          ...tempData,
          cells: newCells,
          isNew: false,
        })
        
        // Update local state
        const updatedData = { ...tempData, cells: newCells, isNew: false }
        setCurrentData(updatedData)
      }
    })
    
    // Restore body styles before closing
    document.body.style.overflow = ''
    document.body.style.pointerEvents = ''
    
    setShowEditor(false)
  }

  const handleCancel = () => {
    // Restore body styles before closing
    document.body.style.overflow = ''
    document.body.style.pointerEvents = ''
    
    if (currentData.isNew) {
      editor.update(() => {
        node.remove()
      })
    } else {
      setTempData(currentData)
      setShowEditor(false)
    }
  }

  const updateCellContent = (rowIndex: number, colIndex: number, content: string) => {
    const newCells = [...tempData.cells]
    if (!newCells[rowIndex]) newCells[rowIndex] = []
    newCells[rowIndex][colIndex] = {
      content,
      isHeader: (newCells[rowIndex][colIndex]?.isHeader) || false,
    }
    setTempData({ ...tempData, cells: newCells })
  }

  const addRow = () => {
    const newRow: TableCellData[] = []
    for (let j = 0; j < tempData.columns; j++) {
      newRow.push({
        content: `Cell ${tempData.rows + 1}-${j + 1}`,
        isHeader: false,
      })
    }
    setTempData({
      ...tempData,
      rows: tempData.rows + 1,
      cells: [...tempData.cells, newRow],
    })
  }

  const removeRow = () => {
    if (tempData.rows > 1) {
      setTempData({
        ...tempData,
        rows: tempData.rows - 1,
        cells: tempData.cells.slice(0, -1),
      })
    }
  }

  const addColumn = () => {
    const newCells = tempData.cells.map((row, i) => [
      ...row,
      {
        content: i === 0 && tempData.showHeader ? `Header ${tempData.columns + 1}` : `Cell ${i + 1}-${tempData.columns + 1}`,
        isHeader: i === 0 && tempData.showHeader,
      },
    ])
    setTempData({
      ...tempData,
      columns: tempData.columns + 1,
      cells: newCells,
    })
  }

  const removeColumn = () => {
    if (tempData.columns > 1) {
      const newCells = tempData.cells.map((row) => row.slice(0, -1))
      setTempData({
        ...tempData,
        columns: tempData.columns - 1,
        cells: newCells,
      })
    }
  }

  const getTableStyleClasses = (style: TableStyle) => {
    const baseClasses = "[&_th]:truncate [&_td]:truncate [&_th]:max-w-0 [&_td]:max-w-0"
    
    switch (style) {
      case "striped":
        return cn(
          baseClasses,
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
          baseClasses,
          "border-collapse border-2 border-gray-400 dark:border-gray-500",
          "[&_th]:border-2 [&_th]:border-gray-400 dark:[&_th]:border-gray-500",
          "[&_td]:border [&_td]:border-gray-300 dark:[&_td]:border-gray-600",
          "[&_th]:bg-gray-200 dark:[&_th]:bg-gray-700 [&_th]:font-bold [&_th]:text-gray-900 dark:[&_th]:text-gray-100",
          "[&_td]:bg-white dark:[&_td]:bg-gray-900",
          "shadow-lg"
        )
      case "minimal":
        return cn(
          baseClasses,
          "border-collapse",
          "[&_th]:border-b-2 [&_th]:border-gray-300 dark:[&_th]:border-gray-600",
          "[&_th]:font-semibold [&_th]:text-gray-800 dark:[&_th]:text-gray-200",
          "[&_td]:border-b [&_td]:border-gray-100 dark:[&_td]:border-gray-800",
          "[&_tr:hover]:bg-gray-50 dark:[&_tr:hover]:bg-gray-800/50",
          "text-gray-700 dark:text-gray-300"
        )
      case "modern":
        return cn(
          baseClasses,
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
          baseClasses,
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
          baseClasses,
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
          baseClasses,
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
          baseClasses,
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
          baseClasses,
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
          baseClasses,
          "border-collapse",
          "[&_th]:border-b [&_th]:border-gray-200 dark:[&_th]:border-gray-700",
          "[&_th]:font-medium [&_th]:text-left",
          "[&_td]:border-b [&_td]:border-gray-200 dark:[&_td]:border-gray-700",
          "text-gray-900 dark:text-gray-100"
        )
    }
  }

  if (isLoading) {
    return (
      <div className="flex items-center justify-center py-8">
        <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-primary"></div>
      </div>
    )
  }

  const data = showEditor ? tempData : currentData

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

  return (
    <div className="table-node my-4 max-w-full">
      <div className="relative">
        {safeData.caption && (
          <div className="mb-2">
            <span className="text-sm text-muted-foreground">- {safeData.caption}</span>
          </div>
        )}
        
        {/* ContentEditMenu for lateral edit button */}
        <ContentEditMenu
          options={[
            {
              id: "edit",
              icon: <Pencil className="h-4 w-4" />,
              label: "Edit Table",
              action: () => setShowEditor(true),
            },
          ]}
        />

        <div>
          <Table className={cn(
            getTableStyleClasses(safeData.style),
            !safeData.showBorders && "[&_th]:border-0 [&_td]:border-0",
            "w-full table-fixed"
          )}>
            {safeData.caption && <caption className="text-sm text-muted-foreground mb-2">{safeData.caption}</caption>}
            {safeData.showHeader && (
              <TableHeader>
                <TableRow>
                  {safeData.cells[0]?.map((cell, colIndex) => (
                    <TableHead key={colIndex} title={cell.content}>
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
                    <TableCell key={colIndex} title={cell.content}>
                      {cell.content}
                    </TableCell>
                  ))}
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </div>
      </div>

      {/* Table Editor Modal */}
      {showEditor && (
        <div 
          className="fixed inset-0 bg-black/60 backdrop-blur-sm flex items-center justify-center z-50 p-4"
          style={{ pointerEvents: 'auto' }}
          onClick={handleCancel}
          onKeyDown={(e) => {
            e.stopPropagation()
            if (e.key === 'Escape') {
              handleCancel()
            }
          }}
          onKeyUp={(e) => e.stopPropagation()}
          onKeyPress={(e) => e.stopPropagation()}
        >
          <div 
            className="bg-white dark:bg-gray-900 border dark:border-gray-700 shadow-2xl w-full max-w-7xl h-[90vh] flex flex-col"
            onClick={(e) => e.stopPropagation()}
          >
            {/* Header */}
            <div className="flex items-center justify-between p-4 border-b border-gray-200 dark:border-gray-800 bg-gray-50 dark:bg-gray-900">
              <div className="flex items-center gap-3">
                <Grid3X3 className="h-5 w-5 text-blue-600 dark:text-blue-400" />
                <h2 className="text-xl font-semibold text-gray-900 dark:text-gray-100">Table Editor</h2>
              </div>
              <Button variant="ghost" size="sm" onClick={handleCancel} className="hover:bg-gray-100 dark:hover:bg-gray-800">
                <X className="h-4 w-4" />
              </Button>
            </div>

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
                        <Label htmlFor="table-rows" className="text-sm font-medium text-gray-700 dark:text-gray-300">Rows</Label>
                        <div className="flex items-center gap-2 mt-1">
                          <Button
                            variant="outline"
                            size="sm"
                            onClick={removeRow}
                            disabled={tempData.rows <= 2}
                            className="h-8 w-8 p-0 border-gray-300 dark:border-gray-600 hover:bg-gray-50 dark:hover:bg-gray-800 bg-transparent"
                          >
                            <Minus className="h-4 w-4" />
                          </Button>
                          <div className="w-16 text-center h-8 bg-white dark:bg-gray-800 border border-gray-300 dark:border-gray-600 rounded-md flex items-center justify-center text-sm font-medium">
                            {tempData.rows}
                          </div>
                          <Button 
                            variant="outline" 
                            size="sm" 
                            onClick={addRow} 
                            disabled={tempData.rows >= 50}
                            className="h-8 w-8 p-0 border-gray-300 dark:border-gray-600 hover:bg-gray-50 dark:hover:bg-gray-800 bg-transparent"
                          >
                            <Plus className="h-4 w-4" />
                          </Button>
                        </div>
                      </div>

                      <div>
                        <Label htmlFor="table-columns" className="text-sm font-medium text-gray-700 dark:text-gray-300">Columns</Label>
                        <div className="flex items-center gap-2 mt-1">
                          <Button
                            variant="outline"
                            size="sm"
                            onClick={removeColumn}
                            disabled={tempData.columns <= 2}
                            className="h-8 w-8 p-0 border-gray-300 dark:border-gray-600 hover:bg-gray-50 dark:hover:bg-gray-800 bg-transparent"
                          >
                            <Minus className="h-4 w-4" />
                          </Button>
                          <div className="w-16 text-center h-8 bg-white dark:bg-gray-800 border border-gray-300 dark:border-gray-600 rounded-md flex items-center justify-center text-sm font-medium">
                            {tempData.columns}
                          </div>
                          <Button 
                            variant="outline" 
                            size="sm" 
                            onClick={addColumn} 
                            disabled={tempData.columns >= 10}
                            className="h-8 w-8 p-0 border-gray-300 dark:border-gray-600 hover:bg-gray-50 dark:hover:bg-gray-800 bg-transparent"
                          >
                            <Plus className="h-4 w-4" />
                          </Button>
                        </div>
                      </div>
                    </div>
                  </div>

                  <div className="space-y-4">
                    
                    <div className="space-y-3">
                      <Label htmlFor="table-style" className="text-sm font-medium text-gray-700 dark:text-gray-300">Table Style</Label>
                      
                      {/* Style Preview Grid */}
                      <div className="grid grid-cols-1 gap-2 p-3 border rounded-lg bg-gray-50 dark:bg-gray-800/50 border-gray-200 dark:border-gray-700 max-h-80 overflow-y-auto">
                        {[
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
                        ].map((style) => (
                          <button
                            key={style.value}
                            type="button"
                            onClick={() => setTempData({ ...tempData, style: style.value as TableStyle })}
                            className={cn(
                              "flex items-center gap-3 p-3 rounded-lg border-2 transition-all hover:shadow-md",
                              tempData.style === style.value
                                ? "border-blue-500 dark:border-blue-400 bg-blue-50 dark:bg-blue-900/20 shadow-lg ring-2 ring-blue-200 dark:ring-blue-800"
                                : "border-gray-200 dark:border-gray-600 hover:border-gray-300 dark:hover:border-gray-500 hover:bg-gray-50 dark:hover:bg-gray-700/30"
                            )}
                          >
                            {/* Mini table preview */}
                            <div className="shrink-0 w-20 h-14 border border-gray-300 dark:border-gray-600 rounded overflow-hidden bg-white dark:bg-gray-900">
                              <div className={cn(
                                "w-full h-full scale-[1] origin-top-left",
                                getTableStyleClasses(style.value as TableStyle)
                              )}>
                                <table className="w-full h-full text-[8px]">
                                  <thead>
                                    <tr>
                                      <th className="px-1 py-0.5">Header 1</th>
                                      <th className="px-1 py-0.5">Header 2</th>
                                    </tr>
                                  </thead>
                                  <tbody>
                                    <tr>
                                      <td className="px-1 py-0.5">Data 1</td>
                                      <td className="px-1 py-0.5">Data 2</td>
                                    </tr>
                                    <tr>
                                      <td className="px-1 py-0.5">Row 2</td>
                                      <td className="px-1 py-0.5">Cell</td>
                                    </tr>
                                    <tr>
                                      <td className="px-1 py-0.5">Row 3</td>
                                      <td className="px-1 py-0.5">Info</td>
                                    </tr>
                                  </tbody>
                                </table>
                              </div>
                            </div>
                            
                            {/* Style info */}
                            <div className="flex-1 text-left">
                              <div className="text-sm font-medium text-gray-900 dark:text-gray-100">{style.name}</div>
                              <div className="text-xs text-gray-500 dark:text-gray-400 mt-1">{style.preview}</div>
                            </div>
                            
                            {/* Selection indicator */}
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
                      <Label htmlFor="table-caption" className="text-xs text-gray-600 dark:text-gray-400">Caption (Optional)</Label>
                      <Input
                        id="table-caption"
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
                    {/* Ensure tempData has valid cells for editing */}
                    {(() => {
                      const safeTempData = {
                        ...tempData,
                        cells: tempData.cells && tempData.cells.length > 0 ? tempData.cells : (() => {
                          const cells = []
                          for (let i = 0; i < tempData.rows; i++) {
                            const row = []
                            for (let j = 0; j < tempData.columns; j++) {
                              row.push({
                                content: i === 0 && tempData.showHeader ? `Header ${j + 1}` : `Cell ${i + 1}-${j + 1}`,
                                isHeader: i === 0 && tempData.showHeader,
                              })
                            }
                            cells.push(row)
                          }
                          return cells
                        })()
                      }
                      
                      return (
                        <Table className={cn(
                          getTableStyleClasses(safeTempData.style),
                          !safeTempData.showBorders && "[&_th]:border-0 [&_td]:border-0",
                          "w-full table-fixed"
                        )}>
                          {safeTempData.caption && <caption className="text-sm text-gray-600 dark:text-gray-400 mb-4 font-medium">{safeTempData.caption}</caption>}
                          {safeTempData.showHeader && (
                            <TableHeader>
                              <TableRow>
                                {safeTempData.cells[0]?.map((cell, colIndex) => (
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
                            {safeTempData.cells.slice(safeTempData.showHeader ? 1 : 0).map((row, rowIndex) => (
                              <TableRow key={rowIndex}>
                                {row.map((cell, colIndex) => (
                                  <TableCell key={colIndex}>
                                    <Input
                                      value={cell.content}
                                      onChange={(e) => updateCellContent(rowIndex + (safeTempData.showHeader ? 1 : 0), colIndex, e.target.value)}
                                      className="w-full h-9 text-sm border-0 bg-transparent focus:bg-white dark:focus:bg-gray-800 focus:ring-2 focus:ring-blue-500 rounded px-2"
                                      placeholder={`Cell ${rowIndex + 1}-${colIndex + 1}`}
                                    />
                                  </TableCell>
                                ))}
                              </TableRow>
                            ))}
                          </TableBody>
                        </Table>
                      )
                    })()}
                  </div>
                </div>
              </div>
            </div>

            {/* Footer */}
            <div className="p-4 border-t border-gray-200 dark:border-gray-800 bg-gray-50 dark:bg-gray-900">
              <div className="flex gap-2 justify-end">
                <Button
                  variant="outline"
                  onClick={handleCancel}
                  className="border-gray-300 dark:border-gray-600 hover:bg-gray-50 dark:hover:bg-gray-800 bg-transparent"
                >
                  Cancel
                </Button>
                <Button
                  onClick={handleSave}
                  className="flex items-center gap-2 bg-blue-600 hover:bg-blue-700 dark:bg-blue-500 dark:hover:bg-blue-600"
                >
                  <Save className="h-4 w-4" />
                  Save Table
                </Button>
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  )
}

export function $createTableNode(data: Partial<TableData>): TableNode {
  const defaultData = {
    rows: 3,
    columns: 3,
    style: "default" as TableStyle,
    showHeader: true,
    showBorders: true,
    cells: [] as TableCellData[][],
    ...data,
  }
  
  // If no cells provided, create empty table
  if (!defaultData.cells || defaultData.cells.length === 0) {
    defaultData.cells = []
    for (let i = 0; i < defaultData.rows; i++) {
      const row: TableCellData[] = []
      for (let j = 0; j < defaultData.columns; j++) {
        row.push({
          content: i === 0 && defaultData.showHeader ? `Header ${j + 1}` : `Cell ${i + 1}-${j + 1}`,
          isHeader: i === 0 && defaultData.showHeader,
        })
      }
      defaultData.cells.push(row)
    }
  }
  
  return new TableNode(defaultData)
}

export function $isTableNode(node: any): node is TableNode {
  return node instanceof TableNode
}
