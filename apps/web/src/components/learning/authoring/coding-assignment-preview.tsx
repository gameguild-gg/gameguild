"use client";

import type { CodingAssignmentContent } from "@/lib/coding-assignment/types";
import type { CodingAssessmentEditorProps } from "@game-guild/emception-ui/assessment/editor";
import {
  createAssessmentWorkspaceConfig,
  type CodingLanguage,
} from "@game-guild/emception-ui/assessment/presets";
import { useEffect, useMemo, useState, type ComponentType } from "react";
import { EMCEPTION_MANIFEST_URL } from "@/lib/emception/manifest-url";

interface CodingAssignmentPreviewProps {
  assignment: CodingAssignmentContent;
}

export function CodingAssignmentPreview({
  assignment,
}: CodingAssignmentPreviewProps) {
  const [Editor, setEditor] =
    useState<ComponentType<CodingAssessmentEditorProps> | null>(null);
  const [loadError, setLoadError] = useState<string | null>(null);
  const language = (assignment.Environment.Language || "cpp") as CodingLanguage;
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
    () => createAssessmentWorkspaceConfig(language, publicFiles),
    [language, publicFiles],
  );

  useEffect(() => {
    let active = true;
    void import("@game-guild/emception-ui/assessment/editor")
      .then(({ CodingAssessmentEditor }) => {
        if (active) setEditor(() => CodingAssessmentEditor);
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
        mode="learner"
        manifestUrl={EMCEPTION_MANIFEST_URL}
        definition={assignment}
        workspaceConfig={workspaceConfig}
        enableWorkspace={false}
        maxScore={assignment.Grading.MaxScore}
      />
    </div>
  );
}
