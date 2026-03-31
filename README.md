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

![screenshot](documentation/Page1.png)

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

## Architecture

Monorepo managed with npm workspaces.

| Component | Path | Stack |
|-----------|------|-------|
| API | `apps/api/` | .NET (C#) |
| Web | `apps/web/` | Next.js |
| Shared packages | `packages/` | TypeScript |
| Documentation | `docs/` | Markdown |

![TypeScript](https://img.shields.io/badge/TypeScript-3178C6?style=for-the-badge&logo=typescript&logoColor=white)
![React](https://img.shields.io/badge/React-61DAFB?style=for-the-badge&logo=react&logoColor=black)
![Next.js](https://img.shields.io/badge/Next.js-000000?style=for-the-badge&logo=nextdotjs&logoColor=white)
![Tailwind CSS](https://img.shields.io/badge/Tailwind_CSS-06B6D4?style=for-the-badge&logo=tailwindcss&logoColor=white)
![.NET](https://img.shields.io/badge/.NET_9-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![GraphQL](https://img.shields.io/badge/GraphQL-E10098?style=for-the-badge&logo=graphql&logoColor=white)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-316192?style=for-the-badge&logo=postgresql&logoColor=white)
![Docker](https://img.shields.io/badge/Docker-2496ED?style=for-the-badge&logo=docker&logoColor=white)
![WebAssembly](https://img.shields.io/badge/WebAssembly-654FF0?style=for-the-badge&logo=webassembly&logoColor=white)
![GitHub Actions](https://img.shields.io/badge/GitHub_Actions-2088FF?style=for-the-badge&logo=githubactions&logoColor=white)

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

By contributing, you agree to the [CLA](CLA.md). Please review our [Code of Conduct](CODE_OF_CONDUCT.md).

## Community

Join us where the conversation happens:

<div style="display: flex; flex-wrap: wrap; gap: 12px; align-items: center;">
  <a href="https://discord.com/invite/9CdJeQ2XKB?ref=gameguild.gg" title="Discord"><img width="40" src="https://img.icons8.com/color/48/000000/discord-logo.png" alt="Discord"/></a>
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

[![Star History Chart](https://api.star-history.com/svg?repos=gameguild-gg/gameguild&type=Date)](https://star-history.com/#gameguild-gg/gameguild&Date)

[![Gource](https://gameguild-gg.github.io/gameguild/gource.gif)](https://gameguild-gg.github.io/gameguild/gource.mp4)

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
