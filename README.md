# Tacnet (GvG Tactical Network)

A small web app that lets guild members report enemy positions, call for help, and receive orders during a large PvP battle by clicking buttons instead of talking or typing over voice chat.

[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![SignalR](https://img.shields.io/badge/SignalR-realtime-2f6fb2)](https://learn.microsoft.com/aspnet/core/signalr)
[![Status](https://img.shields.io/badge/status-Done-success)]()
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://github.com/MordaSkinA/tactical-network/blob/main/LICENSE)

## Quickstart / Installation

You need the .NET 8 SDK installed. From the project folder:

```bash
cd tactical-network
dotnet run
```

The console output will tell you the actual URL, something like `https://localhost:14453`. Open it in a browser and log in

On the very first run, if `accounts.json` is empty, an admin account is created automatically using the `AdminSeedLogin` / `AdminSeedPassword` values in `appsettings.json`. Log in with those, then go change the password immediately, that seed password sits in a config file and isn't meant to be a real credential

If you want other people to test it from outside your network, put it behind a tunnel:

```bash
ngrok http https://localhost:14453
```

## Why this exists

In our guild's GvG fights, coordination happens over Discord voice, but in any given fight only three or four people are actually talking. Not because they don't have information, they usually do. It's things like not comfortable speaking English out loud under pressure, general shyness on comms, or their mic setup is bad and they don't want to add noise

The result is a commander flying half-blind while a bunch of people who saw exactly what happened stay quiet

Tacnet's bet is that if reporting something takes one or two clicks instead of keying up and finding the words, more people will actually do it. It's not trying to replace voice. It's a second channel for the people voice doesn't work for

## Basic Usage

There are four roles, and what you see depends on which one you're logged in as:

- **Player** – sees the current order (and any goal order aimed at them) for their team, and has buttons to report things like enemy role sightings or call an SOS
- **Leader** (team leader) – sees the observer view: a live feed of everything their own team is reporting, and can also push team orders and goal orders to their own team (the commander can still override with a guild-wide order)
- **Commander** – sees the guild-wide feed across every team, and pushes out team orders and goal orders to anyone
- **Admin** – manages the roster (teams T1 through T6, each assigned Defense / Attack / Flex), each member's role/bulwark/build, and creates accounts

The pages line up with the roles:

- `/index.html` – login
- `/menu.html` – sends you to the right panel based on your role
- `/player.html`, `/observer.html`, `/dashboard.html`, `/admin.html` – the four panels above

Everything flows over a single SignalR hub at `/battleHub`: a player clicks a button, it broadcasts, and it shows up on the relevant observer and dashboard feeds in real time

A couple of things worth knowing if you're poking around the code:

- Battle state lives in memory only. If the server restarts mid-fight, that battle's event log is gone. This is intentional for now, it's a POC and persistence wasn't worth the complexity yet. The roster and accounts do persist, to `roster.json` and `accounts.json`
- Rate limiting is baked in: 5 login attempts per 30 seconds per IP, and 5 hub actions (report/order/SOS) per 3 seconds per connection
- Roles and team IDs are attached to the session on the server, not sent up from the client. There used to be a shared admin key a client could use to spoof their identity, that's gone now

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

## Releases

Tagged versions so far, oldest to newest:

- **0.0.1-beta** – first working POC. Click buttons, basic team swap.
- **1.0.0** – first stable tag. English localization pass, UI cleanup, config/secrets cleanup.
- **1.1.0** – SignalR connection fixes, Discord webhook integration, battle points tracking.
- **2.0.0** – multi-team orders, persistent status indicators on team cards (auto-revert after 25s), sidebar menu with a home/stats page, jungle and boss spawn reminders, admin panel improvements, a developer role.

Anything past `2.0.0` in the repo right now hasn't been tagged yet: replay download/delete,
Chinese translation and minor UI fixes are already committed; goal orders and their macros,
per-member Build, Leaders being able to issue team orders to their own team, and the
removal of the `FallBack`/`Rotate`/`ProtectHealer` order types are still local, uncommitted
work on top of that.

## License

MIT. See `LICENSE`.
