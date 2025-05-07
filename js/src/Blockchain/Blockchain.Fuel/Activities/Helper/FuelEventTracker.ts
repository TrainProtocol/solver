
import { Contract, formatUnits, Provider, ReceiptType } from "fuels";
import abi from '../ABIs/ERC20.json';
import { HTLCBlockEventResponse, HTLCCommitEventMessage, HTLCLockEventMessage } from "../../../Blockchain.Abstraction/Models/EventModels/HTLCBlockEventResposne";
import { TokenCommittedEvent } from "../Models/FuelTokenCommitedEvents";
import { TokenLockedEvent } from "../Models/FuelTokenLockedEvent";
import { Tokens } from "../../../../Data/Entities/Tokens";
import { BigIntToAscii } from "../../../Blockchain.Abstraction/Extensions/StringExtensions";

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

        const sourceToken = tokens.find(t => t.asset === BigIntToAscii(decodedData.srcAsset) && t.network.name === networkName);
        const destToken = tokens.find(t => t.asset === BigIntToAscii(decodedData.dstAsset) && t.network.name === BigIntToAscii(decodedData.dstChain));

        const commitMsg: HTLCCommitEventMessage = {
          TxId: transaction.id,
          Id: data.Id.toString(),
          Amount: Number(formatUnits(data.amount, destToken.decimals)),
          AmountInWei: data.amount.toString(),
          ReceiverAddress: solverAddress,
          SourceNetwork: networkName,
          SenderAddress: data.sender.bits,
          SourceAsset: data.srcAsset,
          DestinationAddress: data.dstAddress,
          DestinationNetwork: data.dstChain,
          DestinationAsset: data.dstAsset,
          TimeLock: Number(data.timelock),
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

        const lockMsg: HTLCLockEventMessage = {
          TxId: transaction.id,
          Id: data.Id.toString(),
          HashLock: data.hashlock.toString(),
          TimeLock: Number(data.timelock),
        };

        response.HTLCLockEventMessages.push(lockMsg);

      }
      else {
        throw new Error(`Unknown selector: ${transactionSelector}`);
      }
    }

    return response;
  }
  catch (error) {

    throw error;
  }
}