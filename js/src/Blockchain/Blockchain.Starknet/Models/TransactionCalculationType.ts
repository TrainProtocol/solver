import { BigNumberish, Calldata, constants, ETransactionVersion2 } from "starknet";

export type CalcV2InvokeTxHashArgs = {
  senderAddress: BigNumberish;
  version: ETransactionVersion2;
  compiledCalldata: Calldata;
  maxFee: BigNumberish;
  chainId: constants.StarknetChainId;
  nonce: BigNumberish;
};