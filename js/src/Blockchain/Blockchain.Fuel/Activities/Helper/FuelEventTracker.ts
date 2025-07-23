
import { bn, Contract, DateTime, formatUnits, Provider, ReceiptType } from "fuels";
import abi from '../ABIs/train.json';
import { HTLCBlockEventResponse, HTLCCommitEventMessage, HTLCLockEventMessage } from "../../../Blockchain.Abstraction/Models/EventModels/HTLCBlockEventResposne";
import { TokenCommittedEvent } from "../Models/FuelTokenCommitedEvents";
import { TokenLockedEvent } from "../Models/FuelTokenLockedEvent";
import { Tokens } from "../../../../Data/Entities/Tokens";

export default async function TrackBlockEventsAsync(
  networkName: string,
  nodeUrl: string,
  fromBlock: number,
  toBlock: number,
  htlcContractAddress: string,
  solverAddress: string,
  tokens: Tokens[],
): Promise<HTLCBlockEventResponse> {

  const tokenCommittedSelector = "8695557382153973144";
  const tokenLockAddedSelector = "12557029732458786074";

  const response: HTLCBlockEventResponse = {
    HTLCCommitEventMessages: [],
    HTLCLockEventMessages: [],
  };

  try {
    const provider = new Provider(nodeUrl);
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

    if (fromBlock == toBlock) fromBlock = fromBlock - 1;

    const variables = {
      first: toBlock - fromBlock,
      after: fromBlock.toString(),
    };

    const getBlockResponse = await fetch(nodeUrl, {
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
    const blocks = blockResponseJson?.data?.blocks?.nodes ?? [];

    if (!blocks || blocks.length === 0) {
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
        const sourceToken = tokens.find(t => t.asset === data.srcAsset.trim() && t.network.name === networkName);
        const destToken = tokens.find(t => t.asset === data.dstAsset.trim() && t.network.name === data.dstChain.trim());
        const timelock = DateTime.fromTai64(data.timelock);
        const commitId = bn(data.Id).toHex();
        
        const commitMsg: HTLCCommitEventMessage = {
          TxId: transaction.id,
          Id: commitId,
          Amount: Number(formatUnits(data.amount, sourceToken.decimals)),
          AmountInWei: data.amount.toString(),
          ReceiverAddress: solverAddress,
          SourceNetwork: networkName,
          SenderAddress: data.sender.bits,
          SourceAsset: data.srcAsset.trim(),
          DestinationAddress: data.dstAddress.trim(),
          DestinationNetwork: data.dstChain.trim(),
          DestinationAsset: data.dstAsset.trim(),
          TimeLock: timelock.toUnixSeconds(),
          DestinationNetworkType: destToken.network.type,
          SourceNetworkType: sourceToken.network.type,
        };

        response.HTLCCommitEventMessages.push(commitMsg);

      }
      else if (transactionSelector === tokenLockAddedSelector) {

        const decodedData = contractInstance.interface.decodeLog(
          logDataReceipt.data,
          transactionSelector
        );

        const data = decodedData[0] as TokenLockedEvent;

        const timelock = DateTime.fromTai64(data.timelock);
        const hashlock = bn(data.hashlock).toHex();
        const commitId = bn(data.Id).toHex();

        const lockMsg: HTLCLockEventMessage = {
          TxId: transaction.id,
          Id: commitId,
          HashLock: hashlock,
          TimeLock: timelock.toUnixSeconds(),
        };

        response.HTLCLockEventMessages.push(lockMsg);
      }
    }

    return response;
  }
  catch (error) {

    throw error;
  }
}