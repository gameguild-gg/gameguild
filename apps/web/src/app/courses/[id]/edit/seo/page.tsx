'use client';

import { useCourseEditor } from '@/lib/courses/course-editor.context';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Textarea } from '@/components/ui/textarea';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Label } from '@/components/ui/label';
import { Separator } from '@/components/ui/separator';
import { 
  Globe, Search, Tag, Eye, ExternalLink, Twitter, Facebook, Plus, X, AlertCircle, CheckCircle
} from 'lucide-react';
import { useState } from 'react';

export default function SEOPage() {
  const { state: course, setMetaTitle, setMetaDescription, addSEOKeyword, removeSEOKeyword, setOGData, setTwitterData, setCanonicalUrl } = useCourseEditor();
  const [newKeyword, setNewKeyword] = useState('');
  const [previewMode, setPreviewMode] = useState<'google' | 'twitter' | 'facebook'>('google');

  const seo = course.metadata.seo;

  const handleAddKeyword = () => {
    if (newKeyword.trim() && !seo.keywords.includes(newKeyword.trim())) {
      addSEOKeyword(newKeyword.trim());
      setNewKeyword('');
    }
  };

  const seoScore = calculateSEOScore();

  function calculateSEOScore(): number {
    let score = 0;
    const checks = [
      { condition: (seo.metaTitle ?? '').length > 0 && (seo.metaTitle ?? '').length <= 60, weight: 20 },
      { condition: (seo.metaDescription ?? '').length > 0 && (seo.metaDescription ?? '').length <= 160, weight: 20 },
      { condition: seo.keywords.length >= 3, weight: 15 },
      { condition: (seo.ogTitle ?? '').length > 0, weight: 10 },
      { condition: (seo.ogDescription ?? '').length > 0, weight: 10 },
      { condition: (seo.ogImage ?? '').length > 0, weight: 10 },
      { condition: (seo.canonicalUrl ?? '').length > 0, weight: 10 },
      { condition: (seo.twitterTitle ?? '').length > 0, weight: 5 },
    ];
    
    checks.forEach(check => {
      if (check.condition) score += check.weight;
    });
    
    return score;
  }

  const renderPreview = () => {
    const title = seo.metaTitle || course.title
    const description = seo.metaDescription || course.description
    const url = seo.canonicalUrl || `https://example.com/courses/${course.title.toLowerCase().replace(/\s+/g, '-')}`

    switch (previewMode) {
      case 'google':
        return (
          <div className="border rounded-lg p-4 bg-white">
            <div className="text-sm text-gray-600 mb-1">{url}</div>
            <div className="text-lg text-blue-600 hover:underline cursor-pointer font-medium">
              {title}
            </div>
            <div className="text-sm text-gray-700 mt-1">
              {description}
            </div>
          </div>
        )
      
      case 'twitter':
        const twitterTitle = seo.twitterTitle || seo.ogTitle || title
        const twitterDescription = seo.twitterDescription || seo.ogDescription || description
        const twitterImage = seo.twitterImage || seo.ogImage
        
        return (
          <div className="border rounded-xl overflow-hidden bg-white max-w-md">
            {twitterImage && (
              <div className="aspect-video bg-gray-100 flex items-center justify-center">
                <img src={twitterImage} alt="Twitter preview" className="w-full h-full object-cover" />
              </div>
            )}
            <div className="p-3">
              <div className="text-sm text-gray-600 mb-1">{new URL(url).hostname}</div>
              <div className="font-medium text-sm">{twitterTitle}</div>
              <div className="text-xs text-gray-600 mt-1">{twitterDescription}</div>
            </div>
          </div>
        )
      
      case 'facebook':
        const ogTitle = seo.ogTitle || title
        const ogDescription = seo.ogDescription || description
        const ogImage = seo.ogImage
        
        return (
          <div className="border rounded-lg overflow-hidden bg-white max-w-md">
            {ogImage && (
              <div className="aspect-video bg-gray-100 flex items-center justify-center">
                <img src={ogImage} alt="Facebook preview" className="w-full h-full object-cover" />
              </div>
            )}
            <div className="p-3">
              <div className="text-xs text-gray-500 uppercase tracking-wide mb-1">
                {new URL(url).hostname}
              </div>
              <div className="font-semibold text-sm mb-1">{ogTitle}</div>
              <div className="text-xs text-gray-600">{ogDescription}</div>
            </div>
          </div>
        )
      
      default:
        return null
    }
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold flex items-center gap-2">
            <Search className="h-6 w-6" />
            SEO & Metadata
          </h1>
          <p className="text-muted-foreground">
            Optimize your course for search engines and social media
          </p>
        </div>
        
        <div className="flex items-center gap-4">
          <div className="flex items-center gap-2">
            <div className={`flex items-center gap-1 ${seoScore >= 80 ? 'text-green-600' : seoScore >= 60 ? 'text-yellow-600' : 'text-red-600'}`}>
              {seoScore >= 80 ? <CheckCircle className="h-4 w-4" /> : <AlertCircle className="h-4 w-4" />}
              <span className="font-medium">SEO Score: {seoScore}%</span>
            </div>
          </div>
        </div>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        {/* Main SEO Settings */}
        <div className="lg:col-span-2 space-y-6">
          {/* Basic SEO */}
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center gap-2">
                <Globe className="h-5 w-5" />
                Basic SEO Settings
              </CardTitle>
              <CardDescription>
                Essential metadata for search engine optimization
              </CardDescription>
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="space-y-2">
                <Label htmlFor="metaTitle">Meta Title</Label>
                <Input
                  id="metaTitle"
                  value={seo.metaTitle ?? ''}
                  onChange={(e) => setMetaTitle(e.target.value)}
                  placeholder="Enter SEO title (recommended: 50-60 characters)"
                  maxLength={60}
                />
                <div className="flex justify-between text-xs text-muted-foreground">
                  <span>Appears in search results and browser tabs</span>
                  <span className={(seo.metaTitle ?? '').length > 60 ? 'text-red-500' : 'text-green-500'}>
                    {(seo.metaTitle ?? '').length}/60
                  </span>
                </div>
              </div>

              <div className="space-y-2">
                <Label htmlFor="metaDescription">Meta Description</Label>
                <Textarea
                  id="metaDescription"
                  value={seo.metaDescription ?? ''}
                  onChange={(e) => setMetaDescription(e.target.value)}
                  placeholder="Write a compelling description for search results (recommended: 150-160 characters)"
                  rows={3}
                  maxLength={160}
                />
                <div className="flex justify-between text-xs text-muted-foreground">
                  <span>Appears below the title in search results</span>
                  <span className={(seo.metaDescription ?? '').length > 160 ? 'text-red-500' : 'text-green-500'}>
                    {(seo.metaDescription ?? '').length}/160
                  </span>
                </div>
              </div>

              <div className="space-y-2">
                <Label htmlFor="canonicalUrl">Canonical URL</Label>
                <Input
                  id="canonicalUrl"
                  value={seo.canonicalUrl ?? ''}
                  onChange={(e) => setCanonicalUrl(e.target.value)}
                  placeholder="https://example.com/courses/course-name"
                />
                <div className="text-xs text-muted-foreground">
                  Helps prevent duplicate content issues
                </div>
              </div>
            </CardContent>
          </Card>

          {/* Keywords */}
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center gap-2">
                <Tag className="h-5 w-5" />
                SEO Keywords
              </CardTitle>
              <CardDescription>
                Target keywords for search optimization
              </CardDescription>
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="flex gap-2">
                <Input
                  value={newKeyword}
                  onChange={(e) => setNewKeyword(e.target.value)}
                  placeholder="Add a keyword"
                  onKeyPress={(e) => e.key === 'Enter' && handleAddKeyword()}
                />
                <Button onClick={handleAddKeyword} size="sm">
                  <Plus className="h-4 w-4" />
                </Button>
              </div>
              
              <div className="flex flex-wrap gap-2">
                {seo.keywords.map((keyword: string, index: number) => (
                  <Badge key={index} variant="secondary" className="flex items-center gap-1">
                    {keyword}
                    <button
                      onClick={() => removeSEOKeyword(keyword)}
                      className="ml-1 hover:text-red-500"
                    >
                      <X className="h-3 w-3" />
                    </button>
                  </Badge>
                ))}
              </div>
              
              {seo.keywords.length === 0 && (
                <div className="text-sm text-muted-foreground">
                  Add 3-5 relevant keywords that describe your course content
                </div>
              )}
            </CardContent>
          </Card>

          {/* Social Media */}
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center gap-2">
                <Facebook className="h-5 w-5" />
                Social Media (Open Graph)
              </CardTitle>
              <CardDescription>
                Optimize how your course appears when shared on social media
              </CardDescription>
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                <div className="space-y-2">
                  <Label htmlFor="ogTitle">Open Graph Title</Label>
                  <Input
                    id="ogTitle"
                    value={seo.ogTitle ?? ''}
                    onChange={(e) => setOGData('ogTitle', e.target.value)}
                    placeholder="Social media title"
                  />
                </div>

                <div className="space-y-2">
                  <Label htmlFor="ogImage">Open Graph Image URL</Label>
                  <Input
                    id="ogImage"
                    value={seo.ogImage ?? ''}
                    onChange={(e) => setOGData('ogImage', e.target.value)}
                    placeholder="https://example.com/image.jpg"
                  />
                </div>
              </div>

              <div className="space-y-2">
                <Label htmlFor="ogDescription">Open Graph Description</Label>
                <Textarea
                  id="ogDescription"
                  value={seo.ogDescription ?? ''}
                  onChange={(e) => setOGData('ogDescription', e.target.value)}
                  placeholder="Description for social media shares"
                  rows={2}
                />
              </div>
            </CardContent>
          </Card>

          {/* Twitter */}
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center gap-2">
                <Twitter className="h-5 w-5" />
                Twitter Settings
              </CardTitle>
              <CardDescription>
                Customize appearance on Twitter/X
              </CardDescription>
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                <div className="space-y-2">
                  <Label htmlFor="twitterTitle">Twitter Title</Label>
                  <Input
                    id="twitterTitle"
                    value={seo.twitterTitle ?? ''}
                    onChange={(e) => setTwitterData('twitterTitle', e.target.value)}
                    placeholder="Twitter-specific title"
                  />
                </div>

                <div className="space-y-2">
                  <Label htmlFor="twitterImage">Twitter Image URL</Label>
                  <Input
                    id="twitterImage"
                    value={seo.twitterImage ?? ''}
                    onChange={(e) => setTwitterData('twitterImage', e.target.value)}
                    placeholder="https://example.com/twitter-image.jpg"
                  />
                </div>
              </div>

              <div className="space-y-2">
                <Label htmlFor="twitterDescription">Twitter Description</Label>
                <Textarea
                  id="twitterDescription"
                  value={seo.twitterDescription ?? ''}
                  onChange={(e) => setTwitterData('twitterDescription', e.target.value)}
                  placeholder="Twitter-specific description"
                  rows={2}
                />
              </div>
            </CardContent>
          </Card>
        </div>

        {/* Preview Panel */}
        <div className="space-y-6">
          <Card className="sticky top-6">
            <CardHeader>
              <CardTitle className="flex items-center gap-2">
                <Eye className="h-5 w-5" />
                Preview
              </CardTitle>
              <CardDescription>
                See how your course will appear
              </CardDescription>
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="flex gap-1 p-1 bg-muted rounded-md">
                <Button
                  variant={previewMode === 'google' ? 'default' : 'ghost'}
                  size="sm"
                  onClick={() => setPreviewMode('google')}
                  className="flex-1"
                >
                  Google
                </Button>
                <Button
                  variant={previewMode === 'twitter' ? 'default' : 'ghost'}
                  size="sm"
                  onClick={() => setPreviewMode('twitter')}
                  className="flex-1"
                >
                  Twitter
                </Button>
                <Button
                  variant={previewMode === 'facebook' ? 'default' : 'ghost'}
                  size="sm"
                  onClick={() => setPreviewMode('facebook')}
                  className="flex-1"
                >
                  Facebook
                </Button>
              </div>

              <div className="min-h-[200px]">
                {renderPreview()}
              </div>

              <Separator />

              {/* SEO Checklist */}
              <div className="space-y-2">
                <h4 className="font-medium">SEO Checklist</h4>
                <div className="space-y-1 text-sm">
                  <div className={`flex items-center gap-2 ${(seo.metaTitle ?? '').length > 0 && (seo.metaTitle ?? '').length <= 60 ? 'text-green-600' : 'text-red-600'}`}>
                    {(seo.metaTitle ?? '').length > 0 && (seo.metaTitle ?? '').length <= 60 ? <CheckCircle className="h-3 w-3" /> : <AlertCircle className="h-3 w-3" />}
                    Meta title (50-60 chars)
                  </div>
                  <div className={`flex items-center gap-2 ${(seo.metaDescription ?? '').length > 0 && (seo.metaDescription ?? '').length <= 160 ? 'text-green-600' : 'text-red-600'}`}>
                    {(seo.metaDescription ?? '').length > 0 && (seo.metaDescription ?? '').length <= 160 ? <CheckCircle className="h-3 w-3" /> : <AlertCircle className="h-3 w-3" />}
                    Meta description (150-160 chars)
                  </div>
                  <div className={`flex items-center gap-2 ${seo.keywords.length >= 3 ? 'text-green-600' : 'text-red-600'}`}>
                    {seo.keywords.length >= 3 ? <CheckCircle className="h-3 w-3" /> : <AlertCircle className="h-3 w-3" />}
                    SEO keywords (3+ recommended)
                  </div>
                  <div className={`flex items-center gap-2 ${(seo.ogTitle ?? '').length > 0 ? 'text-green-600' : 'text-red-600'}`}>
                    {(seo.ogTitle ?? '').length > 0 ? <CheckCircle className="h-3 w-3" /> : <AlertCircle className="h-3 w-3" />}
                    Open Graph title
                  </div>
                  <div className={`flex items-center gap-2 ${(seo.ogImage ?? '').length > 0 ? 'text-green-600' : 'text-red-600'}`}>
                    {(seo.ogImage ?? '').length > 0 ? <CheckCircle className="h-3 w-3" /> : <AlertCircle className="h-3 w-3" />}
                    Open Graph image
                  </div>
                </div>
              </div>

              <Button variant="outline" className="w-full" asChild>
                <a href="https://developers.google.com/search/docs/fundamentals/seo-starter-guide" target="_blank" rel="noopener noreferrer">
                  <ExternalLink className="h-4 w-4 mr-2" />
                  SEO Best Practices
                </a>
              </Button>
            </CardContent>
          </Card>
        </div>
      </div>
    </div>
  )
}
