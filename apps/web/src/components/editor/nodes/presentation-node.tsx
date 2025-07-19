'use client';

import type React from 'react';

import { useState, useEffect, useRef, useContext } from 'react';
import { DecoratorNode, type SerializedLexicalNode } from 'lexical';
import { $getNodeByKey } from 'lexical';
import { useLexicalComposerContext } from '@lexical/react/LexicalComposerContext';
import { ChevronLeft, ChevronRight, Edit, Expand, ImageIcon, LayoutGrid, Maximize2, Minimize2, X } from 'lucide-react';
import { Download, Upload, FileType, AlertCircle } from 'lucide-react';

import { Button } from '@/components/editor/ui/button';
import { ContentEditMenu, type EditMenuOption } from '@/components/editor/ui/content-edit-menu';
import { Input } from '@/components/editor/ui/input';
import { Label } from '@/components/editor/ui/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Switch } from '@/components/editor/ui/switch';
import { cn } from '@/lib/utils';
import { Alert, AlertDescription } from '@/components/editor/ui/alert';
import { Progress } from '@/components/editor/ui/progress';
import type { JSX } from 'react/jsx-runtime'; // Import JSX from react/jsx-runtime
import { EditorLoadingContext } from '../lexical-editor';

export type SlideTheme = 'light' | 'dark' | 'gradient' | 'image' | 'standard' | 'custom';
export type SlideLayout = 'title' | 'content' | 'title-content' | 'image-text' | 'text-image' | 'full-image';
export type TransitionEffect = 'none' | 'fade' | 'slide' | 'zoom';

export interface Slide {
  id: string;
  title?: string;
  content?: string;
  layout: SlideLayout;
  theme: SlideTheme;
  backgroundImage?: string;
  backgroundGradient?: string;
  notes?: string;
}

export interface PresentationData {
  slides: Slide[];
  title?: string;
  theme: SlideTheme;
  transitionEffect: TransitionEffect;
  autoAdvance: boolean;
  autoAdvanceDelay: number;
  showControls: boolean;
  isNew?: boolean;
  customThemeColor?: string;
}

export interface SerializedPresentationNode extends SerializedLexicalNode {
  type: 'presentation';
  data: PresentationData;
  version: 1;
}

export class PresentationNode extends DecoratorNode<JSX.Element> {
  __data: PresentationData;

  static getType(): string {
    return 'presentation';
  }

  static clone(node: PresentationNode): PresentationNode {
    return new PresentationNode(node.__data, node.__key);
  }

  constructor(data: PresentationData, key?: string) {
    super(key);
    this.__data = {
      slides: data.slides || [],
      title: data.title || 'Untitled Presentation',
      theme: data.theme || 'light',
      transitionEffect: data.transitionEffect || 'fade',
      autoAdvance: data.autoAdvance || false,
      autoAdvanceDelay: data.autoAdvanceDelay || 5,
      showControls: data.showControls !== undefined ? data.showControls : true,
      isNew: data.isNew,
      customThemeColor: data.customThemeColor,
    };
  }

  createDOM(): HTMLElement {
    return document.createElement('div');
  }

  updateDOM(): false {
    return false;
  }

  setData(data: PresentationData): void {
    const writable = this.getWritable();
    writable.__data = data;
  }

  exportJSON(): SerializedPresentationNode {
    return {
      type: 'presentation',
      data: this.__data,
      version: 1,
    };
  }

  static importJSON(serializedNode: SerializedPresentationNode): PresentationNode {
    return new PresentationNode(serializedNode.data);
  }

  decorate(): JSX.Element {
    return <PresentationComponent data={this.__data} nodeKey={this.__key} />;
  }
}

interface PresentationComponentProps {
  data: PresentationData;
  nodeKey: string;
}

