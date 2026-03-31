# Contributing to GameGuild

First off, thank you for considering contributing! Every bit of help matters — whether it's fixing a typo, reporting a bug, or building a new feature.

Please read our [Code of Conduct](CODE_OF_CONDUCT.md) before participating.

## Where to start

### 1. Find something to work on

Browse the [open issues](https://github.com/gameguild-gg/gameguild/issues) and look for labels that match your interest:

| Label | What it means |
|-------|---------------|
| `good first issue` | Small, well-scoped tasks — great if this is your first contribution |
| `bug` | Something is broken and needs fixing |
| `feature` | A new capability or improvement |
| `documentation` | Docs, guides, or educational content |

We don't assign issues. If you see one you want to tackle, just open a pull request.

### 2. Set up the project locally

> Detailed instructions are in [DEVELOPMENT.md](DEVELOPMENT.md).

Quick version:

```bash
# Fork the repo on GitHub, then:
git clone https://github.com/<your-user>/gameguild.git
cd gameguild
npm install
cp .env.example .env
docker compose up -d
npm run dev
```

If you're new to forking and cloning, GitHub has a good [step-by-step guide](https://docs.github.com/en/get-started/quickstart/fork-a-repo).

### 3. Create a branch

Always work on a new branch, not directly on `main`:

```bash
git checkout -b my-change
```

Use a short, descriptive name like `fix-login-redirect` or `add-search-filter`.

### 4. Make your changes

The project is a monorepo with these main areas:

| Area | Path | Stack |
|------|------|-------|
| API | `apps/api/` | .NET (C#) |
| Web | `apps/web/` | Next.js |
| Shared packages | `packages/` | TypeScript |
| Docs | `docs/` | Markdown |

Pick the area that matches your change. If you're unsure where something lives, search the codebase or ask on [Discord](https://discord.com/invite/9CdJeQ2XKB?ref=gameguild.gg).

### 5. Commit and push

Write clear commit messages that explain *what* changed and *why*:

```bash
git add .
git commit -m "fix: prevent crash when user has no avatar"
git push origin my-change
```

### 6. Open a pull request

Go to your fork on GitHub and click **"Compare & pull request"**. In the PR description:

- Explain what you changed and why
- Link to the issue it solves (e.g., `Closes #42`)
- Enable **"Allow maintainer edits"** so we can help if needed

A maintainer will review your PR. We may suggest changes — that's normal and part of the process. Once everything looks good, we'll merge it.

## Types of contributions

You don't need to write code to help. Here are some ways to contribute:

- **Bug reports** — Found something broken? [Open an issue](https://github.com/gameguild-gg/gameguild/issues/new) with steps to reproduce it.
- **Feature ideas** — Have a suggestion? Open an issue describing what you'd like and why it would be useful.
- **Documentation** — Fix a typo, clarify an explanation, or write a new guide.
- **Code** — Fix bugs, add features, improve performance, write tests.
- **Reviews** — Read open pull requests and leave constructive feedback.

## Small changes

For quick fixes like typos or small doc edits, you can edit files directly on GitHub without cloning:

1. Navigate to the file on GitHub
2. Click the pencil icon (edit)
3. Make your change and submit a pull request

## Notes for Windows users

A few things to watch for when developing on Windows:

- **Line endings** — Windows uses `\r\n`. Use `\r?\n` in regular expressions to support both platforms. Node's `os.EOL` gives you the OS-specific line ending.
- **Paths** — Use `path.posix.join()` when building URLs or cross-platform paths, since `path.join()` returns backslashes on Windows.
- **Long filenames** — Git on Windows has a 260-character path limit. If you hit this, run: `git config --system core.longpaths true`

## Questions?

If anything is unclear or you need help, reach out on [Discord](https://discord.com/invite/9CdJeQ2XKB?ref=gameguild.gg). We're happy to help you get started.
