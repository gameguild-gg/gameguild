"use client";

import { useState } from "react";
import { Button } from "@game-guild/ui/components/button";
import { Input } from "@game-guild/ui/components/input";
import { Label } from "@game-guild/ui/components/label";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@game-guild/ui/components/select";
import { Switch } from "@game-guild/ui/components/switch";
import { Textarea } from "@game-guild/ui/components/textarea";
import { Loader2, Sparkles, X } from "lucide-react";
import { createAiRun, applyProposal, getAiRun } from "@/lib/blogs/actions";
import type { BlogPostAuthorView } from "@/lib/blogs/queries";

const MAX_TAGS = 20;
const TWITTER_CARDS = ["summary", "summary_large_image"] as const;

export interface BlogSettingsState {
  tags: string[];
  excerpt: string;
  metaTitle: string;
  metaDescription: string;
  ogImageUrl: string;
  canonicalUrlOverride: string;
  twitterCard: string;
  structuredDataOverride: string;
  allowComments: boolean;
}

interface BlogSettingsPanelProps {
  postId: string;
  post: BlogPostAuthorView;
  revision: () => number;
  settings: BlogSettingsState;
  onChange: (patch: Partial<BlogSettingsState>) => void;
  onPostSaved: (post: BlogPostAuthorView) => void;
}

