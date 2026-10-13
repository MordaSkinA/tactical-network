# Changelog

All notable changes to this project are recorded here.
Format based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/); versions follow [Semantic Versioning](https://semver.org/).

How to keep it current: add a line under **Unreleased** in the same commit or PR as the change. When tagging a release, rename **Unreleased** to the version and date, then add a fresh empty **Unreleased** section.

## [Unreleased]

### Added
- Goal orders (filter by Build / Role / Side / team, optional countdown and battle phase) and named goal-order macros
- Per-member `Build` field in the roster
- Leaders can issue team orders to their own team
- Replay download and delete
- Vietnamese UI translation
- Dockerfile, `docker-compose.yml` (PostgreSQL, app, Prometheus, Grafana), Ansible playbook for a VPS
- GitHub Actions workflow (build, test, Docker build check)
- Integration tests (`Tacnet.Tests`)
- `tools/MigrateAccountsToPostgres` - one-off migration of `accounts.json` into PostgreSQL

### Changed
- User accounts are stored in PostgreSQL instead of `accounts.json`
- Security hardening: sessions use signed tokens (`SessionSigningKey`); see commit `be73198` for the full list
- Discord login logic moved into a separate service (`IDiscordAuthService`)

### Removed
- `FallBack`, `Rotate` and `ProtectHealer` team order types (use goal orders instead)
- Chinese UI translation (replaced by Vietnamese)

## [2.0.0] - 2026-08-24
- Multi-team orders
- Persistent status indicators on team cards (auto-revert after 25 s)
- Sidebar menu with a home/stats page
- Jungle and boss spawn reminders
- Admin panel improvements
- Developer role

## [1.1.0] - 2026-08-22
- SignalR connection fixes
- Discord webhook integration
- Battle points tracking

## [1.0.0] - 2026-08-19
- First stable tag
- English localization pass, UI cleanup, config/secrets cleanup

## [0.0.1-beta] - 2026-08-18
- First working POC: click buttons, basic team swap

[Unreleased]: https://github.com/MordaSkinA/tactical-network/compare/2.0.0...HEAD
[2.0.0]: https://github.com/MordaSkinA/tactical-network/releases/tag/2.0.0
[1.1.0]: https://github.com/MordaSkinA/tactical-network/releases/tag/1.1.0
[1.0.0]: https://github.com/MordaSkinA/tactical-network/releases/tag/1.0.0
[0.0.1-beta]: https://github.com/MordaSkinA/tactical-network/releases/tag/0.0.1-beta
