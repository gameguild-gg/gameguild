"use client";

import type {
  BlogContentFormat,
  BlogPostDetail,
} from "@/lib/blogs/types";
import {
  applyProposal,
  cancelAiRun,
  createAiRun,
  discardProposal,
} from "@/lib/blogs/actions";
import { selectProposalKind, type CopilotMode } from "@/lib/blogs/copilot-kinds";
import { Badge } from "@game-guild/ui/components/badge";
import { Button } from "@game-guild/ui/components/button";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@game-guild/ui/components/dialog";
import { ScrollArea } from "@game-guild/ui/components/scroll-area";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@game-guild/ui/components/select";
import { Textarea } from "@game-guild/ui/components/textarea";
import { Bot, Check, Loader2, Send, Sparkles } from "lucide-react";
import { useTheme } from "next-themes";
import { lazy, Suspense, useCallback, useEffect, useRef, useState } from "react";

const MonacoDiffEditor = lazy(async () => {
  const monacoReact = await import("@monaco-editor/react");
  return { default: monacoReact.DiffEditor };
});

interface BlogAiMessage {
  id: string;
  role: string;
  content: string;
  runId?: string;
  createdAt: string;
}

interface BlogAiRun {
  id: string;
  conversationId: string;
  status?: string;
  errorCode?: string | null;
  errorMessage?: string | null;
  proposalKind?: string | null;
  proposal?: BlogAiProposal | null;
  usage?: {
    maximumEstimatedCost?: number;
    inputTokens?: number;
    outputTokens?: number;
    settledCost?: number;
  } | null;
}

interface BlogAiProposal {
  id: string;
  kind?: string | null;
  status?: string | null;
  originalContent?: string | null;
  proposedContent?: string | null;
  basePostRevision?: number;
}

interface BlogAiEntitlement {
  availableSoftCredits?: number;
}

interface BlogAiConversation {
  id?: string;
  messages?: Array<{
    id?: string;
    role?: string | null;
    content?: string | null;
    runId?: string | null;
    createdAt?: string;
  }> | null;
}

export interface BlogCopilotPanelProps {
  postId: string;
  postRevision: number;
  format: BlogContentFormat;
  onRevisionChange: (post: BlogPostDetail) => void;
  /** Editor selection snapshot; drives InsertAtCursor kind selection. */
  selection?: { text: string; cursorOffset: number } | null;
}

interface AiRequestFailure {
  error: string;
  status: number;
  code?: string;
}

const ERROR_COPY: Record<string, string> = {
  AI_QUOTA_EXCEEDED:
    "AI usage quota exceeded. Wait for the quota window to reset.",
  INSUFFICIENT_AI_CREDITS:
    "Not enough soft credits to start this run.",
  AI_EXECUTION_FAILED: "The AI run failed. Try again.",
  AI_RUN_INTERRUPTED: "The AI run was interrupted.",
  AI_IDEMPOTENCY_CONFLICT:
    "This run was already submitted with the same key.",
  AI_PROPOSAL_KIND_NOT_ALLOWED:
    "That change type is not available for this post format.",
  BLOG_REVISION_CONFLICT: "The post changed while the AI worked. Reload and retry.",
};

function errorCopy(failure: AiRequestFailure): string {
  if (failure.code && ERROR_COPY[failure.code]) return ERROR_COPY[failure.code];
  return failure.error || "Copilot request failed.";
}

function parseStreamFrame(frame: string): unknown | null {
  const data = frame
    .split("\n")
    .filter((line) => line.startsWith("data:"))
    .map((line) => line.slice(5).trimStart())
    .join("\n");
  if (!data) return null;
  try {
    return JSON.parse(data);
  } catch {
    return null;
  }
}

async function aiRead<T>(path: string): Promise<
  | { success: true; data: T }
  | { success: false; error: string; status: number; code?: string }
> {
  const response = await fetch(path);
  if (!response.ok) {
    const body = (await response.json().catch(() => null)) as
      | { detail?: unknown; title?: unknown; code?: unknown }
      | null;
    return {
      success: false,
      error:
        (typeof body?.detail === "string" && body.detail) ||
        (typeof body?.title === "string" && body.title) ||
        "Copilot request failed.",
      status: response.status,
      code: typeof body?.code === "string" ? body.code : undefined,
    };
  }
  return { success: true, data: (await response.json()) as T };
}

const activeRunStorageKeyFor = (postId: string) => `blog-ai-run:${postId}`;

