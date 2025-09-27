"use client"

import { useState, useEffect, useContext } from "react"
import { DecoratorNode, type SerializedLexicalNode } from "lexical"
import { $getNodeByKey } from "lexical"
import { useLexicalComposerContext } from "@lexical/react/LexicalComposerContext"
import { Pencil, Check, Plus, Minus, Grid3X3, Trash2, X, Save } from "lucide-react"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Table, TableHeader, TableBody, TableHead, TableRow, TableCell } from "@/components/ui/table"
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogFooter } from "@/components/ui/dialog"
import { Label } from "@/components/ui/label"
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select"
import { Switch } from "@/components/ui/switch"
import { cn } from "@/lib/utils"
import type { JSX } from "react/jsx-runtime"
import { EditorLoadingContext } from "../lexical-editor"

export type TableStyle = "default" | "striped" | "bordered" | "minimal" | "modern"

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
  const isLoading = useContext(EditorLoadingContext)

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
    setShowEditor(false)
  }

  const handleCancel = () => {
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
    switch (style) {
      case "striped":
        return "border-collapse border border-border [&_tr:nth-child(even)]:bg-muted/50"
      case "bordered":
        return "border-collapse border-2 border-border [&_th]:border [&_td]:border"
      case "minimal":
        return "border-collapse [&_th]:border-b [&_td]:border-b border-b-0"
      case "modern":
        return "border-collapse shadow-sm rounded-lg overflow-hidden [&_th]:bg-primary/5 [&_th]:font-semibold"
      default:
        return "border-collapse"
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
      <div className="flex items-center justify-between mb-4">
        <div className="flex items-center gap-2">
          <Grid3X3 className="h-5 w-5 text-muted-foreground" />
          <span className="text-sm font-medium">Table</span>
          {safeData.caption && <span className="text-sm text-muted-foreground">- {safeData.caption}</span>}
        </div>
        <div className="flex items-center gap-2">
          {!showEditor ? (
            <>
              <Button variant="ghost" size="sm" onClick={() => {
                setTempData(currentData)
                setShowEditor(true)
              }}>
                <Pencil className="h-4 w-4" />
              </Button>
              <Button
                variant="ghost"
                size="sm"
                onClick={() => {
                  editor.update(() => {
                    node.remove()
                  })
                }}
              >
                <Trash2 className="h-4 w-4" />
              </Button>
            </>
          ) : (
            <>
              <Button variant="ghost" size="sm" onClick={handleCancel}>
                Cancel
              </Button>
              <Button size="sm" onClick={handleSave}>
                <Check className="h-4 w-4 mr-2" />
                Save
              </Button>
            </>
          )}
        </div>
      </div>

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

      {/* Table Editor Modal */}
      {showEditor && (
        <div className="fixed inset-0 bg-black/60 backdrop-blur-sm flex items-center justify-center z-50 p-4">
          <div className="bg-white dark:bg-gray-900 border dark:border-gray-700 rounded-lg shadow-2xl w-full max-w-7xl h-[90vh] flex flex-col">
            {/* Header */}
            <div className="flex items-center justify-between p-4 border-b border-gray-200 dark:border-gray-700 bg-gradient-to-r from-gray-50 to-gray-100 dark:from-gray-800 dark:to-gray-900">
              <div className="flex items-center gap-3">
                <div className="p-2 bg-blue-50 dark:bg-blue-900/30 rounded-lg">
                  <Grid3X3 className="h-5 w-5 text-blue-600 dark:text-blue-400" />
                </div>
                <div>
                  <h2 className="text-xl font-semibold text-gray-900 dark:text-gray-100">Table Editor</h2>
                  <p className="text-sm text-gray-600 dark:text-gray-400 mt-1">
                    Configure and edit your table structure and content
                  </p>
                </div>
              </div>
              <Button variant="ghost" size="sm" onClick={handleCancel} className="hover:bg-gray-100 dark:hover:bg-gray-800">
                <X className="h-4 w-4" />
              </Button>
            </div>

            {/* Main Content */}
            <div className="flex-1 overflow-hidden flex">
              {/* Left Panel - Configuration */}
              <div className="w-80 flex-shrink-0 border-r border-gray-200 dark:border-gray-700 bg-gradient-to-b from-gray-50 to-gray-100 dark:from-gray-800 dark:to-gray-850">
                <div className="h-full overflow-y-auto p-6 space-y-6">
                  <div className="space-y-4">
                    <div className="flex items-center gap-2 pb-2 border-b border-gray-200 dark:border-gray-600">
                      <Grid3X3 className="h-4 w-4 text-gray-600 dark:text-gray-400" />
                      <h3 className="text-sm font-medium text-gray-700 dark:text-gray-300 uppercase tracking-wide">Structure</h3>
                    </div>
                    
                    <div className="grid grid-cols-2 gap-4">
                      <div>
                        <Label htmlFor="table-rows" className="text-sm font-medium text-gray-700 dark:text-gray-300">Rows</Label>
                        <div className="flex items-center gap-2 mt-1">
                          <Button
                            variant="outline"
                            size="sm"
                            onClick={removeRow}
                            disabled={tempData.rows <= 1}
                            className="h-8 w-8 p-0"
                          >
                            <Minus className="h-4 w-4" />
                          </Button>
                          <Input
                            id="table-rows"
                            type="number"
                            min="1"
                            max="20"
                            value={tempData.rows}
                            onChange={(e) => {
                              const newRows = Number.parseInt(e.target.value) || 1
                              const newCells = [...tempData.cells]
                              
                              // Add or remove rows as needed
                              while (newCells.length < newRows) {
                                const newRow: TableCellData[] = []
                                for (let j = 0; j < tempData.columns; j++) {
                                  newRow.push({
                                    content: `Cell ${newCells.length + 1}-${j + 1}`,
                                    isHeader: false,
                                  })
                                }
                                newCells.push(newRow)
                              }
                              while (newCells.length > newRows) {
                                newCells.pop()
                              }
                              
                              setTempData({
                                ...tempData,
                                rows: newRows,
                                cells: newCells,
                              })
                            }}
                            className="w-16 text-center h-8"
                          />
                          <Button variant="outline" size="sm" onClick={addRow} className="h-8 w-8 p-0">
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
                            disabled={tempData.columns <= 1}
                            className="h-8 w-8 p-0"
                          >
                            <Minus className="h-4 w-4" />
                          </Button>
                          <Input
                            id="table-columns"
                            type="number"
                            min="1"
                            max="10"
                            value={tempData.columns}
                            onChange={(e) => {
                              const newColumns = Number.parseInt(e.target.value) || 1
                              const newCells = tempData.cells.map((row, i) => {
                                const newRow = [...row]
                                
                                // Add or remove columns as needed
                                while (newRow.length < newColumns) {
                                  newRow.push({
                                    content: i === 0 && tempData.showHeader ? `Header ${newRow.length + 1}` : `Cell ${i + 1}-${newRow.length + 1}`,
                                    isHeader: i === 0 && tempData.showHeader,
                                  })
                                }
                                while (newRow.length > newColumns) {
                                  newRow.pop()
                                }
                                
                                return newRow
                              })
                              
                              setTempData({
                                ...tempData,
                                columns: newColumns,
                                cells: newCells,
                              })
                            }}
                            className="w-16 text-center h-8"
                          />
                          <Button variant="outline" size="sm" onClick={addColumn} className="h-8 w-8 p-0">
                            <Plus className="h-4 w-4" />
                          </Button>
                        </div>
                      </div>
                    </div>
                  </div>

                  <div className="space-y-4">
                    <div className="flex items-center gap-2 pb-2 border-b border-gray-200 dark:border-gray-600">
                      <div className="h-4 w-4 rounded bg-gradient-to-br from-blue-500 to-purple-600"></div>
                      <h3 className="text-sm font-medium text-gray-700 dark:text-gray-300 uppercase tracking-wide">Appearance</h3>
                    </div>
                    
                    <div>
                      <Label htmlFor="table-style" className="text-sm font-medium text-gray-700 dark:text-gray-300">Table Style</Label>
                      <Select
                        value={tempData.style}
                        onValueChange={(value) => setTempData({ ...tempData, style: value as TableStyle })}
                      >
                        <SelectTrigger className="mt-1">
                          <SelectValue />
                        </SelectTrigger>
                        <SelectContent>
                          <SelectItem value="default">Default</SelectItem>
                          <SelectItem value="striped">Striped</SelectItem>
                          <SelectItem value="bordered">Bordered</SelectItem>
                          <SelectItem value="minimal">Minimal</SelectItem>
                          <SelectItem value="modern">Modern</SelectItem>
                        </SelectContent>
                      </Select>
                    </div>

                    <div className="space-y-3">
                      <div className="flex items-center space-x-3">
                        <Switch
                          id="show-header"
                          checked={tempData.showHeader}
                          onCheckedChange={(checked) => {
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

                      <div className="flex items-center space-x-3">
                        <Switch
                          id="show-borders"
                          checked={tempData.showBorders}
                          onCheckedChange={(checked) => setTempData({ ...tempData, showBorders: checked })}
                        />
                        <Label htmlFor="show-borders" className="text-sm font-medium text-gray-700 dark:text-gray-300">Show Table Borders</Label>
                      </div>
                    </div>

                    <div>
                      <Label htmlFor="table-caption" className="text-sm font-medium text-gray-700 dark:text-gray-300">Caption (Optional)</Label>
                      <Input
                        id="table-caption"
                        placeholder="Table caption..."
                        value={tempData.caption || ""}
                        onChange={(e) => setTempData({ ...tempData, caption: e.target.value })}
                        className="mt-1"
                      />
                    </div>
                  </div>
                </div>
              </div>

              {/* Right Panel - Table Preview and Editing */}
              <div className="flex-1 flex flex-col">
                <div className="flex items-center justify-between p-4 border-b border-gray-200 dark:border-gray-700 bg-gradient-to-r from-gray-50 to-gray-100 dark:from-gray-800 dark:to-gray-850">
                  <h3 className="text-sm font-medium text-gray-700 dark:text-gray-300 uppercase tracking-wide">Preview & Edit Content</h3>
                  <div className="flex items-center gap-2">
                    <Button variant="ghost" size="sm" onClick={handleCancel} className="text-gray-600 dark:text-gray-400">
                      Cancel
                    </Button>
                    <Button size="sm" onClick={handleSave} className="bg-blue-600 hover:bg-blue-700 text-white">
                      <Save className="h-4 w-4 mr-2" />
                      Save Table
                    </Button>
                  </div>
                </div>
                
                <div className="flex-1 overflow-auto p-6 bg-white dark:bg-gray-900">
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
                          "w-full"
                        )}>
                          {safeTempData.caption && <caption className="text-sm text-gray-600 dark:text-gray-400 mb-4 font-medium">{safeTempData.caption}</caption>}
                          {safeTempData.showHeader && (
                            <TableHeader>
                              <TableRow>
                                {safeTempData.cells[0]?.map((cell, colIndex) => (
                                  <TableHead key={colIndex} className="min-w-[120px]">
                                    <Input
                                      value={cell.content}
                                      onChange={(e) => updateCellContent(0, colIndex, e.target.value)}
                                      className="w-full h-9 text-sm font-medium border-0 bg-transparent focus:bg-white dark:focus:bg-gray-800 focus:ring-2 focus:ring-blue-500 rounded"
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
                                  <TableCell key={colIndex} className="min-w-[120px]">
                                    <Input
                                      value={cell.content}
                                      onChange={(e) => updateCellContent(rowIndex + (safeTempData.showHeader ? 1 : 0), colIndex, e.target.value)}
                                      className="w-full h-9 text-sm border-0 bg-transparent focus:bg-white dark:focus:bg-gray-800 focus:ring-2 focus:ring-blue-500 rounded"
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
