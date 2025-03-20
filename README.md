# 🚄 Train.Solver

Train.Solver is a **cross-chain liquidity provider application** that facilitates **atomic swaps** between blockchain networks. It ensures secure, efficient, and trustless asset transfers across multiple chains.

![Docker Image Version For Workflow Runner CS](https://img.shields.io/docker/v/trainprotocol/train-solver-wf-runner-cs/dev?label=WorkflowRunnerCS)
![Docker Image Version For Workflow Runner JS](https://img.shields.io/docker/v/trainprotocol/train-solver-wf-runner-js/dev?label=WorkflowRunnerJS)
![Docker Image Version for API](https://img.shields.io/docker/v/trainprotocol/train-solver-api/dev?label=API)

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
