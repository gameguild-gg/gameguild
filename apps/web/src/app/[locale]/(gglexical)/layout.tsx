import { Metadata }                 from 'next';
import React, { PropsWithChildren } from 'react';

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

export default async function Layout({ children }: PropsWithChildren): Promise<React.JSX.Element> {
  return (
    <div className="flex flex-col flex-1">{ children }</div>
  )
}
