import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";
import { describe, expect, it } from "vitest";

import {
  SOCIAL_IMAGE_MAX_BYTES,
  SOCIAL_MEDIA_ACCEPT,
  SOCIAL_VIDEO_MAX_BYTES,
} from "@/components/feed/social-media-picker";

// This cross-boundary check prevents upload guidance from drifting from the
// server's enforced policy. Source syntax changes fail closed for review.
const policy = readFileSync(resolve(dirname(fileURLToPath(import.meta.url)), "../../../../api/Source/Modules/GameGuild.Social.Assets/SocialMedia/SocialMediaAssetService.cs"), "utf8");

function declaredLimit(name: string): number {
  const declaration = policy.match(new RegExp("public const long " + name + " = ([0-9 *]+);"));
  if (!declaration) throw new Error("The API policy limit must be reviewed: " + name);
  return declaration[1].split("*").map((factor) => Number(factor.trim())).reduce((product, factor) => product * factor, 1);
}

describe("social media upload API contract", () => {
  it("keeps the browser limits equal to the enforced API limits", () => {
    expect(SOCIAL_IMAGE_MAX_BYTES).toBe(declaredLimit("ImageLimitBytes"));
    expect(SOCIAL_VIDEO_MAX_BYTES).toBe(declaredLimit("VideoLimitBytes"));
  });

  it("offers exactly the MIME types accepted by the API policy", () => {
    const mimeTypes = [...policy.matchAll(/\["([^"]+)"\] = (?:Image|Video)LimitBytes/g)].map((match) => match[1]).sort();
    expect(mimeTypes.length).toBeGreaterThan(0);
    expect(SOCIAL_MEDIA_ACCEPT.split(",").sort()).toEqual(mimeTypes);
  });
});
