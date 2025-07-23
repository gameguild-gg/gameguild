import type React from "react"
import type { Metadata } from "next"
import { Inter } from "next/font/google"
import "./globals.css"
import { Toaster } from "sonner"

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
    <html lang="en">
      <body className={inter.className}>
        {children}
        <Toaster position="top-right" />
      </body>
    </html>
  )
}
