<div align="center">

# ⚔️ GvG Tactical Network (Tacnet)

**A tactical coordination tool for GvG battles in MMO guilds**

[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![SignalR](https://img.shields.io/badge/SignalR-realtime-2f6fb2)](https://learn.microsoft.com/aspnet/core/signalr)
[![Status](https://img.shields.io/badge/status-Phase%200%20POC-yellow)]()
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://github.com/MordaSkinA/tactical-network/blob/main/LICENSE)


</div>

---

## 📝 Contents

- [Why this exists](#-why-this-exists)
- [How it works](#-how-it-works)
- [Roles](#-roles)
- [Running it](#-running-it)
- [Architecture](#-architecture)
- [What's done so far](#-whats-done-so-far)
- [Roadmap](#-roadmap)
- [FAQ](#-faq)

---

## 💅 Why this was made

In GvG matches the guild coordinates over Discord voice, but in practice only 3-4 people actually talk. The reasons vary: it's hard to speak English out loud under stress, some people are shy about it, or there's just background noise on their end.

Tacnet's hypothesis is simple. If you remove the need to talk or type, and replace it with a couple of clicks, players who used to stay quiet will start sharing information and, where it makes sense, taking initiative.

## ⚙️ How it works

```
Player clicks a button => SignalR Hub => broadcast to all clients => commander panel / squad leaders
```

- **Player** sees their squad's current order and clicks report buttons (enemy role, SOS, status)
- **Observer** (squad leaders) sees the event feed for their own people
- **Dashboard** (main commander) sees the full guild-wide feed and sends out orders
- **Admin** manages the roster (T1-T6, Defense / Attack / Flex roles) and creates player accounts


## 👯 Roles

| Role | Sees | Can do |
|---|---|---|
| `Player` | Their squad's order | Send an SOS |
| `Leader` | Observer view of their squad | Watch their squad's event feed, issue orders to their squads |
| `Commander` | Guild-wide feed | (macro and micro management happens verbally) |
| `Admin` | Everything | Manage the roster and accounts |

Squad structure: T1-T6, 5 people each. Squads are explicitly assigned a Defense / Attack / Flex role.

## 🏃 Running it 🏃

<details>
<summary><b>Locally</b></summary>

```bash
cd tactical-network
dotnet run
```

The app comes up on `https://localhost:5001` (check the console output for the actual port). Open:

- `/index.html` - login
- `/menu.html` - routes you to the right page based on your role
- `/player.html` - player panel
- `/observer.html` - observer panel
- `/dashboard.html` - live feed for the commander
- `/admin.html` - admin panel

</details>

<details>
<summary><b>Access from outside the network</b></summary>

The local server isn't reachable from outside directly, so for testing you need to forward the port through a tunnel:

```bash
ngrok http https://localhost:5001
# or
cloudflared tunnel --url https://localhost:5001
```

</details>

<details>
<summary><b>First login / seed account</b></summary>

On first run, if `accounts.json` is empty, an Admin account gets created with the login and password from `appsettings.json` (`AdminSeedLogin` / `AdminSeedPassword`, `admin` / `changeme` by default). Change the password right after your first login.

</details>


## 🏩 Architecture

- ASP.NET Core 8, SignalR hub (`/battleHub`)
- Auth: username/password, session tokens, role and `SquadId` are tied to the session on the server. There used to be a shared admin key that let a client fake their name or squad, that's no longer possible.
- Rate limiting: up to 5 login attempts per 30 sec from one IP, and up to 5 actions (report/order/SOS) per 3 sec from one connection
- Enums travel between C# and JS as strings (`JsonStringEnumConverter`) instead of numbers. Without that the frontend broke when comparing roles against strings.
- Persistence: the roster lives in `roster.json`, accounts in `accounts.json`. Battle state itself is kept in memory and doesn't survive a server restart, that's a deliberate tradeoff for a POC.
- UI is the priority here

## ✅ What's done so far

- [x] Phase 0 POC: single project, SignalR broadcast to all clients
- [x] Four roles with access separation (Player / Leader / Commander / Admin)
- [x] Click interface for players (enemy role, SOS, severity)
- [x] Role-based auth: login, redirect to the right panel
- [x] Observer restricted to leaders, Dashboard restricted to the commander
- [x] Admin panel: bulk name paste and dropdown squad assignment
- [x] Rate limiting on login and on hub actions
- [x] Fix: enums as strings in the SignalR protocol

## 🚚 Roadmap

- **Phase 1**: battle history, log export, better event feed
- **Phase 2**: player and squad stats across battles
- **Later**: move the whole UI over to Blazor

## ❓ FAQ

<details>
<summary>Why not Redis / microservices / an event sourcing framework?</summary>

Because the scale of this doesn't call for it: one battle, a few dozen participants, one process. An in-memory append-only `BattleEvent` log already gives us the history and projections we need. Adding architectural complexity here would just add more points of failure.

</details>

<details>
<summary>Why clicks instead of voice chat?</summary>

The guild already has voice, the problem is that only a minority actually use it. The click interface isn't meant to replace voice, it's an alternative channel with a lower barrier to entry for people who find it hard to speak English out loud under the stress of a fight.

</details>

<details>
<summary>Does battle data survive a server restart?</summary>

No, battle state is kept in memory and intentionally isn't persisted (POC). Only the roster (`roster.json`) and accounts (`accounts.json`) are persistent.

</details>

---

<div align="center">


</div>
