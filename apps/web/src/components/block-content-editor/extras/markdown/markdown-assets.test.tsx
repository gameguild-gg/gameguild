import { AssetsProvider } from "@game-guild/assets/react";
import type { AssetRepository } from "@game-guild/assets";
import { render, screen, waitFor } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";

import { MarkdownRenderer } from "./markdown-renderer";

describe("MarkdownRenderer assets", () => {
  it("resolves an asset URI through the scoped repository", async () => {
    const createObjectUrl = vi.fn().mockResolvedValue({
      url: "https://cdn.example.test/assets/lesson-image.png",
      release: vi.fn(),
    });
    const repository = { createObjectUrl } as unknown as AssetRepository;
    const assetUri = "asset://aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa";

    const consoleError = vi.spyOn(console, "error").mockImplementation(() => undefined);

    render(
      <AssetsProvider
        repository={repository}
        scope={{ type: "ProgramContent", id: "lesson-1" }}
      >
        <MarkdownRenderer content={`![Lesson diagram](${assetUri})`} />
      </AssetsProvider>,
    );

    await waitFor(() => {
      expect(screen.getByRole("img", { name: "Lesson diagram" })).toHaveAttribute(
        "src",
        "https://cdn.example.test/assets/lesson-image.png",
      );
    });
    expect(createObjectUrl).toHaveBeenCalledWith(assetUri);
    expect(consoleError).not.toHaveBeenCalled();
    consoleError.mockRestore();
  });
});
