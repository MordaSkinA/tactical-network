# Tacnet (GvG Tactical Network)

A small web app that lets guild members report enemy positions, call for help, and receive orders during a large PvP battle by clicking buttons instead of talking or typing over voice chat.

[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![SignalR](https://img.shields.io/badge/SignalR-realtime-2f6fb2)](https://learn.microsoft.com/aspnet/core/signalr)
[![Status](https://img.shields.io/badge/status-no%20longer%20maintained-lightgrey)]()
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://github.com/MordaSkinA/tactical-network/blob/main/LICENSE)

> **Project status:** Tacnet was used by a real guild in live fights (up to 30 people online at once, about 60 in the roster). It was closed by agreement with the guild leader and is no longer maintained. The code stays public as a reference.

## Why this exists

In our guild's GvG fights, coordination happens over Discord voice, but in any given fight only three or four people are actually talking. Not because they don't have information, they usually do. It's things like not being comfortable speaking English out loud under pressure, general shyness on comms, or a bad mic setup they don't want to add noise with.

The result is a commander flying half-blind while a bunch of people who saw exactly what happened stay quiet.

Tacnet's bet is that if reporting something takes one or two clicks instead of keying up and finding the words, more people will actually do it. It's not trying to replace voice. It's a second channel for the people voice doesn't work for.

## Stack

- ASP.NET Core 8 (minimal APIs) and one SignalR hub
- PostgreSQL, accessed with Dapper
- Static front end in `wwwroot/` (plain HTML and JavaScript, English and Vietnamese UI)
- Docker, Docker Compose, Ansible, GitHub Actions
- Prometheus metrics via `prometheus-net`
- xUnit, `WebApplicationFactory`, Moq for tests

## Quickstart

Tacnet needs PostgreSQL. The app will not start without a connection string.

### Docker (docker compose)

1. Copy `.env.example` to `.env` and fill it in. Compose refuses to start without `POSTGRES_PASSWORD`, `ADMIN_SEED_PASSWORD` and `SESSION_SIGNING_KEY`:

   ```env
   POSTGRES_PASSWORD=...
   ADMIN_SEED_PASSWORD=...
   # 32 random bytes, base64: openssl rand -base64 32
   SESSION_SIGNING_KEY=...
   # only for Discord login:
   DISCORD_CLIENT_SECRET=...
   ```

2. Start everything:

   ```bash
   docker compose up -d --build
   ```

3. Open `http://localhost:8080`. PostgreSQL is published on `127.0.0.1:5433`. On the first start of an empty database volume, compose runs `db/init/001_users.sql` and `db/init/002_v3_migration.sql`, which create all tables.

**Discord login in Docker.** `docker-compose.yml` passes only `DiscordClientSecret` to the app. To use Discord login, also add `DiscordClientId`, `DiscordRedirectUri` and `DiscordGuildId` to the `app` service's `environment` section (the keys in `.env.example` show what is needed).

The compose file also starts Prometheus (`:9090`) and Grafana (`:3000`). See [Observability](#observability) for what works out of the box.

### Local (dotnet run)

You need the .NET 8 SDK and a PostgreSQL instance you can reach.

1. Create `appsettings.json` in the project folder (it is git-ignored) with at least:

   ```json
   {
     "ConnectionStrings": {
       "Tacnet": "Host=localhost;Database=tacnet;Username=...;Password=..."
     },
     "AdminSeedPassword": "..."
   }
   ```

   Optional keys: `AdminSeedLogin` (default `admin`), `SessionSigningKey` (if omitted, a key is generated into `session-signing.key`), `SessionTtlDays` (default 30), `AllowedOrigins` (list of allowed CORS origins), and for Discord login `DiscordClientId`, `DiscordClientSecret`, `DiscordRedirectUri`, `DiscordGuildId`.

2. Create the tables by running `db/init/001_users.sql` and then `db/init/002_v3_migration.sql` against your database.

3. Run:

   ```bash
   dotnet run
   ```

   The console output shows the actual URL.

### First login

If the `users` table is empty at startup, an admin account is created from `AdminSeedLogin` / `AdminSeedPassword` (in Docker: `ADMIN_SEED_PASSWORD`). Log in with it and change the password right away.

### Letting others test a local instance

Put it behind a tunnel, for example `ngrok http <the URL printed by dotnet run>`.

## Basic usage

There are five roles, and what you see depends on which one you are logged in as:

- **Player** sees the current order (and any goal order aimed at them) for their team, and has buttons to report enemy role sightings or call an SOS.
- **Leader** (team leader) sees the observer view, a live feed of everything their own team reports, and can push team orders and goal orders to their own team. The commander can still override with a guild-wide order.
- **Commander** sees the guild-wide feed across every team and pushes team orders and goal orders to anyone.
- **Admin** manages the roster (teams T1 to T6, each assigned Defense / Attack / Flex), each member's role, bulwark and build, and creates accounts.
- **Developer** has the same goal-order and macro permissions as Commander and Admin.

The pages line up with the roles:

- `/index.html`: login
- `/menu.html`: sends you to the right panel for your role
- `/player.html`, `/observer.html`, `/dashboard.html`, `/admin.html`: the role panels
- `/replay.html`: battle replays

Everything flows over a single SignalR hub at `/battleHub`: a player clicks a button, it is broadcast, and it shows up on the relevant observer and dashboard feeds in real time. SignalR groups (`squad-{id}`, `role-commander`, `role-admin`) make sure a message reaches only the people who need it.

A background service (`BattleTimerService`) sends jungle and boss spawn reminders while a battle is running.

Roles and team IDs are attached to the session on the server and are never trusted from the client.

## Goal orders

Alongside the regular team orders, there is a separate free-text channel for ad hoc callouts: **goal orders**. Instead of targeting a team, a goal order is aimed at whoever currently matches a filter, for example "Focus on enemy healer" sent to everyone whose Build is `Twinblades`. Filters can be combined:

- **Build**: free text, matched against the `Build` field admins set per roster member
- **Role**: DPS / Tank / Healer
- **Side**: Attack / Defense / Flex
- **Team**: same team targeting as regular orders, to narrow further

A blank filter means no restriction on that axis. Reserves are excluded by default unless the Reserves team is targeted explicitly. A goal order can carry a countdown timer (seconds) and a **battle phase** (Phase 1/2/3, set manually by the commander). Only players who match the filter receive it, and it is mirrored to the commander/admin feed regardless.

Commander, Admin, Developer and Leader can issue goal orders. A Leader's goal order is restricted server-side to their own team. Frequently used goal orders can be saved as named **macros** (only Commander, Admin and Developer can create or delete them; everyone above can fire an existing one).

The older `FallBack`, `Rotate` and `ProtectHealer` team order types were removed; goal orders replace them.

## Architecture

```
Program.cs            host setup, DI, middleware, CORS, metrics
Hubs/BattleHub.cs     the single SignalR hub (reports, orders, roster, accounts, logs, Discord messages)
Endpoints/            /api/auth, /api/auth/discord, /api/replays
Services/             IDiscordAuthService, BattleTimerService, response templates
State/                stores: accounts, sessions, roster, macros, presets, webhooks, battle state, rate limiting
Data/                 Npgsql connection factory
db/init/              SQL schema
tools/                one-off JSON to PostgreSQL migration tool
Tacnet.Tests/         integration tests
```

### What is stored where

| Data | Storage | Survives app restart | In Docker |
|---|---|---|---|
| User accounts | PostgreSQL, table `users` | yes | yes (`pgdata` volume) |
| Roster (squads, members) | PostgreSQL, tables `squads`, `squad_members` | yes | yes |
| Order macros, goal-order macros | PostgreSQL | yes | yes |
| Member presets, Discord webhooks | PostgreSQL | yes | yes |
| Emoji settings | `emoji-settings.json` | yes | yes (bind mount in compose) |
| Battle log snapshots (replays) | `logs/battle-*.json` and `.txt` | yes | yes (bind mount `./logs`) |
| Live battle state and event history | process memory (`InMemoryBattleState`) | **no** | no |
| Pending Discord logins, connection tracking, revoked session tokens | process memory | no | no |

Live battle state stays in memory on purpose: the hub needs fast reads and writes during a fight, and a battle is short-lived. Each battle is saved to `logs/` as a snapshot for replay.

### Security notes

- Session tokens are `payload.signature`, signed with HMAC-SHA256 using `SessionSigningKey`, with an expiry and a revocation list. The revocation list is in memory, so logging out does not survive a restart.
- Login attempts are limited to 5 per 30 seconds per IP, and hub actions (report, order, SOS) to 5 per 3 seconds per connection.
- Replay downloads require a Commander, Admin or Developer session and strip directory components from the file name (`Path.GetFileName`).
- The replay download endpoint takes the token from the `access_token` query parameter, so it can end up in proxy and server logs.
- Forwarded headers are trusted from any proxy (`KnownProxies` is cleared). Run the app only behind a reverse proxy you control, otherwise the per-IP rate limit can be bypassed with a spoofed `X-Forwarded-For`.
- If `AllowedOrigins` is empty, CORS accepts any origin. Set it in production.

### Known limitations

- If the server stops during a battle, that battle's in-memory event history is lost.
- When a battle ends, the log snapshot is written in the background while the in-memory history is cleared right after, so the snapshot can come out incomplete.
- Account lookups in the stores are synchronous, so the stores are not fully async yet.

### Migration tool

`accounts.json` and the other JSON files are no longer read by the app. `tools/` contains a one-off tool that imports old JSON files into PostgreSQL. It reads `<mode>.json` from the current directory:

```bash
cd tools
# modes: accounts, roster, order-macros, goal-order-macros, member-presets, discord-webhooks
dotnet run -- accounts "Host=localhost;Database=tacnet;Username=...;Password=..."
```

Password hashes are copied as they are.

## Observability

The app exposes Prometheus metrics at `/metrics` (`prometheus-net`, with HTTP request metrics enabled). `prometheus/prometheus.yml` scrapes `app:8080`, so Prometheus works out of the box at `http://localhost:9090`.

Grafana starts with the default `admin` / `admin` login (change it before exposing the host), but it has **no provisioned data source or dashboards**, and in `docker-compose.yml` it sits on a separate network (`tacnet-net`) from Prometheus. To use it, put both services on the same network and add Prometheus (`http://prometheus:9090`) as a data source by hand.

## Infrastructure and tests

- Multi-stage `Dockerfile`; `docker-compose.yml` with PostgreSQL, the app, Prometheus and Grafana.
- `ansible/playbooks/setup_vps.yml` prepares a VPS: UFW, Docker, repository checkout, `.env` from a template, `docker compose up`. Set `repo_url` and the other variables before use.
- GitHub Actions (`.github/workflows/main.yml`): restore, build, test and a Docker build check. It does not push an image to a registry.
- 8 integration tests in `Tacnet.Tests` (`WebApplicationFactory`, Moq): Discord config and login flow, Discord registration, invalid login, and replay download (authorized, unauthorized, forbidden). The stores are mocked, so they do not exercise PostgreSQL. Run them with:

  ```bash
  dotnet test Tacnet.Tests/Tacnet.Tests.csproj
  ```

- `tests/k6/load-test.js` is a small k6 script (config endpoint and a hub connection). No load-test results are published.

## Related projects

- [Tactical-overlay](https://github.com/MordaSkinA/Tactical-overlay): an Electron overlay that shows the player, leader and commander pages on top of the game.

## Releases

Tagged versions are listed in [CHANGELOG.md](CHANGELOG.md) and on the [GitHub Releases](https://github.com/MordaSkinA/tactical-network/releases) page.

## License

MIT. See `LICENSE`.
