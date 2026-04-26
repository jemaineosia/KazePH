# 🌀 KazePH

**Kaze** is a secure peer-to-peer betting platform built for the Philippine market. Kaze is **not a house/bookmaker** — it acts purely as a trusted middleman that holds participant funds in escrow and releases them to winners after the event is resolved. This eliminates the risk of scamming between bettors.

---

## 📋 Table of Contents

- [Overview](#overview)
- [Tech Stack](#tech-stack)
- [Features](#features)
- [User Flow](#user-flow)
  - [Registration & Phone Verification](#registration--phone-verification)
  - [Wallet – Top-Up (Deposit)](#wallet--top-up-deposit)
  - [Wallet – Withdrawal](#wallet--withdrawal)
- [Betting Modes](#betting-modes)
  - [1v1 Betting](#1v1-betting)
  - [Pool / Parimutuel Betting](#pool--parimutuel-betting)
- [Dispute System](#dispute-system)
- [Ranking System](#ranking-system)
- [Social Features](#social-features)
- [Admin Panel](#admin-panel)
- [Platform Configuration](#platform-configuration)
- [Database](#database)
- [Project Structure](#project-structure)
- [Development Modules](#development-modules)
- [Important Notes](#important-notes)

---

## Overview

Kaze provides a safe environment for users who want to bet against each other without the fear of being scammed. The platform:

- Holds all bet money in escrow once a bet is accepted
- Releases funds to the winner(s) after the result is confirmed
- Provides an admin-backed dispute resolution system
- Supports both head-to-head (1v1) and multi-participant (Pool) betting

---

## Tech Stack

| Layer | Technology |
|---|---|
| Frontend & Backend | .NET 10 Blazor (Server-side) |
| Realtime (Chat & Live Updates) | SignalR + Supabase Realtime |
| Database | Supabase (PostgreSQL) |
| ORM | Entity Framework Core + Npgsql |
| File Storage | Supabase Storage (receipts, dispute proofs) |
| Authentication | .NET Identity (stored in Supabase PostgreSQL) |
| Phone / OTP Verification | Twilio or Semaphore (PH) |
| Push Notifications | Firebase Cloud Messaging (FCM) |
| Mobile Readiness | PWA (Progressive Web App) |
| Admin Panel | Blazor (same solution) |

---

## Features

- 📱 Mobile-ready PWA with push notifications
- 📞 Phone number registration with OTP verification
- 💰 Manual top-up via GCash / Bank with receipt submission
- 🔒 Escrow wallet — funds are locked once a bet is accepted
- ⚔️ 1v1 betting with invitation codes
- 🏆 Pool / Parimutuel betting with proportional payouts
- 💬 Real-time chat (1-on-1 and group per event)
- 👥 Friends system
- 🏅 Ranking / tier system
- ⚖️ Admin-managed dispute resolution
- 🛡️ Strike and punishment system for dishonest players
- 💸 Withdrawal to GCash or Bank (admin-processed)
- 🛠️ Full admin panel for deposits, withdrawals, disputes, and user management

---

## User Flow

### Registration & Phone Verification

1. User registers with their phone number
2. OTP is sent via SMS (Twilio / Semaphore)
3. User verifies OTP to activate account
4. User completes profile: username, avatar
5. User adds GCash / Bank details (used for withdrawals)

---

### Wallet – Top-Up (Deposit)

1. User navigates to the **Top-Up** page
2. Platform displays the official GCash number and Bank account details
3. User manually sends money via GCash or Bank transfer
4. User submits a top-up request:
   - Amount
   - Screenshot / receipt (uploaded to Supabase Storage)
5. Admin reviews the submission
6. Admin **approves** → balance is credited to the user's wallet
7. Admin **rejects** → user is notified with a reason

**Wallet States:**

| State | Description |
|---|---|
| `Available` | Spendable balance |
| `Locked` | Funds held in escrow for an active bet |
| `Pending Withdrawal` | Funds requested for withdrawal, awaiting admin processing |

---

### Wallet – Withdrawal

1. User requests a withdrawal
2. User enters:
   - Amount (must meet minimum set in config)
   - Destination: GCash or Bank (pre-saved from profile)
3. Withdrawal fee is automatically deducted (set in config, e.g., flat or percentage)
4. Admin receives a notification
5. Admin processes the transfer manually
6. Admin uploads the receipt and confirms the withdrawal
7. User is notified that the withdrawal is complete

---

## Betting Modes

### 1v1 Betting

A direct head-to-head bet between two users.

#### Creating a 1v1 Event

- Creator sets:
  - Event title / description
  - Event date
  - Creator's bet amount
  - Opponent's required bet amount
- Creator invites a specific user **or** generates a shareable invitation code

#### Accepting the Bet

- Opponent accepts the invitation
- Both wallets are immediately **locked** (escrowed) for the bet amounts

#### Cancellation

- Either party can request cancellation
- **Both parties must agree** before the event is cancelled
- Upon mutual agreement → funds are returned to both wallets

#### Result & Payout

| Scenario | Process |
|---|---|
| Both agree on winner | Winner receives the full pot immediately |
| Both agree it's a draw | Funds returned to both wallets |
| They disagree | Escalates to the **Dispute System** |

---

### Pool / Parimutuel Betting

A multi-participant event where users bet on one of two sides/teams.

#### Creating a Pool Event

- Creator sets:
  - Event title / description
  - Event date
  - Two sides / teams to bet on

#### Joining

- Any user can join and pick a side
- User sets their own bet amount
- Funds are locked once they join

#### Payout Calculation

Winners share the total pot **proportionally to their stake**.

**Example:**
- Total pot: ₱10,000
- Side A wins
- User X bet ₱300 on Side A out of ₱500 total on Side A
- User X receives: `(300 / 500) × 10,000 = ₱6,000`

#### Winner Determination (Recommended: Admin-Verified Proof)

1. Event creator submits the result after the event ends
2. Creator uploads **proof** (screenshot, photo, link)
3. A **24-hour dispute window** opens for any participant to contest
4. If no disputes → Admin does a quick review → Payouts are released
5. If disputed → Escalates to the **Dispute System**

> This approach is consistent with the manual deposit/withdrawal flow and ensures platform trust during early stages.

---

## Dispute System

Activated when participants cannot agree on a result.

### Process

1. Dispute is opened (automatically on disagreement, or manually by a participant)
2. Both parties receive a notification
3. Each party submits their **evidence** within a set window:
   - Photos, screenshots, videos, links, text explanation
4. Admin reviews all submitted evidence
5. Admin declares the official winner
6. Payouts are released accordingly

### Punishments for Dishonesty

If a party is found to have lied or submitted false evidence:

| Strike | Consequence |
|---|---|
| 1st Strike | Warning issued |
| 2nd Strike | Temporary suspension |
| 3rd Strike | Permanent ban |

Strikes are visible on the user's profile to warn other players.

---

## Ranking System

Rank is based on completed events without disputes filed against the user.

| Tier | Name | Completed Events |
|---|---|---|
| 1 | 🥉 Rookie | 0 – 4 |
| 2 | ⚔️ Contender | 5 – 14 |
| 3 | 🛡️ Veteran | 15 – 29 |
| 4 | 💎 Elite | 30 – 49 |
| 5 | 👑 Legend | 50+ |

- Disputes **lost** (found lying) negatively affect rank and add a strike
- Clean record = faster rank progression

---

## Social Features

### Friends System
- Send and accept friend requests
- View friends list and their activity (events joined, rank)

### Real-Time Chat
- 1-on-1 chat between any two users
- Group chat per Pool event (all participants)
- Powered by **SignalR**

### Notifications
Push and in-app notifications are sent for:

- Bet invitation received
- Bet accepted or declined
- Bet cancelled (and reason)
- Event result submitted
- Dispute opened
- Dispute resolved
- Deposit approved or rejected
- Withdrawal processed
- Friend request received / accepted
- Strike issued

---

## Admin Panel

| Module | Capabilities |
|---|---|
| **Deposit Management** | View pending receipts, approve or reject with notes |
| **Withdrawal Management** | View requests, confirm transfer, upload receipt |
| **Dispute Management** | View both sides' evidence, declare winner, issue punishments |
| **User Management** | View all users, issue strikes, suspend or ban accounts |
| **Event Monitoring** | View all active and completed events (1v1 and Pool) |
| **Platform Config** | Manage fees, minimums, GCash/Bank display info |

---

## Platform Configuration

Managed by admin via the Config/Settings panel:

| Setting | Description |
|---|---|
| `MinWithdrawalAmount` | Minimum amount a user can withdraw (e.g., ₱100) |
| `WithdrawalFee` | Fee per withdrawal — flat (e.g., ₱10) or percentage (e.g., 2%) |
| `PlatformGCash` | GCash number displayed on the top-up page |
| `PlatformBankName` | Bank name displayed on the top-up page |
| `PlatformBankAccount` | Bank account number displayed on the top-up page |
| `DisputeWindowHours` | Hours participants have to contest a Pool result (default: 24) |
| `EvidenceSubmissionHours` | Hours each party has to submit dispute evidence |

---

## Database

- **Provider**: Supabase (PostgreSQL)
- **ORM**: Entity Framework Core with Npgsql provider
- **Auth tables**: Managed by .NET Identity, stored in Supabase
- **File storage**: Supabase Storage buckets
  - `receipts` — deposit and withdrawal receipts
  - `dispute-evidence` — photos/videos submitted during disputes
  - `avatars` — user profile pictures
- **Row Level Security (RLS)**: Disabled on tables; access control is enforced entirely by the .NET backend
- **Connection**: Use Supabase's **pooled connection string** (PgBouncer) to avoid exhausting connections

---

## Project Structure

```
KazePH/
├── KazePH.Web/               # Blazor Web App (frontend + backend)
│   ├── Components/           # Blazor components and pages
│   ├── Layout/               # App layout, nav
│   ├── Hubs/                 # SignalR hubs (chat, notifications)
│   └── wwwroot/              # Static assets, PWA manifest, service worker
├── KazePH.Core/              # Domain models, interfaces, enums
├── KazePH.Application/       # Business logic, services
├── KazePH.Infrastructure/    # EF Core, Supabase, external services (SMS, FCM)
├── KazePH.Admin/             # Admin panel (Blazor, same solution)
└── KazePH.Tests/             # Unit and integration tests
```

---

## Development Modules

Planned development order:

1. **Auth & Verification** — Register, Login, OTP, Phone Verification
2. **Wallet** — Balance display, Top-up submission, Withdrawal request
3. **Events – 1v1** — Create, Invite, Accept, Cancel, Vote, Dispute trigger
4. **Events – Pool** — Create, Join, Result submission, Dispute trigger
5. **Dispute System** — Evidence upload, Admin resolution, Punishment
6. **Social** — Friends, Chat, Notifications
7. **Ranking** — Calculation engine, Display, Tier progression
8. **Admin Panel** — All admin capabilities
9. **Config / Settings** — Fee management, Platform payment info

---

## Important Notes

> ⚠️ **Legal**: Online betting platforms in the Philippines may require a **PAGCOR license**. Consult a legal professional before launching publicly.

> 💱 **Currency**: The platform operates in **Philippine Peso (PHP / ₱)**.

> 🕗 **Timezone**: All event dates and times are handled in **Philippine Standard Time (PST, UTC+8)**.

> 📄 **Receipts**: Only accept receipts from official GCash or bank transfer confirmations to reduce fraud risk.

> 🔒 **Escrow Integrity**: Funds are locked immediately upon bet acceptance and cannot be withdrawn until the event is resolved or mutually cancelled.

---

*Built with ❤️ for the Philippine betting community.*
