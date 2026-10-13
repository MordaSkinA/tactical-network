# Tacnet (GvG Tactical Network)

A small web app that lets guild members report enemy positions, call for help, and receive orders during a large PvP battle by clicking buttons instead of talking or typing over voice chat

[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![SignalR](https://img.shields.io/badge/SignalR-realtime-2f6fb2)](https://learn.microsoft.com/aspnet/core/signalr)
[![Status](https://img.shields.io/badge/status-Beta-orange)]()
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://github.com/MordaSkinA/tactical-network/blob/main/LICENSE)

## Why this exists

In our guild's GvG fights, coordination happens over Discord voice, but in any given fight only three or four people are actually talking. Not because they don't have information, they usually do. It's things like not comfortable speaking English out loud under pressure, general shyness on comms, or their mic setup is bad and they don't want to add noise

The result is a commander flying half-blind while a bunch of people who saw exactly what happened stay quiet

Tacnet's bet is that if reporting something takes one or two clicks instead of keying up and finding the words, more people will actually do it. It's not trying to replace voice. It's a second channel for the people voice doesn't work for

## Quickstart

Tacnet needs PostgreSQL: user accounts are stored there, and the app will not start without a connection string.

### Docker (docker compose)

1. Create a `.env` file next to `docker-compose.yml` with the required values (compose refuses to start without them):

   ```env
   POSTGRES_PASSWORD=...
   ADMIN_SEED_PASSWORD=...
   SESSION_SIGNING_KEY=...
   # optional, only if you use Discord login:
   DISCORD_CLIENT_SECRET=...
   ```

2. Start everything:

   ```bash
   docker compose up -d --build
   ```

3. Open `http://localhost:8080`. The PostgreSQL container is published on `127.0.0.1:5433`.

The compose file also starts Prometheus (`:9090`) and Grafana (`:3000`). Grafana's admin password in `docker-compose.yml` is `admin` - change it before exposing the host. The app does not expose a metrics endpoint yet (see [Roadmap](#roadmap)).

> **Data warning:** only PostgreSQL has a volume (`pgdata`). The roster, macros, presets, webhooks and battle logs are plain files inside the app container and are lost when the container is recreated. See [Architecture](#architecture).

### Local (dotnet run)

You need the .NET 8 SDK and a PostgreSQL instance you can reach.

1. Create `appsettings.json` in the project folder (it is git-ignored; there is no example file yet) with at least:

   ```json
   {
     "ConnectionStrings": {
       "Tacnet": "Host=localhost;Database=tacnet;Username=...;Password=..."
     },
     "AdminSeedPassword": "..."
   }
   ```

   Optional keys: `AdminSeedLogin` (default `admin`), `SessionSigningKey` (if omitted, a key is generated into `session-signing.key`), and, for Discord login, `DiscordClientId`, `DiscordClientSecret`, `DiscordRedirectUri`, `DiscordGuildId`.

2. Create the `users` table with `db/init/001_users.sql` (compose does this automatically on first start of an empty database volume).

3. Run:

   ```bash
   dotnet run
   ```

   The console output will tell you the actual URL. Open it in a browser and log in.

### First login

If the `users` table is empty at startup, an admin account is created from `AdminSeedLogin` / `AdminSeedPassword` (in Docker: `ADMIN_SEED_PASSWORD`). Log in with it and change the password immediately - it sits in a config file and isn't meant to be a real credential.

### Letting others test it

If you want other people to test a local instance from outside your network, put it behind a tunnel, for example:

```bash
ngrok http <the URL printed by dotnet run>
```

## Basic Usage

There are five roles, and what you see depends on which one you're logged in as:

- **Player** – sees the current order (and any goal order aimed at them) for their team, and has buttons to report things like enemy role sightings or call an SOS
- **Leader** (team leader) – sees the observer view: a live feed of everything their own team is reporting, and can also push team orders and goal orders to their own team (the commander can still override with a guild-wide order)
- **Commander** – sees the guild-wide feed across every team, and pushes out team orders and goal orders to anyone
- **Admin** – manages the roster (teams T1 through T6, each assigned Defense / Attack / Flex), each member's role/bulwark/build, and creates accounts
- **Developer** – has the same goal-order and macro permissions as Commander and Admin

The pages line up with the roles:

- `/index.html` – login
- `/menu.html` – sends you to the right panel based on your role
- `/player.html`, `/observer.html`, `/dashboard.html`, `/admin.html` – the role panels

Everything flows over a single SignalR hub at `/battleHub`: a player clicks a button, it broadcasts, and it shows up on the relevant observer and dashboard feeds in real time.

The UI is available in English and Vietnamese.

Also worth knowing:

- Rate limiting is baked in: 5 login attempts per 30 seconds per IP, and 5 hub actions (report/order/SOS) per 3 seconds per connection
- Roles and team IDs are attached to the session on the server, not sent up from the client. There used to be a shared admin key a client could use to spoof their identity, that's gone now
- What is stored where (and what is lost on restart) is described in [Architecture](#architecture)

## Goal orders

Alongside the regular team orders, there's a separate free-text channel for ad hoc callouts: **goal orders**. Instead of targeting a team, a goal order is aimed at whoever currently matches a
filter, e.g. "Focus on enemy healer" sent to everyone whose Build is `Twinblades`. Filters
can combine:

- **Build** – free-text, matched against the `Build` field admins set per roster member
- **Role** – DPS / Tank / Healer
- **Side** – Attack / Defense / Flex
- **team** – same team targeting as regular orders, if you want to narrow further

Leaving a filter blank means "no restriction on that axis". Reserves are excluded by
default unless the Reserves team is explicitly targeted. A goal order can optionally
carry a countdown timer (seconds) and a **battle phase** (Phase 1/2/3, set manually by the
commander) for scoping. Only players who actually match the filter receive it - everyone
else's screen is unaffected - and it's mirrored to the commander/admin feed regardless

Commander, Admin, Developer and Leader can all issue goal orders; a Leader's goal order is
restricted server-side to their own team, same as their regular orders. Frequently reused
goal orders can be saved as named **macros** (commander/admin/developer only to create or
delete, everyone above can fire an existing one) so the same callout doesn't need retyping
mid-fight - these persist to `goal-order-macros.json`

Note: the older `FallBack`, `Rotate` and `ProtectHealer` team order types have been
removed - that kind of ad hoc callout is now better served by a goal order instead.

## Architecture

Tacnet is an ASP.NET Core (.NET 8) app: static pages in `wwwroot/`, one SignalR hub (`Hubs/BattleHub.cs`), and a set of stores in `State/`. SignalR groups are used to route messages to the right teams.

### What is stored where

| Data | Storage | Survives app restart | In Docker |
|---|---|---|---|
| User accounts | PostgreSQL, table `users` (`PostgresAccountStore`) | yes | yes (`pgdata` volume) |
| Roster | `roster.json` in the app folder | yes | **no** (no volume) |
| Goal-order macros, order macros | `goal-order-macros.json`, `order-macros.json` | yes | **no** |
| Member presets, Discord webhooks, emoji settings | `member-presets.json`, `discord-webhooks.json`, `emoji-settings.json` | yes | **no** |
| Live battle state and event history | process memory (`InMemoryBattleState`) | **no** | no |
| Battle log snapshots (used by replay download/delete) | `logs/battle-*.json` and `.txt` | yes | **no** |
| Pending Discord logins, connection tracking | process memory | no | no |

Why it is split this way: Tacnet started as a POC, and persistence for battle state wasn't worth the complexity yet. Accounts were the first data moved to PostgreSQL; moving the rest is planned (see [Roadmap](#roadmap)).

### Known limitations

- If the server stops during a battle, that battle's in-memory event history is lost.
- When a battle ends, the log snapshot is written in the background while the in-memory history is cleared right after, so the snapshot can come out incomplete. A fix is planned.
- `accounts.json` is no longer read by the app. It is only the input for the one-off migration tool in `tools/MigrateAccountsToPostgres`, which copies existing accounts into PostgreSQL (password hashes are copied as is):

  ```bash
  cd tools
  dotnet run --project MigrateAccountsToPostgres.csproj -- <path-to-accounts.json> "<connection string>"
  ```

### Infrastructure and tests

- Multi-stage `Dockerfile`; `docker-compose.yml` with PostgreSQL, the app, Prometheus and Grafana
- Ansible playbook for a VPS in `ansible/` (check `repo_url` and the other variables in `ansible/playbooks/setup_vps.yml` before use)
- GitHub Actions (`.github/workflows/main.yml`): restore, build, test and a Docker build check. It does not push an image to a registry
- Integration tests in `Tacnet.Tests` (`WebApplicationFactory`, Moq); k6 load-test scripts in `tests/k6`

## Releases

Tagged versions and what changed in them are listed in [CHANGELOG.md](CHANGELOG.md) and on the [GitHub Releases](https://github.com/MordaSkinA/tactical-network/releases) page. Changes that are not tagged yet live under **Unreleased** in the changelog

## Roadmap


**2.1.0 - stabilisation**

- Make `EndBattle` reliable: finish saving the log snapshot before the history is cleared
- Move admin seeding out of the `PostgresAccountStore` constructor (it currently blocks startup with `.GetAwaiter().GetResult()`)
- Add `appsettings.Example.json` and update this README (Docker quickstart, Ansible deployment)
- Add `prometheus-net` and a `/metrics` endpoint so the Prometheus/Grafana stack in compose has something to scrape

**3.0.0 - data in PostgreSQL**

- Move roster, order macros, goal-order macros, member presets and Discord webhooks from JSON files into PostgreSQL tables (`emoji-settings.json` stays a file)
- Add volumes to `docker-compose.yml` for the data that stays on disk (`logs/`, `emoji-settings.json`)
- Extend `tools/MigrateAccountsToPostgres` to migrate the new tables

## License

MIT. See `LICENSE`.
