<div align="center">

<!-- Replace documentation/banner.png with your actual banner image -->
<a href="https://gameguild.gg">
<img src="documentation/banner.png" alt="GameGuild" width="100%" />
</a>

<br/>

![GitHub Stars](https://img.shields.io/github/stars/gameguild-gg/gameguild?style=social)
![Contributors](https://img.shields.io/github/contributors/gameguild-gg/gameguild)
![GitHub Issues](https://img.shields.io/github/issues/gameguild-gg/gameguild)
![Last Commit](https://img.shields.io/github/last-commit/gameguild-gg/gameguild)
![Website Uptime 30d](<https://status.gameguild.gg/api/badge/1/uptime/720?label=Uptime%20Web%20(30d)>)
![Api Uptime 30d](<https://status.gameguild.gg/api/badge/3/uptime/720?label=Uptime%20Api%20(30d)>)

# GameGuild

### The game industry was built by communities. We're building the platform they deserve.

Most game developers work alone or in small teams, without access to the networks, mentoring, and infrastructure that studios take for granted. GameGuild exists to change that.

We are an **open source platform** where game developers **find each other**, **learn together**, and **ship their games** — without gatekeepers, without paywalls on knowledge, and without giving up ownership of their work.

[Website](https://gameguild.gg) · [Discord](https://discord.com/invite/9CdJeQ2XKB?ref=gameguild.gg) · [Issues](https://github.com/gameguild-gg/gameguild/issues) · [Contributing](CONTRIBUTING.md)

</div>

---

## Table of contents

- [Why GameGuild](#why-gameguild)
- [Screenshots](#screenshots)
- [Architecture](#architecture)
- [Tech stack](#tech-stack)
- [Quick start](#quick-start)
- [Get involved](#get-involved)
- [FAQ](#faq)
- [Community](#community)
- [Project health](#project-health)
- [License](#license)
- [Legal](#legal)

## Why GameGuild

Talent is everywhere. Opportunity is not. Thousands of skilled developers never finish a game — not because they lack ability, but because they lack connection: a mentor to guide them, a team to complement their skills, a community to test and support their work.

GameGuild is the infrastructure for that connection:

<table>
<tr>
<td align="center" valign="top" width="33%">
<h3>Collaborate</h3>
Find teammates, join workshops, attend lectures, and coordinate projects across time zones.
</td>
<td align="center" valign="top" width="33%">
<h3>Learn</h3>
Access mentoring from experienced developers. Share knowledge through courses, articles, and live sessions.
</td>
<td align="center" valign="top" width="33%">
<h3>Launch</h3>
Showcase your game, get playtesting feedback, and reach players. Keep ownership of everything you create.
</td>
</tr>
</table>

This isn't a marketplace. It's a guild — a place where developers grow by helping each other grow.

## Screenshots

A quick look at what you can do inside GameGuild.

<table>
<tr>
<td align="center" width="50%">
<b>Home</b><br/>
<img src="documentation/Page1.png" alt="GameGuild home" width="100%"/>
</td>
<td align="center" width="50%">
<b>Course management</b><br/>
<img src="documentation/screenshots/course-management.png" alt="Course management dashboard" width="100%"/>
</td>
</tr>
<tr>
<td align="center" width="50%">
<b>Online course editor</b><br/>
<img src="documentation/screenshots/course-editor.png" alt="Online course editor" width="100%"/>
</td>
<td align="center" width="50%">
<b>Online IDE</b><br/>
<img src="documentation/screenshots/online-ide.png" alt="Browser-based IDE" width="100%"/>
</td>
</tr>
</table>

## Architecture

Monorepo managed with npm workspaces.

| Component | Path | Stack |
|-----------|------|-------|
| API | `apps/api/` | .NET (C#) |
| Web | `apps/web/` | Next.js |
| Shared packages | `packages/` | TypeScript |
| Documentation | `docs/` | Markdown |

## Tech stack

Click any badge to visit the project's homepage.

**Frontend**

[![Next.js](https://img.shields.io/badge/Next.js_16-000000?style=for-the-badge&logo=nextdotjs&logoColor=white)](https://nextjs.org/)
[![React](https://img.shields.io/badge/React_19-61DAFB?style=for-the-badge&logo=react&logoColor=black)](https://react.dev/)
[![TypeScript](https://img.shields.io/badge/TypeScript-3178C6?style=for-the-badge&logo=typescript&logoColor=white)](https://www.typescriptlang.org/)
[![Tailwind CSS](https://img.shields.io/badge/Tailwind_CSS-06B6D4?style=for-the-badge&logo=tailwindcss&logoColor=white)](https://tailwindcss.com/)
[![shadcn/ui](https://img.shields.io/badge/shadcn%2Fui-000000?style=for-the-badge&logo=shadcnui&logoColor=white)](https://ui.shadcn.com/)

**Backend**

[![.NET](https://img.shields.io/badge/.NET_9-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![HotChocolate](https://img.shields.io/badge/HotChocolate-E10098?style=for-the-badge&logo=graphql&logoColor=white)](https://chillicream.com/docs/hotchocolate)
[![Entity Framework Core](https://img.shields.io/badge/EF_Core-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)](https://learn.microsoft.com/en-us/ef/core/)
[![GraphQL](https://img.shields.io/badge/GraphQL-E10098?style=for-the-badge&logo=graphql&logoColor=white)](https://graphql.org/)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-316192?style=for-the-badge&logo=postgresql&logoColor=white)](https://www.postgresql.org/)

**Platform & runtime**

[![WebAssembly](https://img.shields.io/badge/WebAssembly-654FF0?style=for-the-badge&logo=webassembly&logoColor=white)](https://webassembly.org/)
[![Docker](https://img.shields.io/badge/Docker-2496ED?style=for-the-badge&logo=docker&logoColor=white)](https://www.docker.com/)
[![GitHub Actions](https://img.shields.io/badge/GitHub_Actions-2088FF?style=for-the-badge&logo=githubactions&logoColor=white)](https://github.com/features/actions)

**Languages supported by the online IDE**

[![C](https://img.shields.io/badge/C-00599C?style=for-the-badge&logo=c&logoColor=white)](https://en.cppreference.com/w/c)
[![C++](https://img.shields.io/badge/C%2B%2B-00599C?style=for-the-badge&logo=cplusplus&logoColor=white)](https://isocpp.org/)
[![C#](https://img.shields.io/badge/C%23-239120?style=for-the-badge&logo=csharp&logoColor=white)](https://learn.microsoft.com/en-us/dotnet/csharp/)
[![Python](https://img.shields.io/badge/Python-3776AB?style=for-the-badge&logo=python&logoColor=white)](https://www.python.org/)
[![JavaScript](https://img.shields.io/badge/JavaScript-F7DF1E?style=for-the-badge&logo=javascript&logoColor=black)](https://developer.mozilla.org/docs/Web/JavaScript)
[![TypeScript](https://img.shields.io/badge/TypeScript-3178C6?style=for-the-badge&logo=typescript&logoColor=white)](https://www.typescriptlang.org/)
[![Ruby](https://img.shields.io/badge/Ruby-CC342D?style=for-the-badge&logo=ruby&logoColor=white)](https://www.ruby-lang.org/)
[![PHP](https://img.shields.io/badge/PHP-777BB4?style=for-the-badge&logo=php&logoColor=white)](https://www.php.net/)
[![Lua](https://img.shields.io/badge/Lua-2C2D72?style=for-the-badge&logo=lua&logoColor=white)](https://www.lua.org/)
[![WAT](https://img.shields.io/badge/WAT-654FF0?style=for-the-badge&logo=webassembly&logoColor=white)](https://webassembly.github.io/spec/core/text/index.html)
[![SQL](https://img.shields.io/badge/SQL-4479A1?style=for-the-badge&logo=postgresql&logoColor=white)](https://en.wikipedia.org/wiki/SQL)

## Quick start

> Full setup for all platforms: [DEVELOPMENT.md](DEVELOPMENT.md)

**Prerequisites:** [Docker](https://docs.docker.com/get-docker/) · [Node.js](https://nodejs.org/) >= 18 · [.NET SDK](https://dotnet.microsoft.com/download)

```bash
git clone https://github.com/gameguild-gg/gameguild.git
cd gameguild
npm install
cp .env.example .env
docker compose up -d
npm run dev
```

Web: `http://localhost:3000` — API: `http://localhost:5295`

## Get involved

GameGuild is built by its community. There's a place for you whether you write code, design interfaces, create content, or just report bugs.

1. Read the [Contributing Guide](CONTRIBUTING.md)
2. Pick an issue labeled [`good first issue`](https://github.com/gameguild-gg/gameguild/labels/good%20first%20issue)
3. Fork, branch, and open a pull request

By contributing, you agree to the [CLA](CONTRIBUTOR_LICENSE_AGREEMENT.md.md). Please review our [Code of Conduct](CODE_OF_CONDUCT.md).

## FAQ

<details>
<summary><b>Is GameGuild really free?</b></summary>

Yes. The platform is open source under the [MIT License](LICENSE-MIT.md) and the hosted version at [gameguild.gg](https://gameguild.gg) is free to use. A [Commercial License](LICENSE.md) exists only for organizations that need contractual support and guarantees.

</details>

<details>
<summary><b>Can I self-host GameGuild?</b></summary>

Yes. The entire stack runs locally with `docker compose up -d` and can be deployed to any infrastructure you control. See [DEVELOPMENT.md](DEVELOPMENT.md) for details.

</details>

<details>
<summary><b>How is this different from itch.io, or online course platforms?</b></summary>

Itch.io is for distribution, and course platforms are for lessons. GameGuild combines **collaboration, learning, and launching** in one open platform designed specifically for game developers — with an online IDE, course editor, playtesting tools, and team coordination built in.

</details>

<details>
<summary><b>Do I need to know C# and TypeScript to contribute?</b></summary>

No. There are [`good first issue`](https://github.com/gameguild-gg/gameguild/labels/good%20first%20issue) tasks across docs, design, content, QA, and translations. Code contributions are welcome in the language of the area you want to touch.

</details>

<details>
<summary><b>Where do I report a security issue?</b></summary>

Please follow the responsible disclosure process in [SECURITY.md](SECURITY.md). Do not open public issues for security vulnerabilities.

</details>

## Community

Join us where the conversation happens:

<div style="display: flex; flex-wrap: wrap; gap: 12px; align-items: center;">
  <a href="https://discord.com/invite/9CdJeQ2XKB?ref=gameguild.gg" title="Discord"><img width="40" src="https://img.icons8.com/color/48/000000/discord-logo.png" alt="Discord"/></a>
  <a href="https://instagram.com/" title="Instagram"><img width="40" src="https://img.icons8.com/color/48/000000/instagram-new.png" alt="Instagram"/></a>
  <a href="https://www.youtube.com/@AwesomeGamedevGuild" title="YouTube"><img width="40" src="https://img.icons8.com/color/48/000000/youtube-play.png" alt="YouTube"/></a>
  <a href="https://chat.whatsapp.com/CAboWKtosP673f9EkzxKNb" title="WhatsApp"><img width="40" src="https://img.icons8.com/color/48/000000/whatsapp.png" alt="WhatsApp"/></a>
  <a href="https://x.com/GameGuildDev" title="X"><img width="40" src="https://img.icons8.com/?size=100&id=phOKFKYpe00C&format=png&color=000000" alt="X"/></a>
  <a href="https://bsky.app/profile/gameguild.bsky.social" title="BlueSky"><img width="40" src="https://img.icons8.com/?size=100&id=3ovMFy5JDSWq&format=png&color=000000" alt="BlueSky"/></a>
  <a href="https://mastodon.social/@game-guild" title="Mastodon"><img width="40" src="https://img.icons8.com/?size=100&id=SjG6BzZwdP2-&format=png&color=000000" alt="Mastodon"/></a>
  <a href="https://www.tiktok.com/@awesomegameguild" title="TikTok"><img width="40" src="https://img.icons8.com/?size=100&id=3veRWJpxPPDH&format=png&color=000000" alt="TikTok"/></a>
  <a href="https://www.twitch.tv/awesomegamedevguild" title="Twitch"><img width="40" src="https://img.icons8.com/?size=100&id=MFZCdvQbJtV1&format=png&color=000000" alt="Twitch"/></a>
  <a href="http://gameguild.itch.io/" title="Itch.io"><img width="40" src="https://img.icons8.com/?size=100&id=XrWrgAx9pAYM&format=png&color=000000" alt="Itch.io"/></a>
  <a href="https://gamejolt.com/@game-guild" title="GameJolt"><img width="40" src="https://img.icons8.com/?size=100&id=QxjoLwAXiCXT&format=png&color=000000" alt="GameJolt"/></a>
</div>

<sub>Icons by <a href="https://icons8.com/">Icons8</a></sub>

## Project health

### Star history

How the project's popularity has grown over time.

[![Star History Chart](https://api.star-history.com/svg?repos=gameguild-gg/gameguild&type=Date)](https://star-history.com/#gameguild-gg/gameguild&Date)

### Repository evolution (Gource)

Animated visualization of the repository history — every file and contributor over time. Generated with [Gource](https://gource.io/).

[![Gource](https://gameguild-gg.github.io/gameguild/gource.gif)](https://gameguild-gg.github.io/gameguild/gource.mp4)

### Branching model (GitFlow)

We follow the [GitFlow](https://nvie.com/posts/a-successful-git-branching-model/) branching model: `main` holds production-ready code, `develop` integrates ongoing work, and short-lived `feature/*`, `release/*`, and `hotfix/*` branches feed into them. See our full workflow guide: [link em breve]().

![gitflow.png](documentation/gitflow.png)

## License

Dual-licensed:

- **[MIT License](LICENSE-MIT.md)** — Free for any use. No support or warranty.
- **[Commercial License](LICENSE.md)** — For organizations that need contractual support, maintenance, and guarantees.

Questions? [gameguild.gg/contact](https://gameguild.gg/contact)

## Legal

- [CLA](CLA.md) — Contributor License Agreement
- [Code of Conduct](CODE_OF_CONDUCT.md)
- [Security Policy](SECURITY.md)
- [Legal](LEGAL.md) — Intellectual property and disputes
