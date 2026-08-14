"use client";

import { useState, useEffect } from "react";
import { Button } from "@game-guild/ui/components/button";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@game-guild/ui/components/card";
import { Input } from "@game-guild/ui/components/input";
import { Search, X } from "lucide-react";
import type { MermaidData } from "./mermaid-data";
import {
  getAllTemplates,
  searchTemplates,
  type MermaidTemplate,
} from "./templates/template-loader";

interface MermaidTemplateSelectorProps {
  onSelect: (template: { type: MermaidData["type"]; code: string }) => void;
  onCancel: () => void;
}

export function MermaidTemplateSelector({
  onSelect,
  onCancel,
}: MermaidTemplateSelectorProps) {
  const [searchTerm, setSearchTerm] = useState("");
  const [selectedCategory, setSelectedCategory] = useState<string>("all");
  const [templates, setTemplates] = useState<MermaidTemplate[]>([]);
  const [loading, setLoading] = useState(true);
  const [filteredTemplates, setFilteredTemplates] = useState<MermaidTemplate[]>(
    [],
  );

  // Get unique categories from templates
  const categories = templates.reduce((acc, template) => {
    if (!acc.includes(template.category)) {
      acc.push(template.category);
    }
    return acc;
  }, [] as string[]);

  // Load templates on mount
  useEffect(() => {
    loadTemplates();
  }, []);

  // Filter templates when search or category changes
  useEffect(() => {
    filterTemplates();
  }, [searchTerm, selectedCategory, templates]);

  async function loadTemplates() {
    setLoading(true);
    try {
      const allTemplates = await getAllTemplates();
      setTemplates(allTemplates);
    } catch (error) {
      console.error("Failed to load templates:", error);
    } finally {
      setLoading(false);
    }
  }

  async function filterTemplates() {
    let result = templates;

    // Apply search filter
    if (searchTerm) {
      result = await searchTemplates(searchTerm);
    }

    // Apply category filter
    if (selectedCategory !== "all") {
      result = result.filter((t) => t.category === selectedCategory);
    }

    setFilteredTemplates(result);
  }

  const handleTemplateSelect = (template: MermaidTemplate) => {
    onSelect({
      type: template.type,
      code: template.code,
    });
  };

  return (
    <div className="flex h-full min-h-0 flex-col bg-gray-50 dark:bg-gray-900">
      {/* Header */}
      <div className="px-6 pt-4 pb-2 border-b border-gray-200 dark:border-gray-700">
        <div className="flex items-center justify-between mb-3">
          <div>
            <h3 className="text-lg font-semibold text-gray-900 dark:text-gray-100">
              Choose a Diagram Template
            </h3>
            <p className="text-xs text-gray-600 dark:text-gray-400 mt-0.5">
              Select a template to get started
            </p>
          </div>
        </div>

        {/* Search Bar */}
        <div className="relative mb-3">
          <Search className="absolute left-3 top-2 h-4 w-4 text-gray-400" />
          <Input
            placeholder="Search templates..."
            value={searchTerm}
            onChange={(e) => setSearchTerm(e.target.value)}
            className="pl-10 h-8 text-sm bg-white dark:bg-gray-800 border-gray-300 dark:border-gray-600"
          />
          {searchTerm && (
            <button
              onClick={() => setSearchTerm("")}
              className="absolute right-3 top-2 text-gray-400 hover:text-gray-600"
            >
              <X className="h-4 w-4" />
            </button>
          )}
        </div>

        {/* Category Filter */}
        {categories.length > 0 && (
          <div className="flex flex-wrap gap-2">
            <Button
              size="sm"
              variant={selectedCategory === "all" ? "default" : "outline"}
              onClick={() => setSelectedCategory("all")}
              className="h-6 px-2.5 text-xs"
            >
              All
            </Button>
            {categories.map((category) => (
              <Button
                key={category}
                size="sm"
                variant={selectedCategory === category ? "default" : "outline"}
                onClick={() => setSelectedCategory(category)}
                className="h-6 px-2.5 text-xs capitalize"
              >
                {category.replace(/-/g, " ")}
              </Button>
            ))}
          </div>
        )}
      </div>

      {/* Content */}
      <div className="flex-1 overflow-y-auto px-6 py-4">
        {loading ? (
          <div className="flex items-center justify-center h-full">
            <p className="text-gray-500 dark:text-gray-400">
              Loading templates...
            </p>
          </div>
        ) : filteredTemplates.length === 0 ? (
          <div className="flex items-center justify-center h-full">
            <p className="text-gray-500 dark:text-gray-400 text-center">
              No templates found
              {searchTerm && ` matching "${searchTerm}"`}
            </p>
          </div>
        ) : (
          <div className="max-w-6xl mx-auto">
            {/* Template Grid */}
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 xl:grid-cols-5 gap-4 mb-6">
              {filteredTemplates.map((template) => {
                const IconComponent = template.icon;

                // Try to load preview image
                let previewImageSrc = null;
                if (template.previewImage) {
                  try {
                    previewImageSrc =
                      require(`./templates/${template.previewImage}`).default
                        ?.src ||
                      require(`./templates/${template.previewImage}`);
                  } catch (e) {
                    console.warn(
                      `Preview image not found for template ${template.id}:`,
                      template.previewImage,
                    );
                  }
                }

                return (
                  <Card
                    key={template.id}
                    className="cursor-pointer hover:shadow-lg transition-all duration-200 border-gray-200 dark:border-gray-700 hover:border-blue-300 dark:hover:border-blue-600 bg-white dark:bg-gray-800 overflow-hidden flex flex-col"
                    onClick={() => handleTemplateSelect(template)}
                  >
                    {/* Preview Image - Square aspect ratio */}
                    {previewImageSrc ? (
                      <div className="w-full aspect-square bg-gray-100 dark:bg-gray-700 relative overflow-hidden">
                        <img
                          src={
                            typeof previewImageSrc === "string"
                              ? previewImageSrc
                              : previewImageSrc.src || previewImageSrc
                          }
                          alt={template.title}
                          className="w-full h-full object-contain p-3"
                          onError={(e) => {
                            e.currentTarget.style.display = "none";
                          }}
                        />
                      </div>
                    ) : (
                      <div className="w-full aspect-square bg-gradient-to-br from-blue-50 to-indigo-50 dark:from-blue-900/20 dark:to-indigo-900/20 flex items-center justify-center">
                        <IconComponent className="h-16 w-16 text-blue-300 dark:text-blue-700" />
                      </div>
                    )}

                    <CardHeader className="pb-2 pt-3">
                      <div className="flex items-start gap-2">
                        <div className="p-1.5 rounded-lg bg-blue-100 dark:bg-blue-900/30 flex-shrink-0">
                          <IconComponent className="h-4 w-4 text-blue-600 dark:text-blue-400" />
                        </div>
                        <div className="flex-1 min-w-0">
                          <CardTitle className="text-sm font-medium text-gray-900 dark:text-gray-100 line-clamp-1">
                            {template.title}
                          </CardTitle>
                          <div className="text-xs text-gray-500 dark:text-gray-400 capitalize mt-0.5">
                            {template.category.replace(/-/g, " ")}
                          </div>
                        </div>
                      </div>
                    </CardHeader>

                    <CardContent className="pt-0 pb-3 flex-1">
                      <CardDescription className="text-xs text-gray-600 dark:text-gray-300 leading-relaxed line-clamp-2">
                        {template.description}
                      </CardDescription>
                    </CardContent>

                    <CardContent className="pt-0 pb-3">
                      <Button
                        size="sm"
                        className="w-full h-7 text-xs bg-blue-500 hover:bg-blue-600 text-white"
                      >
                        Select Template
                      </Button>
                    </CardContent>
                  </Card>
                );
              })}
            </div>
          </div>
        )}
      </div>

      {/* Footer */}
      <div className="border-t border-gray-200 dark:border-gray-700 px-6 py-3 bg-gray-50 dark:bg-gray-900 flex justify-center">
        <Button variant="ghost" onClick={onCancel} size="sm">
          Cancel
        </Button>
      </div>
    </div>
  );
}
