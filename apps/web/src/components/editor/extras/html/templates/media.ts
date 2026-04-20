import { Image, PlayCircle, Music, GalleryHorizontalEnd } from "lucide-react"
import type { HTMLTemplate } from "./types"

export const mediaTemplates: HTMLTemplate[] = [
  {
    id: "image-figure",
    title: "Image Figure",
    description: "Image with caption",
    category: "media",
    icon: Image,
    code: `<figure style="max-width:600px;margin:0 auto;font-family:system-ui,sans-serif">
  <div style="aspect-ratio:16/9;background:#f3f4f6;border-radius:8px;overflow:hidden;display:flex;align-items:center;justify-content:center;color:#9ca3af;border:1px solid #e5e7eb">
    <span style="font-size:0.875rem">Replace with &lt;img src="..."&gt;</span>
  </div>
  <figcaption style="margin-top:0.75rem;text-align:center;color:#6b7280;font-size:0.875rem;font-style:italic">
    Figure 1: Description of the image
  </figcaption>
</figure>`,
  },
  {
    id: "image-gallery",
    title: "Image Gallery",
    description: "Grid of images",
    category: "media",
    icon: GalleryHorizontalEnd,
    code: `<div style="display:grid;grid-template-columns:repeat(3,1fr);gap:1rem;max-width:800px;margin:0 auto;font-family:system-ui,sans-serif">
  <div style="aspect-ratio:1;background:#fef3c7;border-radius:8px;display:flex;align-items:center;justify-content:center;color:#92400e;font-size:0.875rem;border:1px solid #fde68a">Image 1</div>
  <div style="aspect-ratio:1;background:#dbeafe;border-radius:8px;display:flex;align-items:center;justify-content:center;color:#1d4ed8;font-size:0.875rem;border:1px solid #bfdbfe">Image 2</div>
  <div style="aspect-ratio:1;background:#dcfce7;border-radius:8px;display:flex;align-items:center;justify-content:center;color:#166534;font-size:0.875rem;border:1px solid #bbf7d0">Image 3</div>
  <div style="aspect-ratio:1;background:#fce7f3;border-radius:8px;display:flex;align-items:center;justify-content:center;color:#9d174d;font-size:0.875rem;border:1px solid #fbcfe8">Image 4</div>
  <div style="aspect-ratio:1;background:#ede9fe;border-radius:8px;display:flex;align-items:center;justify-content:center;color:#5b21b6;font-size:0.875rem;border:1px solid #ddd6fe">Image 5</div>
  <div style="aspect-ratio:1;background:#f3f4f6;border-radius:8px;display:flex;align-items:center;justify-content:center;color:#6b7280;font-size:0.875rem;border:1px solid #e5e7eb">Image 6</div>
</div>`,
  },
  {
    id: "video-embed",
    title: "Video Embed",
    description: "Responsive video container",
    category: "media",
    icon: PlayCircle,
    code: `<div style="max-width:720px;margin:0 auto;font-family:system-ui,sans-serif">
  <div style="position:relative;padding-bottom:56.25%;height:0;overflow:hidden;border-radius:8px;background:#000">
    <iframe
      src="https://www.youtube-nocookie.com/embed/dQw4w9WgXcQ"
      style="position:absolute;top:0;left:0;width:100%;height:100%;border:0"
      allow="accelerometer;autoplay;clipboard-write;encrypted-media;gyroscope;picture-in-picture"
      allowfullscreen
    ></iframe>
  </div>
  <p style="text-align:center;color:#6b7280;margin-top:0.75rem;font-size:0.875rem">Video: Description here</p>
</div>`,
  },
  {
    id: "audio-player",
    title: "Audio Player",
    description: "Styled audio element",
    category: "media",
    icon: Music,
    code: `<div style="max-width:500px;padding:1.5rem;background:#f8fafc;border-radius:12px;border:1px solid #e2e8f0;font-family:system-ui,sans-serif">
  <div style="display:flex;align-items:center;gap:1rem;margin-bottom:1rem">
    <div style="width:56px;height:56px;border-radius:8px;background:linear-gradient(135deg,#ec4899,#8b5cf6);display:flex;align-items:center;justify-content:center;color:#fff;font-size:1.5rem">♪</div>
    <div>
      <div style="font-weight:600;color:#1e293b">Track Title</div>
      <div style="font-size:0.875rem;color:#64748b">Artist Name</div>
    </div>
  </div>
  <audio controls style="width:100%;height:40px" src="">
    Your browser does not support the audio element.
  </audio>
</div>`,
  },
  {
    id: "image-comparison",
    title: "Before / After",
    description: "Side-by-side comparison",
    category: "media",
    icon: Image,
    code: `<div style="display:flex;gap:1rem;max-width:700px;margin:0 auto;font-family:system-ui,sans-serif">
  <div style="flex:1;text-align:center">
    <div style="aspect-ratio:4/3;background:#fee2e2;border-radius:8px;display:flex;align-items:center;justify-content:center;color:#991b1b;font-weight:600;border:2px solid #fecaca">Before</div>
    <p style="color:#6b7280;font-size:0.875rem;margin-top:0.5rem">Original state</p>
  </div>
  <div style="flex:1;text-align:center">
    <div style="aspect-ratio:4/3;background:#dcfce7;border-radius:8px;display:flex;align-items:center;justify-content:center;color:#166534;font-weight:600;border:2px solid #bbf7d0">After</div>
    <p style="color:#6b7280;font-size:0.875rem;margin-top:0.5rem">Improved state</p>
  </div>
</div>`,
  },
]
