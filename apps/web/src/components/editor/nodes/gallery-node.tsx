'use client';

import type React from 'react';
import { useContext, useEffect, useRef, useState } from 'react';
import type { JSX } from 'react/jsx-runtime';
import { $getNodeByKey, DecoratorNode, type SerializedLexicalNode } from 'lexical';
import { useLexicalComposerContext } from '@lexical/react/LexicalComposerContext';
import { Check, Crop, ImageIcon, Maximize, Plus, Settings, Trash2 } from 'lucide-react';

import { Button } from '@/components/ui/button';
import { MediaUploadDialog, type MediaUploadResult } from '@/components/ui/media-upload-dialog';
import { ContentEditMenu, type EditMenuOption } from '@/components/ui/content-edit-menu';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { EditorLoadingContext } from '../lexical-editor';

export type GalleryLayout = '1' | '2' | '3' | '4';

export type ImageDisplayMode = 'crop' | 'adaptive';

export interface GalleryImage {
  id: string;
  src: string;
  alt: string;
  caption?: string;
  displayMode?: ImageDisplayMode;
  span?: '1x1' | '1x2' | '2x1' | '2x2'; // How many cells the image spans in the grid
  aspectRatio?: number; // Width/height ratio
  gridPosition?: { rowStart: number; colStart: number; rowSpan: number; colSpan: number };
}

export interface GalleryData {
  images: GalleryImage[];
  layout: GalleryLayout;
  caption?: string;
  isNew?: boolean;
  defaultDisplayMode?: ImageDisplayMode;
  captionStyle?: {
    fontSize?: 'xs' | 'sm' | 'base' | 'lg';
    fontFamily?: 'sans' | 'serif' | 'mono';
    fontWeight?: 'normal' | 'medium' | 'bold';
  };
}

export interface SerializedGalleryNode extends SerializedLexicalNode {
  type: 'gallery';
  data: GalleryData;
  version: 1;
}

export class GalleryNode extends DecoratorNode<JSX.Element> {
  __data: GalleryData;

  constructor(data: GalleryData, key?: string) {
    super(key);
    this.__data = {
      images: data.images || [],
      layout: data.layout || '1',
      caption: data.caption || '',
      isNew: data.isNew,
      defaultDisplayMode: data.defaultDisplayMode || 'crop',
      captionStyle: data.captionStyle || {
        fontSize: 'sm',
        fontFamily: 'sans',
        fontWeight: 'normal',
      },
    };
  }

  static getType(): string {
    return 'gallery';
  }

  static clone(node: GalleryNode): GalleryNode {
    return new GalleryNode(node.__data, node.__key);
  }

  static importJSON(serializedNode: SerializedGalleryNode): GalleryNode {
    return new GalleryNode(serializedNode.data);
  }

  createDOM(): HTMLElement {
    return document.createElement('div');
  }

  updateDOM(): false {
    return false;
  }

  setData(data: GalleryData): void {
    const writable = this.getWritable();
    writable.__data = data;
  }

  exportJSON(): SerializedGalleryNode {
    return {
      type: 'gallery',
      data: this.__data,
      version: 1,
    };
  }

  decorate(): JSX.Element {
    return <GalleryComponent data={this.__data} nodeKey={this.__key} />;
  }
}

interface GalleryComponentProps {
  data: GalleryData;
  nodeKey: string;
}

