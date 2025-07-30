'use client';

import { useEffect, useState } from 'react';
import { $getNodeByKey, DecoratorNode, type SerializedLexicalNode } from 'lexical';
import { useLexicalComposerContext } from '@lexical/react/LexicalComposerContext';
import { BookOpen, Check, Edit, ExternalLink, Plus, Trash2, X } from 'lucide-react';
import type { JSX } from 'react/jsx-runtime';

import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Textarea } from "@/components/ui/textarea"
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select"
import { cn } from "@/lib/utils"
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog"

export type SourceType = 'book' | 'article' | 'website' | 'journal' | 'paper' | 'other';

export interface SourceItem {
  id: string;
  type: SourceType;
  author: string;
  title: string;
  publication?: string;
  year?: string;
  url?: string;
  doi?: string;
  pages?: string;
  notes?: string;
}

export interface SourceData {
  sources: SourceItem[];
  title?: string;
  style?: 'apa' | 'mla' | 'chicago' | 'harvard' | 'ieee' | 'vancouver' | 'ama' | 'turabian' | 'abnt';
  isNew?: boolean;
}

export interface SerializedSourceNode extends SerializedLexicalNode {
  type: 'source';
  data: SourceData;
  version: 1;
}

export class SourceNode extends DecoratorNode<JSX.Element> {
  __data: SourceData;

  constructor(data: SourceData, key?: string) {
    super(key);
    this.__data = {
      sources: data.sources || [],
      title: data.title || 'References',
      style: data.style || 'apa',
      isNew: data.isNew,
    };
  }

  static getType(): string {
    return 'source';
  }

  static clone(node: SourceNode): SourceNode {
    return new SourceNode(node.__data, node.__key);
  }

  static importJSON(serializedNode: SerializedSourceNode): SourceNode {
    return new SourceNode(serializedNode.data);
  }

  createDOM(): HTMLElement {
    return document.createElement('div');
  }

  updateDOM(): false {
    return false;
  }

  setData(data: SourceData): void {
    const writable = this.getWritable();
    writable.__data = data;
  }

  exportJSON(): SerializedSourceNode {
    return {
      type: 'source',
      data: this.__data,
      version: 1,
    };
  }

  decorate(): JSX.Element {
    return <SourceComponent data={this.__data} nodeKey={this.__key} />;
  }
}

interface SourceComponentProps {
  data: SourceData;
  nodeKey: string;
}

