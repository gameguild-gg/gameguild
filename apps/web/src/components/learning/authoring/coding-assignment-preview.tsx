"use client";

import type { CodingAssignmentContent } from "@/lib/coding-assignment/types";
import type { CodingAuthorTestSurfaceProps } from "@game-guild/coding-surface";
import {
  createCodingWorkspaceConfig,
  type CodingWorkspaceLanguage,
} from "@game-guild/coding-surface";
import { useEffect, useId, useMemo, useState, type ComponentType } from "react";
import { EMCEPTION_MANIFEST_URL } from "@/lib/emception/manifest-url";
import { codingAssignmentToAuthoringDefinition } from "@/lib/coding-assignment/coding-surface-bridge";

interface CodingAssignmentPreviewProps {
  assignment: CodingAssignmentContent;
}

export function CodingAssignmentPreview({
  assignment,
}: CodingAssignmentPreviewProps) {
  const instanceId = useId();
  const encoded = JSON.stringify(assignment);
  const [context, setContext] = useState({ encoded, generation: 0 });
  if (context.encoded !== encoded) setContext({ encoded, generation: context.generation + 1 });
  const [Editor, setEditor] =
    useState<ComponentType<CodingAuthorTestSurfaceProps> | null>(null);
  const [loadError, setLoadError] = useState<string | null>(null);
  const language = (assignment.Environment.Language || "cpp") as CodingWorkspaceLanguage;
  const definition = useMemo(
    () => codingAssignmentToAuthoringDefinition(assignment),
    [assignment],
  );
  const publicFiles = useMemo(
    () =>
      Object.fromEntries(
        Object.entries(assignment.Data.Files)
          .filter(([, file]) => file.Visibility !== "Private")
          .map(([path, file]) => [
            path,
            { encoding: file.Encoding, content: file.Content },
          ]),
      ),
    [assignment],
  );
  const workspaceConfig = useMemo(
    () => createCodingWorkspaceConfig(language, publicFiles),
    [language, publicFiles],
  );

  useEffect(() => {
    let active = true;
    void import("@game-guild/coding-surface")
      .then(({ CodingSurface }) => {
        if (active) setEditor(() => CodingSurface as ComponentType<CodingAuthorTestSurfaceProps>);
      })
      .catch((error: unknown) => {
        if (active) {
          setLoadError(
            error instanceof Error
              ? error.message
              : "Unable to load the coding assignment preview.",
          );
        }
      });
    return () => {
      active = false;
    };
  }, []);

  if (loadError) {
    return (
      <p role="alert" className="text-sm text-destructive">
        {loadError}
      </p>
    );
  }

  if (!Editor) {
    return (
      <div className="flex h-96 items-center justify-center rounded-md border text-sm text-muted-foreground">
        Loading coding assignment preview…
      </div>
    );
  }

  return (
    <div className="h-[70vh] min-h-[500px] overflow-hidden rounded-md border">
      <Editor
        mode="author-test"
        contextKey={`${instanceId}:${context.generation}`}
        manifestUrl={EMCEPTION_MANIFEST_URL}
        definition={definition}
        workspaceConfig={workspaceConfig}
        enableWorkspace={false}
        maxScore={assignment.Grading.MaxScore}
      />
    </div>
  );
}
