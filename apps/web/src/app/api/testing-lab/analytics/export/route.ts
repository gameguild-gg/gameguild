import { getTestingLabAnalyticsCsv } from "@/lib/testing-lab";
import { resolveTestingLabAnalyticsPeriod } from "@/lib/testing-lab/analytics-period";
import { type NextRequest, NextResponse } from "next/server";

export async function GET(request: NextRequest): Promise<Response> {
  const from = request.nextUrl.searchParams.get("from") ?? undefined;
  const to = request.nextUrl.searchParams.get("to") ?? undefined;
  const period = resolveTestingLabAnalyticsPeriod({ from, to });

  if (!from || !to || period.range !== "custom") {
    return NextResponse.json(
      { error: "A valid analytics period is required." },
      { status: 400 },
    );
  }

  const result = await getTestingLabAnalyticsCsv({
    fromDate: period.fromDate,
    toDate: period.toDate,
  });

  if (result.data === null) {
    return NextResponse.json(
      { error: result.issue ?? "Testing Lab analytics export failed." },
      { status: 502 },
    );
  }

  return new Response(result.data, {
    status: 200,
    headers: {
      "Cache-Control": "private, no-store",
      "Content-Disposition": `attachment; filename="testing-lab-${period.fromInput}-to-${period.toInput}.csv"`,
      "Content-Type": "text/csv; charset=utf-8",
    },
  });
}