function SourceComponent({ data, nodeKey }: SourceComponentProps) {
  const [editor] = useLexicalComposerContext();
  const [isEditing, setIsEditing] = useState(data.isNew || false);
  const [sources, setSources] = useState<SourceItem[]>(data.sources || []);
  const [title, setTitle] = useState(data.title || 'References');
  const [style, setStyle] = useState<'apa' | 'mla' | 'chicago' | 'harvard' | 'ieee' | 'vancouver' | 'ama' | 'turabian' | 'abnt'>(data.style || 'apa');
  const [editingSourceId, setEditingSourceId] = useState<string | null>(null);
  const [showMenu, setShowMenu] = useState(false);
  const [sourceToDelete, setSourceToDelete] = useState<string | null>(null);

  // Remove isNew flag after first render
  useEffect(() => {
    if (data.isNew) {
      editor.update(() => {
        const node = $getNodeByKey(nodeKey);
        if (node instanceof SourceNode) {
          const { isNew, ...rest } = data;
          node.setData(rest);
        }
      });
    }
  }, [data, editor, nodeKey]);

  const updateSource = (newData: Partial<SourceData>) => {
    editor.update(() => {
      const node = $getNodeByKey(nodeKey);
      if (node instanceof SourceNode) {
        node.setData({ ...data, ...newData });
      }
    });
  };

  const handleSave = () => {
    updateSource({
      sources,
      title,
      style,
    });
    setIsEditing(false);
  };

  const addSource = () => {
    const newSource: SourceItem = {
      id: Math.random().toString(36).substring(7),
      type: 'book',
      author: '',
      title: '',
    };
    const newSources = [...sources, newSource];
    setSources(newSources);
    setEditingSourceId(newSource.id);
    updateSource({ sources: newSources });
  };

  const updateSourceItem = (id: string, updates: Partial<SourceItem>) => {
    const newSources = sources.map((source) => (source.id === id ? { ...source, ...updates } : source));
    setSources(newSources);
    updateSource({ sources: newSources });
  };

  const removeSourceItem = (id: string) => {
    const newSources = sources.filter((source) => source.id !== id);
    setSources(newSources);
    if (editingSourceId === id) {
      setEditingSourceId(null);
    }
    updateSource({ sources: newSources });
  };

  // Format a source according to the selected style
  const formatSource = (source: SourceItem): string => {
    switch (style) {
      case 'apa':
        return formatAPA(source);
      case 'mla':
        return formatMLA(source);
      case 'chicago':
        return formatChicago(source);
      case 'harvard':
        return formatHarvard(source);
      case 'ieee':
        return formatIEEE(source);
      default:
        return formatAPA(source);
    }
  };

  // APA style: Author, A. A. (Year). Title of work. Publication. URL
  const formatAPA = (source: SourceItem): string => {
    let citation = '';

    // Author
    if (source.author) {
      citation += source.author;
      if (!citation.endsWith('.')) {
        citation += '.';
      }
      citation += ' ';
    }

    // Year
    if (source.year) {
      citation += `(${source.year}). `;
    }

    // Title
    if (source.title) {
      citation += source.title;
      if (!citation.endsWith('.')) {
        citation += '.';
      }
      citation += ' ';
    }

    // Publication
    if (source.publication) {
      citation += source.publication;
      if (!citation.endsWith('.')) {
        citation += '.';
      }
      citation += ' ';
    }

    // Pages
    if (source.pages) {
      citation += source.pages;
      if (!citation.endsWith('.')) {
        citation += '.';
      }
      citation += ' ';
    }

    // DOI
    if (source.doi) {
      citation += `https://doi.org/${source.doi}`;
    } else if (source.url) {
      citation += source.url;
    }

    return citation.trim();
  };

  // MLA style: Author. "Title." Publication, Year. URL
  const formatMLA = (source: SourceItem): string => {
    let citation = '';

    // Author
    if (source.author) {
      citation += source.author;
      if (!citation.endsWith('.')) {
        citation += '.';
      }
      citation += ' ';
    }

    // Title
    if (source.title) {
      citation += `"${source.title}"`;
      if (!citation.endsWith('.')) {
        citation += '.';
      }
      citation += ' ';
    }

    // Publication
    if (source.publication) {
      citation += source.publication;
      if (source.year) {
        citation += `, ${source.year}`;
      }
      if (!citation.endsWith('.')) {
        citation += '.';
      }
      citation += ' ';
    } else if (source.year) {
      citation += source.year;
      if (!citation.endsWith('.')) {
        citation += '.';
      }
      citation += ' ';
    }

    // Pages
    if (source.pages) {
      citation += source.pages;
      if (!citation.endsWith('.')) {
        citation += '.';
      }
      citation += ' ';
    }

    // URL
    if (source.url) {
      citation += source.url;
    }

    return citation.trim();
  };

  // Chicago style: Author. Year. "Title." Publication. URL
  const formatChicago = (source: SourceItem): string => {
    let citation = '';

    // Author
    if (source.author) {
      citation += source.author;
      if (!citation.endsWith('.')) {
        citation += '.';
      }
      citation += ' ';
    }

    // Year
    if (source.year) {
      citation += source.year;
      if (!citation.endsWith('.')) {
        citation += '.';
      }
      citation += ' ';
    }

    // Title
    if (source.title) {
      citation += `"${source.title}"`;
      if (!citation.endsWith('.')) {
        citation += '.';
      }
      citation += ' ';
    }

    // Publication
    if (source.publication) {
      citation += source.publication;
      if (!citation.endsWith('.')) {
        citation += '.';
      }
      citation += ' ';
    }

    // Pages
    if (source.pages) {
      citation += source.pages;
      if (!citation.endsWith('.')) {
        citation += '.';
      }
      citation += ' ';
    }

    // URL
    if (source.url) {
      citation += source.url;
    }

    return citation.trim();
  };

  // Harvard style: Author (Year) Title, Publication, Pages. URL
  const formatHarvard = (source: SourceItem): string => {
    let citation = '';

    // Author
    if (source.author) {
      citation += source.author;
      citation += ' ';
    }

    // Year
    if (source.year) {
      citation += `(${source.year}) `;
    }

    // Title
    if (source.title) {
      citation += source.title;
      if (!citation.endsWith('.')) {
        citation += ',';
      }
      citation += ' ';
    }

    // Publication
    if (source.publication) {
      citation += source.publication;
      if (!citation.endsWith('.')) {
        citation += ',';
      }
      citation += ' ';
    }

    // Pages
    if (source.pages) {
      citation += source.pages;
      if (!citation.endsWith('.')) {
        citation += '.';
      }
      citation += ' ';
    }

    // URL
    if (source.url) {
      citation += source.url;
    }

    return citation.trim();
  };

  // IEEE style: [1] A. Author, "Title," Publication, Year, Pages. URL
  const formatIEEE = (source: SourceItem): string => {
    let citation = '';

    // Author
    if (source.author) {
      citation += source.author;
      if (!citation.endsWith(',')) {
        citation += ',';
      }
      citation += ' ';
    }

    // Title
    if (source.title) {
      citation += `"${source.title}"`;
      if (!citation.endsWith(',')) {
        citation += ',';
      }
      citation += ' ';
    }

    // Publication
    if (source.publication) {
      citation += source.publication;
      if (!citation.endsWith(',')) {
        citation += ',';
      }
      citation += ' ';
    }

    // Year
    if (source.year) {
      citation += source.year;
      if (!citation.endsWith('.')) {
        citation += ',';
      }
      citation += ' ';
    }

    // Pages
    if (source.pages) {
      citation += source.pages;
      if (!citation.endsWith('.')) {
        citation += '.';
      }
      citation += ' ';
    }

    // URL
    if (source.url) {
      citation += source.url;
    }

    return citation.trim();
  };

  const getSourceTypeLabel = (type: SourceType): string => {
    switch (type) {
      case 'book':
        return 'Book';
      case 'article':
        return 'Article';
      case 'website':
        return 'Website';
      case 'journal':
        return 'Journal';
      case 'paper':
        return 'Paper';
      case 'other':
        return 'Other';
      default:
        return 'Book';
    }
  };

  return (
    <>
      {!isEditing ? (
        <div className="my-8 relative group" onMouseEnter={() => setShowMenu(true)} onMouseLeave={() => setShowMenu(false)}>
          <div className="bg-white dark:bg-gray-900 rounded-xl border-2 border-gray-200 dark:border-gray-700 p-6 shadow-sm hover:shadow-md transition-all duration-200 hover:border-gray-300 dark:hover:border-gray-600">
            <div className="flex items-center gap-3 mb-5">
              <div className="p-2 bg-blue-100 dark:bg-blue-900/30 rounded-lg">
                <BookOpen className="h-5 w-5 text-blue-600 dark:text-blue-400" />
              </div>
              <div>
                <h3 className="text-xl font-semibold text-gray-900 dark:text-gray-100">{title}</h3>
                <p className="text-sm text-gray-500 dark:text-gray-400">
                  {sources.length} {sources.length === 1 ? 'source' : 'sources'} • {style.toUpperCase()} format
                </p>
              </div>
            </div>

            {sources.length > 0 ? (
              <div className="space-y-4">
                {sources.map((source, index) => (
                  <div
                    key={source.id}
                    className="group relative bg-gray-50 dark:bg-gray-800/50 rounded-lg p-4 border border-gray-200 dark:border-gray-700 hover:bg-gray-100 dark:hover:bg-gray-800 transition-colors duration-150"
                  >
                    <div className="flex items-start gap-3">
                      <div className="flex-shrink-0 w-8 h-8 bg-white dark:bg-gray-700 border border-gray-300 dark:border-gray-600 rounded-full flex items-center justify-center">
                        <span className="text-sm font-semibold text-gray-600 dark:text-gray-300">{index + 1}</span>
                      </div>
                      <div className="flex-1 min-w-0">
                        <div className="flex items-center gap-2 mb-2">
                          <span className="inline-flex items-center px-2 py-1 rounded-md text-xs font-medium bg-blue-100 dark:bg-blue-900/30 text-blue-700 dark:text-blue-400">
                            {getSourceTypeLabel(source.type)}
                          </span>
                          {source.year && (
                            <span className="inline-flex items-center px-2 py-1 rounded-md text-xs font-medium bg-gray-200 dark:bg-gray-700 text-gray-700 dark:text-gray-300">
                              {source.year}
                            </span>
                          )}
                          {source.url && (
                            <a
                              href={source.url}
                              target="_blank"
                              rel="noopener noreferrer"
                              className="inline-flex items-center text-blue-600 dark:text-blue-400 hover:text-blue-800 dark:hover:text-blue-300 transition-colors"
                              title="Open external link"
                            >
                              <ExternalLink className="h-3 w-3" />
                            </a>
                          )}
                        </div>
                        <div className="text-sm text-gray-800 dark:text-gray-200 leading-relaxed font-mono bg-white dark:bg-gray-900 rounded-md p-3 border border-gray-200 dark:border-gray-700">
                          {formatSource(source)}
                        </div>
                      </div>
                    </div>
                  </div>
                ))}
              </div>
            ) : (
              <div className="text-center py-8">
                <div className="w-16 h-16 bg-gray-100 dark:bg-gray-800 rounded-full flex items-center justify-center mx-auto mb-3">
                  <BookOpen className="h-8 w-8 text-gray-400" />
                </div>
                <p className="text-gray-500 dark:text-gray-400 font-medium">No sources added yet</p>
                <p className="text-sm text-gray-400 dark:text-gray-500 mt-1">Click edit to add your first source</p>
              </div>
            )}
          </div>

          {/* Edit button */}
          {showMenu && (
            <Button
              variant="ghost"
              size="sm"
              className="absolute top-2 right-2 opacity-0 group-hover:opacity-100 transition-opacity"
              onClick={() => setIsEditing(true)}
            >
              <Edit className="h-4 w-4 mr-2" />
              Edit
            </Button>
          )}
        </div>
      ) : (
        <>
          {/* Modal overlay for editing */}
          <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50 p-4">
            <div className="bg-white dark:bg-gray-900 rounded-xl shadow-2xl border max-w-5xl w-full max-h-[95vh] overflow-hidden flex flex-col">
              {/* Enhanced Header */}
              <div className="flex items-center justify-between p-6 border-b bg-gray-50 dark:bg-gray-800/50">
                <div className="flex items-center gap-3">
                  <div className="p-2 bg-blue-100 dark:bg-blue-900/30 rounded-lg">
                    <BookOpen className="h-5 w-5 text-blue-600 dark:text-blue-400" />
                  </div>
                  <div>
                    <h3 className="text-xl font-semibold text-gray-900 dark:text-gray-100">Sources & References</h3>
                    <p className="text-sm text-gray-500 dark:text-gray-400">Manage your bibliography and citations</p>
                  </div>
                </div>
                <div className="flex items-center gap-3">
                  <Button variant="ghost" size="sm" onClick={() => setIsEditing(false)} className="text-gray-500 hover:text-gray-700">
                    <X className="h-4 w-4 mr-2" />
                    Cancel
                  </Button>
                  <Button variant="default" size="sm" onClick={handleSave} className="bg-blue-600 hover:bg-blue-700">
                    <BookOpen className="h-4 w-4 mr-2" />
                    Save Changes
                  </Button>
                </div>
              </div>

              {/* Scrollable Content */}
              <div className="flex-1 overflow-y-auto">
                <div className="p-6 space-y-6">
                  {/* Configuration Section */}
                  <div className="bg-gray-50 dark:bg-gray-800/30 rounded-lg p-4">
                    <h4 className="text-lg font-medium text-gray-900 dark:text-gray-100 mb-4">Configuration</h4>
                    <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                      <div className="space-y-2">
                        <Label htmlFor="source-title" className="text-sm font-medium text-gray-700 dark:text-gray-300">
                          Section Title
                        </Label>
                        <Input
                          id="source-title"
                          value={title}
                          onChange={(e) => setTitle(e.target.value)}
                          placeholder="References"
                          className="bg-white dark:bg-gray-900 border-gray-300 dark:border-gray-600 focus:border-blue-500 focus:ring-blue-500"
                        />
                      </div>
                      <div className="space-y-2">
                        <Label htmlFor="citation-style" className="text-sm font-medium text-gray-700 dark:text-gray-300">
                          Citation Style
                        </Label>
                        <Select value={style} onValueChange={(value) => setStyle(value as any)}>
                          <SelectTrigger id="citation-style" className="bg-white dark:bg-gray-900 border-gray-300 dark:border-gray-600">
                            <SelectValue placeholder="Select citation style" />
                          </SelectTrigger>
                          <SelectContent>
                            <SelectItem value="apa">APA (American Psychological Association)</SelectItem>
                            <SelectItem value="mla">MLA (Modern Language Association)</SelectItem>
                            <SelectItem value="chicago">Chicago Manual of Style</SelectItem>
                            <SelectItem value="harvard">Harvard Referencing</SelectItem>
                            <SelectItem value="ieee">IEEE (Institute of Electrical and Electronics Engineers)</SelectItem>
                            <SelectItem value="vancouver">Vancouver System</SelectItem>
                            <SelectItem value="ama">AMA (American Medical Association)</SelectItem>
                            <SelectItem value="turabian">Turabian Style</SelectItem>
                            <SelectItem value="abnt">ABNT (Brazilian Association of Technical Standards)</SelectItem>
                          </SelectContent>
                        </Select>
                      </div>
                    </div>
                  </div>

                  {/* Sources Section */}
                  <div>
                    <div className="flex items-center justify-between mb-4">
                      <div>
                        <h4 className="text-lg font-medium text-gray-900 dark:text-gray-100">Sources</h4>
                        <p className="text-sm text-gray-500 dark:text-gray-400">Add and manage your bibliography entries</p>
                      </div>
                      <Button
                        variant="outline"
                        size="sm"
                        onClick={addSource}
                        className="bg-blue-50 hover:bg-blue-100 text-blue-700 border-blue-200 hover:border-blue-300"
                      >
                        <Plus className="h-4 w-4 mr-2" />
                        Add New Source
                      </Button>
                    </div>

                    {sources.length === 0 ? (
                      <div className="text-center py-12 border-2 border-dashed border-gray-300 dark:border-gray-600 rounded-xl bg-gray-50 dark:bg-gray-800/30">
                        <div className="p-3 bg-gray-100 dark:bg-gray-700 rounded-full w-16 h-16 mx-auto mb-4 flex items-center justify-center">
                          <BookOpen className="h-8 w-8 text-gray-400" />
                        </div>
                        <h5 className="text-lg font-medium text-gray-900 dark:text-gray-100 mb-2">No sources added yet</h5>
                        <p className="text-gray-500 dark:text-gray-400 mb-4 max-w-sm mx-auto">
                          Start building your bibliography by adding your first source reference
                        </p>
                        <Button variant="default" size="sm" onClick={addSource} className="bg-blue-600 hover:bg-blue-700">
                          <Plus className="h-4 w-4 mr-2" />
                          Add Your First Source
                        </Button>
                      </div>
                    ) : (
                      <div className="space-y-4">
                        {sources.map((source, index) => (
                          <div
                            key={source.id}
                            className={cn(
                              'border-2 rounded-xl p-5 transition-all duration-200',
                              editingSourceId === source.id
                                ? 'border-blue-500 bg-blue-50 dark:bg-blue-950/20 shadow-lg'
                                : 'border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800/50 hover:border-gray-300 dark:hover:border-gray-600 hover:shadow-md',
                            )}
                          >
                            {editingSourceId === source.id ? (
                              <div className="space-y-5">
                                <div className="flex items-center justify-between">
                                  <div className="flex items-center gap-2">
                                    <div className="w-8 h-8 bg-blue-100 dark:bg-blue-900/30 rounded-lg flex items-center justify-center">
                                      <span className="text-sm font-semibold text-blue-600 dark:text-blue-400">{index + 1}</span>
                                    </div>
                                    <h5 className="text-lg font-semibold text-gray-900 dark:text-gray-100">Edit Source</h5>
                                  </div>
                                  <Button
                                    variant="ghost"
                                    size="sm"
                                    onClick={() => setEditingSourceId(null)}
                                    className="text-gray-400 hover:text-gray-600 hover:bg-gray-100 dark:hover:bg-gray-700"
                                  >
                                    <X className="h-4 w-4" />
                                  </Button>
                                </div>

                                <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                                  <div className="space-y-2">
                                    <Label htmlFor={`source-type-${source.id}`} className="text-sm font-medium text-gray-700 dark:text-gray-300">
                                      Source Type
                                    </Label>
                                    <Select value={source.type} onValueChange={(value) => updateSourceItem(source.id, { type: value as SourceType })}>
                                      <SelectTrigger id={`source-type-${source.id}`} className="bg-white dark:bg-gray-900">
                                        <SelectValue placeholder="Select source type" />
                                      </SelectTrigger>
                                      <SelectContent>
                                        <SelectItem value="book">📚 Book</SelectItem>
                                        <SelectItem value="article">📄 Article</SelectItem>
                                        <SelectItem value="website">🌐 Website</SelectItem>
                                        <SelectItem value="journal">📰 Journal</SelectItem>
                                        <SelectItem value="paper">📋 Paper</SelectItem>
                                        <SelectItem value="other">📎 Other</SelectItem>
                                      </SelectContent>
                                    </Select>
                                  </div>

                                  <div className="space-y-2">
                                    <Label htmlFor={`source-year-${source.id}`} className="text-sm font-medium text-gray-700 dark:text-gray-300">
                                      Publication Year
                                    </Label>
                                    <Input
                                      id={`source-year-${source.id}`}
                                      value={source.year || ''}
                                      onChange={(e) => updateSourceItem(source.id, { year: e.target.value })}
                                      placeholder="2024"
                                      className="bg-white dark:bg-gray-900"
                                    />
                                  </div>
                                </div>

                                <div className="space-y-2">
                                  <Label htmlFor={`source-author-${source.id}`} className="text-sm font-medium text-gray-700 dark:text-gray-300">
                                    Author(s)
                                  </Label>
                                  <Input
                                    id={`source-author-${source.id}`}
                                    value={source.author}
                                    onChange={(e) => updateSourceItem(source.id, { author: e.target.value })}
                                    placeholder="Last, First M. & Last, First M."
                                    className="bg-white dark:bg-gray-900"
                                  />
                                </div>

                                <div className="space-y-2">
                                  <Label htmlFor={`source-title-${source.id}`} className="text-sm font-medium text-gray-700 dark:text-gray-300">
                                    Title
                                  </Label>
                                  <Input
                                    id={`source-title-${source.id}`}
                                    value={source.title}
                                    onChange={(e) => updateSourceItem(source.id, { title: e.target.value })}
                                    placeholder="Title of the work"
                                    className="bg-white dark:bg-gray-900"
                                  />
                                </div>

                                <div className="space-y-2">
                                  <Label htmlFor={`source-publication-${source.id}`} className="text-sm font-medium text-gray-700 dark:text-gray-300">
                                    Publication
                                  </Label>
                                  <Input
                                    id={`source-publication-${source.id}`}
                                    value={source.publication || ''}
                                    onChange={(e) => updateSourceItem(source.id, { publication: e.target.value })}
                                    placeholder="Journal name, publisher, or website"
                                    className="bg-white dark:bg-gray-900"
                                  />
                                </div>

                                <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                                  <div className="space-y-2">
                                    <Label htmlFor={`source-pages-${source.id}`} className="text-sm font-medium text-gray-700 dark:text-gray-300">
                                      Pages
                                    </Label>
                                    <Input
                                      id={`source-pages-${source.id}`}
                                      value={source.pages || ''}
                                      onChange={(e) => updateSourceItem(source.id, { pages: e.target.value })}
                                      placeholder="pp. 123-145"
                                      className="bg-white dark:bg-gray-900"
                                    />
                                  </div>

                                  <div className="space-y-2">
                                    <Label htmlFor={`source-doi-${source.id}`} className="text-sm font-medium text-gray-700 dark:text-gray-300">
                                      DOI
                                    </Label>
                                    <Input
                                      id={`source-doi-${source.id}`}
                                      value={source.doi || ''}
                                      onChange={(e) => updateSourceItem(source.id, { doi: e.target.value })}
                                      placeholder="10.1000/xyz123"
                                      className="bg-white dark:bg-gray-900"
                                    />
                                  </div>
                                </div>

                                <div className="space-y-2">
                                  <Label htmlFor={`source-url-${source.id}`} className="text-sm font-medium text-gray-700 dark:text-gray-300">
                                    URL
                                  </Label>
                                  <Input
                                    id={`source-url-${source.id}`}
                                    value={source.url || ''}
                                    onChange={(e) => updateSourceItem(source.id, { url: e.target.value })}
                                    placeholder="https://example.com"
                                    className="bg-white dark:bg-gray-900"
                                  />
                                </div>

                                <div className="space-y-2">
                                  <Label htmlFor={`source-notes-${source.id}`} className="text-sm font-medium text-gray-700 dark:text-gray-300">
                                    Notes (Optional)
                                  </Label>
                                  <Textarea
                                    id={`source-notes-${source.id}`}
                                    value={source.notes || ''}
                                    onChange={(e) => updateSourceItem(source.id, { notes: e.target.value })}
                                    placeholder="Additional notes for your reference (not included in citation)"
                                    className="bg-white dark:bg-gray-900 resize-none"
                                    rows={3}
                                  />
                                </div>

                                <div className="flex justify-between items-center pt-4 border-t border-gray-200 dark:border-gray-600">
                                  <Button
                                    variant="destructive"
                                    size="sm"
                                    onClick={() => removeSourceItem(source.id)}
                                    className="bg-red-50 hover:bg-red-100 text-red-700 border-red-200 hover:border-red-300"
                                  >
                                    <Trash2 className="h-4 w-4 mr-2" />
                                    Remove Source
                                  </Button>
                                  <Button variant="default" size="sm" onClick={() => setEditingSourceId(null)} className="bg-green-600 hover:bg-green-700">
                                    <Check className="h-4 w-4 mr-2" />
                                    Done Editing
                                  </Button>
                                </div>
                              </div>
                            ) : (
                              <div className="flex items-start justify-between gap-4">
                                <div className="flex-1 min-w-0">
                                  <div className="flex items-center gap-3 mb-2">
                                    <div className="w-8 h-8 bg-gray-100 dark:bg-gray-700 rounded-lg flex items-center justify-center flex-shrink-0">
                                      <span className="text-sm font-semibold text-gray-600 dark:text-gray-300">{index + 1}</span>
                                    </div>
                                    <div className="flex items-center gap-2 text-sm text-gray-500 dark:text-gray-400">
                                      <span className="px-2 py-1 bg-gray-100 dark:bg-gray-700 rounded-md font-medium">{getSourceTypeLabel(source.type)}</span>
                                      {source.year && (
                                        <span className="px-2 py-1 bg-blue-100 dark:bg-blue-900/30 text-blue-700 dark:text-blue-400 rounded-md font-medium">
                                          {source.year}
                                        </span>
                                      )}
                                    </div>
                                  </div>
                                  <h6 className="font-semibold text-gray-900 dark:text-gray-100 mb-1 truncate">{source.title || 'Untitled'}</h6>
                                  <p className="text-sm text-gray-600 dark:text-gray-400 mb-2 truncate">{source.author || 'No author specified'}</p>
                                  <div className="text-sm text-gray-500 dark:text-gray-400 bg-gray-50 dark:bg-gray-800 rounded-md p-3">
                                    <strong>Citation preview:</strong>
                                    <br />
                                    <span className="font-mono text-xs">{formatSource(source)}</span>
                                  </div>
                                </div>
                                <div className="flex items-center gap-2 flex-shrink-0">
                                  <Button
                                    variant="ghost"
                                    size="sm"
                                    onClick={() => setEditingSourceId(source.id)}
                                    className="text-gray-500 hover:text-blue-600 hover:bg-blue-50 dark:hover:bg-blue-950/20"
                                  >
                                    <Edit className="h-4 w-4" />
                                  </Button>
                                  <Button
                                    variant="ghost"
                                    size="sm"
                                    onClick={() => setSourceToDelete(source.id)}
                                    className="text-gray-500 hover:text-red-600 hover:bg-red-50 dark:hover:bg-red-950/20"
                                  >
                                    <Trash2 className="h-4 w-4" />
                                  </Button>
                                </div>
                              </div>
                            )}
                          </div>
                        ))}
                      </div>
                    )}
                  </div>
                </div>
              </div>

              {/* Enhanced Footer */}
              <div className="border-t bg-gray-50 dark:bg-gray-800/50 p-6">
                <div className="flex justify-between items-center">
                  <div className="flex items-center gap-3">
                    <Button variant="outline" onClick={() => addSource()} className="bg-white dark:bg-gray-800">
                      <Plus className="h-4 w-4 mr-2" />
                      Add Another Source
                    </Button>
                    <span className="text-sm text-gray-500 dark:text-gray-400">
                      {sources.length} {sources.length === 1 ? 'source' : 'sources'} added
                    </span>
                  </div>
                  <div className="flex gap-3">
                    <Button variant="outline" onClick={() => setIsEditing(false)} className="bg-white dark:bg-gray-800">
                      Cancel
                    </Button>
                    <Button variant="default" onClick={handleSave} className="bg-blue-600 hover:bg-blue-700">
                      <BookOpen className="h-4 w-4 mr-2" />
                      Save All Changes
                    </Button>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </>
      )}

      {/* Confirmation Dialog */}
      <Dialog open={sourceToDelete !== null} onOpenChange={(open) => !open && setSourceToDelete(null)}>
        <DialogContent className="sm:max-w-[425px]">
          <DialogHeader>
            <DialogTitle>Confirm Deletion</DialogTitle>
            <DialogDescription>Are you sure you want to delete this source? This action cannot be undone.</DialogDescription>
          </DialogHeader>
          <DialogFooter>
            <Button variant="outline" onClick={() => setSourceToDelete(null)}>
              Cancel
            </Button>
            <Button
              variant="destructive"
              onClick={() => {
                if (sourceToDelete) {
                  removeSourceItem(sourceToDelete);
                  setSourceToDelete(null);
                }
              }}
            >
              Delete
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </>
  );
}

export function $createSourceNode(): SourceNode {
  return new SourceNode({
    sources: [],
    title: 'References',
    style: 'apa',
    isNew: true,
  });
}
