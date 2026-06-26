"use client"

import { useState, useRef } from "react"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Upload, Trash2, FileText, Copy, Check, FileJson } from "lucide-react"
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from "@/components/ui/dialog"
import { Alert, AlertDescription } from "@/components/ui/alert"

interface VegaLiteCsvManagerProps {
  data: Record<string, string>
  onDataChange: (data: Record<string, string>) => void
}

export function VegaLiteManager({ data, onDataChange }: VegaLiteCsvManagerProps) {
  const [isOpen, setIsOpen] = useState(false)
  const [copiedFile, setCopiedFile] = useState<string | null>(null)
  const fileInputRef = useRef<HTMLInputElement>(null)

  const handleFileUpload = async (event: React.ChangeEvent<HTMLInputElement>) => {
    const files = event.target.files
    if (!files || files.length === 0) return

    const newData = { ...data }

    for (let i = 0; i < files.length; i++) {
      const file = files[i]
      if (!file) continue
      
      const filename = file.name
      const isCSV = filename.endsWith('.csv')
      const isJSON = filename.endsWith('.json')
      
      // Validate file type
      if (!isCSV && !isJSON) {
        alert(`Arquivo "${filename}" não é CSV ou JSON. Por favor, envie apenas arquivos .csv ou .json`)
        continue
      }

      try {
        const content = await file.text()
        
        // Validate JSON files
        if (isJSON) {
          JSON.parse(content) // Throws if invalid
        }
        
        newData[filename] = content
      } catch (error) {
        console.error(`Erro ao ler arquivo ${filename}:`, error)
        if (isJSON) {
          alert(`Erro ao ler arquivo "${filename}". Verifique se é um JSON válido.`)
        } else {
          alert(`Erro ao ler arquivo "${filename}"`)
        }
      }
    }

    onDataChange(newData)
    
    // Reset input
    if (fileInputRef.current) {
      fileInputRef.current.value = ""
    }
  }

  const handleDelete = (filename: string) => {
    const newData = { ...data }
    delete newData[filename]
    onDataChange(newData)
  }

  const handleCopyUrl = (filename: string) => {
    const url = `data:${filename}`
    navigator.clipboard.writeText(url)
    setCopiedFile(filename)
    setTimeout(() => setCopiedFile(null), 2000)
  }

  const totalCount = Object.keys(data).length

  const getFileIcon = (filename: string) => {
    if (filename.endsWith('.json')) {
      return <FileJson className="h-4 w-4 text-blue-500 flex-shrink-0" />
    }
    return <FileText className="h-4 w-4 text-muted-foreground flex-shrink-0" />
  }

  return (
    <Dialog open={isOpen} onOpenChange={setIsOpen}>
      <DialogTrigger asChild>
        <Button
          variant="outline"
          size="sm"
          className="gap-2"
        >
          <FileText className="h-4 w-4" />
          Dados
          {totalCount > 0 && (
            <span className="ml-1 rounded-full bg-blue-500 px-2 py-0.5 text-xs text-white">
              {totalCount}
            </span>
          )}
        </Button>
      </DialogTrigger>
      <DialogContent className="max-w-2xl max-h-[80vh] flex flex-col">
        <DialogHeader>
          <DialogTitle>Gerenciar Dados</DialogTitle>
          <DialogDescription>
            Envie arquivos CSV ou JSON para usar no seu gráfico Vega-Lite. Os arquivos serão salvos junto com a especificação.
          </DialogDescription>
        </DialogHeader>

        <div className="flex-1 overflow-y-auto space-y-4">
          {/* Upload Section */}
          <div className="space-y-2">
            <Label htmlFor="data-upload">Enviar arquivos CSV ou JSON</Label>
            <div className="flex gap-2">
              <Input
                id="data-upload"
                ref={fileInputRef}
                type="file"
                accept=".csv,.json"
                multiple
                onChange={handleFileUpload}
                className="flex-1"
              />
              <Button
                onClick={() => fileInputRef.current?.click()}
                variant="outline"
                size="sm"
              >
                <Upload className="h-4 w-4 mr-2" />
                Selecionar
              </Button>
            </div>
          </div>

          {/* How to use */}
          <Alert>
            <FileText className="h-4 w-4" />
            <AlertDescription>
              <strong>Como usar:</strong> Após enviar um arquivo, use a URL{" "}
              <code className="bg-muted px-1 py-0.5 rounded text-sm">data:arquivo.csv</code> ou{" "}
              <code className="bg-muted px-1 py-0.5 rounded text-sm">data:arquivo.json</code>{" "}
              na especificação Vega-Lite.
            </AlertDescription>
          </Alert>

          {/* Files List */}
          {totalCount > 0 ? (
            <div className="space-y-2">
              <Label>Arquivos enviados ({totalCount})</Label>
              <div className="border rounded-md divide-y max-h-[300px] overflow-y-auto">
                {Object.entries(data).map(([filename, content]) => {
                  const lines = content.split('\n').filter(l => l.trim())
                  const sizeKb = (new Blob([content]).size / 1024).toFixed(1)
                  const fileType = filename.endsWith('.json') ? 'JSON' : 'CSV'
                  
                  return (
                    <div
                      key={filename}
                      className="p-3 flex items-center justify-between hover:bg-muted/50"
                    >
                      <div className="flex-1 min-w-0">
                        <div className="flex items-center gap-2">
                          {getFileIcon(filename)}
                          <span className="font-mono text-sm truncate">{filename}</span>
                          <span className="text-xs text-muted-foreground bg-muted px-2 py-0.5 rounded">
                            {fileType}
                          </span>
                        </div>
                        <div className="text-xs text-muted-foreground mt-1">
                          {lines.length} linhas • {sizeKb} KB
                        </div>
                        <div className="text-xs text-muted-foreground mt-1 font-mono bg-muted px-2 py-1 rounded inline-block">
                          data:{filename}
                        </div>
                      </div>
                      <div className="flex gap-2 ml-4">
                        <Button
                          variant="ghost"
                          size="sm"
                          onClick={() => handleCopyUrl(filename)}
                          className="gap-2"
                        >
                          {copiedFile === filename ? (
                            <>
                              <Check className="h-4 w-4 text-green-500" />
                              <span className="text-xs">Copiado!</span>
                            </>
                          ) : (
                            <>
                              <Copy className="h-4 w-4" />
                              <span className="text-xs">Copiar URL</span>
                            </>
                          )}
                        </Button>
                        <Button
                          variant="ghost"
                          size="sm"
                          onClick={() => handleDelete(filename)}
                          className="text-red-500 hover:text-red-700 hover:bg-red-50 dark:hover:bg-red-950"
                        >
                          <Trash2 className="h-4 w-4" />
                        </Button>
                      </div>
                    </div>
                  )
                })}
              </div>
            </div>
          ) : (
            <div className="text-center py-8 text-muted-foreground border rounded-md border-dashed">
              <Upload className="h-8 w-8 mx-auto mb-2 opacity-50" />
              <p className="text-sm">Nenhum arquivo enviado ainda</p>
              <p className="text-xs mt-1">Envie arquivos CSV ou JSON para usar nos gráficos</p>
            </div>
          )}
        </div>

        <div className="flex justify-end pt-4 border-t">
          <Button onClick={() => setIsOpen(false)}>
            Fechar
          </Button>
        </div>
      </DialogContent>
    </Dialog>
  )
}
