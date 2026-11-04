"use client";

import { Button } from "@game-guild/ui/components/button";
import { Upload } from "lucide-react";
import * as React from "react";
import { SocialMediaPreview } from "./social-media-preview";
import {
  SOCIAL_IMAGE_FORMAT_LABELS,
  SOCIAL_IMAGE_SIZE_LABEL,
  SOCIAL_MEDIA_ACCEPT,
  SOCIAL_VIDEO_FORMAT_LABELS,
  SOCIAL_VIDEO_SIZE_LABEL,
  validateSelectedSocialMedia,
} from "@/lib/feed/social-media-policy";

export { SOCIAL_IMAGE_MAX_BYTES, SOCIAL_MEDIA_ACCEPT, SOCIAL_VIDEO_MAX_BYTES, validateSelectedSocialMedia } from "@/lib/feed/social-media-policy";

export interface SelectedSocialMedia {
  file: File;
  previewUrl: string;
  kind: "image" | "video";
}

export function SocialMediaPicker({
  value,
  onChange,
  disabled = false,
}: {
  value: SelectedSocialMedia | null;
  onChange: (value: SelectedSocialMedia | null) => void;
  disabled?: boolean;
}): React.JSX.Element {
  const descriptionId = React.useId();
  const [error, setError] = React.useState<string | null>(null);
  const inputRef = React.useRef<HTMLInputElement>(null);

  React.useEffect(() => () => {
    if (value) URL.revokeObjectURL(value.previewUrl);
  }, [value]);

  function select(file: File | null) {
    if (!file) return;
    const nextError = validateSelectedSocialMedia(file);
    setError(nextError);
    if (nextError) return;
    onChange({ file, previewUrl: URL.createObjectURL(file), kind: file.type === "video/mp4" ? "video" : "image" });
  }

  return (
    <div>
      {value ? <SocialMediaPreview media={value} disabled={disabled} onRemove={() => onChange(null)} /> : null}
      {error ? <p role="alert" className="mt-2 text-xs text-destructive">{error}</p> : null}
      <input ref={inputRef} type="file" accept={SOCIAL_MEDIA_ACCEPT} className="sr-only" aria-label="Add photo or video" aria-describedby={descriptionId} onChange={(event) => select(event.target.files?.[0] ?? null)} />
      <p id={descriptionId} className="mt-2 text-xs text-muted-foreground">
        {SOCIAL_IMAGE_FORMAT_LABELS} images up to {SOCIAL_IMAGE_SIZE_LABEL}.{" "}
        {SOCIAL_VIDEO_FORMAT_LABELS} videos up to {SOCIAL_VIDEO_SIZE_LABEL}.
      </p>
      <Button type="button" variant="ghost" size="sm" disabled={disabled} onClick={() => inputRef.current?.click()}>
        <Upload className="size-4" /> Photo or video
      </Button>
    </div>
  );
}
