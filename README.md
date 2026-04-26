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
- [Payment Methods](#payment-methods)
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
- 💰 Top-up via GCash, Bank, or PayPal with receipt submission
- 🔒 Escrow wallet — funds are locked once a bet is accepted
- ⚔️ 1v1 betting with invitation codes
- 🏆 Pool / Parimutuel betting with proportional payouts
- 💬 Real-time chat (1-on-1 and group per event)
- 👥 Friends system
- 🏅 Ranking / tier system
- ⚖️ Admin-managed dispute resolution
- 🛡️ Strike and punishment system for dishonest players
- 💸 Withdrawal to GCash, Bank, or PayPal (admin-processed)
- 🛠️ Full admin panel for deposits, withdrawals, disputes, and user management

---

## User Flow

### Registration & Phone Verification

1. User registers with their phone number
2. OTP is sent via SMS (Twilio / Semaphore)
3. User verifies OTP to activate account
4. User completes profile: username, avatar
5. User adds GCash / Bank / PayPal details (used for withdrawals)

---

### Wallet – Top-Up (Deposit)

1. User navigates to the **Top-Up** page
2. User selects their preferred payment method: **GCash**, **Bank**, or **PayPal**
3. Platform displays the corresponding account details for the selected method
4. User manually sends money via the chosen method
5. User submits a top-up request:
   - Amount sent
   - Screenshot / receipt (uploaded to Supabase Storage)
   - Payment method used
6. Admin reviews the submission
7. Admin **approves** → balance is credited to the user's wallet (after PayPal fee deduction if applicable)
8. Admin **rejects** → user is notified with a reason

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
   - Destination: **GCash**, **Bank**, or **PayPal** (pre-saved from profile)
3. Applicable withdrawal fee is automatically deducted based on the chosen method (set in config)
4. Admin receives a notification
5. Admin processes the transfer manually
6. Admin uploads the receipt and confirms the withdrawal
7. User is notified that the withdrawal is complete

---

## Payment Methods

Kaze supports three payment channels for both cash-in (top-up) and cash-out (withdrawal). Each method has its own fee configuration.

| Method | Cash-In | Cash-Out | Extra Fee | Notes |
|---|---|---|---|---|
| **GCash** | ✅ | ✅ | None (standard platform fee only) | Most common for PH users |
| **Bank Transfer** | ✅ | ✅ | None (standard platform fee only) | Instapay / PESONet |
| **PayPal** | ✅ | ✅ | ✅ Yes — PayPal fee passed to user | Fee covers PayPal's transaction charges |

### PayPal Fee Handling

PayPal charges transaction fees on money transfers. To cover this, Kaze adds an **extra fee** on top of the standard platform fee when a user chooses PayPal.

**Cash-In via PayPal:**
- User sends the amount to the platform's PayPal account
- Admin receives the amount minus PayPal's fee
- Only the **net amount received** (after PayPal deduction) is credited to the user's wallet
- The user is informed of this before submitting

**Cash-Out via PayPal:**
- The standard withdrawal fee **plus** the PayPal extra fee are deducted from the user's wallet
- The user sees the exact amount they will receive before confirming
- Admin sends the net amount via PayPal

**Fee Config Example:**

| Setting | GCash / Bank | PayPal |
|---|---|---|
| Standard withdrawal fee | ₱10 flat or 2% | ₱10 flat or 2% |
| PayPal extra fee | — | Configurable (e.g., 4.4% + ₱15 fixed) |
| Total deducted | Standard fee only | Standard fee + PayPal extra fee |

> 💡 The PayPal extra fee values in config should reflect PayPal's current fee structure for the platform's account type and region. Admin can update these in the Config panel.

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
| **Deposit Management** | View pending receipts (GCash / Bank / PayPal), approve or reject with notes |
| **Withdrawal Management** | View requests, confirm transfer, upload receipt |
| **Dispute Management** | View both sides' evidence, declare winner, issue punishments |
| **User Management** | View all users, issue strikes, suspend or ban accounts |
| **Event Monitoring** | View all active and completed events (1v1 and Pool) |
| **Platform Config** | Manage fees, minimums, payment method account details |

---

## Platform Configuration

Managed by admin via the Config/Settings panel:

| Setting | Description |
|---|---|
| `MinWithdrawalAmount` | Minimum amount a user can withdraw (e.g., ₱100) |
| `WithdrawalFee` | Standard fee per withdrawal — flat (e.g., ₱10) or percentage (e.g., 2%) |
| `PayPalExtraFeePercent` | Extra percentage fee added for PayPal transactions (e.g., 4.4%) |
| `PayPalExtraFeeFixed` | Extra fixed fee added for PayPal transactions (e.g., ₱15) |
| `PlatformGCash` | GCash number displayed on the top-up page |
| `PlatformBankName` | Bank name displayed on the top-up page |
| `PlatformBankAccount` | Bank account number displayed on the top-up page |
| `PlatformPayPal` | PayPal email/account displayed on the top-up page |
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
3. **Payment Methods** — GCash, Bank, PayPal with fee logic
4. **Events – 1v1** — Create, Invite, Accept, Cancel, Vote, Dispute trigger
5. **Events – Pool** — Create, Join, Result submission, Dispute trigger
6. **Dispute System** — Evidence upload, Admin resolution, Punishment
7. **Social** — Friends, Chat, Notifications
8. **Ranking** — Calculation engine, Display, Tier progression
9. **Admin Panel** — All admin capabilities
10. **Config / Settings** — Fee management, Platform payment info

---

## Important Notes

> ⚠️ **Legal**: Online betting platforms in the Philippines may require a **PAGCOR license**. Consult a legal professional before launching publicly.

> 💱 **Currency**: The platform operates in **Philippine Peso (PHP / ₱)**. PayPal transactions may involve currency conversion if the sender's PayPal is in a different currency — this should be communicated clearly to the user.

> 🕗 **Timezone**: All event dates and times are handled in **Philippine Standard Time (PST, UTC+8)**.

> 📄 **Receipts**: Only accept receipts from official GCash, bank transfer, or PayPal transaction confirmations to reduce fraud risk.

> 🔒 **Escrow Integrity**: Funds are locked immediately upon bet acceptance and cannot be withdrawn until the event is resolved or mutually cancelled.

> 💳 **PayPal Fees**: PayPal fees are subject to change. Admin should review and update `PayPalExtraFeePercent` and `PayPalExtraFeeFixed` in the config panel whenever PayPal updates its pricing.

---

*Built with ❤️ for the Philippine betting community.*
