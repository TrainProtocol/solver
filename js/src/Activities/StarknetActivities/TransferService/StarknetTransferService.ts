import { WithdrawalRequest } from "../../../lib/models/WithdrawalModels/WithdrawalRequest";
import { Account, RpcProvider, hash, number, BigNumberish, Calldata, constants, Call, CallData, cairo, TransactionType, transaction } from 'starknet';
import { PrivateKeyRepository } from "../../../lib/PrivateKeyRepository";
import { ETransactionVersion2 } from "starknet-types-07";
import { ParseNonces as ParseNonces } from "../Helper/ErrorParser";
import { InvalidTimelockException } from "../../../Exceptions/InvalidTimelockException";

type CalcV2InvokeTxHashArgs = {
  senderAddress: BigNumberish;
  version: ETransactionVersion2;
  compiledCalldata: Calldata;
  maxFee: BigNumberish;
  chainId: constants.StarknetChainId;
  nonce: BigNumberish;
};

export async function StarknetWithdrawalActivity(request: WithdrawalRequest): Promise<string> {
  let result: string;

  const privateKey = await new PrivateKeyRepository().getAsync(request.FromAddress);

  const provider = new RpcProvider({
    nodeUrl: request.NodeUrl
  });

  const account = new Account(provider, request.FromAddress, privateKey, '1');

  var transferCall: Call = JSON.parse(request.CallData);

  const compiledCallData = transaction.getExecuteCalldata([transferCall], await account.getCairoVersion());

  const args: CalcV2InvokeTxHashArgs = {
    senderAddress: request.FromAddress,
    version: ETransactionVersion2.V1,
    compiledCalldata: compiledCallData,
    maxFee: request.Fee.FeeInWei,
    chainId: request.ChainId as constants.StarknetChainId,
    nonce: request.Nonce
  };

  const calcualtedTxHash = await hash.calculateInvokeTransactionHash(args);

  try {

    const executeResponse = await account.execute(
      [transferCall],
      undefined,
      {
        maxFee: request.Fee.FeeInWei,
        nonce: request.Nonce
      },
    );

    result = executeResponse.transaction_hash;

    if (!result || !result.startsWith("0x")) {
      throw new Error(`Withdrawal response didn't contain a correct transaction hash. Response: ${JSON.stringify(executeResponse)}`);
    }

    return result;
  }
  catch (error) {
    const nonceInfo = ParseNonces(error?.message);

    if (nonceInfo && nonceInfo.providedNonce < nonceInfo.expectedNonce) {
      return calcualtedTxHash;
    }

    throw error;
  }
}

export async function StarknetSimulateTransactionActivity(request: WithdrawalRequest): Promise<void> {

  const privateKey = await new PrivateKeyRepository().getAsync(request.FromAddress);

  const provider = new RpcProvider({
    nodeUrl: request.NodeUrl
  });

  const account = new Account(provider, request.FromAddress, privateKey, '1');

  var transferCall: Call = JSON.parse(request.CallData);

  try {

    await account.simulateTransaction(
      [
        {
          type: TransactionType.INVOKE,
          payload: [transferCall]
        }
      ],
      {
        nonce: request.Nonce
      });
  }
  catch (error) {
    const nonceInfo = ParseNonces(error?.message);

    if (nonceInfo && nonceInfo.providedNonce > nonceInfo.expectedNonce) {
      throw new Error(`The nonce is too high. Current nonce: ${nonceInfo.providedNonce}, expected nonce: ${nonceInfo.expectedNonce}`);
    }
    else if (nonceInfo && nonceInfo.providedNonce < nonceInfo.expectedNonce) {
      throw new Error(`The nonce is too low. Current nonce: ${nonceInfo.providedNonce}, expected nonce: ${nonceInfo.expectedNonce}`);
    }
    else if (error?.message && error.message.includes("Invalid TimeLock")) {
      throw new InvalidTimelockException("Invalid TimeLock error encountered");
    }

    throw error;
  }
}
