// Mirrors SocialMediaAssetPolicy in the API's GameGuild.Social.Assets module.
// social-media-policy-contract.test.ts guards the limits and accepted MIME types.
export const SOCIAL_IMAGE_MAX_BYTES = 10 * 1024 * 1024;
export const SOCIAL_VIDEO_MAX_BYTES = 100 * 1024 * 1024;

export const SOCIAL_MEDIA_FORMATS = [
  { mimeType: "image/jpeg", label: "JPEG", kind: "image", maxBytes: SOCIAL_IMAGE_MAX_BYTES },
  { mimeType: "image/png", label: "PNG", kind: "image", maxBytes: SOCIAL_IMAGE_MAX_BYTES },
  { mimeType: "image/webp", label: "WebP", kind: "image", maxBytes: SOCIAL_IMAGE_MAX_BYTES },
  { mimeType: "image/gif", label: "GIF", kind: "image", maxBytes: SOCIAL_IMAGE_MAX_BYTES },
  { mimeType: "video/mp4", label: "MP4", kind: "video", maxBytes: SOCIAL_VIDEO_MAX_BYTES },
] as const;

export const SOCIAL_MEDIA_ACCEPT = SOCIAL_MEDIA_FORMATS.map((format) => format.mimeType).join(",");
export const SOCIAL_IMAGE_FORMAT_LABELS = SOCIAL_MEDIA_FORMATS.filter((format) => format.kind === "image").map((format) => format.label).join(", ");
export const SOCIAL_VIDEO_FORMAT_LABELS = SOCIAL_MEDIA_FORMATS.filter((format) => format.kind === "video").map((format) => format.label).join(", ");
export const SOCIAL_IMAGE_SIZE_LABEL = SOCIAL_IMAGE_MAX_BYTES / (1024 * 1024) + " MB";
export const SOCIAL_VIDEO_SIZE_LABEL = SOCIAL_VIDEO_MAX_BYTES / (1024 * 1024) + " MB";
export const SOCIAL_IMAGE_SIZE_ERROR = "Images can be up to " + SOCIAL_IMAGE_SIZE_LABEL + ".";
export const SOCIAL_VIDEO_SIZE_ERROR = "MP4 videos can be up to " + SOCIAL_VIDEO_SIZE_LABEL + ".";
const formatNames = SOCIAL_MEDIA_FORMATS.map((format) => format.label);
export const SOCIAL_MEDIA_FORMAT_ERROR = "Choose a " + formatNames.slice(0, -1).join(", ") + ", or " + (formatNames.at(-1) ?? "media") + " file.";

export function validateSelectedSocialMedia(file: File): string | null {
  const format = SOCIAL_MEDIA_FORMATS.find((candidate) => candidate.mimeType === file.type);
  if (!format) return SOCIAL_MEDIA_FORMAT_ERROR;
  if (file.size <= 0) return "Choose a non-empty media file.";
  if (file.size > format.maxBytes) return format.kind === "video" ? SOCIAL_VIDEO_SIZE_ERROR : SOCIAL_IMAGE_SIZE_ERROR;
  return null;
}
