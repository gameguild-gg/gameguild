"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { Button } from "@game-guild/ui/components/button";
import { Input } from "@game-guild/ui/components/input";
import { Label } from "@game-guild/ui/components/label";
import { Loader2 } from "lucide-react";
import { createPost } from "@/lib/blogs/actions";
import type { BlogContentFormat } from "@/lib/blogs/types";

const FORMATS: ReadonlyArray<{
  value: BlogContentFormat;
  label: string;
  description: string;
}> = [
  {
    value: "Markdown",
    label: "Markdown",
    description: "Write in plain Markdown with a code-style editor.",
  },
  {
    value: "Lexical",
    label: "Rich text",
    description: "Compose with the visual rich-text editor.",
  },
];

interface NewBlogPostFormProps {
  viewerHandle: string | null;
}

export function NewBlogPostForm({ viewerHandle }: NewBlogPostFormProps) {
  const router = useRouter();
  const [title, setTitle] = useState("");
  const [format, setFormat] = useState<BlogContentFormat>("Markdown");
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  const submit = async () => {
    if (!title.trim() || submitting) return;
    setSubmitting(true);
    setError(null);
    const result = await createPost({ title: title.trim(), format });
    if (!result.success) {
      setError(result.error);
      setSubmitting(false);
      return;
    }
    const post = result.data as { slug?: string };
    const handle = viewerHandle;
    if (!handle || !post.slug) {
      setError("The post was created but its URL could not be determined.");
      setSubmitting(false);
      return;
    }
    router.replace(`/blogs/${handle}/${post.slug}/edit`);
  };

  return (
    <div className="mx-auto w-full max-w-2xl px-4 py-10">
      <h1 className="text-2xl font-semibold tracking-tight">New blog post</h1>
      <p className="mt-1 text-sm text-muted-foreground">
        Pick a title and a format. The format is locked once the post is
        created.
      </p>

      <div className="mt-8 space-y-6">
        <div className="space-y-1.5">
          <Label htmlFor="blog-new-title">Title</Label>
          <Input
            id="blog-new-title"
            value={title}
            placeholder="How we built it"
            onChange={(event) => setTitle(event.target.value)}
            onKeyDown={(event) => {
              if (event.key === "Enter") {
                event.preventDefault();
                void submit();
              }
            }}
            autoFocus
          />
        </div>

        <fieldset>
          <legend className="text-sm font-medium">Format</legend>
          <div
            role="radiogroup"
            aria-label="Post format"
            className="mt-2 grid gap-3 sm:grid-cols-2"
          >
            {FORMATS.map((option) => {
              const selected = format === option.value;
              return (
                <button
                  key={option.value}
                  type="button"
                  role="radio"
                  aria-checked={selected}
                  data-testid={`format-${option.value}`}
                  className={`rounded-lg border p-4 text-left transition-colors ${
                    selected
                      ? "border-primary bg-primary/5"
                      : "border-border hover:border-muted-foreground/40"
                  }`}
                  onClick={() => setFormat(option.value)}
                >
                  <span className="block text-sm font-medium">{option.label}</span>
                  <span className="mt-1 block text-sm text-muted-foreground">
                    {option.description}
                  </span>
                </button>
              );
            })}
          </div>
        </fieldset>

        {error ? (
          <p className="text-sm text-destructive" role="alert">
            {error}
          </p>
        ) : null}

        <div className="flex items-center gap-2">
          <Button disabled={!title.trim() || submitting} onClick={() => void submit()}>
            {submitting ? <Loader2 className="animate-spin" /> : null}
            Create post
          </Button>
          <Button variant="ghost" onClick={() => router.back()}>
            Cancel
          </Button>
        </div>
      </div>
    </div>
  );
}
