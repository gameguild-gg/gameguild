"use client";

import { useCallback, useEffect, useMemo, useReducer, useRef, useState } from "react";
import { useRouter } from "next/navigation";
import { Badge } from "@game-guild/ui/components/badge";
import { Button } from "@game-guild/ui/components/button";
import { Input } from "@game-guild/ui/components/input";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@game-guild/ui/components/select";
import { Separator } from "@game-guild/ui/components/separator";
import { Textarea } from "@game-guild/ui/components/textarea";
import {
  Tooltip,
  TooltipContent,
  TooltipTrigger,
} from "@game-guild/ui/components/tooltip";
import { Loader2, PanelRightClose, PanelRightOpen, RotateCcw, Settings2 } from "lucide-react";
import { changeSlug, reloadLatestPost, updateDraft } from "@/lib/blogs/actions";
import {
  blogEditorReducer,
  buildAutosavePayload,
  createBlogEditorState,
  isPayloadEmpty,
  type BlogEditorDraft,
} from "@/lib/blogs/editor-state";
import type { BlogPostAuthorView } from "@/lib/blogs/queries";
import { BlogBodyEditor } from "./blog-body-editor";
import { BlogPublishBar } from "./blog-publish-bar";
import { BlogSettingsPanel, type BlogSettingsState } from "./blog-settings-panel";
import {
  BlogCoauthorManager,
  type BlogCoauthor,
} from "./blog-coauthor-manager";

const AUTOSAVE_DELAY_MS = 1500;
const TWITTER_CARD_DEFAULT = "summary_large_image";

export interface BlogEditorWorkspaceProps {
  post: BlogPostAuthorView;
  viewerUserId: string;
  primaryAuthorHandle: string;
  coauthors: BlogCoauthor[];
}

function draftFromPost(post: BlogPostAuthorView): BlogEditorDraft {
  return {
    title: post.title ?? "",
    content: post.content ?? "",
    jsonBody: post.jsonBody ?? null,
    excerpt: post.excerpt ?? "",
    tags: post.tags ?? [],
    metaTitle: post.metaTitle ?? "",
    metaDescription: post.metaDescription ?? "",
    ogImageUrl: post.ogImageUrl ?? "",
    canonicalUrlOverride: post.canonicalUrlOverride ?? "",
    twitterCard: post.twitterCard ?? TWITTER_CARD_DEFAULT,
    structuredDataOverride: post.structuredDataOverride ?? "",
    allowComments: post.allowComments ?? true,
    format: post.format ?? "Markdown",
  };
}

