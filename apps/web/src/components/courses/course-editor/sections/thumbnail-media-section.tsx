'use client';

import React, { useRef } from 'react';
import { Label } from '@/components/ui/label';
import { Input } from '@/components/ui/input';
import { Button } from '@/components/ui/button';
import { Upload, X, Play, Pause } from 'lucide-react';
import { useCourseEditor } from '@/lib/courses/course-editor.context';

export function ThumbnailMediaSection() {
  const { state, setThumbnail, setShowcaseVideo } = useCourseEditor();
  const thumbnailInputRef = useRef<HTMLInputElement>(null);
  const videoInputRef = useRef<HTMLInputElement>(null);

  const handleThumbnailUpload = (event: React.ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0];
    if (file) {
      // TODO: Implement actual file upload
      const url = URL.createObjectURL(file);
      setThumbnail({
        url,
        alt: `Thumbnail for ${state.title}`,
        file
      });
    }
  };

  const handleVideoUpload = (event: React.ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0];
    if (file) {
      // TODO: Implement actual file upload
      const url = URL.createObjectURL(file);
      setShowcaseVideo({
        url,
        platform: 'direct' as const,
        embedId: undefined
      });
    }
  };

  const removeThumbnail = () => {
    setThumbnail(undefined);
    if (thumbnailInputRef.current) {
      thumbnailInputRef.current.value = '';
    }
  };

  const removeVideo = () => {
    setShowcaseVideo(undefined);
    if (videoInputRef.current) {
      videoInputRef.current.value = '';
    }
  };

  return (
    <div className="space-y-6">
      {/* Thumbnail Section */}
      <div className="space-y-4">
        <Label className="text-sm font-medium">Course Thumbnail</Label>
        
        {state.media.thumbnail ? (
          <div className="relative">
            <div className="aspect-video bg-muted rounded-lg overflow-hidden border border-border">
              <img 
                src={state.media.thumbnail.url} 
                alt={state.media.thumbnail.alt}
                className="w-full h-full object-cover"
              />
            </div>
            <Button
              variant="destructive"
              size="sm"
              className="absolute top-2 right-2"
              onClick={removeThumbnail}
            >
              <X className="h-4 w-4" />
            </Button>
            <div className="mt-2 text-xs text-muted-foreground">
              {state.media.thumbnail.file?.name} • {Math.round((state.media.thumbnail.file?.size || 0) / 1024)} KB
            </div>
          </div>
        ) : (
          <div className="aspect-video bg-muted rounded-lg border-2 border-dashed border-border hover:border-primary/50 transition-colors">
            <div className="flex flex-col items-center justify-center h-full gap-3">
              <Upload className="h-8 w-8 text-muted-foreground" />
              <div className="text-center">
                <p className="text-sm font-medium">Upload Thumbnail</p>
                <p className="text-xs text-muted-foreground">Recommended: 1280x720px, JPG or PNG</p>
              </div>
              <Button 
                variant="outline" 
                onClick={() => thumbnailInputRef.current?.click()}
              >
                Choose File
              </Button>
            </div>
          </div>
        )}
        
        <input
          ref={thumbnailInputRef}
          type="file"
          accept="image/*"
          onChange={handleThumbnailUpload}
          className="hidden"
        />
        
        {state.errors.thumbnail && (
          <p className="text-sm text-red-600">{state.errors.thumbnail}</p>
        )}
      </div>

      {/* Video Section */}
      <div className="space-y-4">
        <Label className="text-sm font-medium">Showcase Video (Optional)</Label>
        
        {state.media.showcaseVideo ? (
          <div className="relative">
            <div className="aspect-video bg-muted rounded-lg overflow-hidden border border-border">
              <video 
                src={state.media.showcaseVideo.url}
                className="w-full h-full object-cover"
                controls
                preload="metadata"
              >
                Your browser does not support the video tag.
              </video>
            </div>
            <Button
              variant="destructive"
              size="sm"
              className="absolute top-2 right-2"
              onClick={removeVideo}
            >
              <X className="h-4 w-4" />
            </Button>
            <div className="mt-2 text-xs text-muted-foreground">
              Video uploaded • {state.media.showcaseVideo.platform} format
            </div>
          </div>
        ) : (
          <div className="aspect-video bg-muted rounded-lg border-2 border-dashed border-border hover:border-primary/50 transition-colors">
            <div className="flex flex-col items-center justify-center h-full gap-3">
              <Play className="h-8 w-8 text-muted-foreground" />
              <div className="text-center">
                <p className="text-sm font-medium">Upload Showcase Video</p>
                <p className="text-xs text-muted-foreground">Recommended: MP4, max 100MB</p>
              </div>
              <Button 
                variant="outline" 
                onClick={() => videoInputRef.current?.click()}
              >
                Choose Video
              </Button>
            </div>
          </div>
        )}
        
        <input
          ref={videoInputRef}
          type="file"
          accept="video/*"
          onChange={handleVideoUpload}
          className="hidden"
        />
        
        {state.errors.video && (
          <p className="text-sm text-red-600">{state.errors.video}</p>
        )}
      </div>

      {/* Media Guidelines */}
      <div className="p-4 bg-muted/50 rounded-lg space-y-2">
        <h4 className="font-medium text-sm">Media Guidelines</h4>
        <ul className="text-xs text-muted-foreground space-y-1">
          <li>• Thumbnail: Use high-quality images that represent your course content</li>
          <li>• Video: Keep showcase videos under 2 minutes for best engagement</li>
          <li>• Both: Ensure content is appropriate and follows platform guidelines</li>
          <li>• Formats: JPG/PNG for images, MP4 for videos</li>
        </ul>
      </div>
    </div>
  );
}