function PresentationComponent({ data, nodeKey }: PresentationComponentProps) {
  const [editor] = useLexicalComposerContext();
  const isLoading = useContext(EditorLoadingContext);
  const [isEditing, setIsEditing] = useState((data.isNew || false) && !isLoading);
  const [slides, setSlides] = useState<Slide[]>(data.slides || []);
  const [title, setTitle] = useState(data.title || 'Untitled Presentation');
  const [theme, setTheme] = useState<SlideTheme>(data.theme || 'light');
  const [transitionEffect, setTransitionEffect] = useState<TransitionEffect>(data.transitionEffect || 'fade');
  const [autoAdvance, setAutoAdvance] = useState(data.autoAdvance || false);
  const [autoAdvanceDelay, setAutoAdvanceDelay] = useState(data.autoAdvanceDelay || 5);
  const [showControls, setShowControls] = useState(data.showControls !== undefined ? data.showControls : true);
  const [currentSlideIndex, setCurrentSlideIndex] = useState(0);
  const [showMenu, setShowMenu] = useState(false);
  const [isFullscreen, setIsFullscreen] = useState(false);
  const [isPresenting, setIsPresenting] = useState(false);
  const [fileError, setFileError] = useState<string | null>(null);
  const [isImporting, setIsImporting] = useState(false);
  const [importProgress, setImportProgress] = useState(0);
  const [importStatus, setImportStatus] = useState('');
  const [customThemeColor, setCustomThemeColor] = useState<string | null>(null);

  const containerRef = useRef<HTMLDivElement>(null);
  const presentationRef = useRef<HTMLDivElement>(null);
  const fileInputRef = useRef<HTMLInputElement>(null);

  // Remove isNew flag after first render
  useEffect(() => {
    if (data.isNew) {
      editor.update(() => {
        const node = $getNodeByKey(nodeKey);
        if (node instanceof PresentationNode) {
          const { isNew, ...rest } = data;
          node.setData(rest);
        }
      });
    }
  }, [data, editor, nodeKey]);

  useEffect(() => {
    if (isLoading) {
      setIsEditing(false);
    }
  }, [isLoading]);

  // Handle fullscreen mode
  useEffect(() => {
    const handleFullscreenChange = () => {
      setIsFullscreen(!!document.fullscreenElement);
      if (!document.fullscreenElement) {
        setIsPresenting(false);
      }
    };

    document.addEventListener('fullscreenchange', handleFullscreenChange);
    return () => {
      document.removeEventListener('fullscreenchange', handleFullscreenChange);
    };
  }, []);

  // Handle auto-advance
  useEffect(() => {
    let timer: NodeJS.Timeout | null = null;

    if (isPresenting && autoAdvance && !isEditing) {
      timer = setTimeout(() => {
        if (currentSlideIndex < slides.length - 1) {
          setCurrentSlideIndex(currentSlideIndex + 1);
        } else if (isPresenting) {
          // Loop back to the first slide
          setCurrentSlideIndex(0);
        }
      }, autoAdvanceDelay * 1000);
    }

    return () => {
      if (timer) clearTimeout(timer);
    };
  }, [isPresenting, autoAdvance, autoAdvanceDelay, currentSlideIndex, slides.length, isEditing]);

  const updatePresentation = (newData: Partial<PresentationData>) => {
    editor.update(() => {
      const node = $getNodeByKey(nodeKey);
      if (node instanceof PresentationNode) {
        node.setData({
          ...data,
          ...newData,
          customThemeColor: customThemeColor, // Add the custom theme color
        });
      }
    });
  };

  const handleSave = () => {
    updatePresentation({
      slides,
      title,
      theme,
      transitionEffect,
      autoAdvance,
      autoAdvanceDelay,
      showControls,
    });
    setIsEditing(false);
  };

  // Function to parse PowerPoint files
  const parsePPTX = async (file: File): Promise<Slide[]> => {
    return new Promise((resolve, reject) => {
      setImportStatus('Reading PowerPoint file...');
      setImportProgress(10);

      const reader = new FileReader();
      reader.onload = async (e) => {
        try {
          setImportStatus('Parsing slides...');
          setImportProgress(30);

          // Use a library like pptx.js to parse the file
          // This is a simplified example - in reality, you'd need to use a proper PPTX parsing library
          const arrayBuffer = e.target?.result as ArrayBuffer;

          // Simulate parsing with a timeout
          setTimeout(() => {
            setImportStatus('Extracting content...');
            setImportProgress(60);

            // Create slides from the parsed content
            // This is where you'd actually extract slide content from the PPTX
            const extractedSlides: Slide[] = [];

            // Simulate extracting 5 slides
            for (let i = 0; i < 5; i++) {
              extractedSlides.push({
                id: `slide-${Date.now()}-${i}`,
                title: `Slide ${i + 1}`,
                content: `Content extracted from PowerPoint slide ${i + 1}`,
                layout: 'title-content',
                theme: 'light',
              });
            }

            setImportStatus('Processing complete');
            setImportProgress(100);

            resolve(extractedSlides);
          }, 1500);
        } catch (error) {
          reject(error);
        }
      };

      reader.onerror = () => {
        reject(new Error('Error reading file'));
      };

      reader.readAsArrayBuffer(file);
    });
  };

  // Function to parse OpenDocument Presentation files
  const parseODP = async (file: File): Promise<Slide[]> => {
    return new Promise((resolve, reject) => {
      setImportStatus('Reading OpenDocument file...');
      setImportProgress(10);

      const reader = new FileReader();
      reader.onload = async (e) => {
        try {
          setImportStatus('Parsing ODP format...');
          setImportProgress(30);

          // Simulate parsing with a timeout
          setTimeout(() => {
            setImportStatus('Extracting content...');
            setImportProgress(60);

            // Create slides from the parsed content
            const extractedSlides: Slide[] = [];

            // Simulate extracting 3 slides
            for (let i = 0; i < 3; i++) {
              extractedSlides.push({
                id: `slide-${Date.now()}-${i}`,
                title: `ODP Slide ${i + 1}`,
                content: `Content extracted from OpenDocument slide ${i + 1}`,
                layout: 'title-content',
                theme: 'dark', // Different default theme for ODP
              });
            }

            setImportStatus('Processing complete');
            setImportProgress(100);

            resolve(extractedSlides);
          }, 1500);
        } catch (error) {
          reject(error);
        }
      };

      reader.onerror = () => {
        reject(new Error('Error reading file'));
      };

      reader.readAsArrayBuffer(file);
    });
  };

  const handleFileChange = async (event: React.ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0];
    if (!file) return;

    setIsImporting(true);
    setFileError(null);
    setImportProgress(0);
    setImportStatus('Starting import...');

    try {
      let importedSlides: Slide[] = [];

      // Check file type and use appropriate parser
      if (file.name.endsWith('.json')) {
        // Handle JSON files
        const reader = new FileReader();

        const jsonContent = await new Promise<string>((resolve, reject) => {
          reader.onload = (e) => resolve(e.target?.result as string);
          reader.onerror = () => reject(new Error('Error reading file'));
          reader.readAsText(file);
        });

        const parsedData = JSON.parse(jsonContent);

        // Validate the imported data
        if (!Array.isArray(parsedData.slides)) {
          throw new Error('Invalid presentation format: slides array is missing');
        }

        // Map the imported data to our slide format
        importedSlides = parsedData.slides.map((slide: any) => {
          // Ensure each slide has required properties
          if (!slide.id || !slide.layout || !slide.theme) {
            throw new Error('Invalid slide format: missing required properties');
          }

          return {
            id: slide.id,
            title: slide.title || '',
            content: slide.content || '',
            layout: slide.layout,
            theme: slide.theme,
            backgroundImage: slide.backgroundImage,
            backgroundGradient: slide.backgroundGradient,
            notes: slide.notes || '',
          } as Slide;
        });

        // Update presentation settings from JSON
        if (parsedData.title) setTitle(parsedData.title);
        if (parsedData.theme) setTheme(parsedData.theme);
        if (parsedData.transitionEffect) setTransitionEffect(parsedData.transitionEffect);
        if (parsedData.autoAdvance !== undefined) setAutoAdvance(parsedData.autoAdvance);
        if (parsedData.autoAdvanceDelay) setAutoAdvanceDelay(parsedData.autoAdvanceDelay);
        if (parsedData.showControls !== undefined) setShowControls(parsedData.showControls);
      } else if (file.name.endsWith('.pptx')) {
        // Handle PowerPoint files
        importedSlides = await parsePPTX(file);
        setTitle(file.name.replace('.pptx', ''));
      } else if (file.name.endsWith('.odp')) {
        // Handle OpenDocument Presentation files
        importedSlides = await parseODP(file);
        setTitle(file.name.replace('.odp', ''));
      } else {
        throw new Error('Unsupported file format. Please upload a .json, .pptx, or .odp file');
      }

      // Update the slides state
      setSlides(importedSlides);

      // Update the node data
      updatePresentation({
        slides: importedSlides,
        title,
        theme,
        transitionEffect,
        autoAdvance,
        autoAdvanceDelay,
        showControls,
      });

      setIsImporting(false);
      setIsEditing(false);
    } catch (error) {
      console.error('Error importing presentation file:', error);
      setFileError(error instanceof Error ? error.message : 'Invalid file format');
      setIsImporting(false);
    }
  };

  const handleImportClick = () => {
    fileInputRef.current?.click();
  };

  const handleExportClick = () => {
    const exportData = {
      slides,
      title,
      theme,
      transitionEffect,
      autoAdvance,
      autoAdvanceDelay,
      showControls,
    };

    const blob = new Blob([JSON.stringify(exportData, null, 2)], { type: 'application/json' });
    const url = URL.createObjectURL(blob);

    const a = document.createElement('a');
    a.href = url;
    a.download = `${title.replace(/\s+/g, '-').toLowerCase()}.json`;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(url);
  };

  const handlePrevSlide = () => {
    if (currentSlideIndex > 0) {
      setCurrentSlideIndex(currentSlideIndex - 1);
    }
  };

  const handleNextSlide = () => {
    if (currentSlideIndex < slides.length - 1) {
      setCurrentSlideIndex(currentSlideIndex + 1);
    }
  };

  const toggleFullscreen = () => {
    if (!isFullscreen && presentationRef.current) {
      if (presentationRef.current.requestFullscreen) {
        presentationRef.current.requestFullscreen();
        setIsPresenting(true);
      }
    } else if (isFullscreen && document.fullscreenElement) {
      document
        .exitFullscreen()
        .then(() => {
          setIsPresenting(false);
        })
        .catch((err) => {
          console.error('Error exiting fullscreen:', err);
        });
    }
  };

  // Edit menu options
  const editMenuOptions: EditMenuOption[] = [
    {
      id: 'edit',
      icon: <Edit className="h-4 w-4" />,
      label: 'Edit presentation',
      action: () => setIsEditing(true),
    },
    {
      id: 'present',
      icon: <Expand className="h-4 w-4" />,
      label: 'Present',
      action: () => {
        setIsPresenting(true);
        toggleFullscreen();
      },
    },
    {
      id: 'export',
      icon: <Download className="h-4 w-4" />,
      label: 'Export presentation',
      action: handleExportClick,
    },
  ];

  // Get background style based on theme
  const getBackgroundStyle = (slideTheme: SlideTheme, backgroundImage?: string, backgroundGradient?: string) => {
    switch (slideTheme) {
      case 'light':
        return { backgroundColor: 'white', color: 'black' };
      case 'dark':
        return { backgroundColor: '#1a1a1a', color: 'white' };
      case 'standard':
        return { backgroundColor: '#f8f9fa', color: '#333333' };
      case 'gradient':
        return {
          background: backgroundGradient || 'linear-gradient(135deg, #6366f1, #8b5cf6)',
          color: 'white',
        };
      case 'custom':
        return {
          backgroundColor: customThemeColor || '#3b82f6',
          color: '#ffffff',
        };
      case 'image':
        return {
          backgroundImage: `url(${backgroundImage || '/placeholder.svg'})`,
          backgroundSize: 'cover',
          backgroundPosition: 'center',
          color: 'white',
          position: 'relative' as const,
        };
      default:
        return { backgroundColor: 'white', color: 'black' };
    }
  };

  // Get theme color for controls and outline
  const getThemeColor = () => {
    switch (theme) {
      case 'light':
        return 'border-gray-200 bg-gray-100 text-gray-800';
      case 'dark':
        return 'border-gray-700 bg-gray-800 text-gray-200';
      case 'standard':
        return 'border-blue-200 bg-blue-100 text-blue-800';
      case 'custom':
        return `border-[${customThemeColor || '#3b82f6'}] bg-opacity-10 bg-[${customThemeColor || '#3b82f6'}] text-[${customThemeColor || '#3b82f6'}]`;
      case 'gradient':
        return 'border-purple-200 bg-purple-100 text-purple-800';
      case 'image':
        return 'border-gray-300 bg-gray-200 text-gray-800';
      default:
        return 'border-gray-200 bg-gray-100 text-gray-800';
    }
  };

  // Get layout style based on layout type
  const getLayoutStyle = (layout: SlideLayout) => {
    switch (layout) {
      case 'title':
        return 'flex flex-col items-center justify-center text-center';
      case 'content':
        return 'flex flex-col p-8';
      case 'title-content':
        return 'flex flex-col p-8';
      case 'image-text':
        return 'flex flex-col gap-4 p-8';
      case 'text-image':
        return 'flex flex-col gap-4 p-8';
      case 'full-image':
        return 'relative';
      default:
        return 'flex flex-col p-8';
    }
  };

  // Render a slide based on its layout
  const renderSlide = (slide: Slide) => {
    const backgroundStyle = getBackgroundStyle(slide.theme, slide.backgroundImage, slide.backgroundGradient);
    const layoutClass = getLayoutStyle(slide.layout);

    // Add overlay for image backgrounds to ensure text readability
    const hasOverlay = slide.theme === 'image';

    return (
      <div className={cn('w-full h-full overflow-hidden relative aspect-video')} style={backgroundStyle}>
        {hasOverlay && <div className="absolute inset-0 bg-black bg-opacity-40"></div>}

        <div className={cn('relative z-5 w-full h-full', layoutClass)}>
          {slide.layout === 'title' && (
            <div className="p-8 flex flex-col items-center justify-center h-full">
              <h1 className="text-4xl font-bold mb-4">{slide.title}</h1>
            </div>
          )}

          {slide.layout === 'content' && (
            <div className="p-8">
              <div className="prose max-w-none">{slide.content}</div>
            </div>
          )}

          {slide.layout === 'title-content' && (
            <div className="p-8 flex flex-col h-full">
              <h2 className="text-3xl font-bold mb-6">{slide.title}</h2>
              <div className="prose max-w-none">{slide.content}</div>
            </div>
          )}

          {slide.layout === 'image-text' && (
            <div className="flex flex-col gap-8 p-8 h-full">
              {slide.backgroundImage ? (
                <img src={slide.backgroundImage || '/placeholder.svg'} alt="Slide image" className="max-w-full max-h-full object-contain rounded-lg" />
              ) : (
                <div className="w-full h-full bg-gray-200 flex items-center justify-center rounded-lg">
                  <ImageIcon className="h-12 w-12 text-gray-400" />
                </div>
              )}
              <div className="prose max-w-none">{slide.content}</div>
            </div>
          )}

          {slide.layout === 'text-image' && (
            <div className="flex flex-col gap-8 p-8 h-full">
              <div className="prose max-w-none">{slide.content}</div>
              {slide.backgroundImage ? (
                <img src={slide.backgroundImage || '/placeholder.svg'} alt="Slide image" className="max-w-full max-h-full object-contain rounded-lg" />
              ) : (
                <div className="w-full h-full bg-gray-200 flex items-center justify-center rounded-lg">
                  <ImageIcon className="h-12 w-12 text-gray-400" />
                </div>
              )}
            </div>
          )}

          {slide.layout === 'full-image' && (
            <div className="relative w-full h-full">
              <div className="absolute bottom-0 left-0 right-0 p-8 bg-gradient-to-t from-black/70 to-transparent">
                <h2 className="text-3xl font-bold text-white">{slide.title}</h2>
              </div>
            </div>
          )}
        </div>
      </div>
    );
  };

  // Render the presentation in view mode
  if (!isEditing) {
    const currentSlide = slides[currentSlideIndex] || null;

    return (
      <div ref={containerRef} className="my-8 relative group" onMouseEnter={() => setShowMenu(true)} onMouseLeave={() => setShowMenu(false)}>
        <div ref={presentationRef} className="relative">
          {slides.length > 0 ? (
            <div className={`rounded-lg overflow-hidden border shadow-sm ${getThemeColor()}`}>
              <div className="bg-gray-100 dark:bg-gray-800 p-2 flex items-center justify-between">
                <h3 className="text-sm font-medium truncate">{title}</h3>
                <div className="flex items-center gap-1">
                  <span className="text-xs text-muted-foreground">
                    {currentSlideIndex + 1} / {slides.length}
                  </span>
                </div>
              </div>

              <div className="aspect-video relative">
                {currentSlide && renderSlide(currentSlide)}

                {/* Navigation controls */}
                {showControls && slides.length > 1 && !isPresenting && (
                  <>
                    <Button
                      variant="ghost"
                      size="icon"
                      className={`absolute left-2 top-1/2 -translate-y-1/2 ${theme === 'dark' ? 'bg-white/20 hover:bg-white/40 text-black' : 'bg-black/20 hover:bg-black/40 text-white'} rounded-full h-8 w-8 z-20`}
                      onClick={handlePrevSlide}
                      disabled={currentSlideIndex === 0}
                    >
                      <ChevronLeft className="h-4 w-4" />
                    </Button>
                    <Button
                      variant="ghost"
                      size="icon"
                      className={`absolute right-2 top-1/2 -translate-y-1/2 ${theme === 'dark' ? 'bg-white/20 hover:bg-white/40 text-black' : 'bg-black/20 hover:bg-black/40 text-white'} rounded-full h-8 w-8 z-20`}
                      onClick={handleNextSlide}
                      disabled={currentSlideIndex === slides.length - 1}
                    >
                      <ChevronRight className="h-4 w-4" />
                    </Button>
                  </>
                )}
              </div>

              {/* Slide thumbnails */}
              {!isPresenting && (
                <div className={`p-2 overflow-x-auto ${getThemeColor()}`}>
                  <div className="flex gap-2">
                    {slides.map((slide, index) => (
                      <button
                        key={slide.id}
                        className={cn(
                          'flex-shrink-0 w-16 h-10 rounded overflow-hidden border-2',
                          index === currentSlideIndex ? 'border-primary' : 'border-transparent hover:border-gray-300',
                        )}
                        onClick={() => setCurrentSlideIndex(index)}
                      >
                        <div className="w-full h-full relative" style={getBackgroundStyle(slide.theme, slide.backgroundImage)}>
                          {slide.theme === 'image' && <div className="absolute inset-0 bg-black/40"></div>}
                          <div className="w-full h-full flex items-center justify-center">
                            <span className="text-xs">{index + 1}</span>
                          </div>
                        </div>
                      </button>
                    ))}
                  </div>
                </div>
              )}
            </div>
          ) : (
            <div className="flex items-center justify-center h-40 bg-muted/20 rounded-lg border border-dashed relative">
              <div className="text-center">
                <LayoutGrid className="h-8 w-8 mx-auto text-muted-foreground" />
                <p className="mt-2 text-sm text-muted-foreground">No slides in presentation</p>
                <div className="flex items-center justify-center gap-2 mt-4">
                  <Button variant="outline" onClick={() => setIsEditing(true)}>
                    <Edit className="h-4 w-4 mr-2" />
                    Edit Settings
                  </Button>
                  <Button variant="outline" onClick={() => setIsEditing(true)}>
                    <Upload className="h-4 w-4 mr-2" />
                    Import Presentation
                  </Button>
                </div>
              </div>

              {/* Always visible edit menu */}
              <div className="absolute top-2 right-2">
                <ContentEditMenu options={editMenuOptions} />
              </div>
            </div>
          )}

          {/* Fullscreen presentation mode */}
          {isPresenting && (
            <div className="fixed inset-0 bg-black z-50 flex flex-col">
              <div className="flex-1 flex items-center justify-center">{currentSlide && renderSlide(currentSlide)}</div>

              {/* Presentation controls */}
              {showControls && (
                <div className="absolute bottom-4 left-1/2 -translate-x-1/2 flex items-center gap-2 bg-black/50 rounded-full p-2">
                  <Button variant="ghost" size="icon" className="text-white h-8 w-8" onClick={handlePrevSlide} disabled={currentSlideIndex === 0}>
                    <ChevronLeft className="h-4 w-4" />
                  </Button>

                  <span className="text-white text-xs px-2">
                    {currentSlideIndex + 1} / {slides.length}
                  </span>

                  <Button
                    variant="ghost"
                    size="icon"
                    className="text-white h-8 w-8"
                    onClick={handleNextSlide}
                    disabled={currentSlideIndex === slides.length - 1}
                  >
                    <ChevronRight className="h-4 w-4" />
                  </Button>

                  <Button variant="ghost" size="icon" className="text-white h-8 w-8" onClick={() => document.exitFullscreen()}>
                    <X className="h-4 w-4" />
                  </Button>
                </div>
              )}
            </div>
          )}

          {/* Edit menu */}
          {showMenu && !isPresenting && <ContentEditMenu options={editMenuOptions} />}
        </div>
      </div>
    );
  }

  // Render the presentation in edit mode (now focused on import/export)
  return (
    <>
      {/* Modal overlay for editing */}
      <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50 p-4">
        <div className="bg-white dark:bg-gray-900 rounded-lg border max-w-6xl max-h-[90vh] overflow-y-auto shadow-2xl w-9/12">
          <div className="sticky top-0 bg-white dark:bg-gray-900 border-b p-6 flex items-center justify-between">
            <div className="flex items-center gap-3">
              <div className="p-2 bg-blue-100 dark:bg-blue-900/30 rounded-lg">
                <LayoutGrid className="h-5 w-5 text-blue-600 dark:text-blue-400" />
              </div>
              <div>
                <h2 className="text-xl font-bold">Presentation Settings</h2>
                <p className="text-sm text-muted-foreground">Configure your presentation import/export and display options</p>
              </div>
            </div>
            <div className="flex items-center gap-2">
              <Button variant="outline" size="sm" onClick={toggleFullscreen}>
                {isFullscreen ? <Minimize2 className="h-4 w-4 mr-2" /> : <Maximize2 className="h-4 w-4 mr-2" />}
                {isFullscreen ? 'Exit Fullscreen' : 'Fullscreen'}
              </Button>
              <Button variant="default" size="sm" onClick={handleSave}>
                Save Changes
              </Button>
            </div>
          </div>

          <div className="p-6 space-y-8">
            <div className="grid gap-6">
              <div className="grid grid-cols-1 md:grid-cols-4 items-center gap-4">
                <Label htmlFor="presentation-title" className="md:text-right font-medium">
                  Presentation Title
                </Label>
                <div className="md:col-span-3">
                  <Input
                    id="presentation-title"
                    value={title}
                    onChange={(e) => setTitle(e.target.value)}
                    placeholder="Enter presentation title"
                    className="w-full"
                  />
                </div>
              </div>

              <div className="grid grid-cols-1 md:grid-cols-4 items-start gap-4">
                <Label className="md:text-right font-medium pt-2">Import/Export</Label>
                <div className="md:col-span-3">
                  <div className="space-y-4">
                    <div className="flex items-center gap-3">
                      <Button variant="outline" onClick={handleImportClick} disabled={isImporting} className="flex-1 sm:flex-none">
                        <Upload className="h-4 w-4 mr-2" />
                        {isImporting ? 'Importing...' : 'Import Presentation'}
                      </Button>
                      <Button variant="outline" onClick={handleExportClick} disabled={slides.length === 0} className="flex-1 sm:flex-none">
                        <Download className="h-4 w-4 mr-2" />
                        Export Presentation
                      </Button>
                      <input type="file" ref={fileInputRef} onChange={handleFileChange} accept=".json,.pptx,.odp" className="hidden" />
                    </div>

                    {isImporting && (
                      <div className="bg-blue-50 dark:bg-blue-900/20 p-4 rounded-lg">
                        <p className="text-sm font-medium mb-2">{importStatus}</p>
                        <Progress value={importProgress} className="h-2" />
                      </div>
                    )}

                    {fileError && (
                      <Alert variant="destructive">
                        <AlertCircle className="h-4 w-4" />
                        <AlertDescription>{fileError}</AlertDescription>
                      </Alert>
                    )}

                    <div className="bg-gray-50 dark:bg-gray-800/50 p-4 rounded-lg">
                      <p className="text-sm font-medium mb-2">Supported formats:</p>
                      <div className="flex items-center gap-6">
                        <div className="flex items-center gap-2">
                          <FileType className="h-4 w-4 text-blue-500" />
                          <span className="text-sm">.json (Native format)</span>
                        </div>
                        <div className="flex items-center gap-2">
                          <FileType className="h-4 w-4 text-red-500" />
                          <span className="text-sm">.pptx (PowerPoint)</span>
                        </div>
                        <div className="flex items-center gap-2">
                          <FileType className="h-4 w-4 text-green-500" />
                          <span className="text-sm">.odp (OpenDocument)</span>
                        </div>
                      </div>
                    </div>
                  </div>
                </div>
              </div>

              <div className="grid grid-cols-1 md:grid-cols-4 items-center gap-4">
                <Label htmlFor="presentation-theme" className="md:text-right font-medium">
                  Theme
                </Label>
                <div className="md:col-span-3">
                  <div className="space-y-3">
                    <Select
                      value={theme}
                      onValueChange={(value) => {
                        setTheme(value as SlideTheme);
                        if (value === 'custom' && !customThemeColor) {
                          setCustomThemeColor('#3b82f6');
                        }
                      }}
                    >
                      <SelectTrigger id="presentation-theme">
                        <SelectValue placeholder="Select theme" />
                      </SelectTrigger>
                      <SelectContent>
                        <SelectItem value="light">Light Theme</SelectItem>
                        <SelectItem value="dark">Dark Theme</SelectItem>
                        <SelectItem value="standard">Standard Theme</SelectItem>
                        <SelectItem value="gradient">Gradient Theme</SelectItem>
                        <SelectItem value="custom">Custom Color</SelectItem>
                        <SelectItem value="image">Image Background</SelectItem>
                      </SelectContent>
                    </Select>

                    {theme === 'custom' && (
                      <div className="flex items-center gap-3 p-3 bg-gray-50 dark:bg-gray-800/50 rounded-lg">
                        <Label htmlFor="custom-color" className="text-sm font-medium">
                          Custom Color:
                        </Label>
                        <div className="flex items-center gap-2">
                          <input
                            type="color"
                            id="custom-color"
                            value={customThemeColor || '#3b82f6'}
                            onChange={(e) => setCustomThemeColor(e.target.value)}
                            className="w-10 h-10 rounded-lg cursor-pointer border"
                          />
                          <Input
                            value={customThemeColor || '#3b82f6'}
                            onChange={(e) => setCustomThemeColor(e.target.value)}
                            className="w-28 h-10 text-sm font-mono"
                            placeholder="#3b82f6"
                          />
                        </div>
                      </div>
                    )}
                  </div>
                </div>
              </div>

              <div className="grid grid-cols-1 md:grid-cols-4 items-center gap-4">
                <Label htmlFor="transition-effect" className="md:text-right font-medium">
                  Transition Effect
                </Label>
                <div className="md:col-span-3">
                  <Select value={transitionEffect} onValueChange={(value) => setTransitionEffect(value as TransitionEffect)}>
                    <SelectTrigger id="transition-effect">
                      <SelectValue placeholder="Select transition" />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="none">No Transition</SelectItem>
                      <SelectItem value="fade">Fade Effect</SelectItem>
                      <SelectItem value="slide">Slide Effect</SelectItem>
                      <SelectItem value="zoom">Zoom Effect</SelectItem>
                    </SelectContent>
                  </Select>
                </div>
              </div>

              <div className="grid grid-cols-1 md:grid-cols-4 items-center gap-4">
                <Label htmlFor="auto-advance" className="md:text-right font-medium">
                  Auto Advance
                </Label>
                <div className="md:col-span-3 flex items-center gap-4">
                  <Switch id="auto-advance" checked={autoAdvance} onCheckedChange={setAutoAdvance} />
                  {autoAdvance && (
                    <div className="flex items-center gap-2">
                      <Label htmlFor="auto-advance-delay" className="text-sm">
                        Delay:
                      </Label>
                      <Input
                        id="auto-advance-delay"
                        type="number"
                        min="1"
                        max="60"
                        value={autoAdvanceDelay}
                        onChange={(e) => setAutoAdvanceDelay(Number.parseInt(e.target.value) || 5)}
                        className="w-20"
                      />
                      <span className="text-sm text-muted-foreground">seconds</span>
                    </div>
                  )}
                </div>
              </div>

              <div className="grid grid-cols-1 md:grid-cols-4 items-center gap-4">
                <Label htmlFor="show-controls" className="md:text-right font-medium">
                  Show Controls
                </Label>
                <div className="md:col-span-3">
                  <Switch id="show-controls" checked={showControls} onCheckedChange={setShowControls} />
                  <p className="text-sm text-muted-foreground mt-1">Display navigation controls during presentation</p>
                </div>
              </div>
            </div>

            {slides.length > 0 && (
              <div className="border-t pt-6">
                <div className="flex items-center gap-3 mb-4">
                  <div className="p-2 bg-green-100 dark:bg-green-900/30 rounded-lg">
                    <LayoutGrid className="h-4 w-4 text-green-600 dark:text-green-400" />
                  </div>
                  <div>
                    <h3 className="text-lg font-semibold">Presentation Preview</h3>
                    <p className="text-sm text-muted-foreground">Preview your imported presentation</p>
                  </div>
                </div>

                <div className="bg-gray-50 dark:bg-gray-800/50 rounded-lg p-4">
                  <div className="aspect-video rounded-lg overflow-hidden border bg-white dark:bg-gray-900 shadow-sm">
                    {renderSlide(slides[currentSlideIndex] || slides[0])}
                  </div>

                  <div className="mt-4">
                    <div className="flex items-center justify-between mb-3">
                      <h4 className="text-sm font-medium">Slides ({slides.length})</h4>
                      <div className="text-xs text-muted-foreground">
                        Slide {currentSlideIndex + 1} of {slides.length}
                      </div>
                    </div>
                    <div className="flex gap-2 overflow-x-auto p-2 bg-white dark:bg-gray-900 rounded-lg border">
                      {slides.map((slide, index) => (
                        <button
                          key={slide.id}
                          className={cn(
                            'flex-shrink-0 w-20 h-12 rounded-lg overflow-hidden border-2 transition-all',
                            index === currentSlideIndex
                              ? 'border-blue-500 ring-2 ring-blue-200 dark:ring-blue-800'
                              : 'border-gray-200 dark:border-gray-700 hover:border-gray-300 dark:hover:border-gray-600',
                          )}
                          onClick={() => setCurrentSlideIndex(index)}
                        >
                          <div className="w-full h-full relative" style={getBackgroundStyle(slide.theme, slide.backgroundImage)}>
                            {slide.theme === 'image' && <div className="absolute inset-0 bg-black/40"></div>}
                            <div className="w-full h-full flex items-center justify-center">
                              <span className="text-xs font-medium">{index + 1}</span>
                            </div>
                          </div>
                        </button>
                      ))}
                    </div>
                  </div>
                </div>
              </div>
            )}
          </div>
        </div>
      </div>
    </>
  );
}

export function $createPresentationNode(): PresentationNode {
  return new PresentationNode({
    slides: [],
    title: 'Untitled Presentation',
    theme: 'light',
    transitionEffect: 'fade',
    autoAdvance: false,
    autoAdvanceDelay: 5,
    showControls: true,
    isNew: true,
  });
}
