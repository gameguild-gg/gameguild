# Development Setup

This guide covers how to set up the GameGuild development environment on all major platforms.

## Prerequisites

| Tool | Version | Purpose |
|------|---------|---------|
| [Docker](https://docs.docker.com/get-docker/) | Latest | Database and services |
| [Node.js](https://nodejs.org/) | >= 18 | Web frontend and tooling |
| [.NET SDK](https://dotnet.microsoft.com/download) | 8.0+ | API backend |
| npm | >= 10 | Package management (comes with Node.js) |

## Installation

### 1. Clone the repository

```bash
git clone https://github.com/gameguild-gg/gameguild.git
cd gameguild
```

### 2. Install Node.js (if needed)

We recommend using [nvm](https://github.com/nvm-sh/nvm) to manage Node.js versions:

```bash
# Install nvm
curl -o- https://raw.githubusercontent.com/nvm-sh/nvm/v0.40.0/install.sh | bash

# Install and use the correct Node.js version
nvm install 20
nvm use 20
```

### 3. Install dependencies

```bash
npm install
```

### 4. Configure environment variables

```bash
cp .env.example .env
```

Edit `.env` as needed. Default values work for local development with Docker.

Key defaults:

| Variable | Default | Description |
|----------|---------|-------------|
| `POSTGRES_HOST` | `localhost` | Database host |
| `POSTGRES_PORT` | `5432` | Database port |
| `POSTGRES_DB` | `postgres` | Database name |
| `POSTGRES_USER` | `postgres` | Database user |
| `POSTGRES_PASSWORD` | `postgres` | Database password |
| `ASPNETCORE_ENVIRONMENT` | `Development` | .NET environment |

For production, override all sensitive values (passwords, JWT secrets, API keys).

### 5. Start services

```bash
# Start the database and supporting services
docker compose up -d
```

### 6. Run the platform

```bash
# Start both API and web frontend
npm run dev
```

Or run them separately:

```bash
# Terminal 1 — API (.NET)
npm run dev:api

# Terminal 2 — Web (Next.js)
npm run dev:web
```

The web app will be available at `http://localhost:3000` and the API at `http://localhost:5295`.

## Project structure

```
gameguild/
├── apps/
│   ├── api/          # .NET API (C#)
│   ├── web/          # Next.js web application
│   └── website/      # Static website
├── packages/         # Shared TypeScript packages
│   ├── analytics/
│   ├── common/
│   ├── config/
│   ├── cookies/
│   ├── dotnet-wasm/
│   ├── emception/
│   ├── errors/
│   ├── rust-wasm/
│   ├── ui/
│   └── web3/
├── docs/             # Documentation
├── tools/            # Build and development tools
├── demos/            # Demo applications
├── scripts/          # Automation scripts
└── compose.yaml      # Docker Compose configuration
```

## Common commands

| Command | Description |
|---------|-------------|
| `npm run dev` | Start API and web in parallel |
| `npm run dev:api` | Start only the API |
| `npm run dev:web` | Start only the web frontend |
| `npm run build` | Build all workspaces |
| `npm run test` | Run tests across all workspaces |
| `npm run test:ci` | Run tests in CI mode with coverage |
| `npm run lint` | Lint all workspaces |
| `npm run format` | Format all workspaces |
| `npm run clean` | Remove all build artifacts and `node_modules` |

## Troubleshooting

### Docker issues

- Make sure Docker is running before starting services.
- On Linux, you may need `sudo` for Docker commands, or add your user to the `docker` group.
- If ports are already in use, check for other services on ports `5432`, `3000`, or `5295`.

### Node.js version mismatch

If you see unexpected errors, verify your Node.js version:

```bash
node -v  # Should be v18+ (v20 recommended)
npm -v   # Should be v10+
```

### Fresh start

To reset everything and start clean:

```bash
npm run clean
docker compose down -v
npm install
docker compose up -d
npm run dev
```
