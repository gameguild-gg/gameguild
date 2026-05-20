import { NextResponse } from "next/server"
import { promises as fs } from "fs"
import path from "path"

// Base directory containing static block-content-editor data files.
const FILES_BASE_DIR = path.join(process.cwd(), "src", "data")

// Allow common safe characters in path segments. No `..`, no absolute paths.
const SEGMENT_PATTERN = /^[a-zA-Z0-9._-]+$/

export const GET = async (
  _req: Request,
  context: { params: Promise<{ path: string[] }> },
): Promise<NextResponse> => {
  try {
    const { path: segments } = await context.params

    if (!Array.isArray(segments) || segments.length === 0) {
      return NextResponse.json({ error: "Missing file path" }, { status: 400 })
    }
    if (!segments.every((s) => SEGMENT_PATTERN.test(s))) {
      return NextResponse.json({ error: "Invalid path segment" }, { status: 400 })
    }

    const filePath = path.resolve(FILES_BASE_DIR, ...segments)
    if (!filePath.startsWith(FILES_BASE_DIR + path.sep)) {
      return NextResponse.json({ error: "Invalid file path" }, { status: 400 })
    }

    const raw = await fs.readFile(filePath, "utf8")
    // Validate it's a JSON-shaped block-content-editor file
    JSON.parse(raw)

    return NextResponse.json({ data: raw }, { status: 200 })
  } catch (error: any) {
    if (error?.code === "ENOENT") {
      return NextResponse.json({ error: "File not found" }, { status: 404 })
    }
    if (error instanceof SyntaxError) {
      return NextResponse.json({ error: "File is not valid JSON" }, { status: 422 })
    }
    return NextResponse.json(
      { error: error instanceof Error ? error.message : "Unknown error" },
      { status: 500 },
    )
  }
}
