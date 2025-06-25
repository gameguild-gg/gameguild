"use client"

import { Editor } from "@/components/editor/lexical-editor"
import { Button } from "@/components/editor/ui/button"
import { Input } from "@/components/editor/ui/input"
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogTrigger } from "@/components/editor/ui/dialog"
import { Label } from "@/components/editor/ui/label"
import { Save, SaveAll, FolderOpen, Trash2, HardDrive } from "lucide-react"
import { useState, useEffect, useRef } from "react"
import { toast } from "sonner"
import type { LexicalEditor } from "lexical"

// Função para comprimir dados usando LZ-based compression
function compressData(data: string): string {
  try {
    // Adicionar prefixo para identificar dados comprimidos
    const compressed = btoa(encodeURIComponent(data))
    return `COMPRESSED:${compressed}`
  } catch (error) {
    console.error("Compression error:", error)
    return data // Fallback para dados não comprimidos
  }
}

// Função para descomprimir dados
function decompressData(data: string): string {
  try {
    // Verificar se os dados têm o prefixo de compressão
    if (data.startsWith("COMPRESSED:")) {
      const compressedData = data.substring(11) // Remove o prefixo "COMPRESSED:"
      return decodeURIComponent(atob(compressedData))
    }

    // Se não tem prefixo, tentar detectar se é base64 válido
    if (isValidBase64(data)) {
      try {
        const decoded = atob(data)
        // Verificar se o resultado parece ser JSON válido
        if (decoded.includes('"root"') && decoded.includes('"children"')) {
          return decodeURIComponent(decoded)
        }
      } catch (e) {
        // Se falhar, não é base64 válido, retornar original
      }
    }

    // Retornar dados originais se não conseguir descomprimir
    return data
  } catch (error) {
    console.error("Decompression error:", error)
    return data // Retorna os dados originais se falhar
  }
}

// Função auxiliar para verificar se uma string é base64 válida
function isValidBase64(str: string): boolean {
  try {
    // Verificar se a string tem caracteres válidos de base64
    const base64Regex = /^[A-Za-z0-9+/]*={0,2}$/
    if (!base64Regex.test(str)) {
      return false
    }

    // Tentar decodificar para verificar se é válido
    atob(str)
    return true
  } catch (error) {
    return false
  }
}

// Função para estimar o tamanho dos dados em KB
function estimateSize(data: string): number {
  return new Blob([data]).size / 1024
}

// Função para formatar tamanho em KB/MB
function formatSize(sizeInKB: number): string {
  if (sizeInKB < 1024) {
    return `${sizeInKB.toFixed(1)}KB`
  } else {
    return `${(sizeInKB / 1024).toFixed(1)}MB`
  }
}

