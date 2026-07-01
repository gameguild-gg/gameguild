import type { Metadata } from "next";
import "./globals.css";
import { RouteLayoutContent } from "./route-layout-content";
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
    <RouteLayoutContent>
      {children}
    </RouteLayoutContent>
  )
}
