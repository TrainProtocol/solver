


# 🚄 Train.Solver

**TrainSolver** is a modular and extensible application built on [Temporal.io](https://temporal.io), implementing the [TRAIN Protocol](https://train.xyz). It provides a trustless, permissionless, and scalable framework for cross-chain asset swaps across multiple blockchain networks, such as Ethereum-compatible chains (EVM), Starknet, Solana, Fuel, TON, Aptos & any altVM.


| Component | Dockerfile Location | Image Badge |
|-----------|---------------------|-------------|
| API | `csharp/src/API/Dockerfile` | [![API](https://img.shields.io/docker/v/trainprotocol/solver-api?label=API&logo=docker)](https://hub.docker.com/r/trainprotocol/solver-api) |
| Runner (Swap Core) | `csharp/src/Workflow.Swap/Dockerfile` | [![Swap Core](https://img.shields.io/docker/v/trainprotocol/solver-swap?label=Swap&logo=docker)](https://hub.docker.com/r/trainprotocol/solver-api) |
| Runner (EVM) | `csharp/src/Workflow.EVM/Dockerfile` | [![EVM](https://img.shields.io/docker/v/trainprotocol/solver-evm?label=EVM&logo=docker)](https://hub.docker.com/r/trainprotocol/solver-evm) |
| Runner (Solana) | `csharp/src/Workflow.Solana/Dockerfile` | [![Solana](https://img.shields.io/docker/v/trainprotocol/solver-solana?label=Solana&logo=docker)](https://hub.docker.com/r/trainprotocol/solver-solana) |
| Runner (Starknet) | `js/Dockerfile ARG starknet` | [![Starknet](https://img.shields.io/docker/v/trainprotocol/solver-starknet?label=Starknet&logo=docker)](https://hub.docker.com/r/trainprotocol/solver-starknet) |
| Runner (Fuel) | `js/Dockerfile ARG fuel` | [![Fuel](https://img.shields.io/docker/v/trainprotocol/solver-fuel?label=Fuel&logo=docker)](https://hub.docker.com/r/trainprotocol/solver-Fuel) |

---

## 📚 Table of Contents

- [Overview](#-overview)
- [Protocol Design](#-protocol-design)
- [Project Structure](#️-project-structure)
  - [Core Components](#-core-components)
  - [Pluggable Integrations](#-pluggable-integrations)    
  - [Currently Integrated Networks](#-currently-integrated-networks)
- [Infrastructure](#-infrastructure)
- [Deployment](#-deployment)

---

## 🧭 Overview

TrainSolver enables secure, atomic, and permissionless cross-chain asset transfers by coordinating on-chain events through Temporal workflows. Users retain full control of their assets at all times, while new blockchain networks can onboard seamlessly via a shared security and workflow abstraction.

The architecture ensures:

- **Trustless Transfers** — assets are only moved under user-approved conditions.
- **Permissionless Integration** — no central approval is required for adding new blockchains.
- **Scalable Design** — supports horizontal onboarding of new chains and workflows.

---

## 🚆 Protocol Design

The **TRAIN Protocol** leverages an *intent-and-solver* model secured by **Atomic Swaps** and **Local Verification** mechanisms (such as light clients in browsers). It defines a universal workflow-based interface for performing cross-chain transfers that includes:

- **Intent Creation** — user signals desire to move assets across chains.
- **Lock Mechanism** — assets are locked on the source chain.
- **Verification & Confirmation** — the swap is validated locally and cryptographically.
- **Destination Unlocking** — funds are released upon confirmation on the destination chain.

This ensures a uniform and secure experience regardless of the underlying blockchain.

---

## 🗱️ Project Structure

TrainSolver is composed of two primary layers:

- **Core Components** – maintained by the protocol team; responsible for orchestration, APIs, and system infrastructure.
- **Pluggable Integrations** – modular blockchain adapters implemented by external contributors or the core team using any supported Temporal SDK.

---

### 🔧 Core Components

These components are typically written in .NET and form the backbone of the system:

- **Solver API**\
  Public HTTP service responsible for:

  - `getQuote`
  - `getSwapInfo`
  - `getAvailableRoutes`

- **Admin API & Dashboard**\
  Internal management interface used to:

  - Configure supported blockchains
  - Register tokens and routes
  - Adjust system behavior

- **Core Workflows**\
  Temporal-based orchestrators that manage the full swap lifecycle:

  - Lock and release funds
  - Handle confirmations
  - Monitor balances and route status
  - Fetch and update token prices

- **SignerAgent**\
  Lightweight signing microservice hosted by the client, used to:

  - Store and manage private keys securely using **HashiCorp Vault**
  - Sign transactions using the appropriate chain-specific algorithm
  - Expose signing endpoints for use by TrainSolver Cloud or hybrid deployments

  > 🛡️ Allows clients to retain full control over their keys while enabling delegated execution.

---

### 🔌 Pluggable Integrations

Each blockchain integration is a standalone Temporal worker that interfaces with the Core Workflows and SignerAgent.

Pluggable components must be implemented in **any language supported by** [**Temporal SDKs**](https://docs.temporal.io/docs/sdk-overview) — including TypeScript, Go, Python, Java, and .NET.

To integrate a new blockchain, implement the following:

#### 1. `TransactionProcessorWorkflow`

A Temporal workflow that:

- Constructs and submits blockchain transactions
- Monitors transaction confirmations
- Triggers state transitions in the swap lifecycle
- Handles retries and error scenarios

> This is the **only required workflow** per integration.

---

#### 2. Core Blockchain Activity Interface

The following activities must be implemented, as they are called by **core workflows** (e.g., `RouteStatusUpdater`, `SwapWorkflow`, `EventListenerUpdater`):

```ts
interface BlockchainActivities {
    getBalance(BalanceRequest): BalanceResponse;
    getLastConfirmedBlockNumber(BaseRequest): BlockNumberResponse;
    validateAddLockSignature(AddLockSignatureRequest): boolean;
    getEvents(EventRequest): HTLCBlockEventResponse;
    buildTransaction(TransactionBuilderRequest): PrepareTransaction;
}
```

---

#### 3. Additional Activities (Optional)

Depending on your blockchain’s requirements, you may implement additional activities used **within** your own `TransactionProcessorWorkflow` (e.g., for fee estimation, nonce retrieval, or custom signing logic).

---

#### 4. SignerAgent Implementation

You must also extend the **SignerAgent** to support your blockchain’s native signing algorithm:

- Implement transaction signing logic specific to your chain (e.g., ECDSA, Ed25519, Cairo)
- Ensure private key access via Vault is secure and isolated
- Expose HTTP endpoints used by TrainSolver Cloud to request signatures

> 🔐 SignerAgent ensures keys are never exposed to shared infrastructure, maintaining strict key custody boundaries.

---

### ✅ Currently Integrated Networks

| Chain Type     | Language   |
| -------------- | ---------- |
| EVM-compatible | .NET       |
| Solana         | .NET       |
| Starknet       | TypeScript |
| Fuel           | TypeScript |

> ✨ More integrations are actively being developed, including Bitcoin, Aztec, and others.

---

## 🧩 Infrastructure

- **Database**: PostgreSQL with Entity Framework Core  
- **Secrets Management**: Hashicorp Key Vault (for private key storage)  
- **Observability**: OpenTelemetry instrumentation with SigNoz as the backend  
- **Price Feeds**: Coingecko-based token pricing service  

---

## 🚀 Deployment

TrainSolver supports Docker-based deployments for local development or production. A `docker-compose.yml` is provided to start up the full stack, including Temporal services, API, and required infrastructure components.

---