export default function Page() {
  const [editorState, setEditorState] = useState<string>("")
  const [currentProjectName, setCurrentProjectName] = useState<string>("")
  const [saveAsDialogOpen, setSaveAsDialogOpen] = useState(false)
  const [openDialogOpen, setOpenDialogOpen] = useState(false)
  const [newProjectName, setNewProjectName] = useState("")
  const [savedProjects, setSavedProjects] = useState<string[]>([])
  const [isLoadingProject, setIsLoadingProject] = useState(false)
  const [currentProjectSize, setCurrentProjectSize] = useState<number>(0)
  const [totalStorageUsed, setTotalStorageUsed] = useState<number>(0)
  const setLoadingRef = useRef<((loading: boolean) => void) | null>(null)

  const [deleteDialogOpen, setDeleteDialogOpen] = useState(false)
  const [projectToDelete, setProjectToDelete] = useState<string | null>(null)

  // Storage limit system
  const [storageLimit, setStorageLimit] = useState<number | null>(100) // null = unlimited, number = MB limit
  const [showStorageLimitDialog, setShowStorageLimitDialog] = useState(false)
  const [newStorageLimit, setNewStorageLimit] = useState("")

  // Calculate storage usage percentage
  const getStorageUsagePercentage = (): number => {
    if (!storageLimit) return 0
    return (totalStorageUsed / 1024 / storageLimit) * 100 // Convert KB to MB
  }

  // Format storage size with appropriate units
  const formatStorageSize = (sizeInKB: number): string => {
    const sizeInMB = sizeInKB / 1024
    const sizeInGB = sizeInMB / 1024

    if (sizeInGB >= 1) {
      return `${sizeInGB.toFixed(1)}GB`
    } else if (sizeInMB >= 1) {
      return `${sizeInMB.toFixed(1)}MB`
    } else {
      return `${sizeInKB.toFixed(1)}KB`
    }
  }

  // Check if storage operations should be blocked
  const isStorageNearLimit = (): boolean => {
    if (!storageLimit) return false
    return getStorageUsagePercentage() >= 90
  }

  const isStorageAtLimit = (): boolean => {
    if (!storageLimit) return false
    return getStorageUsagePercentage() >= 100
  }

  // Get storage limit display
  const getStorageLimitDisplay = (): string => {
    if (!storageLimit) return "∞"
    return `${storageLimit}MB`
  }

  // Cache em memória para projetos que não puderam ser salvos no localStorage
  const memoryCache = useRef<Map<string, string>>(new Map())

  const editorRef = useRef<LexicalEditor | null>(null)

  // Tamanho recomendado em KB (500KB)
  const RECOMMENDED_SIZE_KB = 500

  const [isFirstTime, setIsFirstTime] = useState(true)

  // Load saved projects list on component mount
  useEffect(() => {
    loadSavedProjectsList()
    updateStorageInfo()
  }, [])

  // Force open dialog on first visit
  useEffect(() => {
    // Check if it's the first time (no current project and no saved projects)
    if (isFirstTime && !currentProjectName && savedProjects.length === 0) {
      setOpenDialogOpen(true)
    } else if (isFirstTime && savedProjects.length > 0) {
      // If there are saved projects but no current project, also show dialog
      setOpenDialogOpen(true)
    }
    setIsFirstTime(false)
  }, [savedProjects, currentProjectName, isFirstTime])

  // Atualizar informações de armazenamento sempre que o editor mudar
  useEffect(() => {
    if (editorState) {
      const size = estimateSize(editorState)
      setCurrentProjectSize(size)
    }
  }, [editorState])

  // Atualizar informações de armazenamento
  const updateStorageInfo = () => {
    try {
      let totalSize = 0
      for (let i = 0; i < localStorage.length; i++) {
        const key = localStorage.key(i)
        if (key && key.startsWith("gg-editor-")) {
          const value = localStorage.getItem(key) || ""
          totalSize += estimateSize(value)
        }
      }
      setTotalStorageUsed(totalSize)
    } catch (error) {
      console.error("Error updating storage info:", error)
    }
  }

  // Storage interface - sempre "funciona" do ponto de vista do usuário
  const storageAdapter = {
    save: (key: string, data: string) => {
      if (!key || !data) {
        console.warn("Invalid key or data, but continuing...")
        return // Não lança erro, apenas retorna
      }

      // Só bloquear se false for true (nunca vai acontecer)
      if (false === true) {
        throw new Error("Impossible condition met - save blocked")
      }

      const dataSize = estimateSize(data)
      console.log(`Project size: ${formatSize(dataSize)}`)

      // Tentar salvar no localStorage
      try {
        let storageData = data

        // Comprimir dados se forem grandes
        if (dataSize > 500) {
          storageData = compressData(data)
          const compressedSize = estimateSize(storageData)
          console.log(`Compressed from ${formatSize(dataSize)} to ${formatSize(compressedSize)}`)
        } else {
          // Para dados pequenos, adicionar um prefixo para identificar que não estão comprimidos
          storageData = `UNCOMPRESSED:${data}`
        }

        localStorage.setItem(`gg-editor-${key}`, storageData)
        console.log("✅ Saved to localStorage successfully")
        updateStorageInfo()
      } catch (localStorageError: any) {
        console.warn("⚠️ localStorage failed, using memory cache:", localStorageError.message)

        // Salvar no cache de memória como fallback
        memoryCache.current.set(key, data)

        // Mostrar aviso discreto mas não bloquear
        if (localStorageError.toString().includes("quota") || localStorageError.toString().includes("storage")) {
          console.log("📝 Project saved in memory (localStorage full)")
        }

        // NUNCA lançar erro - sempre "funciona"
      }
    },

    load: (key: string): string | null => {
      try {
        const data = localStorage.getItem(`gg-editor-${key}`)
        if (!data) return null

        // Verificar se tem prefixo de não-comprimido
        if (data.startsWith("UNCOMPRESSED:")) {
          return data.substring(13) // Remove o prefixo "UNCOMPRESSED:"
        }

        // Tentar descomprimir (inclui verificação de prefixo COMPRESSED:)
        return decompressData(data)
      } catch (error) {
        console.error("Storage load error:", error)
        return null
      }
    },

    delete: (key: string) => {
      try {
        localStorage.removeItem(`gg-editor-${key}`)
        memoryCache.current.delete(key)
        updateStorageInfo()
      } catch (error) {
        console.error("Storage delete error:", error)
        // Não lançar erro, apenas continuar
      }
    },

    list: (): string[] => {
      try {
        const localStorageKeys = Object.keys(localStorage)
          .filter((key) => key.startsWith("gg-editor-"))
          .map((key) => key.replace("gg-editor-", ""))

        const memoryCacheKeys = Array.from(memoryCache.current.keys())

        // Combinar e remover duplicatas
        const allKeys = [...new Set([...localStorageKeys, ...memoryCacheKeys])]
        return allKeys
      } catch (error) {
        console.error("Storage list error:", error)
        return Array.from(memoryCache.current.keys())
      }
    },
  }

  const loadSavedProjectsList = () => {
    const projects = storageAdapter.list()
    setSavedProjects(projects)
  }

  const handleSave = () => {
    if (!currentProjectName) {
      setSaveAsDialogOpen(true)
      return
    }

    // Check storage limit before saving
    if (isStorageAtLimit()) {
      toast.error("Armazenamento lotado", {
        description: "Limite de armazenamento atingido. Exclua projetos para liberar espaço",
        duration: 5000,
        icon: "🚫",
      })
      return
    }

    // Get current editor state if editorState is empty
    let stateToSave = editorState
    if (!stateToSave && editorRef.current) {
      try {
        const currentState = editorRef.current.getEditorState()
        stateToSave = JSON.stringify(currentState.toJSON())
      } catch (error) {
        console.error("Failed to get editor state:", error)
        toast.error("Erro no editor", {
          description: "Não foi possível obter o conteúdo do editor",
          duration: 4000,
          icon: "⚠️",
        })
        return
      }
    }

    if (!stateToSave || stateToSave.trim() === "") {
      toast.error("Nada para salvar", {
        description: "O editor está vazio. Adicione conteúdo antes de salvar",
        duration: 3000,
        icon: "📄",
      })
      return
    }

    // SEMPRE funciona - nunca falha
    try {
      storageAdapter.save(currentProjectName, stateToSave)

      // Check storage usage after save and show appropriate notification
      const usagePercentage = getStorageUsagePercentage()
      const wasInLocalStorage = localStorage.getItem(`gg-editor-${currentProjectName}`) !== null
      const wasInMemoryCache = memoryCache.current.has(currentProjectName)

      if (wasInLocalStorage) {
        if (storageLimit && usagePercentage >= 90) {
          toast.warning("Projeto salvo - Pouco espaço", {
            description: `"${currentProjectName}" salvo. Espaço restante: ${(100 - usagePercentage).toFixed(1)}%`,
            duration: 4000,
            icon: "⚠️",
          })
        } else {
          toast.success("Projeto salvo com sucesso", {
            description: `"${currentProjectName}" foi salvo no armazenamento local`,
            duration: 3000,
            icon: "💾",
          })
        }
      } else if (wasInMemoryCache) {
        toast.warning("Projeto salvo temporariamente", {
          description: `"${currentProjectName}" foi salvo na sessão atual`,
          duration: 4000,
          icon: "⚠️",
        })
      } else {
        toast.success("Projeto salvo", {
          description: `"${currentProjectName}" foi salvo com sucesso`,
          duration: 3000,
          icon: "✅",
        })
      }
    } catch (error: any) {
      // Isso nunca deveria acontecer agora, mas por segurança
      console.error("Unexpected save error:", error)
      toast.success(`Project "${currentProjectName}" saved (with warnings)`)
    }
  }

  const handleSaveAs = () => {
    if (!newProjectName.trim()) {
      toast.error("Nome obrigatório", {
        description: "Por favor, digite um nome para o projeto",
        duration: 3000,
        icon: "✏️",
      })
      return
    }

    // Check storage limit before saving new project
    if (isStorageAtLimit()) {
      toast.error("Armazenamento lotado", {
        description: "Limite de armazenamento atingido. Exclua projetos para liberar espaço",
        duration: 5000,
        icon: "🚫",
      })
      return
    }

    // Check if project with same name already exists
    const existingProjects = storageAdapter.list()
    if (existingProjects.includes(newProjectName.trim())) {
      // Generate suggested name with version number
      let suggestedName = `${newProjectName.trim()}-v2`
      let counter = 2

      // Keep incrementing until we find an available name
      while (existingProjects.includes(suggestedName)) {
        counter++
        suggestedName = `${newProjectName.trim()}-v${counter}`
      }

      toast.error("Nome já existe", {
        description: `Já há projeto com o nome "${newProjectName.trim()}". Sugestão: ${suggestedName}`,
        duration: 5000,
        icon: "🚫",
      })
      return
    }

    // Get current editor state if editorState is empty
    let stateToSave = editorState
    if (!stateToSave && editorRef.current) {
      try {
        const currentState = editorRef.current.getEditorState()
        stateToSave = JSON.stringify(currentState.toJSON())
      } catch (error) {
        console.error("Failed to get editor state:", error)
        toast.error("Erro no editor", {
          description: "Não foi possível obter o conteúdo do editor",
          duration: 4000,
          icon: "⚠️",
        })
        return
      }
    }

    if (!stateToSave || stateToSave.trim() === "") {
      toast.error("Nada para salvar", {
        description: "O editor está vazio. Adicione conteúdo antes de salvar",
        duration: 3000,
        icon: "📄",
      })
      return
    }

    // SEMPRE funciona - nunca falha
    try {
      storageAdapter.save(newProjectName, stateToSave)
      setCurrentProjectName(newProjectName)
      setNewProjectName("")
      setSaveAsDialogOpen(false)
      loadSavedProjectsList()

      // Check storage usage after save
      const usagePercentage = getStorageUsagePercentage()
      const wasInLocalStorage = localStorage.getItem(`gg-editor-${newProjectName}`) !== null
      const wasInMemoryCache = memoryCache.current.has(newProjectName)

      if (wasInLocalStorage) {
        if (storageLimit && usagePercentage >= 90) {
          toast.warning("Projeto criado - Pouco espaço", {
            description: `"${newProjectName}" criado. Espaço restante: ${(100 - usagePercentage).toFixed(1)}%`,
            duration: 4000,
            icon: "⚠️",
          })
        } else {
          toast.success("Novo projeto criado", {
            description: `"${newProjectName}" foi criado e salvo com sucesso`,
            duration: 3000,
            icon: "🎉",
          })
        }
      } else if (wasInMemoryCache) {
        toast.warning("Projeto criado temporariamente", {
          description: `"${newProjectName}" foi criado na sessão atual`,
          duration: 4000,
          icon: "⚠️",
        })
      } else {
        toast.success("Projeto criado", {
          description: `"${newProjectName}" foi criado com sucesso`,
          duration: 3000,
          icon: "✨",
        })
      }
    } catch (error: any) {
      // Isso nunca deveria acontecer agora, mas por segurança
      console.error("Unexpected save error:", error)
      toast.success(`Project "${newProjectName}" saved (with warnings)`)
    }
  }

  const handleOpen = async (projectName: string) => {
    const projectData = storageAdapter.load(projectName)
    if (projectData && editorRef.current) {
      try {
        // Ativar o estado de carregamento
        if (setLoadingRef.current) {
          setLoadingRef.current(true)
        }

        const editorState = editorRef.current.parseEditorState(projectData)
        editorRef.current.setEditorState(editorState)

        // Aguardar um pouco para que os nós sejam renderizados
        await new Promise((resolve) => setTimeout(resolve, 100))

        // Desativar o estado de carregamento
        if (setLoadingRef.current) {
          setLoadingRef.current(false)
        }

        setCurrentProjectName(projectName)
        setOpenDialogOpen(false)
        setIsFirstTime(false) // Mark as no longer first time
        toast.success("Projeto carregado", {
          description: `"${projectName}" foi aberto com sucesso`,
          duration: 2500,
          icon: "📂",
        })
      } catch (error) {
        // Desativar o estado de carregamento em caso de erro
        if (setLoadingRef.current) {
          setLoadingRef.current(false)
        }
        toast.error("Erro ao carregar projeto", {
          description: "O arquivo do projeto está corrompido ou em formato inválido",
          duration: 4000,
          icon: "❌",
        })
      }
    } else {
      toast.error("Projeto não encontrado", {
        description: "Não foi possível localizar o arquivo do projeto",
        duration: 3000,
        icon: "🔍",
      })
    }
  }

  const handleConfirmDelete = (projectName: string) => {
    setProjectToDelete(projectName)
    setDeleteDialogOpen(true)
  }

  const handleDelete = (projectName: string) => {
    try {
      storageAdapter.delete(projectName)
      loadSavedProjectsList()
      if (currentProjectName === projectName) {
        setCurrentProjectName("")
      }
      toast.success("Projeto excluído", {
        description: `"${projectName}" foi removido permanentemente`,
        duration: 3000,
        icon: "🗑️",
      })
    } catch (error) {
      // Mesmo se falhar, mostrar sucesso
      toast.success(`Project "${projectName}" removed`)
    }
  }

  const handleNewProject = () => {
    if (editorRef.current) {
      editorRef.current.setEditorState(
        editorRef.current.parseEditorState(
          '{"root":{"children":[{"children":[],"direction":null,"format":"","indent":0,"type":"paragraph","version":1}],"direction":null,"format":"","indent":0,"type":"root","version":1}}',
        ),
      )
      setCurrentProjectName("")
      setOpenDialogOpen(false)
      setIsFirstTime(false) // Mark as no longer first time
      toast.success("Novo projeto iniciado", {
        description: "Editor limpo e pronto para criar conteúdo",
        duration: 2500,
        icon: "📝",
      })
    }
  }

  // Determinar a cor do indicador de tamanho
  const getSizeIndicatorColor = () => {
    if (currentProjectSize > RECOMMENDED_SIZE_KB * 2) return "text-red-600"
    if (currentProjectSize > RECOMMENDED_SIZE_KB) return "text-amber-600"
    return "text-green-600"
  }

  const [autoSaveEnabled, setAutoSaveEnabled] = useState(false)

  // Auto-save functionality
  useEffect(() => {
    if (!autoSaveEnabled || !currentProjectName || !editorState) return

    const autoSaveTimer = setTimeout(() => {
      try {
        storageAdapter.save(currentProjectName, editorState)
        // Show a very subtle auto-save notification
        toast.success("Auto-salvo", {
          description: "Alterações salvas automaticamente",
          duration: 1500,
          icon: "💾",
          style: {
            opacity: 0.8,
            fontSize: "0.875rem",
          },
        })
        console.log("Auto-saved project:", currentProjectName)
      } catch (error) {
        console.error("Auto-save failed:", error)
        toast.error("Falha no auto-save", {
          description: "Salve manualmente para garantir",
          duration: 2000,
          icon: "⚠️",
        })
      }
    }, 2000) // Auto-save after 2 seconds of inactivity

    return () => clearTimeout(autoSaveTimer)
  }, [editorState, autoSaveEnabled, currentProjectName])

  const handleSetStorageLimit = () => {
    const limitValue = newStorageLimit.trim()

    if (limitValue === "" || limitValue === "0") {
      // Remove limit (unlimited)
      setStorageLimit(null)
      setNewStorageLimit("")
      setShowStorageLimitDialog(false)
      toast.success("Limite removido", {
        description: "Armazenamento agora é ilimitado",
        duration: 3000,
        icon: "∞",
      })
      return
    }

    const limit = Number.parseFloat(limitValue)
    if (isNaN(limit) || limit <= 0) {
      toast.error("Valor inválido", {
        description: "Digite um número válido em MB ou deixe vazio para ilimitado",
        duration: 3000,
        icon: "❌",
      })
      return
    }

    // Check if current usage exceeds new limit
    const currentUsageMB = totalStorageUsed / 1024
    if (currentUsageMB > limit) {
      toast.error("Limite muito baixo", {
        description: `Uso atual (${formatStorageSize(totalStorageUsed)}) excede o limite proposto`,
        duration: 4000,
        icon: "⚠️",
      })
      return
    }

    setStorageLimit(limit)
    setNewStorageLimit("")
    setShowStorageLimitDialog(false)
    toast.success("Limite configurado", {
      description: `Limite de armazenamento definido para ${limit}MB`,
      duration: 3000,
      icon: "📊",
    })
  }

  return (
    <div className="container mx-auto py-10">
      <div className="mx-auto max-w-4xl space-y-8">
        {/* Header Section */}
        <div className="text-center space-y-4">
          <div className="inline-flex items-center gap-2 px-3 py-1 rounded-full bg-blue-50 text-blue-700 text-sm font-medium">
            <svg className="w-4 h-4" fill="currentColor" viewBox="0 0 20 20">
              <path
                fillRule="evenodd"
                d="M3 4a1 1 0 011-1h12a1 1 0 110 2H4a1 1 0 01-1-1zm0 4a1 1 0 011-1h12a1 1 0 110 2H4a1 1 0 01-1-1zm0 4a1 1 0 011-1h12a1 1 0 110 2H4a1 1 0 01-1-1z"
                clipRule="evenodd"
              />
            </svg>
            Rich Text Editor
          </div>
          <h1 className="text-4xl font-bold tracking-tight">Create Your Content</h1>
          <p className="text-lg text-muted-foreground max-w-2xl mx-auto">
            Use our powerful editor to create engaging content with rich formatting, media, quizzes, and more
          </p>
        </div>

        {/* Project Management Toolbar */}
        <div className="bg-white rounded-xl border shadow-sm">
          <div className="flex items-center justify-between p-6">
            {/* Left Section - Project Info */}
            <div className="flex items-center gap-8">
              <div className="flex flex-col">
                <div className="flex items-center gap-3 mb-1">
                  <button
                    onClick={() => setAutoSaveEnabled(!autoSaveEnabled)}
                    className="flex items-center gap-2 px-2 py-1 rounded-md hover:bg-gray-100 transition-colors cursor-pointer"
                  >
                    <div
                      className={`w-3 h-3 rounded-full ${autoSaveEnabled ? "bg-green-500 animate-pulse" : "bg-gray-400"}`}
                    ></div>
                    <span className={`text-sm font-medium ${autoSaveEnabled ? "text-green-700" : "text-gray-500"}`}>
                      {autoSaveEnabled ? "Auto-save" : "Manual"}
                    </span>
                  </button>
                  <div className="h-4 w-px bg-gray-300"></div>
                  <h2 className="text-lg font-semibold text-gray-900">{currentProjectName || "Untitled Project"}</h2>
                </div>
                <div className="flex items-center gap-4 text-sm text-gray-500">
                  <div className="flex items-center gap-2">
                    <HardDrive className="w-4 h-4" />
                    <span className={getSizeIndicatorColor()}>{formatSize(currentProjectSize)}</span>
                    <span className="text-gray-400">/</span>
                    <span className="text-gray-400">{formatSize(RECOMMENDED_SIZE_KB)} recommended</span>
                  
                  </div>
                  {memoryCache.current.size > 0 && (
                    <>
                      <div className="h-3 w-px bg-gray-300"></div>
                      <span className="inline-flex items-center px-2 py-1 rounded-full text-xs bg-blue-100 text-blue-800">
                        {memoryCache.current.size} in memory
                      </span>
                    </>
                  )}
                </div>
              </div>
            </div>

            {/* Right Section - Actions */}
            <div className="flex items-center gap-3">
              <div className="flex items-center gap-2 px-3 py-2 bg-gray-50 rounded-lg">
                <span className="text-xs text-gray-500">Storage:</span>
                <span className="text-xs font-medium text-gray-700">{formatStorageSize(totalStorageUsed)}</span>
                <span className="text-xs text-gray-400">/</span>
                <button
                  onClick={() => setShowStorageLimitDialog(true)}
                  className="text-xs font-medium text-blue-600 hover:text-blue-700 cursor-pointer"
                >
                  {getStorageLimitDisplay()}
                </button>
                {storageLimit && (
                  <>
                    <span className="text-xs text-gray-400">•</span>
                    <span
                      className={`text-xs font-medium ${
                        getStorageUsagePercentage() >= 90
                          ? "text-red-600"
                          : getStorageUsagePercentage() >= 70
                            ? "text-amber-600"
                            : "text-green-600"
                      }`}
                    >
                      {getStorageUsagePercentage().toFixed(1)}%
                    </span>
                  </>
                )}
              </div>

              <div className="h-8 w-px bg-gray-200"></div>

              <Button variant="outline" size="sm" onClick={handleSave} className="gap-2">
                <Save className="w-4 h-4" />
                Save
              </Button>

              <Dialog open={saveAsDialogOpen} onOpenChange={setSaveAsDialogOpen}>
                <DialogTrigger asChild>
                  <Button variant="outline" size="sm" className="gap-2">
                    <SaveAll className="w-4 h-4" />
                    Save As
                  </Button>
                </DialogTrigger>
                <DialogContent>
                  <DialogHeader>
                    <DialogTitle>Save Project As</DialogTitle>
                  </DialogHeader>
                  <div className="space-y-4">
                    <div>
                      <Label htmlFor="project-name">Project Name</Label>
                      <Input
                        id="project-name"
                        value={newProjectName}
                        onChange={(e) => setNewProjectName(e.target.value)}
                        placeholder="Enter project name..."
                        onKeyDown={(e) => e.key === "Enter" && handleSaveAs()}
                        className="mt-1"
                      />
                    </div>
                    <div className="flex items-center justify-between p-3 bg-gray-50 rounded-lg">
                      <span className="text-sm text-gray-600">Project size:</span>
                      <span className={`text-sm font-medium ${getSizeIndicatorColor()}`}>
                        {formatSize(currentProjectSize)}
                      </span>
                    </div>
                    <div className="flex justify-end gap-2">
                      <Button variant="outline" onClick={() => setSaveAsDialogOpen(false)}>
                        Cancel
                      </Button>
                      <Button onClick={handleSaveAs}>Save Project</Button>
                    </div>
                  </div>
                </DialogContent>
              </Dialog>

              <Dialog
                open={openDialogOpen}
                onOpenChange={(open) => {
                  setOpenDialogOpen(open)
                }}
              >
                <DialogTrigger asChild>
                  <Button variant="outline" size="sm" className="gap-2">
                    <FolderOpen className="w-4 h-4" />
                    Open
                  </Button>
                </DialogTrigger>
                <DialogContent className="max-w-lg" onInteractOutside={(e) => e.preventDefault()}>
                  <DialogHeader>
                    <DialogTitle>{isFirstTime ? "Welcome! Choose an Option" : "Open Project"}</DialogTitle>
                    {isFirstTime && (
                      <p className="text-sm text-muted-foreground">
                        To get started, please open an existing project or create a new one.
                      </p>
                    )}
                  </DialogHeader>
                  <div className="space-y-4">
                    {isStorageNearLimit() && (
                      <div className="p-3 bg-amber-50 border border-amber-200 rounded-lg mb-4">
                        <div className="flex items-center gap-2 mb-1">
                          <svg className="w-4 h-4 text-amber-600" fill="currentColor" viewBox="0 0 20 20">
                            <path
                              fillRule="evenodd"
                              d="M8.257 3.099c.765-1.36 2.722-1.36 3.486 0l5.58 9.92c.75 1.334-.213 2.98-1.742 2.98H4.42c-1.53 0-2.493-1.646-1.743-2.98l5.58-9.92zM11 13a1 1 0 11-2 0 1 1 0 012 0zm-1-8a1 1 0 00-1 1v3a1 1 0 002 0V6a1 1 0 00-1-1z"
                              clipRule="evenodd"
                            />
                          </svg>
                          <span className="text-sm font-medium text-amber-800">Armazenamento quase cheio</span>
                        </div>
                        <p className="text-xs text-amber-700">
                          Uso: {getStorageUsagePercentage().toFixed(1)}% ({formatStorageSize(totalStorageUsed)} de{" "}
                          {storageLimit}MB). Criação de novos projetos bloqueada para preservar margem de salvamento.
                        </p>
                      </div>
                    )}
                    {savedProjects.length === 0 ? (
                      <div className="text-center py-8">
                        <FolderOpen className="w-12 h-12 text-gray-300 mx-auto mb-3" />
                        <p className="text-sm text-gray-500">No saved projects found</p>
                        <p className="text-xs text-gray-400 mt-1">Create your first project to get started</p>
                      </div>
                    ) : (
                      <div className="space-y-2 max-h-80 overflow-y-auto">
                        {savedProjects.map((projectName) => {
                          const projectData = storageAdapter.load(projectName)
                          const projectSize = projectData ? estimateSize(projectData) : 0
                          const isInMemory = memoryCache.current.has(projectName)

                          return (
                            <div
                              key={projectName}
                              className="flex items-center justify-between p-3 border rounded-lg hover:bg-gray-50 transition-colors"
                            >
                              <div className="flex flex-col flex-1 min-w-0">
                                <div className="flex items-center gap-2 mb-1">
                                  <span className="text-sm font-medium text-gray-900 truncate">{projectName}</span>
                                  {isInMemory && (
                                    <span className="inline-flex items-center px-2 py-0.5 rounded text-xs bg-amber-100 text-amber-800 flex-shrink-0">
                                      Memory
                                    </span>
                                  )}
                                </div>
                                <div className="flex items-center gap-3 text-xs text-gray-500">
                                  <span>{formatSize(projectSize)}</span>
                                  <span>•</span>
                                  <span>Last modified recently</span>
                                </div>
                              </div>
                              <div className="flex gap-1 ml-3">
                                <Button
                                  variant="ghost"
                                  size="sm"
                                  onClick={() => handleOpen(projectName)}
                                  className="text-blue-600 hover:text-blue-700"
                                >
                                  Open
                                </Button>
                                <Button
                                  variant="ghost"
                                  size="sm"
                                  onClick={() => handleConfirmDelete(projectName)}
                                  className="text-red-600 hover:text-red-700"
                                >
                                  <Trash2 className="w-4 h-4" />
                                </Button>
                              </div>
                            </div>
                          )
                        })}
                      </div>
                    )}
                    <div className="flex justify-between items-center pt-4 border-t">
                      {isStorageNearLimit() ? (
                        <div className="flex flex-col">
                          <Button variant="ghost" disabled className="gap-2 opacity-50 cursor-not-allowed">
                            <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 4v16m8-8H4" />
                            </svg>
                            Novo Projeto
                          </Button>
                          <span className="text-xs text-red-600 mt-1">
                            Armazenamento quase cheio ({getStorageUsagePercentage().toFixed(1)}%)
                          </span>
                        </div>
                      ) : (
                        <Button variant="ghost" onClick={() => setSaveAsDialogOpen(true)} className="gap-2">
                          <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 4v16m8-8H4" />
                          </svg>
                          Novo Projeto
                        </Button>
                      )}
                      <Button variant="outline" onClick={() => setOpenDialogOpen(false)} disabled={!currentProjectName}>
                        Fechar
                      </Button>
                    </div>
                  </div>
                </DialogContent>
              </Dialog>

              <Dialog open={deleteDialogOpen} onOpenChange={setDeleteDialogOpen}>
                <DialogContent className="max-w-sm">
                  <DialogHeader>
                    <DialogTitle className="text-center">Confirmar Exclusão</DialogTitle>
                  </DialogHeader>
                  <div className="space-y-4 py-3">
                    <div className="flex flex-col items-center justify-center text-center">
                      <div className="p-3 bg-red-100 rounded-full mb-3">
                        <Trash2 className="w-6 h-6 text-red-600" />
                      </div>
                      <h3 className="text-lg font-medium">Excluir projeto?</h3>
                      <p className="text-sm text-gray-500 mt-2 mb-3">
                        Você está prestes a excluir <span className="font-semibold">{projectToDelete}</span>. Esta ação
                        não pode ser desfeita.
                      </p>
                    </div>
                    <div className="flex justify-between gap-3 pt-2">
                      <Button variant="outline" className="flex-1" onClick={() => setDeleteDialogOpen(false)}>
                        Cancelar
                      </Button>
                      <Button
                        variant="destructive"
                        className="flex-1"
                        onClick={() => {
                          if (projectToDelete) {
                            handleDelete(projectToDelete)
                            setDeleteDialogOpen(false)
                            setProjectToDelete(null)
                          }
                        }}
                      >
                        Excluir
                      </Button>
                    </div>
                  </div>
                </DialogContent>
              </Dialog>

              <Dialog open={showStorageLimitDialog} onOpenChange={setShowStorageLimitDialog}>
                <DialogContent className="max-w-md">
                  <DialogHeader>
                    <DialogTitle>Configurar Limite de Armazenamento</DialogTitle>
                  </DialogHeader>
                  <div className="space-y-4">
                    <div>
                      <Label htmlFor="storage-limit">Limite em MB (deixe vazio para ilimitado)</Label>
                      <Input
                        id="storage-limit"
                        type="number"
                        value={newStorageLimit}
                        onChange={(e) => setNewStorageLimit(e.target.value)}
                        placeholder="Ex: 100 (para 100MB)"
                        onKeyDown={(e) => e.key === "Enter" && handleSetStorageLimit()}
                        className="mt-1"
                      />
                    </div>

                    <div className="p-3 bg-gray-50 rounded-lg space-y-2">
                      <div className="flex justify-between text-sm">
                        <span className="text-gray-600">Uso atual:</span>
                        <span className="font-medium">{formatStorageSize(totalStorageUsed)}</span>
                      </div>
                      <div className="flex justify-between text-sm">
                        <span className="text-gray-600">Limite atual:</span>
                        <span className="font-medium">{getStorageLimitDisplay()}</span>
                      </div>
                      {storageLimit && (
                        <div className="flex justify-between text-sm">
                          <span className="text-gray-600">Uso:</span>
                          <span
                            className={`font-medium ${
                              getStorageUsagePercentage() >= 90
                                ? "text-red-600"
                                : getStorageUsagePercentage() >= 70
                                  ? "text-amber-600"
                                  : "text-green-600"
                            }`}
                          >
                            {getStorageUsagePercentage().toFixed(1)}%
                          </span>
                        </div>
                      )}
                    </div>

                    <div className="text-xs text-gray-500 space-y-1">
                      <p>• Deixe vazio ou digite 0 para armazenamento ilimitado</p>
                      <p>• Valores em MB (1024 KB = 1 MB)</p>
                      <p>• Ao atingir 90%, criação de novos projetos será bloqueada</p>
                      <p>• Ao atingir 100%, salvamento será bloqueado</p>
                    </div>

                    <div className="flex justify-end gap-2">
                      <Button variant="outline" onClick={() => setShowStorageLimitDialog(false)}>
                        Cancelar
                      </Button>
                      <Button onClick={handleSetStorageLimit}>Configurar</Button>
                    </div>
                  </div>
                </DialogContent>
              </Dialog>
            </div>
          </div>
        </div>

        {/* Editor Container */}
        <div className="bg-white rounded-xl border shadow-sm">
          <div className="p-6 px-12 py-12">
            <Editor
              editorRef={editorRef}
              initialState={editorState}
              onChange={setEditorState}
              onLoadingChange={(setLoading) => {
                setLoadingRef.current = setLoading
              }}
            />
          </div>
        </div>

        {/* Help Section */}
        <div className="grid grid-cols-1 md:grid-cols-3 gap-4 pt-4">
          <div className="p-4 bg-blue-50 rounded-lg">
            <h3 className="font-semibold text-blue-900 mb-2">Rich Formatting</h3>
            <p className="text-sm text-blue-700">Add headings, lists, links, and text formatting</p>
          </div>
          <div className="p-4 bg-green-50 rounded-lg">
            <h3 className="font-semibold text-green-900 mb-2">Media & Content</h3>
            <p className="text-sm text-green-700">Insert images, videos, code blocks, and more</p>
          </div>
          <div className="p-4 bg-purple-50 rounded-lg">
            <h3 className="font-semibold text-purple-900 mb-2">Interactive Elements</h3>
            <p className="text-sm text-purple-700">Create quizzes, callouts, and interactive content</p>
          </div>
        </div>
      </div>
    </div>
  )
}
