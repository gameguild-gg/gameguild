"use client"
import { useState, useEffect, useRef, useContext } from "react"
import { DecoratorNode, type SerializedLexicalNode } from "lexical"
import { $getNodeByKey } from "lexical"
import { useLexicalComposerContext } from "@lexical/react/LexicalComposerContext"
import { Edit, Expand, LayoutGrid } from 'lucide-react'

import { Button } from "@/components/ui/button"
import { ContentEditMenu, type EditMenuOption } from "@/components/editor/extras/content-edit-menu"
import { SlidePlayer } from "@/components/editor/extras/presentation/slide-player"
import type { JSX } from "react/jsx-runtime"
import { EditorLoadingContext } from "../lexical-editor"
import type { MediaUploadResult } from "@/components/editor/extras/media-upload-dialog"

// Import the componentized parts
import type { PresentationData, Slide, SlideTheme, TransitionEffect, ParseProgress } from "./presentation/types"
import { PPTXParser } from "./presentation/parsers/pptx-parser"
import { ODPParser } from "./presentation/parsers/odp-parser"
import { PresentationSettings } from "./presentation/presentation-settings"
import { PresentationPreview } from "./presentation/presentation-preview"

export interface SerializedPresentationNode extends SerializedLexicalNode {
 type: "presentation"
 data: PresentationData
 version: 1
}

export class PresentationNode extends DecoratorNode<JSX.Element> {
 __data: PresentationData

 static getType(): string {
   return "presentation"
 }

 static clone(node: PresentationNode): PresentationNode {
   return new PresentationNode(node.__data, node.__key)
 }

 constructor(data: PresentationData, key?: string) {
   super(key)
   this.__data = {
     slides: data.slides || [],
     title: data.title || "Untitled Presentation",
     theme: data.theme || "light",
     transitionEffect: data.transitionEffect || "fade",
     autoAdvance: data.autoAdvance || false,
     autoAdvanceDelay: data.autoAdvanceDelay || 5,
     autoAdvanceLoop: data.autoAdvanceLoop || false,
     showControls: data.showControls !== undefined ? data.showControls : true,
     isNew: data.isNew,
     customThemeColor: data.customThemeColor,
   }
 }

 createDOM(): HTMLElement {
   return document.createElement("div")
 }

 updateDOM(): false {
   return false
 }

 setData(data: PresentationData): void {
   const writable = this.getWritable()
   writable.__data = data
 }

 exportJSON(): SerializedPresentationNode {
   return {
     type: "presentation",
     data: this.__data,
     version: 1,
   }
 }

 static importJSON(serializedNode: SerializedPresentationNode): PresentationNode {
   return new PresentationNode(serializedNode.data)
 }

 decorate(): JSX.Element {
   return <PresentationComponent data={this.__data} nodeKey={this.__key} />
 }
}

interface PresentationComponentProps {
 data: PresentationData
 nodeKey: string
}

