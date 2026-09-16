import {
  legacyAssessmentToken,
  workspaceStorageKey,
} from "@game-guild/emception-ui/assessment/storage";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import {
  hasRestorableDraft,
  publicSeedFiles,
  resolveSeed,
  type SeedFile,
} from "./resolve-seed";

const seedFiles: SeedFile[] = [
  { path: "main.cpp", content: "// starter", encoding: "text" },
  { path: "util.h", content: "#pragma once", encoding: "text" },
];

const submissionFiles: SeedFile[] = [
  { path: "main.cpp", content: "int main(){}", encoding: "text" },
  { path: "extra.py", content: 'print("hi")', encoding: "text" },
];

describe("resolveSeed", () => {
  it("returns seed mode with the seed files when there is no submission (null)", () => {
    expect(
      resolveSeed({ draftExists: false, submissionFiles: null, seedFiles }),
    ).toEqual({
      mode: "seed",
      files: seedFiles,
    });
  });

  it("treats an empty submission array as no submission (seed mode)", () => {
    expect(
      resolveSeed({ draftExists: false, submissionFiles: [], seedFiles }),
    ).toEqual({
      mode: "seed",
      files: seedFiles,
    });
  });

  it("submission mode replaces matching paths and adds new ones", () => {
    const { mode, files } = resolveSeed({
      draftExists: false,
      submissionFiles,
      seedFiles,
    });
    expect(mode).toBe("submission");
    const byPath = new Map(files.map((f) => [f.path, f]));
    expect(byPath.get("main.cpp")?.content).toBe("int main(){}");
    expect(byPath.get("extra.py")?.content).toBe('print("hi")');
  });

  it("seed files absent from the submission survive the overlay", () => {
    const { files } = resolveSeed({
      draftExists: false,
      submissionFiles,
      seedFiles,
    });
    const byPath = new Map(files.map((f) => [f.path, f]));
    expect(byPath.get("util.h")?.content).toBe("#pragma once");
  });

  it("keeps seed order first, replacements in place, additions appended last", () => {
    const { files } = resolveSeed({
      draftExists: false,
      submissionFiles,
      seedFiles,
    });
    expect(files.map((f) => f.path)).toEqual([
      "main.cpp",
      "util.h",
      "extra.py",
    ]);
  });

  it("draft short-circuits to empty files regardless of a submission", () => {
    expect(
      resolveSeed({ draftExists: true, submissionFiles, seedFiles }),
    ).toEqual({
      mode: "draft",
      files: [],
    });
  });

  it("draft short-circuits with a null submission too", () => {
    expect(
      resolveSeed({ draftExists: true, submissionFiles: null, seedFiles }),
    ).toEqual({
      mode: "draft",
      files: [],
    });
  });
});

describe("publicSeedFiles", () => {
  it("keeps only public files and defaults missing encodings to text", () => {
    const files = publicSeedFiles({
      Data: {
        Files: {
          "main.cpp": {
            Content: "int main() {}",
            Encoding: "text",
            Modifiable: true,
            Visibility: "Public",
          },
          "sprite.png": {
            Content: "base64-image",
            Encoding: "base64",
            Modifiable: false,
            Visibility: "Public",
          },
          "solution.cpp": {
            Content: "private solution",
            Modifiable: false,
            Visibility: "Private",
          },
          "readme.md": {
            Content: "instructions",
            Modifiable: true,
            Visibility: "Public",
          },
        },
      },
    } as never);

    expect(files).toEqual([
      {
        path: "main.cpp",
        content: "int main() {}",
        encoding: "text",
        modifiable: true,
      },
      {
        path: "sprite.png",
        content: "base64-image",
        encoding: "base64",
        modifiable: false,
      },
      {
        path: "readme.md",
        content: "instructions",
        encoding: "text",
        modifiable: true,
      },
    ]);
  });
});

describe("hasRestorableDraft", () => {
  const token = "learner-token";
  const presetId = "assessment-1";
  const currentKey = workspaceStorageKey(token, presetId);
  const legacyKey = workspaceStorageKey(legacyAssessmentToken(token), presetId);

  beforeEach(() => {
    window.localStorage.clear();
  });

  afterEach(() => {
    vi.unstubAllGlobals();
    window.localStorage.clear();
  });

  it("returns false during server rendering", () => {
    vi.stubGlobal("window", undefined);

    expect(hasRestorableDraft(token, presetId)).toBe(false);
  });

  it("returns false when neither current nor legacy storage has a draft", () => {
    expect(hasRestorableDraft(token, presetId)).toBe(false);
  });

  it("accepts a parseable current draft", () => {
    window.localStorage.setItem(currentKey, JSON.stringify({ files: [] }));

    expect(hasRestorableDraft(token, presetId)).toBe(true);
  });

  it("skips a corrupt current draft and restores a valid legacy draft", () => {
    window.localStorage.setItem(currentKey, "{corrupt");
    window.localStorage.setItem(legacyKey, JSON.stringify({ files: [] }));

    expect(hasRestorableDraft(token, presetId)).toBe(true);
  });

  it("returns false when every stored draft is corrupt", () => {
    window.localStorage.setItem(currentKey, "{corrupt");
    window.localStorage.setItem(legacyKey, "{also-corrupt");

    expect(hasRestorableDraft(token, presetId)).toBe(false);
  });
});
