import type { Metadata } from "next";
import "./globals.css";
import { TopMenu } from "@/components/block-content-editor/top-menu";
import React from 'react';

export const metadata: Metadata = {
  title: "GameGuild Block Content Editor",
  description: "Block editor for game development community",
  generator: 'Next.js',
  applicationName: "GameGuild Editor",
  keywords: [
    "game development",
    "editor",
    "community",
    "game guild",
    "open source",
    "collaboration",
    "game programming",
    "code learning"
  ],
  authors: [
    {
      name: "Miguel Eduardo Senna Moroni",
      url: "https://gameguild.gg",
    },
  ]
}

export default function RootLayout({
  children,
}: {
  children: React.ReactNode
}) {
  return (
    <div>
      <TopMenu />
      <div className="flex flex-col flex-1 mt-20">{children}</div>
    </div>
  )
}