export function BlogCopilotPanel({
  postId,
  postRevision,
  format,
  onRevisionChange,
  selection = null,
}: BlogCopilotPanelProps) {
  const [messages, setMessages] = useState<BlogAiMessage[]>([]);
  const [conversationId, setConversationId] = useState<string | null>(null);
  const [prompt, setPrompt] = useState("");
  const [isRunning, setIsRunning] = useState(false);
  const [activeRun, setActiveRun] = useState<BlogAiRun | null>(null);
  const [proposal, setProposal] = useState<BlogAiProposal | null>(null);
  const [diffOpen, setDiffOpen] = useState(false);
  const [entitlement, setEntitlement] = useState<BlogAiEntitlement | null>(null);
  const [aiError, setAiError] = useState<string | null>(null);
  const [mode, setMode] = useState<CopilotMode>("content");
  const [unifiedDiff, setUnifiedDiff] = useState(false);
  const { resolvedTheme } = useTheme();

  const streamAbortRef = useRef<AbortController | null>(null);
  const restoredRunRef = useRef(false);
  const revisionRef = useRef(postRevision);
  const activeRunStorageKey = activeRunStorageKeyFor(postId);

  useEffect(() => {
    revisionRef.current = postRevision;
  }, [postRevision]);

  useEffect(() => {
    void Promise.all([
      aiRead<BlogAiEntitlement>(
        `/api/blogs/authoring/${encodeURIComponent(postId)}/ai/entitlement`,
      ),
      aiRead<BlogAiConversation[]>(
        `/api/blogs/authoring/${encodeURIComponent(postId)}/ai/conversations`,
      ),
    ]).then(([creditResult, conversationResult]) => {
      if (creditResult.success) setEntitlement(creditResult.data);
      if (conversationResult.success && conversationResult.data[0]?.id) {
        setConversationId(conversationResult.data[0].id as string);
        setMessages(
          (conversationResult.data[0].messages ?? []).map(
            (message, index) => ({
              id: message.id ?? `restored-${index}`,
              role: message.role ?? "assistant",
              content: message.content ?? "",
              runId: message.runId ?? undefined,
              createdAt: message.createdAt ?? new Date().toISOString(),
            }),
          ),
        );
      }
    });
    // eslint-disable-next-line react-hooks/exhaustive-deps -- postId is stable per mount
  }, [postId]);

  const readRunStream = useCallback(
    async (run: BlogAiRun) => {
      let lastEventId = 0;
      let reconnectAttempts = 0;
      streamAbortRef.current?.abort();
      const abortController = new AbortController();
      streamAbortRef.current = abortController;
      window.sessionStorage.setItem(activeRunStorageKey, run.id);

      while (!abortController.signal.aborted) {
        try {
          const response = await fetch(
            `/api/blogs/authoring/${encodeURIComponent(postId)}/runs/${encodeURIComponent(run.id)}/stream`,
            {
              headers: lastEventId
                ? { "Last-Event-ID": String(lastEventId) }
                : {},
              signal: abortController.signal,
            },
          );
          if (!response.ok || !response.body)
            throw new Error("The Copilot stream could not be opened.");
          reconnectAttempts = 0;
          const reader = response.body.getReader();
          const decoder = new TextDecoder();
          let buffer = "";
          while (true) {
            const { done, value } = await reader.read();
            buffer += decoder.decode(value, { stream: !done });
            const frames = buffer.split("\n\n");
            // String#split always returns at least one segment.
            buffer = frames.pop() as string;
            for (const frame of frames) {
              const id = frame.match(/^id:\s*(\d+)/m)?.[1];
              if (id) lastEventId = Number(id);
              const event = parseStreamFrame(frame) as {
                delta?: string;
                proposal?: BlogAiProposal;
                errorCode?: string;
              } | null;
              if (event?.delta) {
                const delta = event.delta;
                setMessages((current) => {
                  const last = current.at(-1);
                  if (last?.role === "assistant" && last.runId === run.id)
                    return [
                      ...current.slice(0, -1),
                      { ...last, content: last.content + delta },
                    ];
                  return [
                    ...current,
                    {
                      id: `stream-${run.id}`,
                      role: "assistant",
                      content: delta,
                      runId: run.id,
                      createdAt: new Date().toISOString(),
                    },
                  ];
                });
              }
              if (event?.proposal) {
                setProposal(event.proposal);
                setDiffOpen(true);
              }
              if (
                event?.errorCode &&
                !["AI_CANCEL_REQUESTED", "AI_CANCELLED"].includes(
                  event.errorCode,
                )
              )
                setAiError(ERROR_COPY[event.errorCode] ?? event.errorCode);
            }
            if (done) break;
          }

          const latest = await aiRead<BlogAiRun>(
            `/api/blogs/authoring/${encodeURIComponent(postId)}/ai/runs/${encodeURIComponent(run.id)}`,
          );
          if (!latest.success)
            throw new Error(latest.error || "Copilot run lookup failed.");
          setActiveRun(latest.data);
          if (latest.data.proposal) {
            setProposal(latest.data.proposal);
            setDiffOpen(latest.data.proposal.status === "Pending");
          }
          if (
            ["Completed", "Failed", "Cancelled"].includes(
              latest.data.status ?? "",
            )
          ) {
            window.sessionStorage.removeItem(activeRunStorageKey);
            if (latest.data.status === "Failed")
              setAiError(latest.data.errorMessage ?? "Copilot generation failed.");
            const balance = await aiRead<BlogAiEntitlement>(
              `/api/blogs/authoring/${encodeURIComponent(postId)}/ai/entitlement`,
            );
            if (balance.success) setEntitlement(balance.data);
            return;
          }
        } catch (error) {
          if (abortController.signal.aborted) return;
          reconnectAttempts += 1;
          if (reconnectAttempts > 4) throw error;
        }
        await new Promise((resolve) =>
          window.setTimeout(
            resolve,
            Math.min(750 * 2 ** reconnectAttempts, 5000),
          ),
        );
      }
    },
    [activeRunStorageKey, postId],
  );

  useEffect(() => {
    if (restoredRunRef.current) return;
    restoredRunRef.current = true;
    const runId = window.sessionStorage.getItem(activeRunStorageKey);
    if (!runId) return;
    void aiRead<BlogAiRun>(
      `/api/blogs/authoring/${encodeURIComponent(postId)}/ai/runs/${encodeURIComponent(runId)}`,
    ).then(async (result) => {
      if (!result.success) {
        window.sessionStorage.removeItem(activeRunStorageKey);
        return;
      }
      setActiveRun(result.data);
      if (result.data.proposal) {
        setProposal(result.data.proposal);
        setDiffOpen(result.data.proposal.status === "Pending");
      }
      if (["Completed", "Failed", "Cancelled"].includes(result.data.status ?? "")) {
        window.sessionStorage.removeItem(activeRunStorageKey);
        return;
      }
      setIsRunning(true);
      try {
        await readRunStream(result.data);
      } catch (error) {
        setAiError(
          error instanceof Error
            ? error.message
            : "Copilot stopped unexpectedly.",
        );
      } finally {
        setIsRunning(false);
      }
    });
  }, [activeRunStorageKey, postId, readRunStream]);

  const runCopilot = async (instruction = prompt) => {
    if (!instruction.trim() || isRunning) return;
    setAiError(null);
    setIsRunning(true);
    setMessages((current) => [
      ...current,
      {
        id: `local-${crypto.randomUUID()}`,
        role: "user",
        content: instruction.trim(),
        createdAt: new Date().toISOString(),
      },
    ]);
    const kind = selectProposalKind(format, !!selection?.text, mode);
    const result = await createAiRun(postId, {
      conversationId,
      postRevision: revisionRef.current,
      instruction: instruction.trim(),
      proposalKind: kind,
      selection: selection?.text ?? null,
      idempotencyKey: crypto.randomUUID(),
    });
    if (!result.success) {
      setAiError(errorCopy(result));
      setIsRunning(false);
      return;
    }
    setPrompt("");
    const run = result.data as unknown as BlogAiRun;
    setActiveRun(run);
    setConversationId(run.conversationId);
    try {
      await readRunStream(run);
    } catch (error) {
      setAiError(
        error instanceof Error
          ? error.message
          : "Copilot stopped unexpectedly.",
      );
    } finally {
      setIsRunning(false);
    }
  };

  const stopCopilot = async () => {
    if (!activeRun) return;
    const result = await cancelAiRun(postId, activeRun.id);
    if (!result.success) {
      setAiError(errorCopy(result));
      return;
    }
    setActiveRun(result.data as unknown as BlogAiRun);
    if ((result.data as unknown as BlogAiRun).status === "Cancelled") {
      streamAbortRef.current?.abort();
      window.sessionStorage.removeItem(activeRunStorageKey);
      setIsRunning(false);
      const balance = await aiRead<BlogAiEntitlement>(
        `/api/blogs/authoring/${encodeURIComponent(postId)}/ai/entitlement`,
      );
      if (balance.success) setEntitlement(balance.data);
    }
  };

  const acceptProposal = async (proposalToApply: BlogAiProposal) => {
    const result = await applyProposal(
      postId,
      proposalToApply.id,
      revisionRef.current,
      proposalToApply.kind === "InsertAtCursor"
        ? selection?.cursorOffset ?? 0
        : undefined,
    );
    if (!result.success) {
      setAiError(errorCopy(result));
      return;
    }
    const updated = result.data as BlogPostDetail;
    onRevisionChange(updated);
    revisionRef.current = updated.revision ?? revisionRef.current;
    setProposal({ ...proposalToApply, status: "Applied" });
    setDiffOpen(false);
  };

  const rejectProposal = async (proposalToDiscard: BlogAiProposal) => {
    const result = await discardProposal(postId, proposalToDiscard.id);
    if (result.success) setProposal(result.data as unknown as BlogAiProposal);
    setDiffOpen(false);
  };

  const newConversation = () => {
    if (isRunning) return;
    setConversationId(null);
    setMessages([]);
    setAiError(null);
  };

  return (
    <div className="flex min-h-0 flex-1 flex-col">
      <div className="border-b px-4 py-3">
        <div className="flex items-center justify-between gap-2">
          <div>
            <p className="text-sm font-medium">Blog Copilot</p>
            <p className="text-sm text-muted-foreground">
              Changes always require your approval.
            </p>
          </div>
          <Badge variant="outline" className="font-mono text-sm">
            {entitlement
              ? `${entitlement.availableSoftCredits} SC`
              : "… SC"}
          </Badge>
        </div>
        <Button
          variant="ghost"
          size="xs"
          className="mt-2"
          disabled={isRunning}
          onClick={newConversation}
        >
          <Sparkles /> New conversation
        </Button>
      </div>
      <ScrollArea className="min-h-0 flex-1">
        <div className="space-y-3 p-3">
          {messages.length === 0 ? (
            <div className="py-5 text-center">
              <Sparkles className="mx-auto size-5 text-primary" />
              <p className="mt-2 text-sm font-medium">Improve this post</p>
              <p className="mx-auto mt-1 max-w-56 text-sm text-muted-foreground">
                Ask for a rewrite, a better title, an excerpt, tags, or SEO
                metadata.
              </p>
              <div className="mt-4 grid gap-1.5">
                {[
                  "Make this clearer and more concise",
                  "Write a compelling excerpt",
                  "Suggest tags and SEO metadata",
                ].map((suggestion) => (
                  <Button
                    key={suggestion}
                    variant="outline"
                    size="sm"
                    className="h-auto justify-start py-2 text-left text-sm"
                    onClick={() => void runCopilot(suggestion)}
                  >
                    {suggestion}
                  </Button>
                ))}
              </div>
            </div>
          ) : null}
          {messages.map((message) => (
            <div
              key={message.id}
              className={
                message.role === "user"
                  ? "ml-7 rounded-lg bg-primary px-3 py-2 text-sm text-primary-foreground"
                  : "mr-3 rounded-lg bg-muted px-3 py-2 text-sm leading-relaxed"
              }
            >
              {message.content}
            </div>
          ))}
          {isRunning ? (
            <div className="flex items-center justify-between gap-3 px-2 py-1 text-sm text-muted-foreground">
              <span className="flex items-center gap-2">
                <Loader2 className="size-3 animate-spin" />
                {activeRun?.errorCode === "AI_CANCEL_REQUESTED"
                  ? "Stopping Copilot…"
                  : "Generating proposal…"}
              </span>
              <Button
                variant="ghost"
                size="xs"
                disabled={activeRun?.errorCode === "AI_CANCEL_REQUESTED"}
                onClick={() => void stopCopilot()}
              >
                Stop
              </Button>
            </div>
          ) : null}
          {aiError ? (
            <p className="rounded-md bg-destructive/10 p-2 text-sm text-destructive">
              {aiError}
            </p>
          ) : null}
          {activeRun?.usage?.maximumEstimatedCost ? (
            <div className="rounded-md bg-muted/50 p-2 text-sm text-muted-foreground">
              Max {activeRun.usage.maximumEstimatedCost} SC · Used{" "}
              {(activeRun.usage.inputTokens ?? 0) +
                (activeRun.usage.outputTokens ?? 0)}{" "}
              tokens · Settled {activeRun.usage.settledCost ?? 0} SC
            </div>
          ) : null}
        </div>
      </ScrollArea>
      <div className="space-y-2 border-t p-3">
        <div className="flex items-center gap-2 text-sm text-muted-foreground">
          <Bot className="size-4" />
          <Select
            value={mode}
            onValueChange={(value) => setMode(value as CopilotMode)}
          >
            <SelectTrigger
              aria-label="Proposal application"
              className="h-8 flex-1 text-sm"
            >
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="content">Revise post content</SelectItem>
              <SelectItem value="metadata">
                Improve settings / metadata
              </SelectItem>
            </SelectContent>
          </Select>
          {format === "Markdown" ? (
            <label className="flex items-center gap-1.5 text-xs">
              <input
                type="checkbox"
                checked={!!selection?.text}
                readOnly
              />
              Selection {selection?.text ? "attached" : "none"}
            </label>
          ) : null}
        </div>
        <p className="rounded-md bg-muted/50 px-2.5 py-2 text-xs text-muted-foreground">
          Applies as{" "}
          <span className="font-mono">
            {selectProposalKind(format, !!selection?.text, mode)}
          </span>
        </p>
        <div className="relative">
          <Textarea
            aria-label="Ask Copilot"
            value={prompt}
            onChange={(event) => setPrompt(event.target.value)}
            onKeyDown={(event) => {
              if (event.key === "Enter" && !event.shiftKey) {
                event.preventDefault();
                void runCopilot();
              }
            }}
            rows={3}
            className="resize-none pr-10 text-sm"
            placeholder="Ask AI to revise this post…"
          />
          <Button
            size="icon-sm"
            className="absolute bottom-2 right-2"
            aria-label="Send to Copilot"
            disabled={!prompt.trim() || isRunning}
            onClick={() => void runCopilot()}
          >
            {isRunning ? <Loader2 className="animate-spin" /> : <Send />}
          </Button>
        </div>
        <p className="text-sm text-muted-foreground">
          Maximum cost is reserved first. Only actual token usage is charged to
          your wallet.
        </p>
      </div>

      <Dialog open={diffOpen} onOpenChange={setDiffOpen}>
        <DialogContent className="grid h-[88dvh] max-w-[94vw] grid-rows-[auto_minmax(0,1fr)_auto] gap-0 overflow-hidden p-0 sm:max-w-[94vw]">
          <DialogHeader className="border-b px-5 py-4">
            <div className="flex items-start justify-between gap-4 pr-10">
              <div>
                <DialogTitle>Review AI proposal</DialogTitle>
                <DialogDescription>
                  Compare the current post with the proposed content before
                  applying it.
                </DialogDescription>
              </div>
              <div className="flex rounded-md border p-0.5">
                <Button
                  variant={!unifiedDiff ? "secondary" : "ghost"}
                  size="xs"
                  onClick={() => setUnifiedDiff(false)}
                >
                  Side by side
                </Button>
                <Button
                  variant={unifiedDiff ? "secondary" : "ghost"}
                  size="xs"
                  onClick={() => setUnifiedDiff(true)}
                >
                  Unified
                </Button>
              </div>
            </div>
          </DialogHeader>
          <div className="min-h-0 bg-background">
            {proposal ? (
              <Suspense
                fallback={
                  <div className="flex h-full items-center justify-center">
                    <Loader2 className="animate-spin" />
                  </div>
                }
              >
                <MonacoDiffEditor
                  original={proposal.originalContent ?? ""}
                  modified={proposal.proposedContent ?? ""}
                  language={format === "Lexical" ? "json" : "markdown"}
                  theme={resolvedTheme === "light" ? "vs-light" : "vs-dark"}
                  height="100%"
                  options={{
                    readOnly: true,
                    renderSideBySide: !unifiedDiff,
                    minimap: { enabled: false },
                    wordWrap: "on",
                  }}
                />
              </Suspense>
            ) : null}
          </div>
          <DialogFooter className="border-t px-5 py-3">
            <div className="mr-auto text-sm text-muted-foreground">
              Base post revision {proposal?.basePostRevision}
            </div>
            {proposal ? (
              <>
                <Button
                  variant="outline"
                  onClick={() => void rejectProposal(proposal)}
                >
                  Discard
                </Button>
                <Button onClick={() => void acceptProposal(proposal)}>
                  <Check /> Accept and apply
                </Button>
              </>
            ) : null}
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}

export default BlogCopilotPanel;
