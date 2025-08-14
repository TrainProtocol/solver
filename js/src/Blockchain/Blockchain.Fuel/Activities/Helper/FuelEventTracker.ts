
import { bn, Contract, DateTime, Provider, ReceiptType } from "fuels";
import abi from '../ABIs/train.json';
import { HTLCBlockEventResponse, HTLCCommitEventMessage, HTLCLockEventMessage } from "../../../Blockchain.Abstraction/Models/EventModels/HTLCBlockEventResposne";
import { TokenCommittedEvent } from "../Models/FuelTokenCommitedEvents";
import { TokenLockedEvent } from "../Models/FuelTokenLockedEvent";
import { DetailedNetworkDto } from "../../../Blockchain.Abstraction/Models/DetailedNetworkDto";
import { formatAddress } from "../FuelBlockchainActivities";
import { ethers } from "ethers";

export default async function TrackBlockEventsAsync(
  network: DetailedNetworkDto,
  fromBlock: number,
  toBlock: number,
  solverAddresses: string[],
): Promise<HTLCBlockEventResponse> {

  const tokenCommittedSelector = "8695557382153973144";
  const tokenLockAddedSelector = "12557029732458786074";
  const htlcContractAddress = network.htlcTokenContractAddress;

  const response: HTLCBlockEventResponse = {
    htlcCommitEventMessages: [],
    htlcLockEventMessages: [],
  };

  try {
    const provider = new Provider(network.nodes[0].url);
    const contractInstance = new Contract(htlcContractAddress, abi, provider);

    const query = `
      query ($first: Int!, $after: String!) {
        blocks(first: $first, after: $after) {
          nodes {
            height
            transactions {
              id
              inputContracts
            }
          }
        }
      }
    `;

    if (fromBlock == toBlock) {
      fromBlock = fromBlock - 1;
    }

    const variables = {
      first: toBlock - fromBlock,
      after: fromBlock.toString(),
    };

    const getBlockResponse = await fetch(network.nodes[0].url, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Accept': 'application/json',
      },
      body: JSON.stringify({ query, variables }),
    });

    if (!getBlockResponse.ok) {
      const errorText = await getBlockResponse.text();
      throw new Error(`Network error: ${getBlockResponse.status} - ${errorText}`);
    }

    const blockResponseJson = await getBlockResponse.json();
    const blocks = blockResponseJson?.data?.blocks?.nodes;

    if (!blocks) {
      throw new Error(`No blocks found between ${fromBlock} and ${toBlock}`);
    }

    const filteredTransactions = blocks.flatMap(b => b.transactions).filter(tx =>
      tx.inputContracts.some(
        (c: string) => c.toLowerCase() === htlcContractAddress.toLowerCase()
      ));

    for (const transaction of filteredTransactions) {

      const transactionSummary = await (await provider.getTransactionResponse(transaction.id)).getTransactionSummary();

      const logDataReceipt = transactionSummary.receipts.find(r => r.type === ReceiptType.LogData);
      if (!logDataReceipt) {

        continue;
      }

      const transactionSelector = logDataReceipt.rb.toString();

      if (transactionSelector === tokenCommittedSelector) {

        const decodedData = contractInstance.interface.decodeLog(
          logDataReceipt.data,
          transactionSelector
        );

        const data = decodedData[0] as TokenCommittedEvent;
        const timelock = DateTime.fromTai64(data.timelock);

        const commitId = ensureHexLength(bn(data.Id).toString(16), 32);

        const receiverAddress = solverAddresses.find(
          x => formatAddress(x) === formatAddress(data.srcReceiver.bits)
        );

        const commitMsg: HTLCCommitEventMessage = {
          txId: transaction.id,
          commitId: commitId,
          amount: Number(data.amount).toString(),
          receiverAddress: receiverAddress,
          sourceNetwork: network.name,
          senderAddress: data.sender.bits,
          sourceAsset: data.srcAsset.trim(),
          destinationAddress: data.dstAddress.trim(),
          destinationNetwork: data.dstChain.trim(),
          destinationAsset: data.dstAsset.trim(),
          timeLock: timelock.toUnixSeconds(),
        };

        response.htlcCommitEventMessages.push(commitMsg);

      }
      else if (transactionSelector === tokenLockAddedSelector) {

        const decodedData = contractInstance.interface.decodeLog(
          logDataReceipt.data,
          transactionSelector
        );

        const data = decodedData[0] as TokenLockedEvent;

        const timelock = DateTime.fromTai64(data.timelock);
        const hashlock = bn(data.hashlock).toHex();
        const commitId = ensureHexLength(bn(data.Id).toString(16), 32);
        
        const lockMsg: HTLCLockEventMessage = {
          txId: transaction.id,
          commitId: commitId,
          hashLock: hashlock,
          timeLock: timelock.toUnixSeconds(),
        };

        response.htlcLockEventMessages.push(lockMsg);
      }
    }

    return response;
  }
  catch (error) {

    throw error;
  }
}

function ensureHexLength(hex: string, bytes: number): string {
  // Remove 0x prefix if present
  const cleanHex = hex.startsWith("0x") ? hex.slice(2) : hex;
  // Pad to desired byte length (bytes → hex chars = bytes * 2)
  return "0x" + cleanHex.padStart(bytes * 2, "0");
}