function GalleryComponent({ data, nodeKey }: GalleryComponentProps) {
  // Add these styles to the document
  useEffect(() => {
    const style = document.createElement('style');
    style.innerHTML = `
      .drop-target-before {
        box-shadow: inset 4px 0 0 0 #3b82f6 !important;
        position: relative;
      }
      .drop-target-before::before {
        content: '';
        position: absolute;
        left: 0;
        top: 0;
        bottom: 0;
        width: 4px;
        background-color: #3b82f6;
        z-index: 10;
      }
      .drop-target-after {
        box-shadow: inset -4px 0 0 0 #3b82f6 !important;
        position: relative;
      }
      .drop-target-after::after {
        content: '';
        position: absolute;
        right: 0;
        top: 0;
        bottom: 0;
        width: 4px;
        background-color: #3b82f6;
        z-index: 10;
      }
    `;
    document.head.appendChild(style);

    return () => {
      document.head.removeChild(style);
    };
  }, []);
  const [editor] = useLexicalComposerContext();
  const isLoading = useContext(EditorLoadingContext);
  const [isEditing, setIsEditing] = useState((data.isNew || false) && !isLoading);
  const [images, setImages] = useState<GalleryImage[]>(data.images || []);
  const [layout, setLayout] = useState<GalleryLayout>(data.layout || '1');
  const [caption, setCaption] = useState(data.caption || '');
  const [defaultDisplayMode, setDefaultDisplayMode] = useState<ImageDisplayMode>(data.defaultDisplayMode || 'crop');
  const [showImageDialog, setShowImageDialog] = useState(false);
  const [showMenu, setShowMenu] = useState(false);
  const [activeTab, setActiveTab] = useState<string>('images');
  const [selectedImageId, setSelectedImageId] = useState<string | null>(null);
  const [captionStyle, setCaptionStyle] = useState(
    data.captionStyle || {
      fontSize: 'sm',
      fontFamily: 'sans',
      fontWeight: 'normal',
    },
  );

  // Add a ref to the main container
  const containerRef = useRef<HTMLDivElement>(null);

  // Remove isNew flag after first render
  useEffect(() => {
    if (data.isNew) {
      editor.update(() => {
        const node = $getNodeByKey(nodeKey);
        if (node instanceof GalleryNode) {
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

  useEffect(() => {
    // Only run this effect when editing starts (not on every render)
    if (data.isNew) {
      // No scroll manipulation needed
    }
  }, [data.isNew]);

  const updateGallery = (newData: Partial<GalleryData>) => {
    editor.update(() => {
      const node = $getNodeByKey(nodeKey);
      if (node instanceof GalleryNode) {
        node.setData({ ...data, ...newData });
      }
    });
  };

  // Function to calculate aspect ratio from an image
  const calculateAspectRatio = (src: string): Promise<number> => {
    return new Promise((resolve) => {
      const img = new Image();
      img.onload = () => {
        resolve(img.width / img.height);
      };
      img.onerror = () => {
        resolve(1); // Default to 1:1 if there's an error
      };
      img.src = src;
    });
  };

  // Function to determine optimal span based on aspect ratio
  const determineOptimalSpan = (aspectRatio: number): '1x1' | '1x2' | '2x1' | '2x2' => {
    if (aspectRatio > 1.8) return '1x2'; // Wide panorama
    if (aspectRatio < 0.6) return '2x1'; // Tall portrait
    return '1x1'; // Default square-ish
  };

  const handleImageSelected = async (result: MediaUploadResult | MediaUploadResult[]) => {
    let newImages = [...images];

    if (Array.isArray(result)) {
      // Handle multiple images
      const multipleImagesPromises = result.map(async (item) => {
        const aspectRatio = await calculateAspectRatio(item.data);
        const span = determineOptimalSpan(aspectRatio);

        return {
          id: Math.random().toString(36).substring(7),
          src: item.data,
          alt: item.name || 'Gallery image',
          caption: '', // Add empty caption
          displayMode: defaultDisplayMode,
          span: defaultDisplayMode === 'adaptive' ? span : '1x1',
          aspectRatio,
        };
      });

      const multipleImages = await Promise.all(multipleImagesPromises);
      newImages = [...images, ...multipleImages];
    } else {
      // Handle single image
      const aspectRatio = await calculateAspectRatio(result.data);
      const span = determineOptimalSpan(aspectRatio);

      const newImage: GalleryImage = {
        id: Math.random().toString(36).substring(7),
        src: result.data,
        alt: result.name || 'Gallery image',
        caption: '', // Add empty caption
        displayMode: defaultDisplayMode,
        span: defaultDisplayMode === 'adaptive' ? span : '1x1',
        aspectRatio,
      };
      newImages = [...images, newImage];
    }

    setImages(newImages);
    updateGallery({ images: newImages });
  };

  const handleRemoveImage = (id: string) => {
    const newImages = images.filter((img) => img.id !== id);
    setImages(newImages);
    updateGallery({ images: newImages });
  };

  const handleLayoutChange = (newLayout: GalleryLayout) => {
    setLayout(newLayout);
    updateGallery({ layout: newLayout });
  };

  const handleCaptionChange = (newCaption: string) => {
    setCaption(newCaption);
    updateGallery({ caption: newCaption });
  };

  const handleDefaultDisplayModeChange = (newMode: ImageDisplayMode) => {
    setDefaultDisplayMode(newMode);
    updateGallery({ defaultDisplayMode: newMode });
  };

  const handleSave = () => {
    updateGallery({
      images,
      layout,
      caption,
      defaultDisplayMode,
      captionStyle: {
        fontSize: captionStyle.fontSize as 'xs' | 'sm' | 'base' | 'lg' | undefined,
        fontFamily: captionStyle.fontFamily as 'sans' | 'serif' | 'mono' | undefined,
        fontWeight: captionStyle.fontWeight as 'normal' | 'medium' | 'bold' | undefined,
      },
    });
    setIsEditing(false);
  };

  // Edit menu options
  const editMenuOptions: EditMenuOption[] = [
    {
      id: 'edit',
      icon: <ImageIcon className="h-4 w-4" />,
      label: 'Edit gallery',
      action: () => setIsEditing(true),
    },
  ];

  // Get grid template columns based on layout
  const getGridTemplateColumns = () => {
    return `repeat(${layout}, 1fr)`;
  };

  // Get grid template rows based on content
  const getGridTemplateRows = (itemCount: number) => {
    const columnCount = Number.parseInt(layout);
    const rowCount = Math.ceil(itemCount / columnCount);
    return rowCount > 0 ? `repeat(${rowCount}, auto)` : 'auto';
  };

  const handleImageCaptionChange = (id: string, caption: string) => {
    const newImages = images.map((img) => (img.id === id ? { ...img, caption } : img));
    setImages(newImages);
    updateGallery({ images: newImages });
  };

  const handleImageAltChange = (id: string, alt: string) => {
    const newImages = images.map((img) => (img.id === id ? { ...img, alt } : img));
    setImages(newImages);
    updateGallery({ images: newImages });
  };

  const handleImageDisplayModeChange = (id: string, displayMode: ImageDisplayMode) => {
    const newImages = images.map((img) => {
      if (img.id === id) {
        // If switching to adaptive mode, use the optimal span based on aspect ratio
        // If switching to crop mode, reset to 1x1
        const span = displayMode === 'adaptive' && img.aspectRatio ? determineOptimalSpan(img.aspectRatio) : '1x1';

        return { ...img, displayMode, span };
      }
      return img;
    });
    setImages(newImages);
    updateGallery({ images: newImages });
  };

  const handleImageSpanChange = (id: string, span: '1x1' | '1x2' | '2x1' | '2x2') => {
    const newImages = images.map((img) => (img.id === id ? { ...img, span } : img));
    setImages(newImages);
    updateGallery({ images: newImages });
  };

  // Function to get grid area for an image based on its span
  const getGridArea = (image: GalleryImage) => {
    if (image.displayMode !== 'adaptive' || !image.span || image.span === '1x1') {
      return {};
    }

    // For adaptive mode with spans other than 1x1
    switch (image.span) {
      case '1x2':
        return { gridColumn: 'span 2' };
      case '2x1':
        return { gridRow: 'span 2' };
      case '2x2':
        return { gridColumn: 'span 2', gridRow: 'span 2' };
      default:
        return {};
    }
  };

  // Function to get caption style classes based on the current captionStyle state
  const getCaptionStyleClasses = () => {
    const classes = ['text-center text-muted-foreground'];

    // Font size
    if (captionStyle.fontSize === 'xs') classes.push('text-xs');
    else if (captionStyle.fontSize === 'sm') classes.push('text-sm');
    else if (captionStyle.fontSize === 'base') classes.push('text-base');
    else if (captionStyle.fontSize === 'lg') classes.push('text-lg');
    else classes.push('text-sm'); // Default

    // Font family
    if (captionStyle.fontFamily === 'serif') classes.push('font-serif');
    else if (captionStyle.fontFamily === 'mono') classes.push('font-mono');
    else classes.push('font-sans'); // Default

    // Font weight
    if (captionStyle.fontWeight === 'medium') classes.push('font-medium');
    else if (captionStyle.fontWeight === 'bold') classes.push('font-bold');
    else classes.push('font-normal'); // Default

    return classes.join(' ');
  };

  // Modify the renderGalleryGrid function to create rows as needed
  const renderGalleryGrid = () => {
    const columnCount = Number.parseInt(layout);
    const displayImages: GalleryImage[] = [];

    // Process images for display
    for (const image of images) {
      // Skip if we've processed all images
      if (displayImages.length >= images.length) break;

      // Determine span based on image settings
      let colSpan = 1;
      if (image.displayMode === 'adaptive' && image.span) {
        if (image.span === '1x2')
          colSpan = Math.min(2, columnCount); // Limit span to available columns
        else if (image.span === '2x2') colSpan = Math.min(2, columnCount);
      }

      // Add image with position info
      displayImages.push({
        ...image,
        gridPosition: {
          rowStart: 0, // Will be auto-placed by CSS grid
          colStart: 0, // Will be auto-placed by CSS grid
          rowSpan: image.span === '2x1' || image.span === '2x2' ? 2 : 1,
          colSpan,
        },
      });
    }

    return (
      <div
        className="grid gap-2"
        style={{
          gridTemplateColumns: getGridTemplateColumns(),
          gridAutoRows: 'auto',
        }}
      >
        {displayImages.map((image) => {
          // Calculate grid area style
          const gridAreaStyle = {
            gridRow: image.gridPosition?.rowSpan && image.gridPosition.rowSpan > 1 ? `span ${image.gridPosition.rowSpan}` : undefined,
            gridColumn: image.gridPosition?.colSpan && image.gridPosition.colSpan > 1 ? `span ${image.gridPosition.colSpan}` : undefined,
          };

          return (
            <div key={image.id} className="space-y-1" style={gridAreaStyle}>
              <div className="relative overflow-hidden rounded-md" style={{ aspectRatio: image.displayMode === 'crop' ? '1/1' : 'auto' }}>
                <img
                  src={image.src || '/placeholder.svg'}
                  alt={image.alt || 'Gallery image'}
                  title={image.alt || ''}
                  className={image.displayMode === 'crop' ? 'h-full w-full object-cover' : 'h-auto w-full object-contain'}
                />
              </div>
              {image.caption && <div className={getCaptionStyleClasses()}>{image.caption}</div>}
            </div>
          );
        })}
      </div>
    );
  };

  // Layout options
  const layoutOptions: { value: GalleryLayout; label: string }[] = [
    { value: '1', label: '1 Column' },
    { value: '2', label: '2 Columns' },
    { value: '3', label: '3 Columns' },
    { value: '4', label: '4 Columns' },
  ];

  // Span options for adaptive mode
  const spanOptions: { value: '1x1' | '1x2' | '2x1' | '2x2'; label: string }[] = [
    { value: '1x1', label: '1×1 (Standard)' },
    { value: '1x2', label: '1×2 (Wide)' },
    { value: '2x1', label: '2×1 (Tall)' },
    { value: '2x2', label: '2×2 (Large)' },
  ];

  // Drag and drop handlers for image reordering
  const handleDragStart = (e: React.DragEvent, id: string) => {
    e.dataTransfer.setData('imageId', id);
    e.currentTarget.classList.add('opacity-60', 'scale-95', 'shadow-xl', 'z-10');

    // Create a smaller ghost image
    const originalImg = images.find((img) => img.id === id);
    if (originalImg) {
      // Create a canvas to resize the image
      const canvas = document.createElement('canvas');
      const ctx = canvas.getContext('2d');

      // Set canvas size to be smaller (80x80 pixels)
      canvas.width = 80;
      canvas.height = 80;

      // Create a temporary image to draw on canvas
      const tempImg = new Image();
      tempImg.src = originalImg.src;
      tempImg.crossOrigin = 'anonymous';

      // Once the image is loaded, draw it on canvas and use as drag image
      tempImg.onload = () => {
        if (ctx) {
          // Draw the image scaled down to the canvas size
          ctx.drawImage(tempImg, 0, 0, 80, 80);

          // Create an image from the canvas
          const smallImg = new Image();
          smallImg.src = canvas.toDataURL();

          // Set the drag image with an offset to position it near the cursor
          e.dataTransfer.setDragImage(smallImg, 40, 40);
        }
      };

      // Fallback in case the image doesn't load
      tempImg.onerror = () => {
        const div = document.createElement('div');
        div.style.width = '80px';
        div.style.height = '80px';
        div.style.background = '#f0f0f0';
        div.style.borderRadius = '4px';
        document.body.appendChild(div);
        e.dataTransfer.setDragImage(div, 40, 40);
        setTimeout(() => document.body.removeChild(div), 0);
      };
    }
  };

  const handleDragOver = (e: React.DragEvent) => {
    e.preventDefault();
    e.dataTransfer.dropEffect = 'move';
  };

  const handleDragEnd = (e: React.DragEvent) => {
    e.currentTarget.classList.remove('opacity-60', 'scale-95', 'shadow-xl', 'z-10');

    // Remove drop target indicators from all elements
    document.querySelectorAll('.drop-target-before, .drop-target-after').forEach((el) => {
      el.classList.remove('drop-target-before', 'drop-target-after');
    });
  };

  const handleDrop = (e: React.DragEvent, targetId: string) => {
    e.preventDefault();
    const draggedId = e.dataTransfer.getData('imageId');

    if (draggedId === targetId) return;

    const newImages = [...images];
    const draggedIndex = newImages.findIndex((img) => img.id === draggedId);
    const targetIndex = newImages.findIndex((img) => img.id === targetId);

    if (draggedIndex < 0 || targetIndex < 0) return;

    // Remove the dragged item and insert it at the target position
    const [draggedItem] = newImages.splice(draggedIndex, 1);
    newImages.splice(targetIndex, 0, draggedItem);

    // Clear all drop target indicators
    document.querySelectorAll('.drop-target-before, .drop-target-after').forEach((el) => {
      el.classList.remove('drop-target-before', 'drop-target-after');
    });

    setImages(newImages);
    updateGallery({ images: newImages });
  };

  const handleDragEnter = (e: React.DragEvent) => {
    e.preventDefault();
    const target = e.currentTarget as HTMLElement;
    const draggedId = e.dataTransfer.getData('imageId');

    if (target.getAttribute('data-id') !== draggedId) {
      // Determine if we're in the first or second half of the element
      const rect = target.getBoundingClientRect();
      const x = e.clientX - rect.left;
      const y = e.clientY - rect.top;

      // For grid layout, use both x and y to determine position
      const isLeft = x < rect.width / 2;
      const isTop = y < rect.height / 2;

      // Clear previous indicators
      target.classList.remove('drop-target-before', 'drop-target-after');

      // Add indicator based on position
      if ((isLeft && isTop) || (!isLeft && isTop)) {
        target.classList.add('drop-target-before');
      } else {
        target.classList.add('drop-target-after');
      }
    }
  };

  const handleDragLeave = (e: React.DragEvent) => {
    e.preventDefault();
    const target = e.currentTarget as HTMLElement;
    target.classList.remove('drop-target-before', 'drop-target-after');
  };

  const handleCaptionStyleChange = (property: string, value: string) => {
    const newStyle = { ...captionStyle, [property]: value };
    setCaptionStyle(newStyle);
    updateGallery({
      captionStyle: {
        fontSize: newStyle.fontSize as 'xs' | 'sm' | 'base' | 'lg' | undefined,
        fontFamily: newStyle.fontFamily as 'sans' | 'serif' | 'mono' | undefined,
        fontWeight: newStyle.fontWeight as 'normal' | 'medium' | 'bold' | undefined,
      },
    });
  };

  if (!isEditing) {
    return (
      <div className="my-8 relative group">
        <div className="relative">
          {images.length > 0 ? (
            <>
              {renderGalleryGrid()}
              {caption && <div className={`mt-2 ${getCaptionStyleClasses()}`}>{caption}</div>}
            </>
          ) : (
            <div className="flex items-center justify-center h-40 bg-muted/20 rounded-lg border border-dashed">
              <div className="text-center">
                <ImageIcon className="h-8 w-8 mx-auto text-muted-foreground" />
                <p className="mt-2 text-sm text-muted-foreground">No images in gallery</p>
              </div>
            </div>
          )}

          {/* Edit menu */}
          <ContentEditMenu options={editMenuOptions} className="opacity-100" />
        </div>
      </div>
    );
  }

  return (
    <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50" onClick={() => setIsEditing(false)}>
      <div
        ref={containerRef}
        className="bg-white rounded-lg border shadow-xl max-w-4xl w-full mx-4 max-h-[90vh] overflow-y-auto"
        onClick={(e) => e.stopPropagation()}
      >
        {/* Header with title and actions */}
        <div className="flex items-center justify-between p-4 border-b bg-muted/30 sticky top-0 z-10">
          <div className="flex items-center gap-2">
            <ImageIcon className="h-5 w-5 text-muted-foreground" />
            <h3 className="font-semibold text-lg">Gallery Editor</h3>
            <span className="text-sm text-muted-foreground">
              ({images.length} {images.length === 1 ? 'image' : 'images'})
            </span>
          </div>
          <div className="flex items-center gap-2">
            <Button variant="outline" size="sm" onClick={() => setIsEditing(false)}>
              Cancel
            </Button>
            <Button variant="default" size="sm" onClick={handleSave}>
              <Check className="h-4 w-4 mr-2" />
              Save Gallery
            </Button>
          </div>
        </div>

        {/* Main content area */}
        <div className="p-4">
          <Tabs value={activeTab} onValueChange={setActiveTab} className="w-full">
            <TabsList className="grid w-full grid-cols-3 mb-6">
              <TabsTrigger value="images" className="flex items-center gap-2">
                <ImageIcon className="h-4 w-4" />
                Images
              </TabsTrigger>
              <TabsTrigger value="layout" className="flex items-center gap-2">
                <Maximize className="h-4 w-4" />
                Layout
              </TabsTrigger>
              <TabsTrigger value="settings" className="flex items-center gap-2">
                <Settings className="h-4 w-4" />
                Settings
              </TabsTrigger>
            </TabsList>

            <TabsContent value="images" className="space-y-4">
              <div className="grid grid-cols-2 gap-4">
                {images.map((image) => (
                  <div key={image.id} className="space-y-2">
                    <div
                      data-id={image.id}
                      className="relative rounded-md overflow-hidden group border cursor-move transition-all duration-200 hover:shadow-md active:shadow-lg"
                      style={{ aspectRatio: image.displayMode === 'crop' ? '1/1' : 'auto' }}
                      draggable
                      onDragStart={(e) => handleDragStart(e, image.id)}
                      onDragOver={handleDragOver}
                      onDragEnd={handleDragEnd}
                      onDrop={(e) => handleDrop(e, image.id)}
                      onDragEnter={(e) => handleDragEnter(e)}
                      onDragLeave={(e) => handleDragLeave(e)}
                      onClick={() => setSelectedImageId(image.id === selectedImageId ? null : image.id)}
                    >
                      <img
                        src={image.src || '/placeholder.svg'}
                        alt={image.alt || 'Gallery image'}
                        title={image.alt || ''}
                        className={
                          image.displayMode === 'crop' ? 'h-full w-full object-cover pointer-events-none' : 'h-auto w-full object-contain pointer-events-none'
                        }
                      />
                      <Button
                        variant="destructive"
                        size="icon"
                        className="absolute top-2 right-2 opacity-0 group-hover:opacity-100 transition-opacity"
                        onClick={(e) => {
                          e.stopPropagation();
                          handleRemoveImage(image.id);
                        }}
                      >
                        <Trash2 className="h-4 w-4" />
                      </Button>
                    </div>

                    {/* Image settings */}
                    {selectedImageId === image.id && (
                      <div className="p-2 border rounded-md space-y-2 bg-muted/10">
                        <div className="flex items-center justify-between">
                          <Label htmlFor={`display-mode-${image.id}`} className="text-xs">
                            Display Mode
                          </Label>
                          <div className="flex items-center gap-2">
                            <Button
                              variant={image.displayMode === 'crop' ? 'secondary' : 'outline'}
                              size="sm"
                              className="h-7 text-xs px-2"
                              onClick={() => handleImageDisplayModeChange(image.id, 'crop')}
                            >
                              <Crop className="h-3 w-3 mr-1" />
                              Crop
                            </Button>
                            <Button
                              variant={image.displayMode === 'adaptive' ? 'secondary' : 'outline'}
                              size="sm"
                              className="h-7 text-xs px-2"
                              onClick={() => handleImageDisplayModeChange(image.id, 'adaptive')}
                            >
                              <Maximize className="h-3 w-3 mr-1" />
                              Adapt
                            </Button>
                          </div>
                        </div>

                        {image.displayMode === 'adaptive' && (
                          <div className="flex flex-col gap-1">
                            <Label htmlFor={`span-${image.id}`} className="text-xs">
                              Grid Span
                            </Label>
                            <Select
                              value={image.span || '1x1'}
                              onValueChange={(value) => handleImageSpanChange(image.id, value as '1x1' | '1x2' | '2x1' | '2x2')}
                            >
                              <SelectTrigger id={`span-${image.id}`} className="h-7 text-xs">
                                <SelectValue placeholder="Select span" />
                              </SelectTrigger>
                              <SelectContent>
                                {spanOptions.map((option) => (
                                  <SelectItem key={option.value} value={option.value} className="text-xs">
                                    {option.label}
                                  </SelectItem>
                                ))}
                              </SelectContent>
                            </Select>
                          </div>
                        )}

                        <div className="flex flex-col gap-1">
                          <Label htmlFor={`alt-${image.id}`} className="text-xs">
                            Alt Text (for accessibility)
                          </Label>
                          <Input
                            id={`alt-${image.id}`}
                            placeholder="Describe the image for screen readers"
                            value={image.alt || ''}
                            onChange={(e) => handleImageAltChange(image.id, e.target.value)}
                            className="text-xs h-7"
                          />
                        </div>

                        <div className="flex flex-col gap-1">
                          <Label htmlFor={`caption-${image.id}`} className="text-xs">
                            Caption
                          </Label>
                          <Input
                            id={`caption-${image.id}`}
                            placeholder="Image caption (optional)"
                            value={image.caption || ''}
                            onChange={(e) => handleImageCaptionChange(image.id, e.target.value)}
                            className="text-xs h-7"
                          />
                        </div>
                      </div>
                    )}

                    {selectedImageId !== image.id && (
                      <div className="space-y-2">
                        <Input
                          placeholder="Alt text (for accessibility)"
                          value={image.alt || ''}
                          onChange={(e) => handleImageAltChange(image.id, e.target.value)}
                          className="text-xs"
                        />
                        <Input
                          placeholder="Image caption (optional)"
                          value={image.caption || ''}
                          onChange={(e) => handleImageCaptionChange(image.id, e.target.value)}
                          className="text-xs"
                        />
                      </div>
                    )}
                  </div>
                ))}
                <Button
                  variant="outline"
                  className="flex flex-col items-center justify-center h-32 aspect-square border-dashed mx-auto"
                  onClick={() => setShowImageDialog(true)}
                >
                  <Plus className="h-8 w-8 mb-2" />
                  <span>Add Images</span>
                </Button>
              </div>
              {images.length === 0 && <div className="text-center text-muted-foreground text-sm mt-4">Add at least one image to your gallery</div>}
            </TabsContent>

            <TabsContent value="layout" className="space-y-4">
              <div className="grid grid-cols-4 gap-4">
                {layoutOptions.map((option) => {
                  // Get number of columns from the option value
                  const cols = Number.parseInt(option.value);

                  return (
                    <Button
                      key={option.value}
                      variant={layout === option.value ? 'default' : 'outline'}
                      className="h-20 flex flex-col items-center justify-center p-1"
                      onClick={() => handleLayoutChange(option.value)}
                    >
                      {/* Grid visualization */}
                      <div
                        className="grid gap-0.5 mb-1 w-full h-10"
                        style={{
                          gridTemplateColumns: `repeat(${cols}, 1fr)`,
                          gridTemplateRows: 'repeat(2, 1fr)',
                        }}
                      >
                        {Array.from({ length: cols * 2 }).map((_, i) => (
                          <div key={i} className="bg-muted rounded-sm" />
                        ))}
                      </div>
                      <span>{option.label}</span>
                    </Button>
                  );
                })}
              </div>
              <div className="mt-6 space-y-4">
                <div className="border rounded-lg p-4 overflow-visible">
                  <h4 className="text-sm font-medium mb-3">Image Adaptation</h4>
                  {images.length > 0 ? (
                    <div className="space-y-3">
                      {images.map((image) => (
                        <div
                          key={image.id}
                          className="flex items-center gap-3 p-2 border rounded-md cursor-move hover:shadow-sm transition-all"
                          draggable
                          data-id={image.id}
                          onDragStart={(e) => handleDragStart(e, image.id)}
                          onDragOver={handleDragOver}
                          onDragEnd={handleDragEnd}
                          onDrop={(e) => handleDrop(e, image.id)}
                          onDragEnter={(e) => {
                            e.preventDefault();
                            const target = e.currentTarget as HTMLElement;
                            const rect = target.getBoundingClientRect();
                            const y = e.clientY - rect.top;

                            // Clear previous indicators
                            target.classList.remove('border-t-2', 'border-b-2', 'border-t-primary', 'border-b-primary');

                            // Add indicator based on position (top or bottom half)
                            if (y < rect.height / 2) {
                              target.classList.add('border-t-2', 'border-t-primary');
                            } else {
                              target.classList.add('border-b-2', 'border-b-primary');
                            }
                          }}
                          onDragLeave={(e) => {
                            e.preventDefault();
                            const target = e.currentTarget as HTMLElement;
                            target.classList.remove('border-t-2', 'border-b-2', 'border-t-primary', 'border-b-primary');
                          }}
                        >
                          {/* Thumbnail */}
                          <div className="h-16 w-16 rounded-md overflow-hidden flex-shrink-0 border" style={{ aspectRatio: '1/1' }}>
                            <img src={image.src || '/placeholder.svg'} alt={image.alt} className="h-full w-full object-cover" />
                          </div>

                          {/* Controls */}
                          <div className="flex-grow space-y-2">
                            <div className="flex items-center justify-between">
                              <span className="text-xs font-medium truncate max-w-[120px]">{image.alt || 'Image'}</span>
                              <div className="flex items-center gap-2">
                                <Button
                                  variant={image.displayMode === 'crop' ? 'secondary' : 'outline'}
                                  size="sm"
                                  className="h-7 text-xs px-2"
                                  onClick={(e) => {
                                    e.preventDefault();
                                    e.stopPropagation();
                                    handleImageDisplayModeChange(image.id, 'crop');
                                  }}
                                >
                                  <Crop className="h-3 w-3 mr-1" />
                                  Crop
                                </Button>
                                <Button
                                  variant={image.displayMode === 'adaptive' ? 'secondary' : 'outline'}
                                  size="sm"
                                  className="h-7 text-xs px-2"
                                  onClick={(e) => {
                                    e.preventDefault();
                                    e.stopPropagation();
                                    handleImageDisplayModeChange(image.id, 'adaptive');
                                  }}
                                >
                                  <Maximize className="h-3 w-3 mr-1" />
                                  Adapt
                                </Button>
                              </div>
                            </div>

                            {image.displayMode === 'adaptive' && (
                              <div className="flex items-center gap-2" onClick={(e) => e.stopPropagation()}>
                                <span className="text-xs">Span:</span>
                                <Select
                                  value={image.span || '1x1'}
                                  onValueChange={(value) => {
                                    handleImageSpanChange(image.id, value as '1x1' | '1x2' | '2x1' | '2x2');
                                  }}
                                >
                                  <SelectTrigger className="h-7 text-xs">
                                    <SelectValue placeholder="Grid span" />
                                  </SelectTrigger>
                                  <SelectContent
                                    onCloseAutoFocus={(e) => e.preventDefault()}
                                    onEscapeKeyDown={(e) => e.preventDefault()}
                                    onPointerDownOutside={(e) => e.preventDefault()}
                                  >
                                    {spanOptions.map((option) => (
                                      <SelectItem key={option.value} value={option.value} className="text-xs">
                                        {option.label}
                                      </SelectItem>
                                    ))}
                                  </SelectContent>
                                </Select>
                              </div>
                            )}
                          </div>
                        </div>
                      ))}
                    </div>
                  ) : (
                    <div className="text-sm text-muted-foreground text-center py-4">Add images to customize their display</div>
                  )}
                </div>

                <div className="space-y-2 mt-4">
                  <Label htmlFor="caption-style" className="text-sm font-medium">
                    Caption Style
                  </Label>
                  <div className="grid grid-cols-3 gap-4">
                    <div className="space-y-1">
                      <Label htmlFor="caption-font-size" className="text-xs">
                        Font Size
                      </Label>
                      <Select value={captionStyle.fontSize || 'sm'} onValueChange={(value) => handleCaptionStyleChange('fontSize', value)}>
                        <SelectTrigger id="caption-font-size">
                          <SelectValue placeholder="Font size" />
                        </SelectTrigger>
                        <SelectContent>
                          <SelectItem value="xs">Extra Small</SelectItem>
                          <SelectItem value="sm">Small</SelectItem>
                          <SelectItem value="base">Medium</SelectItem>
                          <SelectItem value="lg">Large</SelectItem>
                        </SelectContent>
                      </Select>
                    </div>

                    <div className="space-y-1">
                      <Label htmlFor="caption-font-family" className="text-xs">
                        Font Family
                      </Label>
                      <Select value={captionStyle.fontFamily || 'sans'} onValueChange={(value) => handleCaptionStyleChange('fontFamily', value)}>
                        <SelectTrigger id="caption-font-family">
                          <SelectValue placeholder="Font family" />
                        </SelectTrigger>
                        <SelectContent>
                          <SelectItem value="sans">Sans-serif</SelectItem>
                          <SelectItem value="serif">Serif</SelectItem>
                          <SelectItem value="mono">Monospace</SelectItem>
                        </SelectContent>
                      </Select>
                    </div>

                    <div className="space-y-1">
                      <Label htmlFor="caption-font-weight" className="text-xs">
                        Font Weight
                      </Label>
                      <Select value={captionStyle.fontWeight || 'normal'} onValueChange={(value) => handleCaptionStyleChange('fontWeight', value)}>
                        <SelectTrigger id="caption-font-weight">
                          <SelectValue placeholder="Font weight" />
                        </SelectTrigger>
                        <SelectContent>
                          <SelectItem value="normal">Normal</SelectItem>
                          <SelectItem value="medium">Medium</SelectItem>
                          <SelectItem value="bold">Bold</SelectItem>
                        </SelectContent>
                      </Select>
                    </div>
                  </div>
                </div>

                <div>
                  <h4 className="text-sm font-medium mb-2">Preview</h4>
                  {images.length > 0 ? (
                    renderGalleryGrid()
                  ) : (
                    <div className="flex items-center justify-center h-40 bg-muted/20 rounded-lg border border-dashed">
                      <p className="text-sm text-muted-foreground">Add images to preview layout</p>
                    </div>
                  )}
                </div>
              </div>
            </TabsContent>

            <TabsContent value="settings" className="space-y-4">
              <div className="space-y-4">
                <div className="space-y-2">
                  <Label htmlFor="default-display-mode" className="text-sm font-medium">
                    Default Display Mode
                  </Label>
                  <div className="flex flex-col space-y-1.5">
                    <div className="flex items-center justify-between">
                      <div className="flex items-center gap-2">
                        <Button
                          variant={defaultDisplayMode === 'crop' ? 'default' : 'outline'}
                          size="sm"
                          onClick={() => handleDefaultDisplayModeChange('crop')}
                        >
                          <Crop className="h-4 w-4 mr-2" />
                          Crop to Square
                        </Button>
                        <Button
                          variant={defaultDisplayMode === 'adaptive' ? 'default' : 'outline'}
                          size="sm"
                          onClick={() => handleDefaultDisplayModeChange('adaptive')}
                        >
                          <Maximize className="h-4 w-4 mr-2" />
                          Adaptive
                        </Button>
                      </div>
                    </div>
                    <p className="text-xs text-muted-foreground">
                      {defaultDisplayMode === 'crop'
                        ? 'Images will be cropped to maintain a square aspect ratio'
                        : 'Images will adapt to the grid and may span multiple cells based on their aspect ratio'}
                    </p>
                  </div>
                </div>

                <div className="space-y-2">
                  <Label htmlFor="gallery-caption" className="text-sm font-medium">
                    Gallery Caption
                  </Label>
                  <Input
                    id="gallery-caption"
                    value={caption}
                    onChange={(e) => handleCaptionChange(e.target.value)}
                    placeholder="Add a caption for your gallery..."
                  />
                </div>
              </div>

              {caption && (
                <div className="mt-4">
                  <h4 className="text-sm font-medium mb-2">Preview</h4>
                  <div className="p-2 border rounded-md">
                    <p className={`text-sm text-center ${getCaptionStyleClasses()}`}>{caption}</p>
                  </div>
                </div>
              )}
            </TabsContent>
          </Tabs>

          <MediaUploadDialog
            open={showImageDialog}
            onOpenChange={setShowImageDialog}
            onMediaSelected={handleImageSelected}
            title="Add Images to Gallery"
            mode={0}
            acceptTypes="image/*"
            urlPlaceholder="https://example.com/image.jpg"
            uploadLabel="Select images from your device"
            urlLabel="Enter the image URL"
            maxSizeKB={5120}
            multiple={true}
          />
        </div>
      </div>
    </div>
  );
}

export function $createGalleryNode(): GalleryNode {
  return new GalleryNode({
    images: [],
    layout: '1',
    caption: '',
    isNew: true,
    defaultDisplayMode: 'crop',
    captionStyle: {
      fontSize: 'sm',
      fontFamily: 'sans',
      fontWeight: 'normal',
    },
  });
}
