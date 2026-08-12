import { auth } from "@/auth";
import { routing } from "@/i18n/routing";
import createMiddleware from "next-intl/middleware";
import type { NextRequest } from "next/server";

const intlMiddleware = createMiddleware(routing);

export default auth((request) =>
  intlMiddleware(request as unknown as NextRequest),
);

export const config = {
  matcher: "/((?!api|trpc|_next|_vercel|.*\\..*).*)",
};
