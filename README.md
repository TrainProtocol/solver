


# 🚄 Train.Solver

**TrainSolver** is a modular and extensible C# application built on [Temporal.io](https://temporal.io), implementing the [TRAIN Protocol](https://train.xyz). It provides a trustless, permissionless, and scalable framework for cross-chain asset swaps across multiple blockchain networks, such as Ethereum-compatible chains (EVM), Starknet, and Solana.


| Component | Dockerfile Location | Image Badge |
|-----------|---------------------|-------------|
| API | `csharp/src/API/Dockerfile` | [![API](https://img.shields.io/docker/v/trainprotocol/train-solver-api?label=API&logo=docker)](https://hub.docker.com/r/trainprotocol/train-solver-api) |
| Workflow Runner (Swap) | `csharp/src/WorkflowRunner.Swap/Dockerfile` | [![WorkflowRunner-Swap](https://img.shields.io/docker/v/trainprotocol/train-solver-runner-swap?label=WorkflowRunner-Swap&logo=docker)](https://hub.docker.com/r/trainprotocol/train-solver-api) |
| Workflow Runner (EVM) | `csharp/src/WorkflowRunner.EVM/Dockerfile` | [![WorkflowRunner-EVM](https://img.shields.io/docker/v/trainprotocol/train-solver-runner-evm?label=WorkflowRunner-EVM&logo=docker)](https://hub.docker.com/r/trainprotocol/train-solver-runner-evm) |
| Workflow Runner (Solana) | `csharp/src/WorkflowRunner.Solana/Dockerfile` | [![WorkflowRunner-Solana](https://img.shields.io/docker/v/trainprotocol/train-solver-runner-solana?label=WorkflowRunner-Solana&logo=docker)](https://hub.docker.com/r/trainprotocol/train-solver-runner-solana) |
| Workflow Runner (Starknet) | `csharp/src/WorkflowRunner.Starknet/Dockerfile` | [![WorkflowRunner-Starknet](https://img.shields.io/docker/v/trainprotocol/train-solver-runner-starknet?label=WorkflowRunner-Starknet&logo=docker)](https://hub.docker.com/r/trainprotocol/train-solver-runner-starknet) |
| Workflow Runner (Starknet JS) | `js/Dockerfile` | [![WorkflowRunner-Starknet-JS](https://img.shields.io/docker/v/trainprotocol/train-solver-runner-starknet-js?label=WorkflowRunner-Starknet-JS&logo=docker)](https://hub.docker.com/r/trainprotocol/train-solver-runner-starknet-js) |

---

## 📚 Table of Contents

- [Overview](#overview)
- [Protocol Design](#protocol-design)
- [Project Structure](#project-structure)
- [Core Components](#core-components)
  - [Temporal Workflows](#temporal-workflows)
  - [Blockchain Activity Interface](#blockchain-activity-interface)
  - [System Workflows](#system-workflows)
- [Configuration](#configuration)
- [Infrastructure](#infrastructure)
- [Deployment](#deployment)
- [Extending the System](#extending-the-system)
- [License](#license)

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

## 🧱 Project Structure

```plaintext
TrainSolver.sln
└── src/
    ├── API/                             # Entry point / HTTP Interface
    ├── Blockchain/
    │   ├── Blockchain.Abstractions/    # Workflow & Activity interfaces
    │   ├── Blockchain.Common/          # Shared blockchain logic
    │   ├── Blockchain.EVM/             # EVM implementation
    │   ├── Blockchain.Starknet/        # Starknet implementation
    │   ├── Blockchain.Solana/          # Solana implementation
    │   ├── Blockchain.Swap/            # Core swap workflow
    │   └── Blockchain.Helpers/
    ├── Data/
    │   ├── Data.Abstractions/          # Repository interfaces
    │   └── Data.Npgsql/                # PostgreSQL + EF Core
    ├── Infrastructure/
    │   ├── Infrastructure.Abstractions/
    │   ├── Infrastructure.DependencyInjection/
    │   ├── Infrastructure.Logging.OpenTelemetry/
    │   ├── Infrastructure.Secret.AzureKeyVault/
    │   └── Infrastructure.TokenPrice.Coingecko/
    └── Shared/
        └── Util/                       # Shared utilities
```

---

## ⚙️ Core Components

### Temporal Workflows

Each blockchain integration must implement two Temporal workflows:

- **`TransactionProcessorWorkflow`**  
  Responsible for building and submitting transactions, handling nonces, fees, and confirmations. This workflow is *mandatory* for all integrations.

- **`EventListenerWorkflow`**  
  Continuously scans blockchain blocks for relevant smart contract events (e.g., `UserLock`). Upon detecting an event, it triggers the core `SwapWorkflow`.

The central `SwapWorkflow` (provided) orchestrates:
1. Locking destination funds by calling `TransactionProcessorWorkflow`.
2. Awaiting user confirmation.
3. Releasing funds upon approval.

> These workflows can be implemented in **any Temporal-supported language** (e.g., Go, TypeScript, Java) and registered as long as the Temporal Worker is configured properly.

---

### Blockchain Activity Interface

All blockchain interactions are defined in the `IBlockchainActivities` interface:

```csharp
public interface IBlockchainActivities
{
    Task<BalanceResponse> GetBalanceAsync(BalanceRequest request);
    Task<string> GetSpenderAddressAsync(SpenderAddressRequest request);
    Task<BlockNumberResponse> GetLastConfirmedBlockNumberAsync(BaseRequest request);
    Task<Fee> EstimateFeeAsync(EstimateFeeRequest request);
    Task<bool> ValidateAddLockSignatureAsync(AddLockSignatureRequest request);
    Task<HTLCBlockEventResponse> GetEventsAsync(EventRequest request);
    Task<string> GetNextNonceAsync(NextNonceRequest request);
    Task<PrepareTransactionResponse> BuildTransactionAsync(TransactionBuilderRequest request);
    Task<TransactionResponse> GetTransactionAsync(GetTransactionRequest request);
}
```

Default implementations are provided, but developers may customize and extend as needed for their specific chain logic.

---

### System Workflows

The following **scheduled workflows** handle critical background tasks:

- **`RouteStatusUpdater`**  
  Monitors hot wallet balances and toggles the availability of transfer routes.

- **`EventListenerUpdater`**  
  Starts or stops `EventListenerWorkflow` instances depending on route availability.

- **`TokenPriceUpdater`**  
  Periodically fetches token price data (default: Coingecko) and updates the database.

---

## 🛠 Configuration

Chain and route metadata is defined dynamically via a PostgreSQL database. Configuration includes:

- Registered blockchain networks
- Contract addresses
- Node URLs
- Token definitions
- Swap routing information

> The system dynamically interacts with blockchain integrations based on the configuration stored in the database.

---

## 🧩 Infrastructure

- **Database**: PostgreSQL with Entity Framework Core  
- **Secrets Management**: Azure Key Vault (for private key storage)  
- **Observability**: OpenTelemetry instrumentation with SigNoz as the backend  
- **Price Feeds**: Coingecko-based token pricing service  

---

## 🚀 Deployment

TrainSolver supports Docker-based deployments for local development or production. A `docker-compose.yml` is provided to start up the full stack, including Temporal services, API, and required infrastructure components.

---

## 🔌 Extending the System

To integrate a new blockchain:

1. Implement `TransactionProcessorWorkflow` and `EventListenerWorkflow`.
2. Implement `IBlockchainActivities` with required logic.
3. Register chain configuration in the database.
4. Build and deploy a Temporal worker that registers your workflows and activities.

> 🧠 These implementations can be done in any language supported by Temporal.io.

> 📦 Final step: Package your worker as a Docker image to run alongside the system.
