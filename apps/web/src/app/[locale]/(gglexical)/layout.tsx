import type React from "react"
import type { Metadata } from "next"
import { Inter } from "next/font/google"
import "./globals.css"
import { Toaster } from "sonner"
import { ThemeProvider } from "@/components/theme/theme-provider"
import { themeScript } from "@/lib/theme-script"
import { TopMenu } from "@/components/editor/top-menu"

const inter = Inter({ subsets: ["latin"] })

export const metadata: Metadata = {
  title: "GameGuild Editor",
  description: "Rich text editor for game development community",
    generator: 'v0.dev',
  applicationName: "GameGuild Editor",
  keywords: [
    "game development",
    "editor",
    "rich text editor",
    "community",
    "game guild",
    "lexical",
    "react",
    "next.js",
    "javascript",
    "typescript",
    "open source",
    "collaboration",
    "game programming",
    "code learning"
  ],
  authors: [
    {
      name: "Miguel Eduardo Senna Moroni",
      url: "migmoroni.com",
    },
  ]
}

export default function RootLayout({
  children,
}: {
  children: React.ReactNode
}) {
  return (
    <html lang="en" suppressHydrationWarning>
      <head>
        <script dangerouslySetInnerHTML={{ __html: themeScript }} />
      </head>
      <body className={inter.className}>
        <ThemeProvider attribute="class" defaultTheme="system" enableSystem>
          <TopMenu />
          <main className="pt-16 container mx-auto py-10">{children}</main>
          <Toaster position="top-right" />
        </ThemeProvider>
      </body>
    </html>
  )
}