export function BlogSettingsPanel({
  postId,
  post,
  revision,
  settings,
  onChange,
  onPostSaved,
}: BlogSettingsPanelProps) {
  const [tagInput, setTagInput] = useState("");
  const [summaryStatus, setSummaryStatus] = useState<
    "idle" | "generating" | "error"
  >("idle");
  const [summaryError, setSummaryError] = useState<string | null>(null);

  const addTag = () => {
    const tag = tagInput.trim().toLowerCase();
    if (!tag) return;
    if (settings.tags.length >= MAX_TAGS || settings.tags.includes(tag)) {
      setTagInput("");
      return;
    }
    onChange({ tags: [...settings.tags, tag] });
    setTagInput("");
  };

  const generateSummary = async () => {
    if (summaryStatus === "generating") return;
    setSummaryStatus("generating");
    setSummaryError(null);
    const run = await createAiRun(postId, {
      postRevision: revision(),
      instruction:
        "Write a concise summary (excerpt) for this post based on its current content.",
      proposalKind: "MetadataPatch",
      idempotencyKey: crypto.randomUUID(),
    });
    if (!run.success) {
      setSummaryStatus("error");
      setSummaryError(run.error);
      return;
    }
    const runId = (run.data as { id?: string }).id;
    if (!runId) {
      setSummaryStatus("error");
      setSummaryError("The AI run could not be started.");
      return;
    }
    const interval = window.setInterval(async () => {
      const applied = await pollAndApply(runId);
      if (applied !== "pending") window.clearInterval(interval);
    }, 3000);
    window.setTimeout(() => {
      window.clearInterval(interval);
      setSummaryStatus((status) =>
        status === "generating" ? "error" : status,
      );
      setSummaryError((error) => error ?? "Summary generation timed out.");
    }, 180_000);
  };

  const pollAndApply = async (runId: string): Promise<"pending" | "done" | "failed"> => {
    const result = await getAiRun(postId, runId);
    if (!result.success) {
      setSummaryStatus("error");
      setSummaryError(result.error);
      return "failed";
    }
    const run = result.data as {
      status?: string;
      proposal?: { id?: string } | null;
      errorMessage?: string | null;
    };
    if (!["Completed", "Failed", "Cancelled"].includes(run.status ?? "")) {
      return "pending";
    }
    const proposal = run.proposal;
    if (run.status !== "Completed" || !proposal?.id) {
      setSummaryStatus("error");
      setSummaryError(run.errorMessage ?? "Summary generation failed.");
      return "failed";
    }
    const applied = await applyProposal(postId, proposal.id, revision());
    if (!applied.success) {
      setSummaryStatus("error");
      setSummaryError(applied.error);
      return "failed";
    }
    const updated = applied.data as { excerpt?: string | null };
    if (typeof updated.excerpt === "string") {
      onChange({ excerpt: updated.excerpt });
    } else {
      onPostSaved(post);
    }
    setSummaryStatus("idle");
    return "done";
  };

  return (
    <div className="space-y-6 p-4">
      <div className="space-y-2">
        <Label htmlFor="blog-tags">Tags</Label>
        <div className="flex flex-wrap gap-1.5">
          {settings.tags.map((tag) => (
            <span
              key={tag}
              className="inline-flex items-center gap-1 rounded-md bg-muted px-2 py-0.5 text-xs lowercase"
            >
              {tag}
              <button
                type="button"
                aria-label={`Remove tag ${tag}`}
                className="text-muted-foreground hover:text-foreground"
                onClick={() =>
                  onChange({ tags: settings.tags.filter((t) => t !== tag) })
                }
              >
                <X className="size-3" />
              </button>
            </span>
          ))}
        </div>
        <div className="flex gap-2">
          <Input
            id="blog-tags"
            value={tagInput}
            placeholder={settings.tags.length >= MAX_TAGS ? `Max ${MAX_TAGS} tags` : "Add a tag"}
            disabled={settings.tags.length >= MAX_TAGS}
            onChange={(event) => setTagInput(event.target.value)}
            onKeyDown={(event) => {
              if (event.key === "Enter") {
                event.preventDefault();
                addTag();
              }
            }}
            onBlur={addTag}
          />
        </div>
      </div>

      <div className="space-y-1.5">
        <div className="flex items-center justify-between">
          <Label htmlFor="blog-excerpt">Excerpt</Label>
          <Button
            type="button"
            variant="ghost"
            size="xs"
            disabled={summaryStatus === "generating"}
            onClick={() => void generateSummary()}
          >
            {summaryStatus === "generating" ? (
              <Loader2 className="animate-spin" />
            ) : (
              <Sparkles />
            )}
            Generate summary
          </Button>
        </div>
        {summaryStatus === "generating" ? (
          <p className="text-xs text-muted-foreground" role="status">
            Generating summary… Reload the panel when the run completes if it
            does not fill in automatically.
          </p>
        ) : null}
        {summaryStatus === "error" && summaryError ? (
          <p className="text-xs text-destructive" role="alert">
            {summaryError}
          </p>
        ) : null}
        <Textarea
          id="blog-excerpt"
          rows={3}
          value={settings.excerpt}
          onChange={(event) => onChange({ excerpt: event.target.value })}
        />
      </div>

      <div className="space-y-3">
        <h3 className="text-sm font-semibold uppercase tracking-wide text-muted-foreground">
          SEO
        </h3>
        <div className="space-y-1.5">
          <Label htmlFor="blog-meta-title">Meta title</Label>
          <Input
            id="blog-meta-title"
            value={settings.metaTitle}
            onChange={(event) => onChange({ metaTitle: event.target.value })}
          />
        </div>
        <div className="space-y-1.5">
          <Label htmlFor="blog-meta-description">Meta description</Label>
          <Textarea
            id="blog-meta-description"
            rows={2}
            value={settings.metaDescription}
            onChange={(event) =>
              onChange({ metaDescription: event.target.value })
            }
          />
        </div>
        <div className="space-y-1.5">
          <Label htmlFor="blog-og-image">OG image URL</Label>
          <Input
            id="blog-og-image"
            value={settings.ogImageUrl}
            onChange={(event) => onChange({ ogImageUrl: event.target.value })}
          />
        </div>
        <div className="space-y-1.5">
          <Label htmlFor="blog-canonical">Canonical URL override</Label>
          <Input
            id="blog-canonical"
            value={settings.canonicalUrlOverride}
            onChange={(event) =>
              onChange({ canonicalUrlOverride: event.target.value })
            }
          />
        </div>
        <div className="space-y-1.5">
          <Label htmlFor="blog-twitter-card">Twitter card</Label>
          <Select
            value={settings.twitterCard || TWITTER_CARDS[1]}
            onValueChange={(value) => onChange({ twitterCard: value ?? TWITTER_CARDS[1] })}
          >
            <SelectTrigger id="blog-twitter-card" className="w-full">
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              {TWITTER_CARDS.map((card) => (
                <SelectItem key={card} value={card}>
                  {card}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>
        <div className="space-y-1.5">
          <Label htmlFor="blog-structured-data">Structured data override (JSON-LD)</Label>
          <Textarea
            id="blog-structured-data"
            rows={3}
            value={settings.structuredDataOverride}
            onChange={(event) =>
              onChange({ structuredDataOverride: event.target.value })
            }
          />
        </div>
      </div>

      <div className="flex items-center justify-between gap-3">
        <div>
          <Label htmlFor="blog-allow-comments">Allow comments</Label>
          <p className="mt-0.5 text-sm text-muted-foreground">
            Readers can reply to this post.
          </p>
        </div>
        <Switch
          id="blog-allow-comments"
          checked={settings.allowComments}
          onCheckedChange={(value) => onChange({ allowComments: value })}
        />
      </div>
    </div>
  );
}
