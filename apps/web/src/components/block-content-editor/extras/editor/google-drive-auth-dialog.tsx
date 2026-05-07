"use client"

import { useState } from "react"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogDescription,
  DialogFooter,
} from "@/components/ui/dialog"
import { Alert, AlertDescription } from "@/components/ui/alert"
import { Badge } from "@/components/ui/badge"
import { Loader2, Shield, FolderPlus, ExternalLink, CheckCircle } from "lucide-react"
import { useGoogleDriveAuth } from "@/components/block-content-editor/hooks/editor/use-google-drive-auth"

interface GoogleDriveAuthDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  onAuthSuccess: () => void
}

export function GoogleDriveAuthDialog({
  open,
  onOpenChange,
  onAuthSuccess,
}: GoogleDriveAuthDialogProps) {
  const {
    isAuthenticated,
    isLoading,
    selectedFolder,
    folderName,
    error,
    authenticate,
    signOut,
    createOrFindFolder,
    hasValidSetup,
  } = useGoogleDriveAuth()

  const [customFolderName, setCustomFolderName] = useState("Block Content Editor Projects")
  const [isCreatingFolder, setIsCreatingFolder] = useState(false)

  const handleAuthenticate = async () => {
    const success = await authenticate()
    if (!success) return

    // If no folder is selected, try to create/find the default folder
    if (!selectedFolder) {
      await handleCreateFolder()
    }
  }

  const handleCreateFolder = async () => {
    if (!customFolderName.trim()) return

    setIsCreatingFolder(true)
    const folderId = await createOrFindFolder(customFolderName.trim())
    setIsCreatingFolder(false)

    if (folderId) {
      onAuthSuccess()
      onOpenChange(false)
    }
  }

  const handleComplete = () => {
    if (hasValidSetup) {
      onAuthSuccess()
      onOpenChange(false)
    }
  }

  const handleSignOut = async () => {
    await signOut()
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2">
            <Shield className="w-5 h-5 text-blue-600" />
            Configurar Google Drive
          </DialogTitle>
          <DialogDescription>
            Configure a integração segura com o Google Drive para sincronizar seus projetos.
          </DialogDescription>
        </DialogHeader>

        <div className="space-y-4">
          {/* Security Notice */}
          <Alert>
            <Shield className="h-4 w-4" />
            <AlertDescription className="text-sm">
              <strong>Segurança:</strong> Solicitamos apenas permissões mínimas para acessar 
              arquivos criados por esta aplicação. Seus outros arquivos permanecem privados.
            </AlertDescription>
          </Alert>

          {/* Error Display */}
          {error && (
            <Alert variant="destructive">
              <AlertDescription>{error}</AlertDescription>
            </Alert>
          )}

          {/* Step 1: Authentication */}
          <div className="space-y-3">
            <div className="flex items-center gap-2">
              <Badge variant={isAuthenticated ? "default" : "secondary"} className="text-xs">
                {isAuthenticated ? "✓" : "1"}
              </Badge>
              <Label className="text-sm font-medium">Autenticação Google</Label>
            </div>

            {!isAuthenticated ? (
              <div className="space-y-2">
                <p className="text-sm text-gray-600 dark:text-gray-400">
                  Faça login com sua conta Google para continuar.
                </p>
                <Button
                  onClick={handleAuthenticate}
                  disabled={isLoading}
                  className="w-full"
                  variant="outline"
                >
                  {isLoading ? (
                    <Loader2 className="w-4 h-4 mr-2 animate-spin" />
                  ) : (
                    <ExternalLink className="w-4 h-4 mr-2" />
                  )}
                  Conectar com Google Drive
                </Button>
              </div>
            ) : (
              <div className="flex items-center justify-between p-2 bg-green-50 dark:bg-green-900/20 rounded-lg">
                <div className="flex items-center gap-2">
                  <CheckCircle className="w-4 h-4 text-green-600" />
                  <span className="text-sm text-green-800 dark:text-green-200">
                    Conectado com sucesso
                  </span>
                </div>
                <Button
                  onClick={handleSignOut}
                  variant="ghost"
                  size="sm"
                  className="text-xs"
                >
                  Desconectar
                </Button>
              </div>
            )}
          </div>

          {/* Step 2: Folder Selection/Creation */}
          {isAuthenticated && (
            <div className="space-y-3">
              <div className="flex items-center gap-2">
                <Badge variant={selectedFolder ? "default" : "secondary"} className="text-xs">
                  {selectedFolder ? "✓" : "2"}
                </Badge>
                <Label className="text-sm font-medium">Pasta de Projetos</Label>
              </div>

              {!selectedFolder ? (
                <div className="space-y-3">
                  <p className="text-sm text-gray-600 dark:text-gray-400">
                    Escolha ou crie uma pasta onde seus projetos serão salvos.
                  </p>
                  
                  <div className="space-y-2">
                    <Label htmlFor="folder-name" className="text-sm">
                      Nome da pasta
                    </Label>
                    <Input
                      id="folder-name"
                      value={customFolderName}
                      onChange={(e) => setCustomFolderName(e.target.value)}
                      placeholder="Block Content Editor Projects"
                      className="text-sm"
                    />
                  </div>

                  <Button
                    onClick={handleCreateFolder}
                    disabled={isCreatingFolder || !customFolderName.trim()}
                    className="w-full"
                    size="sm"
                  >
                    {isCreatingFolder ? (
                      <Loader2 className="w-4 h-4 mr-2 animate-spin" />
                    ) : (
                      <FolderPlus className="w-4 h-4 mr-2" />
                    )}
                    Criar/Encontrar Pasta
                  </Button>
                </div>
              ) : (
                <div className="p-2 bg-green-50 dark:bg-green-900/20 rounded-lg">
                  <div className="flex items-center gap-2">
                    <CheckCircle className="w-4 h-4 text-green-600" />
                    <span className="text-sm text-green-800 dark:text-green-200">
                      Pasta configurada: <strong>{folderName}</strong>
                    </span>
                  </div>
                </div>
              )}
            </div>
          )}

          {/* Permissions Info */}
          <div className="text-xs text-gray-500 dark:text-gray-400 space-y-1">
            <p><strong>Permissões solicitadas:</strong></p>
            <ul className="list-disc list-inside space-y-0.5 ml-2">
              <li>Criar, ler, editar e excluir apenas arquivos criados por esta aplicação</li>
              <li>Criar pastas para organizar projetos</li>
              <li>Acesso limitado à pasta selecionada</li>
            </ul>
          </div>
        </div>

        <DialogFooter>
          <Button variant="outline" onClick={() => onOpenChange(false)}>
            Cancelar
          </Button>
          {hasValidSetup && (
            <Button onClick={handleComplete} className="bg-green-600 hover:bg-green-700">
              <CheckCircle className="w-4 h-4 mr-2" />
              Configuração Completa
            </Button>
          )}
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
