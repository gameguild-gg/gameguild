import { NextResponse } from "next/server"
import { promises as fs } from "fs"
import path from "path"

// Base directory containing static project folders (each shaped like `projeto-*` with index.json + data.block-content-editor)
const PROJECTS_BASE_DIR = path.join(process.cwd(), "src", "data", "test-blocks")
const INDEX_FILENAME = "index.json"
const DATA_FILENAME = "data.block-content-editor"

const FOLDER_NAME_PATTERN = /^[a-zA-Z0-9._-]+$/

interface RawIndexJson {
  id?: string
  name?: string
  tags?: string[]
  size?: number
  hash?: string
  createdAt?: string
  updatedAt?: string
  storageType?: string
  preferences?: unknown
  metadata?: {
    size?: number
    hash?: string
    createdAt?: string
    updatedAt?: string
  }
}

export const GET = async (
  _req: Request,
  context: { params: Promise<{ folderName: string }> },
): Promise<NextResponse> => {
  try {
    const { folderName } = await context.params

    if (!folderName || !FOLDER_NAME_PATTERN.test(folderName)) {
      return NextResponse.json({ error: "Invalid folder name" }, { status: 400 })
    }

    const projectDir = path.resolve(PROJECTS_BASE_DIR, folderName)
    // Defense-in-depth against path traversal: ensure resolved path stays inside base dir
    if (!projectDir.startsWith(PROJECTS_BASE_DIR + path.sep)) {
      return NextResponse.json({ error: "Invalid folder path" }, { status: 400 })
    }

    const indexPath = path.join(projectDir, INDEX_FILENAME)
    const dataPath = path.join(projectDir, DATA_FILENAME)

    const [indexRaw, dataRaw] = await Promise.all([
      fs.readFile(indexPath, "utf8"),
      fs.readFile(dataPath, "utf8"),
    ])

    const parsed: RawIndexJson = JSON.parse(indexRaw)
    // Validate data is JSON
    JSON.parse(dataRaw)

    // Normalize: accept both legacy (flat) and new (nested metadata) index.json shapes
    const metadata = {
      size: parsed.metadata?.size ?? parsed.size ?? new Blob([dataRaw]).size,
      hash: parsed.metadata?.hash ?? parsed.hash ?? "",
      createdAt: parsed.metadata?.createdAt ?? parsed.createdAt ?? new Date(0).toISOString(),
      updatedAt: parsed.metadata?.updatedAt ?? parsed.updatedAt ?? new Date(0).toISOString(),
    }

    const project = {
      id: parsed.id ?? folderName,
      name: parsed.name ?? folderName,
      tags: Array.isArray(parsed.tags) ? parsed.tags : [],
      data: dataRaw,
      metadata,
      storageType: (parsed.storageType as "local" | "gameguild-cloud" | "google-drive") ?? "local",
      preferences: parsed.preferences,
    }

    return NextResponse.json({ project }, { status: 200 })
  } catch (error: any) {
    if (error?.code === "ENOENT") {
      return NextResponse.json({ error: "Project folder or required file not found" }, { status: 404 })
    }
    return NextResponse.json(
      { error: error instanceof Error ? error.message : "Unknown error" },
      { status: 500 },
    )
  }
}
