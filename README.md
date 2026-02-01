# proxmox-self-service-portal (VmPortal)

[![Project Status: Active](https://img.shields.io/badge/status-active-brightgreen)](https://github.com/)
[![License: MIT + Commons Clause](https://img.shields.io/badge/license-MIT%20%2B%20Commons--Clause-blue)](./LICENSE)
[![Build](https://img.shields.io/github/actions/workflow/status/OWNER/REPO/ci.yml)](https://github.com/OWNER/REPO/actions)
[![Docker Pulls](https://img.shields.io/docker/pulls/OWNER/REPO)](https://hub.docker.com/)

A secure, identity‑aware self‑service virtual machine portal for Proxmox VE — VmPortal enables governed VM provisioning, auditing, and browser console access via Microsoft Entra ID SSO and the Proxmox API.

## Table of Contents
- [Overview](#overview)
- [Key Features](#key-features)
- [Why Use VmPortal](#why-use-vmportal)
- [Quick Start](#quick-start)
- [Configuration](#configuration)
- [Architecture](#architecture)
- [Security & Compliance](#security--compliance)
- [Data & Privacy](#data--privacy)
- [Production Readiness & Risks](#production-readiness--risks)
- [Usage Examples](#usage-examples)
- [Contributing](#contributing)
- [Support & Contact](#support--contact)
- [License](#license)

## Overview

VmPortal (proxmox-self-service-portal) is an enterprise‑oriented self‑service portal that provides a governed, identity‑aware layer on top of Proxmox VE. It enables employees to safely provision and manage VMs using Microsoft Entra ID (Azure AD) single sign‑on (SSO), while administrators keep policy control, auditing, and resource governance.

## Key Features

- Microsoft Entra ID (Azure AD) OpenID Connect SSO and app-role based authorization (Admin, Employee)
- Template-driven VM provisioning and cloning via the Proxmox API
- Per-template and global resource limits (CPU, memory, disk)
- Browser console access via noVNC and proxied WebSocket sessions
- Audit logging of authentication and VM lifecycle events
- Simple Blazor Server UI for employees and administrators
- SQLite (default), EF Core migrations, and optional container deployment

## Why Use VmPortal

- Reduce helpdesk workload by enabling controlled self‑service provisioning
- Keep Proxmox as the authoritative VM platform while protecting it behind policies
- Enforce identity-based ownership and auditable VM lifecycle actions
- Use as a reference implementation or starting point for internal tooling

## Quick Start

### Prerequisites

- .NET 10 SDK (for local development)
- Proxmox VE instance with API access (TokenId / TokenSecret)
- Microsoft Entra (Azure) tenant and an app registration for OIDC
- Docker & Docker Compose (recommended for container testing)

### Local (CLI) Run

```powershell
# from repo root
dotnet clean
dotnet build VmPortal.Web
dotnet run --project VmPortal.Web
# Open: http://localhost:5000 (or configured port)
```

### Docker (Recommended for Testing)
```powershell
# copy sample env and edit
Copy-Item .env.sample .env
# edit .env to set secrets: AzureAd, Proxmox tokens, Console creds, etc.

docker compose build
docker compose up
# Open: http://localhost:8080 (mapping in docker-compose.yml)
```

## Configuration

Configuration is supported via:
- `appsettings.json` / `appsettings.{Environment}.json`
- Environment variables (`Section__Key`)
- User secrets (development)

### Important Configuration Keys

| Section | Keys |
| ------- | ---- |
| AzureAd | Instance, TenantId, ClientId, ClientSecret, CallbackPath |
| Proxmox | BaseUrl, TokenId, TokenSecret, DevIgnoreCertErrors, Tags |
| ProxmoxConsole | Username, Password |
| Security | SessionSecretKey, SessionTimeoutMinutes, EnableSecurityLogging |
| VmResourceLimits | MaxCpuCores, MaxMemoryMiB, MaxDiskGiB |
| ConnectionStrings | Default (default: `Data Source=./data/vmportal.db`) |

## Architecture

VmPortal uses a layered architecture separating UI, application logic, infrastructure, and Proxmox integration.

```
Web Browser (Employee / Admin UI)
            ↓
VmPortal.Web (Blazor Server, Auth, Policies, APIs)
            ↓
VmPortal.Application (Orchestration, rules, console sessions)
            ↓
VmPortal.Infrastructure (EF Core, Proxmox API client, background services)
            ↓
Proxmox VE (VM lifecycle, vncwebsocket)
```


## Security & Compliance

VmPortal is built with security awareness, but is not a certified compliance product.

### Security Highlights

- SSO via Microsoft Entra ID (OIDC). No local password storage.
- Role-based access enforced at the app layer (Admin / Employee).
- Session cookies set HttpOnly, Secure, SameSite=Strict.
- All VM actions are authenticated, authorized, validated, executed, and logged.

### Important Notes

- Default storage is SQLite and not intended for high‑scale production.
- No guarantees of GDPR or ISO 27001 compliance out of the box — treat this as a reference implementation.
- You are responsible for validating and hardening deployments for production use (security review, pen test, architecture approvals).

## Data & Privacy

### What Is Stored (by Default)

- **Database:** SQLite (`./data/vmportal.db`)
- **User metadata from Entra ID:** object ID, UPN, display name, email, created timestamp, IsActive flag
- **VM metadata:** Proxmox VMID, node, name, template linkage, CPU/RAM/Disk, owner link
- **Security events:** login attempts, VM lifecycle actions, admin changes — with IP, user agent, severity, timestamps

### Retention

Security events retention is configurable via `Security:SecurityEvents:Retention` (RetentionDays, CleanupIntervalHours).

## Production Readiness & Risks

**Status:** Active development; intended for internal/lab/educational use.

**Not production-ready.** Key risks to address before production:

- Use a production-grade datastore (e.g., PostgreSQL) and run migrations in CI/CD
- Secure and rotate secrets using a vault (Azure Key Vault, HashiCorp Vault)
- Harden network boundaries, TLS, and Proxmox access
- Add monitoring, alerting, and backup strategies
- Conduct security assessment and compliance review

### Operational Recommendations

- Use least-privilege Proxmox API tokens scoped to required actions
- Run VmPortal behind an ingress with strong TLS and Web Application Firewall rules
- Centralise logs (SIEM) and export audit events
- Implement secure backup for database and configuration

## Usage Examples

- Create a VM from an admin-approved template (Employee role)
- Start/Stop/Shutdown a managed VM
- Open browser console (noVNC) with temporary proxied session
- Admins: Create templates, manage resource limits, review audit logs

## Contributing

Contributions are welcome. Please follow these steps:

1. Fork the repository.
2. Create a feature branch: `git checkout -b feat/my-change`
3. Add tests and documentation for new features.
4. Open a pull request describing the change and the rationale.

Please include:

- A clear PR title and description
- Summary of security/privacy considerations if applicable
- Tests or manual steps to validate the change

Consider adding a `CODE_OF_CONDUCT.md`, `CONTRIBUTING.md`, and `SECURITY.md` for formalised processes.

## Support & Contact

- **Issues:** Open a GitHub Issue with reproducible steps, logs, and configuration snippets (redact secrets).
- **Maintainer:** Job Dirks (GitHub: [@JobDirks](https://github.com/JobDirks))

## License

This project is licensed under **MIT + Commons Clause**.

**Summary:**

- **Permitted:** Free use for personal and internal business purposes, modifications, internal distributions.
- **Not permitted:** Selling the software itself as a standalone commercial product.

This is not legal advice. Consult legal counsel for commercial or regulated uses.