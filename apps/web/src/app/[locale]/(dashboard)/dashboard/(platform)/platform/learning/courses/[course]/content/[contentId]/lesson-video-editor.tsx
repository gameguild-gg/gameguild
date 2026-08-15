"use client";

import { useState } from "react";
import { Input } from "@game-guild/ui/components/input";
import { Label } from "@game-guild/ui/components/label";

interface LessonVideoEditorProps {
  initialValue: string;
  onChange: (url: string) => void;
}

export function LessonVideoEditor({
  initialValue,
  onChange,
}: LessonVideoEditorProps) {
  const [url, setUrl] = useState<string>(initialValue);

  return (
    <div className="space-y-2">
      <Label htmlFor="video-url">Video URL</Label>
      <Input
        id="video-url"
        value={url}
        onChange={(e) => {
          setUrl(e.target.value);
          onChange(e.target.value);
        }}
        placeholder="https://www.youtube.com/watch?v=..."
      />
      <p className="text-muted-foreground text-xs">
        Paste a YouTube, Vimeo, or direct video link. Learners see an embedded
        player.
      </p>
    </div>
  );
}
