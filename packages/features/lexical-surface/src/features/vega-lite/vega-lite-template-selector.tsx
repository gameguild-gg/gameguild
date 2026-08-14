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
import { BarChart3, Search, X, ChevronRight } from "lucide-react";
import {
  getAllTemplates,
  searchTemplates,
  TEMPLATE_CATEGORIES,
  type VegaLiteTemplate,
} from "./templates/template-loader";

interface VegaLiteTemplateSelectorProps {
  onSelect: (template: { type: string; spec: string; title?: string }) => void;
  onCancel: () => void;
}

const PREVIEW_IMAGES: Record<string, string> = {
  "single-view-plots/bar-charts/simple-bar.png": new URL(
    "./templates/single-view-plots/bar-charts/simple-bar.png",
    import.meta.url,
  ).href,
};

export function VegaLiteTemplateSelector({
  onSelect,
  onCancel,
}: VegaLiteTemplateSelectorProps) {
  const [searchTerm, setSearchTerm] = useState("");
  const [selectedCategory, setSelectedCategory] = useState<string>("all");
  const [selectedSubcategory, setSelectedSubcategory] = useState<string | null>(
    null,
  );
  const [templates, setTemplates] = useState<VegaLiteTemplate[]>([]);
  const [loading, setLoading] = useState(true);
  const [filteredTemplates, setFilteredTemplates] = useState<
    VegaLiteTemplate[]
  >([]);

  // Load templates on mount
  useEffect(() => {
    loadTemplates();
  }, []);

  // Filter templates when search or category changes
  useEffect(() => {
    filterTemplates();
  }, [searchTerm, selectedCategory, selectedSubcategory, templates]);

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

    // Apply subcategory filter
    if (selectedSubcategory) {
      result = result.filter((t) => t.subcategory === selectedSubcategory);
    }

    setFilteredTemplates(result);
  }

  const handleTemplateSelect = (template: VegaLiteTemplate) => {
    onSelect({
      type: template.type,
      spec: JSON.stringify(template.spec, null, 2),
      title: template.title,
    });
  };

  const handleCategorySelect = (categoryId: string) => {
    setSelectedCategory(categoryId);
    setSelectedSubcategory(null);
  };

  const handleSubcategorySelect = (subcategoryId: string) => {
    setSelectedSubcategory(subcategoryId);
  };

  const getCurrentCategory = () => {
    return TEMPLATE_CATEGORIES.find((c) => c.id === selectedCategory);
  };

  if (loading) {
    return (
      <div className="p-6 border-b border-gray-200 dark:border-gray-700 bg-gradient-to-r from-blue-50 to-indigo-50 dark:from-blue-950/30 dark:to-indigo-950/30">
        <div className="max-w-6xl mx-auto">
          <div className="flex items-center justify-center py-12">
            <div className="text-gray-600 dark:text-gray-300">
              Loading templates...
            </div>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="flex h-full min-h-0 flex-col overflow-hidden bg-gray-50 dark:bg-gray-900">
      <div className="flex-none px-6 pt-4 pb-2">
        <div className="max-w-6xl mx-auto">
          <div className="flex items-center justify-between mb-3">
            <div>
              <h3 className="text-lg font-semibold text-gray-900 dark:text-gray-100 mb-0.5">
                Choose a Chart Template
              </h3>
              <p className="text-xs text-gray-600 dark:text-gray-300">
                Start with a template and customize it to your needs
              </p>
            </div>
            <Button
              variant="ghost"
              size="sm"
              onClick={onCancel}
              className="hover:bg-gray-100 dark:hover:bg-gray-800"
            >
              <X className="h-4 w-4" />
            </Button>
          </div>

          {/* Search */}
          <div className="mb-2">
            <div className="relative max-w-md">
              <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 h-3.5 w-3.5 text-gray-400" />
              <Input
                placeholder="Search templates..."
                value={searchTerm}
                onChange={(e) => setSearchTerm(e.target.value)}
                className="pl-9 h-8 text-sm bg-white dark:bg-gray-800 border-gray-300 dark:border-gray-600"
              />
            </div>
          </div>

          {/* Category Navigation */}
          {!searchTerm && (
            <div className="mb-2">
              <div className="flex gap-1.5 mb-2 flex-wrap">
                <Button
                  variant={selectedCategory === "all" ? "default" : "outline"}
                  size="sm"
                  onClick={() => handleCategorySelect("all")}
                  className="border-gray-300 dark:border-gray-600 h-7 text-xs px-2.5"
                >
                  All Templates
                </Button>
                {TEMPLATE_CATEGORIES.map((category) => (
                  <Button
                    key={category.id}
                    variant={
                      selectedCategory === category.id ? "default" : "outline"
                    }
                    size="sm"
                    onClick={() => handleCategorySelect(category.id)}
                    className="border-gray-300 dark:border-gray-600 h-7 text-xs px-2.5"
                  >
                    {category.label}
                  </Button>
                ))}
              </div>

              {/* Subcategory Navigation */}
              {selectedCategory !== "all" && getCurrentCategory() && (
                <div className="flex gap-1.5 flex-wrap">
                  <Button
                    variant={!selectedSubcategory ? "secondary" : "ghost"}
                    size="sm"
                    onClick={() => setSelectedSubcategory(null)}
                    className="text-xs h-6 px-2"
                  >
                    All {getCurrentCategory()?.label}
                  </Button>
                  {getCurrentCategory()?.subcategories.map((subcategory) => (
                    <Button
                      key={subcategory.id}
                      variant={
                        selectedSubcategory === subcategory.id
                          ? "secondary"
                          : "ghost"
                      }
                      size="sm"
                      onClick={() => handleSubcategorySelect(subcategory.id)}
                      className="text-xs h-6 px-2"
                    >
                      <ChevronRight className="h-3 w-3 mr-1" />
                      {subcategory.label}
                    </Button>
                  ))}
                </div>
              )}
            </div>
          )}
        </div>
      </div>

      {/* Scrollable Template Area */}
      <div className="flex-1 overflow-y-auto overscroll-contain px-6 pb-6">
        <div className="max-w-6xl mx-auto">
          {/* Template Grid */}
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 xl:grid-cols-5 gap-4 mb-6">
            {filteredTemplates.map((template) => {
              const IconComponent = template.icon;

              const previewImageSrc = template.previewImage
                ? PREVIEW_IMAGES[
                    `${template.category}/${template.subcategory}/${template.previewImage}`
                  ]
                : undefined;

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
                        src={previewImageSrc}
                        alt={template.title}
                        className="w-full h-full object-contain p-3"
                        onError={(e) => {
                          // If image fails to load, hide it
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
                        <div className="text-xs text-gray-500 dark:text-gray-400 capitalize mt-0.5 line-clamp-1">
                          {template.subcategory.replace(/-/g, " ")}
                        </div>
                      </div>
                    </div>
                  </CardHeader>
                  <CardContent className="pt-0 pb-3 flex-1">
                    <CardDescription className="text-xs text-gray-600 dark:text-gray-300 leading-relaxed line-clamp-2">
                      {template.description}
                    </CardDescription>
                  </CardContent>
                </Card>
              );
            })}
          </div>

          {filteredTemplates.length === 0 && !loading && (
            <div className="text-center py-12">
              <div className="text-gray-400 dark:text-gray-600 mb-2">
                <Search className="h-12 w-12 mx-auto" />
              </div>
              <h3 className="text-lg font-medium text-gray-900 dark:text-gray-100 mb-1">
                No templates found
              </h3>
              <p className="text-gray-600 dark:text-gray-300">
                Try adjusting your search or filter criteria
              </p>
            </div>
          )}

          {/* Custom Template Option */}
          <div className="pt-6 border-t border-gray-200 dark:border-gray-700">
            <Card
              className="cursor-pointer hover:shadow-lg transition-all duration-200 border-dashed border-2 border-gray-300 dark:border-gray-600 hover:border-blue-400 dark:hover:border-blue-500 bg-gray-50 dark:bg-gray-800/50"
              onClick={() =>
                onSelect({
                  type: "custom",
                  spec: JSON.stringify(
                    {
                      $schema:
                        "https://vega.github.io/schema/vega-lite/v6.json",
                      data: {
                        values: [],
                      },
                      mark: "point",
                      encoding: {},
                    },
                    null,
                    2,
                  ),
                  title: "Custom Chart",
                })
              }
            >
              <CardContent className="p-6 text-center">
                <div className="p-3 rounded-lg bg-gray-200 dark:bg-gray-700 inline-block mb-3">
                  <BarChart3 className="h-6 w-6 text-gray-600 dark:text-gray-400" />
                </div>
                <h3 className="text-base font-medium text-gray-900 dark:text-gray-100 mb-1">
                  Start from Blank
                </h3>
                <p className="text-sm text-gray-600 dark:text-gray-300">
                  Create your own custom Vega-Lite specification
                </p>
              </CardContent>
            </Card>
          </div>
        </div>
      </div>
    </div>
  );
}
