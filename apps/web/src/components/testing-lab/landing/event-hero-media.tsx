"use client";

import { useEffect, useState } from "react";
import { EventCoverArt } from "./event-cover-art";

/**
 * Landing-page hero media: a slow crossfading carousel of the submitted
 * games' cover images. Falls back to the seeded event artwork while no
 * game images are available.
 */
export function EventHeroMedia({
  seed,
  images,
  className = "",
}: {
  seed: string;
  images: string[];
  className?: string;
}) {
  const [index, setIndex] = useState(0);

  useEffect(() => {
    if (images.length < 2) return;
    const timer = setInterval(
      () => setIndex((current) => (current + 1) % images.length),
      5000,
    );
    return () => clearInterval(timer);
  }, [images.length]);

  if (images.length === 0) {
    return (
      <div aria-hidden="true" className={`absolute inset-0 ${className}`}>
        <EventCoverArt seed={seed} />
      </div>
    );
  }

  return (
    <div aria-hidden="true" className={`absolute inset-0 ${className}`}>
      {images.map((src, imageIndex) => (
        // eslint-disable-next-line @next/next/no-img-element
        <img
          key={src}
          src={src}
          alt=""
          className={`absolute inset-0 h-full w-full object-cover transition-opacity duration-1000 ${
            imageIndex === index ? "opacity-100" : "opacity-0"
          }`}
        />
      ))}
      {/* Keep the event identity on top of photos and unify the text scrim. */}
      <div className="absolute inset-0 bg-gradient-to-t from-black/85 via-black/40 to-black/25" />
    </div>
  );
}
