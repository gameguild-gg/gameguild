"use client";

import { useState } from "react";
import { Input } from "@game-guild/ui/components/input";
import { Label } from "@game-guild/ui/components/label";

interface LessonExternalLinkEditorProps {
  initialValue: string;
  onChange: (url: string) => void;
}

export function LessonExternalLinkEditor({
  initialValue,
  onChange,
}: LessonExternalLinkEditorProps) {
  const [url, setUrl] = useState(initialValue);

  return (
    <div className="space-y-2">
      <Label htmlFor="external-link-url">External link URL</Label>
      <Input
        id="external-link-url"
        type="url"
        value={url}
        onChange={(e) => {
          setUrl(e.target.value);
          onChange(e.target.value);
        }}
        placeholder="https://example.com/article"
      />
      <p className="text-muted-foreground text-xs">
        Learners see a button that opens this link in a new tab.
      </p>
    </div>
  );
}