function PresentationComponent({ data, nodeKey }: PresentationComponentProps) {
 const [editor] = useLexicalComposerContext()
 const isLoading = useContext(EditorLoadingContext)
 const [isEditing, setIsEditing] = useState((data.isNew || false) && !isLoading)
 const [slides, setSlides] = useState<Slide[]>(data.slides || [])
 const [title, setTitle] = useState(data.title || "Untitled Presentation")
 const [theme, setTheme] = useState<SlideTheme>(data.theme || "light")
 const [transitionEffect, setTransitionEffect] = useState<TransitionEffect>(data.transitionEffect || "fade")
 const [autoAdvance, setAutoAdvance] = useState(data.autoAdvance || false)
 const [autoAdvanceDelay, setAutoAdvanceDelay] = useState(data.autoAdvanceDelay || 5)
 const [autoAdvanceLoop, setAutoAdvanceLoop] = useState(data.autoAdvanceLoop || false)
 const [showControls, setShowControls] = useState(data.showControls !== undefined ? data.showControls : true)
 const [currentSlideIndex, setCurrentSlideIndex] = useState(0)
 const [showMenu, setShowMenu] = useState(false)
 const [isPresenting, setIsPresenting] = useState(false)
 const [fileError, setFileError] = useState<string | null>(null)
 const [isImporting, setIsImporting] = useState(false)
 const [importProgress, setImportProgress] = useState(0)
 const [importStatus, setImportStatus] = useState("")
 const [customThemeColor, setCustomThemeColor] = useState<string | null>(null)
 const [hasPresentation, setHasPresentation] = useState(false)
 const [isImageSlideshow, setIsImageSlideshow] = useState(false)

 const containerRef = useRef<HTMLDivElement>(null)

 // Remove isNew flag after first render
 useEffect(() => {
   if (data.isNew) {
     editor.update(() => {
       const node = $getNodeByKey(nodeKey)
       if (node instanceof PresentationNode) {
         const { isNew, ...rest } = data
         node.setData(rest)
       }
     })
   }
 }, [data, editor, nodeKey])

 useEffect(() => {
   if (isLoading) {
     setIsEditing(false)
   }
 }, [isLoading])

 // Sincronizar estado local com dados do nó
 useEffect(() => {
   setSlides(data.slides || [])
   setTitle(data.title || "Untitled Presentation")
   setTheme(data.theme || "light")
   setTransitionEffect(data.transitionEffect || "fade")
   setAutoAdvance(data.autoAdvance || false)
   setAutoAdvanceDelay(data.autoAdvanceDelay || 5)
   setAutoAdvanceLoop(data.autoAdvanceLoop || false)
   setShowControls(data.showControls !== undefined ? data.showControls : true)
   setCustomThemeColor(data.customThemeColor || null)
   
   // Detectar tipo de apresentação
   if (data.slides && data.slides.length > 0) {
     const hasImageSlides = data.slides.every(slide => slide.layout === "full-image" && slide.theme === "image")
     setIsImageSlideshow(hasImageSlides)
     setHasPresentation(!hasImageSlides)
   }
 }, [data])

 const updatePresentation = (newData: Partial<PresentationData>) => {
   editor.update(() => {
     const node = $getNodeByKey(nodeKey)
     if (node instanceof PresentationNode) {
       node.setData({
         ...data,
         ...newData,
         customThemeColor: customThemeColor,
       })
     }
   })
 }

 const handleSave = () => {
   updatePresentation({
     slides,
     title,
     theme,
     transitionEffect,
     autoAdvance,
     autoAdvanceDelay,
     autoAdvanceLoop,
     showControls,
   })
   setIsEditing(false)
 }

 const handleCancel = () => {
   // Reset to original values
   setSlides(data.slides || [])
   setTitle(data.title || "Untitled Presentation")
   setTheme(data.theme || "light")
   setTransitionEffect(data.transitionEffect || "fade")
   setAutoAdvance(data.autoAdvance || false)
   setAutoAdvanceDelay(data.autoAdvanceDelay || 5)
   setAutoAdvanceLoop(data.autoAdvanceLoop || false)
   setShowControls(data.showControls !== undefined ? data.showControls : true)
   setIsEditing(false)
 }

 // Helper function to convert data URL to blob
 const dataURLToBlob = (dataURL: string): Blob => {
   const arr = dataURL.split(",")
   const mime = arr[0].match(/:(.*?);/)?.[1] || "application/octet-stream"
   const bstr = atob(arr[1])
   let n = bstr.length
   const u8arr = new Uint8Array(n)
   while (n--) {
     u8arr[n] = bstr.charCodeAt(n)
   }
   return new Blob([u8arr], { type: mime })
 }

 const handleImportPresentation = async (result: MediaUploadResult) => {
   setIsImporting(true)
   setFileError(null)
   setImportProgress(0)
   setImportStatus("Starting import...")

   const parserCallbacks = {
     onProgress: (progress: ParseProgress) => {
       setImportProgress(progress.progress)
       setImportStatus(progress.status)
     },
   }

   try {
     let importedSlides: Slide[] = []

     if (result.type === "file") {
       // Convert data URL to blob and then to file
       const blob = dataURLToBlob(result.data)
       const file = new File([blob], result.name || "presentation", { type: blob.type })

       if (result.name?.endsWith(".json")) {
         const text = await file.text()
         const parsedData = JSON.parse(text)

         if (!Array.isArray(parsedData.slides)) {
           throw new Error("Invalid presentation format: slides array is missing")
         }

         importedSlides = parsedData.slides.map((slide: any) => ({
           id: slide.id,
           title: slide.title || "",
           content: slide.content || "",
           layout: slide.layout,
           theme: slide.theme,
           backgroundImage: slide.backgroundImage,
           backgroundGradient: slide.backgroundGradient,
           notes: slide.notes || "",
         }))

         if (parsedData.title) setTitle(parsedData.title)
         if (parsedData.theme) setTheme(parsedData.theme)
         if (parsedData.transitionEffect) setTransitionEffect(parsedData.transitionEffect)
         if (parsedData.autoAdvance !== undefined) setAutoAdvance(parsedData.autoAdvance)
         if (parsedData.autoAdvanceDelay) setAutoAdvanceDelay(parsedData.autoAdvanceDelay)
         if (parsedData.showControls !== undefined) setShowControls(parsedData.showControls)
       } else if (result.name?.endsWith(".pptx")) {
         const parser = new PPTXParser(parserCallbacks)
         importedSlides = await parser.parse(file)
         setTitle(result.name.replace(".pptx", ""))
       } else if (result.name?.endsWith(".odp")) {
         const parser = new ODPParser(parserCallbacks)
         importedSlides = await parser.parse(file)
         setTitle(result.name.replace(".odp", ""))
       }

       setHasPresentation(true)
       setIsImageSlideshow(false)
     } else if (result.type === "url") {
       // Handle URL imports
       setImportStatus("Downloading from URL...")
       const response = await fetch(result.data)
       if (!response.ok) {
         throw new Error(`Failed to download file: ${response.statusText}`)
       }

       const blob = await response.blob()
       const filename = result.data.split("/").pop() || "presentation"
       const file = new File([blob], filename, { type: blob.type })

       if (filename.endsWith(".pptx")) {
         const parser = new PPTXParser(parserCallbacks)
         importedSlides = await parser.parse(file)
         setTitle(filename.replace(".pptx", ""))
       } else if (filename.endsWith(".odp")) {
         const parser = new ODPParser(parserCallbacks)
         importedSlides = await parser.parse(file)
         setTitle(filename.replace(".odp", ""))
       } else {
         throw new Error("Unsupported file format from URL")
       }

       setHasPresentation(true)
       setIsImageSlideshow(false)
     }

     setSlides(importedSlides)
     setImportProgress(100)
     setImportStatus("Import completed successfully")

     // Auto-save after successful import
     setTimeout(() => {
       updatePresentation({
         slides: importedSlides,
         title,
         theme,
         transitionEffect,
         autoAdvance,
         autoAdvanceDelay,
         autoAdvanceLoop,
         showControls,
       })
       setIsImporting(false)
     }, 500)
   } catch (error) {
     console.error("Error importing presentation file:", error)
     setFileError(error instanceof Error ? error.message : "Invalid file format")
     setIsImporting(false)
   }
 }

 const handleImportImages = async (results: MediaUploadResult[]) => {
   setIsImporting(true)
   setFileError(null)
   setImportProgress(0)
   setImportStatus("Processing images...")

   try {
     const imageSlides: Slide[] = results.map((result, index) => ({
       id: `image-slide-${Date.now()}-${index}`,
       title: `Slide ${index + 1}`,
       content: "",
       layout: "full-image" as const,
       theme: "image" as const,
       backgroundImage: result.data,
       notes: "",
     }))

     setSlides(imageSlides)
     setHasPresentation(false)
     setIsImageSlideshow(true)
     setImportProgress(100)
     setImportStatus("Images processed successfully")

     // Auto-save after successful import
     setTimeout(() => {
       updatePresentation({
         slides: imageSlides,
         title: title || "Image Slideshow",
         theme,
         transitionEffect,
         autoAdvance,
         autoAdvanceDelay,
         autoAdvanceLoop,
         showControls,
       })
       setIsImporting(false)
     }, 500)
   } catch (error) {
     console.error("Error processing images:", error)
     setFileError(error instanceof Error ? error.message : "Failed to process images")
     setIsImporting(false)
   }
 }

 // Edit menu options
 const editMenuOptions: EditMenuOption[] = [
   {
     id: "edit",
     icon: <Edit className="h-4 w-4" />,
     label: "Edit presentation",
     action: () => setIsEditing(true),
   },
   {
     id: "present",
     icon: <Expand className="h-4 w-4" />,
     label: "Present",
     action: () => setIsPresenting(true),
   },
 ]

 // Render the presentation in view mode
 if (!isEditing) {
   return (
     <div
       ref={containerRef}
       className="my-8 relative group"
       onMouseEnter={() => setShowMenu(true)}
       onMouseLeave={() => setShowMenu(false)}
     >
       {slides.length > 0 ? (
         <SlidePlayer
           slides={slides}
           title={title}
           currentSlideIndex={currentSlideIndex}
           onSlideChange={setCurrentSlideIndex}
           autoAdvance={autoAdvance}
           autoAdvanceDelay={autoAdvanceDelay}
           autoAdvanceLoop={autoAdvanceLoop}
           transitionEffect={transitionEffect}
           showControls={showControls}
           showThumbnails={true}
           showHeader={true}
           showFullscreenButton={true}
           theme={theme}
           customThemeColor={customThemeColor}
           size="lg"
           isPresenting={isPresenting}
           onFullscreen={() => setIsPresenting(true)}
           onExitFullscreen={() => setIsPresenting(false)}
         />
       ) : (
         <div className="flex items-center justify-center h-40 bg-muted/20 rounded-lg border border-dashed relative">
           <div className="text-center">
             <LayoutGrid className="h-8 w-8 mx-auto text-muted-foreground" />
             <p className="mt-2 text-sm text-muted-foreground">No slides in presentation</p>
             <div className="flex items-center justify-center gap-2 mt-4">
               <Button variant="outline" onClick={() => setIsEditing(true)}>
                 <Edit className="h-4 w-4 mr-2" />
                 Create Presentation
               </Button>
             </div>
           </div>

           {/* Always visible edit menu */}
           <div className="absolute top-2 right-2">
             <ContentEditMenu options={editMenuOptions} />
           </div>
         </div>
       )}

       {/* Edit menu */}
       {!isPresenting && <ContentEditMenu options={editMenuOptions} />}
     </div>
   )
 }

 // Render the presentation in edit mode
 return (
  <div className="fixed inset-0 z-[5] bg-black/50 flex items-center justify-center p-4 overflow-y-auto">
    <div className="bg-background rounded-lg shadow-xl max-w-6xl w-full max-h-[90vh] overflow-y-auto">
      <PresentationSettings
        title={title}
        setTitle={setTitle}
        transitionEffect={transitionEffect}
        setTransitionEffect={setTransitionEffect}
        autoAdvance={autoAdvance}
        setAutoAdvance={setAutoAdvance}
        autoAdvanceDelay={autoAdvanceDelay}
        setAutoAdvanceDelay={setAutoAdvanceDelay}
        autoAdvanceLoop={autoAdvanceLoop}
        setAutoAdvanceLoop={setAutoAdvanceLoop}
        showControls={showControls}
        setShowControls={setShowControls}
        isImporting={isImporting}
        importProgress={importProgress}
        importStatus={importStatus}
        fileError={fileError}
        onSave={handleSave}
        onCancel={handleCancel}
        slidesCount={slides.length}
        hasPresentation={hasPresentation}
        isImageSlideshow={isImageSlideshow}
        slides={slides}
        setSlides={setSlides}
        onImportPresentation={handleImportPresentation}
        onImportImages={handleImportImages}
      />
      {slides.length > 0 && (
        <PresentationPreview
          slides={slides}
          currentSlideIndex={currentSlideIndex}
          setCurrentSlideIndex={setCurrentSlideIndex}
          customThemeColor={customThemeColor}
          autoAdvance={autoAdvance}
          autoAdvanceDelay={autoAdvanceDelay}
          autoAdvanceLoop={autoAdvanceLoop}
          transitionEffect={transitionEffect}
          showControls={showControls}
        />
      )}
    </div>
  </div>
)
}

export function $createPresentationNode(): PresentationNode {
 return new PresentationNode({
   slides: [],
   title: "Untitled Presentation",
   theme: "light",
   transitionEffect: "fade",
   autoAdvance: false,
   autoAdvanceDelay: 5,
   autoAdvanceLoop: false,
   showControls: true,
   isNew: true,
 })
}
