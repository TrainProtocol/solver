# 🚄 Train.Solver

The following Docker images are available for Train.Solver components:

| Component | Dockerfile Location | Image Badge |
|-----------|---------------------|-------------|
| API | `csharp/src/API/Dockerfile` | [![API](https://img.shields.io/docker/v/trainsolver/api?label=API&logo=docker)](https://hub.docker.com/r/trainsolver/api) |
| Workflow Runner | `csharp/src/WorkflowRunner/Dockerfile` | [![WorkflowRunner](https://img.shields.io/docker/v/trainsolver/workflow-runner?label=WorkflowRunner&logo=docker)](https://hub.docker.com/r/trainsolver/workflow-runner) |
| Workflow Runner (EVM) | `csharp/src/WorkflowRunner.EVM/Dockerfile` | [![WorkflowRunner-EVM](https://img.shields.io/docker/v/trainsolver/workflow-runner-evm?label=WorkflowRunner-EVM&logo=docker)](https://hub.docker.com/r/trainsolver/workflow-runner-evm) |
| Workflow Runner (Solana) | `csharp/src/WorkflowRunner.Solana/Dockerfile` | [![WorkflowRunner-Solana](https://img.shields.io/docker/v/trainsolver/workflow-runner-solana?label=WorkflowRunner-Solana&logo=docker)](https://hub.docker.com/r/trainsolver/workflow-runner-solana) |
| Workflow Runner (Starknet) | `csharp/src/WorkflowRunner.Starknet/Dockerfile` | [![WorkflowRunner-Starknet](https://img.shields.io/docker/v/trainsolver/workflow-runner-starknet?label=WorkflowRunner-Starknet&logo=docker)](https://hub.docker.com/r/trainsolver/workflow-runner-starknet) |
| Workflow Runner (Starknet JS) | `js/Dockerfile` | [![WorkflowRunner-Starknet-JS](https://img.shields.io/docker/v/trainsolver/workflow-runner-starknet-js?label=WorkflowRunner-Starknet-JS&logo=docker)](https://hub.docker.com/r/trainsolver/workflow-runner-starknet-js) |
| Workflow Runner (Swap) | `csharp/src/WorkflowRunner.Swap/Dockerfile` | [![WorkflowRunner-Swap](https://img.shields.io/docker/v/trainsolver/workflow-runner-swap?label=WorkflowRunner-Swap&logo=docker)](https://hub.docker.com/r/trainsolver/workflow-runner-swap) |

---

Train.Solver is a **cross-chain liquidity provider application** that facilitates **atomic swaps** between blockchain networks. It ensures secure, efficient, and trustless asset transfers across multiple chains.

---

## 🔍 How It Works 

Train.Solver continuously monitors events across all configured blockchains and waits for a **user funds lock event**. Once detected, the following steps occur:

1. The application locks an equivalent amount, minus fees, on the **destination chain**.
2. After the user **confirms the quote**, the app **releases the funds** in the destination chain.
3. Finally, Train.Solver **claims the funds** from the source chain.

This mechanism guarantees a **secure and atomic** cross-chain transaction process, ensuring seamless liquidity provision.

---

## 🛠 Technologies Used

Train.Solver leverages the following technologies to ensure efficient and secure operations:

- **Temporal.io** - Used for managing workflows and transaction execution.
- **PostgreSQL** - Stores network configurations, routing information, and archived swap data.
- **Azure KeyVault** - Securely stores private keys for transaction signing.
- **Redis** - Implements distributed locks to ensure consistency across multiple instances.
- **C#** - Primary language for core components, with support for additional network integrations in any language supported by Temporal.io.

---

## 🏗 System Components

Train.Solver consists of **three core components**:

### 🏦 Core

The Core module manages blockchain-related operations, including:

- Retrieving balances
- Fetching transactions
- Publishing transactions
- Signing and verifying transactions

### 🌐 API

The API provides external access for:

- Retrieving available blockchain networks
- Fetching liquidity quotes
- Checking swap limits
- Querying active swaps

### 🔄 Temporal.io Workflow Runner

The Workflow Runner automates swap execution by managing:

- Transaction monitoring
- Workflow execution for swaps
- Failure handling and recovery mechanisms

---

## 🔗 Integrating a New Network

To integrate a new blockchain network into Train.Solver, follow these steps:

### ⚡ Create Workflow Execution Logic

Develop a corresponding **transaction handler** in Temporal workflows to execute the necessary transaction steps:

- Define workflow logic for swap execution.
- Handle fund locking, unlocking, and failure recovery.

### ⚙️ Implement Core Services

Ensure required **Core services** are implemented to be callable from workflow activities:

- Interaction with blockchain nodes
- Execution of swap transactions
- Status monitoring

### 🗄 Configure Database
Store essential network details such as chain ID, network name, logo, RPC nodes, routes, and other metadata.


Once these steps are completed, the network will be fully integrated into Train.Solver, ensuring seamless cross-chain swaps.
