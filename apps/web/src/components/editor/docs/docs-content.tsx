"use client"

import type React from "react"

interface DocsContentProps {
  activeSection: string
}

const contentMap: Record<string, { title: string; content: React.ReactNode }> = {
  introduction: {
    title: "About the GameGuild Editor",
    content: (
      <div className="prose dark:prose-invert max-w-none">
        <p className="text-lg text-gray-600 dark:text-gray-300 mb-6">
          The GameGuild Editor is a powerful, modern content creation tool designed specifically for game development
          communities.
        </p>

        <h2 id="what-is-gameguild-editor">What is the GameGuild Editor?</h2>
        <p>
          The GameGuild Editor is a rich text editor built with modern web technologies that allows you to create, edit,
          and manage content with ease. It features a clean, intuitive interface and powerful functionality for creating
          documentation, tutorials, game guides, and more.
        </p>

        <h2 id="key-features">Key Features</h2>
        <ul>
          <li>
            <strong>Rich Text Editing</strong> - Full-featured text formatting with support for headings, lists, links,
            and more
          </li>
          <li>
            <strong>Media Support</strong> - Embed images, videos, and other media content seamlessly
          </li>
          <li>
            <strong>Interactive Elements</strong> - Create quizzes, forms, and interactive content
          </li>
          <li>
            <strong>Code Blocks</strong> - Syntax-highlighted code blocks for technical documentation
          </li>
          <li>
            <strong>Project Management</strong> - Organize your content with projects and tags
          </li>
          <li>
            <strong>Dark/Light Theme</strong> - Choose your preferred editing environment
          </li>
        </ul>

        <h2 id="getting-started">Getting Started</h2>
        <p>
          To begin using the GameGuild Editor, simply navigate to the editor page and start creating your first project.
          The editor automatically saves your work locally, so you never have to worry about losing your progress.
        </p>

        <h3 id="system-requirements">System Requirements</h3>
        <p>The GameGuild Editor works in all modern web browsers and requires:</p>
        <ul>
          <li>A modern web browser (Chrome, Firefox, Safari, Edge)</li>
          <li>JavaScript enabled</li>
          <li>Local storage support for saving projects</li>
        </ul>

        <h3 id="browser-support">Browser Support</h3>
        <p>The editor is tested and supported on:</p>
        <ul>
          <li>Chrome 90+</li>
          <li>Firefox 88+</li>
          <li>Safari 14+</li>
          <li>Edge 90+</li>
        </ul>
      </div>
    ),
  },
  "about-editor": {
    title: "About the Editor",
    content: (
      <div className="prose dark:prose-invert max-w-none">
        <p className="text-lg text-gray-600 dark:text-gray-300 mb-6">
          Learn about the core concepts and architecture of the GameGuild Editor.
        </p>

        <h2 id="editor-architecture">Editor Architecture</h2>
        <p>
          The GameGuild Editor is built on top of Lexical, Facebook's modern rich text editor framework. This provides a
          solid foundation for extensible, performant text editing experiences.
        </p>

        <h2 id="core-components">Core Components</h2>
        <p>The editor consists of several key components working together:</p>

        <h3 id="lexical-framework">Lexical Framework</h3>
        <p>
          Lexical provides the core editing functionality, including text manipulation, selection handling, and
          undo/redo operations.
        </p>

        <h3 id="plugin-system">Plugin System</h3>
        <p>
          The editor's functionality is extended through a comprehensive plugin system that handles features like image
          uploads, code highlighting, and interactive elements.
        </p>

        <h2 id="storage-system">Storage System</h2>
        <p>Projects are stored locally using IndexedDB, providing fast access and offline capabilities.</p>

        <h3 id="indexeddb">IndexedDB Integration</h3>
        <p>
          The editor uses IndexedDB for persistent storage, allowing you to work offline and sync your projects across
          sessions.
        </p>
      </div>
    ),
  },
  "getting-started": {
    title: "Getting Started",
    content: (
      <div className="prose dark:prose-invert max-w-none">
        <p className="text-lg text-gray-600 dark:text-gray-300 mb-6">
          Quick start guide to begin using the GameGuild Editor effectively.
        </p>

        <h2 id="your-first-project">Your First Project</h2>
        <p>Creating your first project is simple:</p>
        <ol>
          <li>Navigate to the Editor page</li>
          <li>Click "Create New Project"</li>
          <li>Give your project a name and optional description</li>
          <li>Start writing!</li>
        </ol>

        <h2 id="basic-navigation">Basic Navigation</h2>
        <p>The editor interface is designed to be intuitive and easy to navigate.</p>

        <h3 id="editor-interface">Editor Interface</h3>
        <p>The main editor interface consists of:</p>
        <ul>
          <li>Toolbar with formatting options</li>
          <li>Main editing area</li>
          <li>Project sidebar for navigation</li>
        </ul>

        <h3 id="toolbar-overview">Toolbar Overview</h3>
        <p>The toolbar provides quick access to common formatting options and features.</p>

        <h2 id="saving-and-loading">Saving and Loading</h2>
        <p>Your work is automatically saved as you type, but you can also manually save projects.</p>

        <h3 id="auto-save">Auto-save Feature</h3>
        <p>The editor automatically saves your changes every few seconds to prevent data loss.</p>
      </div>
    ),
  },
  "text-formatting": {
    title: "Text Formatting",
    content: (
      <div className="prose dark:prose-invert max-w-none">
        <p className="text-lg text-gray-600 dark:text-gray-300 mb-6">
          Learn how to format text using the rich text editing features.
        </p>

        <h2 id="basic-formatting">Basic Formatting</h2>
        <p>The editor supports all standard text formatting options:</p>

        <h3 id="bold-italic-underline">Bold, Italic, Underline</h3>
        <p>Use the toolbar buttons or keyboard shortcuts:</p>
        <ul>
          <li>
            <strong>Bold</strong>: Ctrl/Cmd + B
          </li>
          <li>
            <em>Italic</em>: Ctrl/Cmd + I
          </li>
          <li>
            <u>Underline</u>: Ctrl/Cmd + U
          </li>
        </ul>

        <h3 id="headings">Headings</h3>
        <p>Create headings using the heading dropdown or markdown syntax:</p>
        <ul>
          <li># Heading 1</li>
          <li>## Heading 2</li>
          <li>### Heading 3</li>
        </ul>

        <h2 id="advanced-styling">Advanced Styling</h2>
        <p>Beyond basic formatting, the editor offers advanced styling options.</p>

        <h3 id="custom-styles">Custom Styles</h3>
        <p>Apply custom styles and classes to your content for advanced formatting.</p>

        <h2 id="keyboard-shortcuts">Keyboard Shortcuts</h2>
        <p>Master these keyboard shortcuts for efficient editing:</p>
        <ul>
          <li>Ctrl/Cmd + Z: Undo</li>
          <li>Ctrl/Cmd + Y: Redo</li>
          <li>Ctrl/Cmd + A: Select All</li>
          <li>Ctrl/Cmd + S: Save Project</li>
        </ul>
      </div>
    ),
  },
  // Add more content sections as needed...
}

export function DocsContent({ activeSection }: DocsContentProps) {
  const content = contentMap[activeSection] || contentMap["introduction"]

  return (
    <article>
      <header className="mb-8">
        <h1 className="text-3xl font-bold text-gray-900 dark:text-white mb-4">{content.title}</h1>
      </header>

      <div className="text-gray-700 dark:text-gray-300">{content.content}</div>
    </article>
  )
}
