import { afterEach, describe, expect, it, vi } from "vitest"

import type { ProjectExportMetadata } from "../../lib/interopAdapter/interop-types"
import { GoogleDriveService } from "./google-drive-service"

const metadata: ProjectExportMetadata = {
  id: "project-one",
  name: "Course notes",
  tags: ["teaching", "typescript"],
  size: 128,
  hash: "project-content-hash",
  createdAt: "2026-10-01T12:00:00.000Z",
  updatedAt: "2026-10-02T12:00:00.000Z",
  storageType: "google-drive",
  version: "4.0",
}

async function listProjectsWithIndex(index: unknown) {
  const list = vi.fn()
    .mockResolvedValueOnce({ result: { files: [{ id: "project-folder", name: "projeto-project-one" }] } })
    .mockResolvedValueOnce({ result: { files: [{ id: "project-index" }] } })
  const get = vi.fn().mockResolvedValue({ body: JSON.stringify(index) })
  vi.stubGlobal("gapi", { client: { setToken: vi.fn(), drive: { files: { list, get } } } })

  const service = GoogleDriveService.getInstance()
  service.setAuth(crypto.randomUUID(), "projects-root")
  const projects = await service.listProjects()

  expect(get).toHaveBeenCalledWith({ fileId: "project-index", alt: "media" })
  return projects
}

describe("GoogleDriveService project metadata", () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it("lists valid exported metadata without changing its tags or timestamps", async () => {
    expect(await listProjectsWithIndex(metadata)).toEqual([{
      id: metadata.id,
      name: metadata.name,
      data: "",
      tags: metadata.tags,
      size: metadata.size,
      hash: metadata.hash,
      createdAt: metadata.createdAt,
      updatedAt: metadata.updatedAt,
      driveFileId: "project-folder",
      storageType: "google-drive",
    }])
  })

  it("accepts an empty tag collection", async () => {
    const projects = await listProjectsWithIndex({ ...metadata, tags: [] })
    expect(projects).toHaveLength(1)
    expect(projects[0]?.tags).toEqual([])
  })

  it.each([
    { name: "a number", tags: ["teaching", 42] },
    { name: "an object", tags: ["teaching", { name: "typescript" }] },
    { name: "null", tags: [null] },
    { name: "a nested collection", tags: [["teaching"]] },
  ])("rejects metadata with $name in its tags instead of returning a malformed project", async ({ tags }) => {
    expect(await listProjectsWithIndex({ ...metadata, tags })).toEqual([])
  })

  it.each([
    null,
    [],
    "project",
    { ...metadata, tags: "teaching" },
    { ...metadata, id: 42 },
    { ...metadata, name: null },
    { ...metadata, size: "128" },
    { ...metadata, hash: null },
    { ...metadata, createdAt: null },
    { ...metadata, updatedAt: 42 },
  ])("keeps rejecting invalid metadata: %j", async (index) => {
    expect(await listProjectsWithIndex(index)).toEqual([])
  })
})
