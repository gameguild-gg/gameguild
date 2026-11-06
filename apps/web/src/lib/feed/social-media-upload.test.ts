import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

import { uploadSocialMediaWithProgress } from "./social-media-upload";

class FakeUpload {
  onprogress: ((event: ProgressEvent<EventTarget>) => void) | null = null;
}

class FakeXhr {
  static instance: FakeXhr | null = null;
  static completeImmediately = true;
  upload = new FakeUpload();
  status = 201;
  response: unknown = null;
  responseText = JSON.stringify({ assetReferenceId: "asset-1", state: "Ready", sizeBytes: 5 });
  onload: (() => void) | null = null;
  onerror: (() => void) | null = null;
  onabort: (() => void) | null = null;
  open = vi.fn();
  send = vi.fn(() => {
    if (!FakeXhr.completeImmediately) return;
    this.upload.onprogress?.({ lengthComputable: true, loaded: 5, total: 10 } as ProgressEvent<EventTarget>);
    this.onload?.();
  });
  abort = vi.fn(() => this.onabort?.());
  constructor() { FakeXhr.instance = this; }
}

describe("uploadSocialMediaWithProgress", () => {
  beforeEach(() => {
    FakeXhr.instance = null;
    FakeXhr.completeImmediately = true;
    vi.stubGlobal("XMLHttpRequest", FakeXhr);
  });
  afterEach(() => {
    FakeXhr.completeImmediately = true;
    vi.unstubAllGlobals();
  });

  function failWith(status: number, body: unknown, responseText = "") {
    FakeXhr.completeImmediately = false;
    const pending = uploadSocialMediaWithProgress(new File(["png"], "build.png", { type: "image/png" }));
    const xhr = FakeXhr.instance;
    if (!xhr) throw new Error("The upload transport must be created.");
    xhr.status = status;
    xhr.response = body;
    xhr.responseText = responseText;
    xhr.onload?.();
    return pending;
  }

  it("uses the same-origin upload route and reports uploaded bytes", async () => {
    vi.stubGlobal("XMLHttpRequest", FakeXhr);
    const onProgress = vi.fn();
    const file = new File(["hello"], "build.png", { type: "image/png" });

    await expect(uploadSocialMediaWithProgress(file, { onProgress })).resolves.toMatchObject({ assetReferenceId: "asset-1", state: "Ready" });
    expect(FakeXhr.instance?.open).toHaveBeenCalledWith("POST", "/api/social/media");
    expect(onProgress).toHaveBeenCalledWith({ loaded: 5, total: 10, percent: 50 });
    vi.unstubAllGlobals();
  });

  it("aborts the browser transport when cancelled", async () => {
    vi.stubGlobal("XMLHttpRequest", FakeXhr);
    FakeXhr.completeImmediately = false;
    const controller = new AbortController();
    const upload = uploadSocialMediaWithProgress(new File(["hello"], "build.png", { type: "image/png" }), { signal: controller.signal });
    controller.abort();
    await expect(upload).rejects.toThrow(/cancelled/i);
    expect(FakeXhr.instance?.abort).toHaveBeenCalledTimes(1);
    FakeXhr.completeImmediately = true;
    vi.unstubAllGlobals();
  });

  it.each([
    [401, "Sign in again to upload media."],
    [403, "You do not have permission to upload media here."],
    [413, "The file is too large. Images can be up to 10 MB and MP4 videos up to 100 MB."],
    [415, "Choose a JPEG, PNG, WebP, GIF, or MP4 file."],
    [429, "Too many uploads. Wait a moment and try again."],
    [500, "Media uploads are temporarily unavailable. Try again shortly."],
    [502, "Media uploads are temporarily unavailable. Try again shortly."],
    [503, "Media uploads are temporarily unavailable. Try again shortly."],
  ])("gives a safe, actionable message for HTTP %s", async (status, message) => {
    await expect(failWith(status, { detail: "private-provider-password", error: "<script>unsafe()</script>" }))
      .rejects.toMatchObject({ name: "SocialMediaUploadError", message });
  });

  it.each([
    ["Only JPEG, PNG, WebP, GIF, and MP4 files are supported.", "Choose a JPEG, PNG, WebP, GIF, or MP4 file."],
    ["The file exceeds the 10485760 byte limit for image/png.", "Images can be up to 10 MB."],
    ["The file exceeds the 104857600 byte limit for video/mp4.", "MP4 videos can be up to 100 MB."],
    ["The file content does not match its declared media type.", "The file contents do not match its media type. Export it again and try another file."],
    ["The uploaded file cannot be inspected safely.", "This file could not be verified. Export it again and try another file."],
  ])("explains the API validation failure %s", async (detail, message) => {
    await expect(failWith(400, { title: "Invalid social media upload", detail }))
      .rejects.toMatchObject({ message });
  });

  it("explains the API's empty-file response", async () => {
    await expect(failWith(400, { title: "A media file is required." }))
      .rejects.toMatchObject({ message: "Choose a non-empty media file." });
  });

  it.each([
    { title: "Invalid social media upload", detail: "<img src=x onerror=steal()> private-token" },
    { title: "Invalid social media upload", detail: "The file exceeds the 1 byte limit for image/png." },
    { title: "Invalid social media upload", detail: "Only JPEG, PNG, WebP, GIF, and MP4 files are supported.\nprivate-token" },
    { error: "Database password=private-token; SQL failed" },
    { detail: { message: "private-token" } },
    ["private-token"],
    null,
  ])("uses a generic fallback for unknown or unsafe error content", async (body) => {
    await expect(failWith(400, body))
      .rejects.toMatchObject({ message: "Media upload failed. Try again." });
  });

  it("handles an HTML service error without exposing its response body", async () => {
    await expect(failWith(502, null, "<html>private-provider-token</html>"))
      .rejects.toMatchObject({ message: "Media uploads are temporarily unavailable. Try again shortly." });
  });

  it("distinguishes browser network failure from server failure", async () => {
    FakeXhr.completeImmediately = false;
    const pending = uploadSocialMediaWithProgress(new File(["png"], "build.png", { type: "image/png" }));
    FakeXhr.instance?.onerror?.();
    await expect(pending).rejects.toMatchObject({ message: "Check your connection and try the upload again." });
  });

  it("cancels an already aborted request without sending it", async () => {
    const controller = new AbortController();
    controller.abort();
    const pending = uploadSocialMediaWithProgress(new File(["png"], "build.png", { type: "image/png" }), { signal: controller.signal });
    await expect(pending).rejects.toThrow(/cancelled/i);
    expect(FakeXhr.instance?.send).not.toHaveBeenCalled();
  });
});
