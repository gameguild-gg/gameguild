"use client"

import { useCallback, useState } from "react"
import { useLexicalComposerContext } from "@lexical/react/LexicalComposerContext"
import { $getSelection, $isRangeSelection } from "lexical"
import { TOGGLE_LINK_COMMAND, $isLinkNode } from "@lexical/link"
import { LinkIcon } from "lucide-react"
import { DropdownMenuItem } from "@/components/ui/dropdown-menu"
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogFooter } from "@/components/ui/dialog"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Button } from "@/components/ui/button"
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select"

interface LinkMenuComponentProps {
  // Props vazias por enquanto, mas permitindo extensibilidade futura
}

export function LinkMenuComponent({}: LinkMenuComponentProps) {
  const [editor] = useLexicalComposerContext()
  const [showLinkDialog, setShowLinkDialog] = useState(false)
  const [linkUrl, setLinkUrl] = useState("")
  const [linkProtocol, setLinkProtocol] = useState("https://")
  const [selectedText, setSelectedText] = useState("")
  const [isEditingExistingLink, setIsEditingExistingLink] = useState(false)

  const handleInsertLink = useCallback(() => {
    if (linkUrl.trim()) {
      let finalUrl = linkUrl.trim()
      
      // Processar diferentes tipos de links
      if (linkProtocol === "/") {
        // Link local - garantir que comece com /
        finalUrl = finalUrl.startsWith("/") ? finalUrl : `/${finalUrl}`
      } else if (linkProtocol === "#/") {
        // Link de âncora - garantir que comece com #
        finalUrl = finalUrl.startsWith("#") ? finalUrl : `#${finalUrl}`
      } else {
        // Links HTTP/HTTPS - adicionar protocolo se não estiver presente
        if (!finalUrl.match(/^https?:\/\//)) {
          finalUrl = linkProtocol + finalUrl
        }
      }
      
      editor.dispatchCommand(TOGGLE_LINK_COMMAND, finalUrl)
      setShowLinkDialog(false)
      setLinkUrl("")
      setSelectedText("")
      setLinkProtocol("https://") // Resetar para o padrão
      setIsEditingExistingLink(false) // Resetar o estado de edição
    }
  }, [editor, linkUrl, linkProtocol])

  const handleLinkButtonClick = useCallback(() => {
    // Usar setTimeout para evitar conflitos de eventos
    setTimeout(() => {
      editor.getEditorState().read(() => {
        const selection = $getSelection()
        if ($isRangeSelection(selection)) {
          const text = selection.getTextContent()
          setSelectedText(text)
          
          // Verificar se o texto selecionado já é um link
          const nodes = selection.getNodes()
          let existingUrl = ""
          let existingProtocol = "https://"
          
          // Procurar por nós de link na seleção
          for (const node of nodes) {
            const parent = node.getParent()
            if ($isLinkNode(parent)) {
              existingUrl = parent.getURL()
              break
            }
            if ($isLinkNode(node)) {
              existingUrl = node.getURL()
              break
            }
          }
          
          // Se encontrou um link existente, extrair protocolo e URL
          if (existingUrl) {
            setIsEditingExistingLink(true)
            if (existingUrl.startsWith("https://")) {
              existingProtocol = "https://"
              existingUrl = existingUrl.replace("https://", "")
            } else if (existingUrl.startsWith("http://")) {
              existingProtocol = "http://"
              existingUrl = existingUrl.replace("http://", "")
            } else if (existingUrl.startsWith("/")) {
              existingProtocol = "/"
              existingUrl = existingUrl
            } else if (existingUrl.startsWith("#")) {
              existingProtocol = "#/"
              existingUrl = existingUrl.replace("#", "")
            }
            
            setLinkProtocol(existingProtocol)
            setLinkUrl(existingUrl)
          } else {
            // Resetar para valores padrão se não houver link existente
            setIsEditingExistingLink(false)
            setLinkProtocol("https://")
            setLinkUrl("")
          }
          
          setShowLinkDialog(true)
        }
      })
    }, 100) // Pequeno delay para evitar conflitos
  }, [editor])

  // Função para validar se o link é válido baseado no tipo
  const isLinkValid = useCallback(() => {
    if (!linkUrl.trim()) return false
    
    // Para links locais e âncoras, aceitar qualquer coisa não vazia
    if (linkProtocol === "/" || linkProtocol === "#/") {
      return true
    }
    
    // Para HTTP/HTTPS, validar formato básico de URL
    const url = linkUrl.trim()
    return url.length > 0 && !url.includes(" ")
  }, [linkUrl, linkProtocol])

  return (
    <>
      <DropdownMenuItem 
        onSelect={(e) => e.preventDefault()}
        onClick={(e) => {
          e.preventDefault()
          e.stopPropagation()
          handleLinkButtonClick()
        }}
      >
        <LinkIcon className="mr-2 h-5 w-5" />
        <span>Link</span>
      </DropdownMenuItem>

      <Dialog 
        open={showLinkDialog} 
        onOpenChange={(open) => {
          setShowLinkDialog(open)
          if (!open) {
            // Resetar estados quando o diálogo é fechado
            setLinkUrl("")
            setSelectedText("")
            setLinkProtocol("https://")
            setIsEditingExistingLink(false)
          }
        }}
      >
        <DialogContent className="sm:max-w-md">
          <DialogHeader>
            <DialogTitle>
              {isEditingExistingLink ? "Edit Link" : "Add Link"}
            </DialogTitle>
          </DialogHeader>
          <div className="space-y-4">
            <div>
              <Label htmlFor="selected-text">Selected Text</Label>
              <Input id="selected-text" value={selectedText} readOnly className="bg-muted" />
            </div>
            <div>
              <Label htmlFor="link-protocol">Link Type</Label>
              <Select value={linkProtocol} onValueChange={setLinkProtocol}>
                <SelectTrigger>
                  <SelectValue placeholder="Select protocol" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="https://">HTTPS (Secure)</SelectItem>
                  <SelectItem value="http://">HTTP (Insecure)</SelectItem>
                  <SelectItem value="/">Local Link</SelectItem>
                  <SelectItem value="#/">Anchor Link</SelectItem>
                </SelectContent>
              </Select>
            </div>
            <div>
              <Label htmlFor="link-url">
                {linkProtocol === "/" ? "Path" : linkProtocol === "#/" ? "Anchor" : "URL"}
              </Label>
              <div className="flex items-center space-x-2">
                {linkProtocol !== "/" && linkProtocol !== "#/" && (
                  <span className="text-sm text-muted-foreground min-w-fit">{linkProtocol}</span>
                )}
                <Input
                  id="link-url"
                  type="text"
                  placeholder={
                    linkProtocol === "/" 
                      ? "/page-name" 
                      : linkProtocol === "#/"
                      ? "section-name"
                      : "example.com"
                  }
                  value={linkUrl}
                  onChange={(e) => setLinkUrl(e.target.value)}
                  onKeyDown={(e) => {
                    if (e.key === "Enter") {
                      handleInsertLink()
                    }
                  }}
                  className="flex-1"
                />
              </div>
            </div>
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setShowLinkDialog(false)}>
              Cancel
            </Button>
            <Button onClick={handleInsertLink} disabled={!isLinkValid()}>
              {isEditingExistingLink ? "Update Link" : "Add Link"}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </>
  )
}
