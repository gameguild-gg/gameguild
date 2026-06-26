import { NextResponse } from "next/server"
import { promises as fs } from "fs"
import path from "path"
import { elapsedMs, getRequestId, logWebRequest } from "@/lib/server/request-logging"

// Base directory containing static block-content-editor data files.
const FILES_BASE_DIR = path.join(process.cwd(), "src", "data")

// Allow common safe characters in path segments. No `..`, no absolute paths.
const SEGMENT_PATTERN = /^[a-zA-Z0-9._-]+$/

export const GET = async (
  req: Request,
  context: { params: Promise<{ path: string[] }> },
): Promise<NextResponse> => {
  const startedAt = performance.now()
  const requestId = getRequestId(req.headers)
  const requestPath = new URL(req.url).pathname

  const json = (body: unknown, status: number, error?: unknown): NextResponse => {
    const response = NextResponse.json(body, { status })
    response.headers.set("x-request-id", requestId)
    logWebRequest({
      event: status >= 500 ? "web.route.error" : "web.route.complete",
      method: req.method,
      path: requestPath,
      status,
      durationMs: elapsedMs(startedAt),
      requestId,
      ...(error ? { error } : {}),
    })
    return response
  }

  try {
    const { path: segments } = await context.params

    if (!Array.isArray(segments) || segments.length === 0) {
      return json({ error: "Missing file path" }, 400)
    }
    if (!segments.every((s) => SEGMENT_PATTERN.test(s))) {
      return json({ error: "Invalid path segment" }, 400)
    }

    const filePath = path.resolve(FILES_BASE_DIR, ...segments)
    if (!filePath.startsWith(FILES_BASE_DIR + path.sep)) {
      return json({ error: "Invalid file path" }, 400)
    }

    const raw = await fs.readFile(filePath, "utf8")
    // Validate it's a JSON-shaped block-content-editor file
    JSON.parse(raw)

    return json({ data: raw }, 200)
  } catch (error: any) {
    if (error?.code === "ENOENT") {
      return json({ error: "File not found" }, 404, error)
    }
    if (error instanceof SyntaxError) {
      return json({ error: "File is not valid JSON" }, 422, error)
    }
    return json({ error: error instanceof Error ? error.message : "Unknown error" }, 500, error)
  }
}