export function BlogEditorWorkspace({
  post: initialPost,
  viewerUserId,
  primaryAuthorHandle,
  coauthors,
}: BlogEditorWorkspaceProps) {
  const router = useRouter();
  const [post, setPost] = useState(initialPost);
  const [state, dispatch] = useReducer(
    blogEditorReducer,
    { draft: draftFromPost(initialPost), revision: initialPost.revision ?? 1 },
    ({ draft, revision }) => createBlogEditorState(draft, revision),
  );
  const baseline = useRef<BlogEditorDraft>(draftFromPost(initialPost));
  const [settings, setSettings] = useState<BlogSettingsState>({
    tags: initialPost.tags ?? [],
    excerpt: initialPost.excerpt ?? "",
    metaTitle: initialPost.metaTitle ?? "",
    metaDescription: initialPost.metaDescription ?? "",
    ogImageUrl: initialPost.ogImageUrl ?? "",
    canonicalUrlOverride: initialPost.canonicalUrlOverride ?? "",
    twitterCard: initialPost.twitterCard ?? TWITTER_CARD_DEFAULT,
    structuredDataOverride: initialPost.structuredDataOverride ?? "",
    allowComments: initialPost.allowComments ?? true,
  });
  const [slugEditing, setSlugEditing] = useState(false);
  const [slugValue, setSlugValue] = useState(initialPost.slug ?? "");
  const [slugError, setSlugError] = useState<string | null>(null);
  const [rightOpen, setRightOpen] = useState(true);
  const [rightPanel, setRightPanel] = useState<"settings" | "authors">("settings");

  const viewerIsPrimary = post.primaryAuthorId === viewerUserId;
  const revisionRef = useRef(state.revision);
  revisionRef.current = state.revision;
  const stateRef = useRef(state);
  stateRef.current = state;

  const persist = useCallback(async () => {
    const current = stateRef.current;
    if (current.status === "conflict") return;
    const payload = buildAutosavePayload(current.draft, baseline.current);
    if (isPayloadEmpty(payload)) return;
    dispatch({ type: "saving" });
    const result = await updateDraft(post.id, {
      ...payload,
      revision: current.revision,
    });
    if (!result.success) {
      if (result.code === "revision-conflict" && "expectedRevision" in result) {
        dispatch({
          type: "conflict",
          expectedRevision: result.expectedRevision,
          currentRevision: result.currentRevision,
          message: result.error,
        });
      } else {
        dispatch({ type: "error", message: result.error });
      }
      return;
    }
    const saved = result.data as unknown as BlogPostAuthorView;
    baseline.current = { ...current.draft };
    dispatch({ type: "saved", revision: saved.revision ?? current.revision + 1, savedAt: new Date().toISOString() });
    setPost((prev) => ({ ...prev, revision: saved.revision, updatedAt: saved.updatedAt }));
  }, [post.id]);

  const dirty = useMemo(
    () => !isPayloadEmpty(buildAutosavePayload(state.draft, baseline.current)),
    [state.draft],
  );

  useEffect(() => {
    if (!dirty || state.status === "conflict" || state.status === "saving") return;
    const timer = window.setTimeout(() => void persist(), AUTOSAVE_DELAY_MS);
    return () => window.clearTimeout(timer);
  }, [dirty, state.status, persist]);

  useEffect(() => {
    if (state.status !== "conflict") return;
    const warn = (event: BeforeUnloadEvent) => {
      event.preventDefault();
      event.returnValue = "";
    };
    window.addEventListener("beforeunload", warn);
    return () => window.removeEventListener("beforeunload", warn);
  }, [state.status]);

  useEffect(() => {
    const onKeyDown = (event: KeyboardEvent) => {
      if ((event.ctrlKey || event.metaKey) && event.key.toLowerCase() === "s") {
        event.preventDefault();
        void persist();
      }
    };
    window.addEventListener("keydown", onKeyDown);
    return () => window.removeEventListener("keydown", onKeyDown);
  }, [persist]);

  const reloadLatest = async () => {
    const result = await reloadLatestPost(primaryAuthorHandle, post.slug ?? "");
    if (!result.success) {
      dispatch({ type: "error", message: result.error });
      return;
    }
    const fresh = result.post;
    const freshDraft = draftFromPost(fresh);
    baseline.current = { ...freshDraft };
    dispatch({ type: "reload-latest", post: { revision: fresh.revision ?? 1, draft: freshDraft } });
    setPost(fresh);
    setSettings({
      tags: freshDraft.tags,
      excerpt: freshDraft.excerpt,
      metaTitle: freshDraft.metaTitle,
      metaDescription: freshDraft.metaDescription,
      ogImageUrl: freshDraft.ogImageUrl,
      canonicalUrlOverride: freshDraft.canonicalUrlOverride,
      twitterCard: freshDraft.twitterCard,
      structuredDataOverride: freshDraft.structuredDataOverride,
      allowComments: freshDraft.allowComments,
    });
  };

  const edit = (patch: Partial<Omit<BlogEditorDraft, "format">>) => {
    dispatch({ type: "edit", patch });
  };

  const editSettings = (patch: Partial<BlogSettingsState>) => {
    setSettings((current) => ({ ...current, ...patch }));
    edit(patch);
  };

  const saveSlug = async () => {
    const next = slugValue.trim().replace(/^-+|-+$/g, "");
    if (!next || next === post.slug) {
      setSlugEditing(false);
      setSlugValue(post.slug ?? "");
      return;
    }
    const result = await changeSlug(post.id, next);
    if (!result.success) {
      setSlugError(result.error);
      return;
    }
    const saved = result.data as unknown as BlogPostAuthorView;
    setSlugError(null);
    setSlugEditing(false);
    setPost(saved);
    baseline.current = draftFromPost(saved);
    dispatch({ type: "saved", revision: saved.revision ?? state.revision, savedAt: new Date().toISOString() });
    router.replace(`/blogs/${primaryAuthorHandle}/${saved.slug}/edit`);
  };

  const settingsDraft = useMemo(
    () => ({
      tags: state.draft.tags,
      excerpt: state.draft.excerpt,
      metaTitle: state.draft.metaTitle,
      metaDescription: state.draft.metaDescription,
      ogImageUrl: state.draft.ogImageUrl,
      canonicalUrlOverride: state.draft.canonicalUrlOverride,
      twitterCard: state.draft.twitterCard,
      structuredDataOverride: state.draft.structuredDataOverride,
      allowComments: state.draft.allowComments,
    }),
    [state.draft],
  );

  const statusCopy =
    state.status === "saving"
      ? "Saving…"
      : state.status === "conflict"
        ? "Conflict"
        : state.status === "error"
          ? state.lastError ?? "Save failed"
          : state.lastSavedAt
            ? `Saved ${new Date(state.lastSavedAt).toLocaleTimeString()}`
            : "Saved";

  return (
    <div className="flex h-dvh min-h-0 flex-col overflow-hidden bg-background text-foreground">
      <header className="relative z-50 flex h-14 shrink-0 items-center gap-2 border-b bg-background px-3">
        <div className="min-w-0 flex-1">
          <p className="truncate text-sm font-medium">
            {state.draft.title || "Untitled post"}
          </p>
          <div className="flex items-center gap-2 text-sm text-muted-foreground">
            <span className={state.status === "conflict" ? "text-destructive" : ""}>
              {state.status === "saving" ? (
                <Loader2 className="mr-1 inline size-3 animate-spin" />
              ) : null}
              {statusCopy}
            </span>
            <Badge variant="outline" className="h-5 rounded-sm px-1.5 text-xs">
              Rev {state.revision}
            </Badge>
          </div>
        </div>
        <BlogPublishBar
          post={post}
          viewerUserId={viewerUserId}
          onChanged={(updated) => {
            setPost(updated);
            baseline.current = draftFromPost(updated);
            dispatch({
              type: "reload-latest",
              post: { revision: updated.revision ?? state.revision, draft: draftFromPost(updated) },
            });
          }}
          onDeleted={() => router.push("/blogs")}
        />
        <Tooltip>
          <TooltipTrigger
            render={
              <Button
                variant="ghost"
                size="icon-sm"
                aria-label="Toggle blog panel"
                onClick={() => setRightOpen((open) => !open)}
              />
            }
          >
            {rightOpen ? <PanelRightClose /> : <PanelRightOpen />}
          </TooltipTrigger>
          <TooltipContent>Toggle blog panel</TooltipContent>
        </Tooltip>
      </header>

      {state.status === "conflict" && state.conflict ? (
        <div
          role="alert"
          className="flex min-h-10 shrink-0 items-center justify-between gap-3 bg-destructive/10 px-4 py-1.5 text-sm text-destructive"
        >
          <span className="truncate">
            This post changed elsewhere (revision {state.conflict.currentRevision}). Reloading
            discards your unsaved local edits.
          </span>
          <Button variant="ghost" size="xs" onClick={() => void reloadLatest()}>
            <RotateCcw /> Reload latest (discard local)
          </Button>
        </div>
      ) : null}

      <main className="relative flex min-h-0 flex-1">
        <section className="h-full min-w-0 flex-1 overflow-auto bg-background px-5 py-6 xl:px-8">
          <div className="mx-auto w-full max-w-4xl">
            <Textarea
              aria-label="Post title"
              value={state.draft.title}
              onChange={(event) => edit({ title: event.target.value })}
              rows={1}
              className="mb-3 min-h-0 resize-none overflow-hidden border-0 bg-transparent px-0 text-2xl font-semibold leading-tight shadow-none focus-visible:ring-0 md:text-3xl [field-sizing:content]"
            />
            <div className="mb-5 flex flex-wrap items-center gap-2 text-sm text-muted-foreground">
              <span>/blogs/{primaryAuthorHandle}/</span>
              {slugEditing && viewerIsPrimary ? (
                <span className="flex items-center gap-1">
                  <Input
                    aria-label="URL slug"
                    value={slugValue}
                    onChange={(event) => setSlugValue(event.target.value)}
                    onKeyDown={(event) => {
                      if (event.key === "Enter") {
                        event.preventDefault();
                        void saveSlug();
                      }
                    }}
                    className="h-7 w-52 text-sm"
                    autoFocus
                  />
                  <Button variant="secondary" size="xs" onClick={() => void saveSlug()}>
                    Save
                  </Button>
                  <Button
                    variant="ghost"
                    size="xs"
                    onClick={() => {
                      setSlugEditing(false);
                      setSlugValue(post.slug ?? "");
                      setSlugError(null);
                    }}
                  >
                    Cancel
                  </Button>
                </span>
              ) : (
                <button
                  type="button"
                  className="underline decoration-dotted hover:text-foreground"
                  disabled={!viewerIsPrimary}
                  onClick={() => {
                    setSlugValue(post.slug ?? "");
                    setSlugEditing(true);
                  }}
                >
                  {post.slug}
                </button>
              )}
              {viewerIsPrimary ? (
                <Tooltip>
                  <TooltipTrigger
                    render={<span className="cursor-help text-xs">ⓘ</span>}
                  >
                    <span />
                  </TooltipTrigger>
                  <TooltipContent>
                    Changing the slug is primary-author only. Old links will
                    redirect to the new URL.
                  </TooltipContent>
                </Tooltip>
              ) : null}
              {slugError ? <span className="text-destructive">{slugError}</span> : null}
            </div>
            <BlogBodyEditor
              postId={post.id}
              format={state.draft.format}
              markdown={state.draft.content}
              jsonBody={state.draft.jsonBody}
              onMarkdownChange={(value) => edit({ content: value })}
              onJsonBodyChange={(value) => edit({ jsonBody: value })}
            />
          </div>
        </section>

        {rightOpen ? (
          <aside
            aria-label="Blog settings"
            className="hidden w-80 shrink-0 flex-col border-l bg-background xl:flex"
          >
            <div className="flex h-12 items-center gap-1 border-b px-2">
              <Button
                variant={rightPanel === "settings" ? "secondary" : "ghost"}
                size="sm"
                className="flex-1"
                onClick={() => setRightPanel("settings")}
              >
                <Settings2 /> Settings
              </Button>
              <Button
                variant={rightPanel === "authors" ? "secondary" : "ghost"}
                size="sm"
                className="flex-1"
                onClick={() => setRightPanel("authors")}
              >
                Authors
              </Button>
            </div>
            <div className="min-h-0 flex-1 overflow-auto">
              {rightPanel === "settings" ? (
                <>
                  <BlogSettingsPanel
                    postId={post.id}
                    post={post}
                    revision={() => stateRef.current.revision}
                    settings={settingsDraft}
                    onChange={editSettings}
                    onPostSaved={(updated) => setPost(updated)}
                  />
                  <Separator />
                  <div className="space-y-1 p-4 text-sm text-muted-foreground">
                    <div className="flex justify-between">
                      <span>Format</span>
                      <span className="text-foreground">{state.draft.format}</span>
                    </div>
                    <div className="flex justify-between">
                      <span>Revision</span>
                      <span className="text-foreground">{state.revision}</span>
                    </div>
                  </div>
                </>
              ) : (
                <BlogCoauthorManager
                  post={post}
                  viewerUserId={viewerUserId}
                  coauthors={coauthors}
                  onChanged={(updated) => setPost(updated)}
                />
              )}
            </div>
          </aside>
        ) : null}
      </main>
    </div>
  );
}
