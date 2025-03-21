import { BigNumberish, Calldata, constants } from "starknet";
import { ETransactionVersion2 } from "starknet-types-07";


export type CalcV2InvokeTxHashArgs = {
    senderAddress: BigNumberish;
    version: ETransactionVersion2;
    compiledCalldata: Calldata;
    maxFee: BigNumberish;
    chainId: constants.StarknetChainId;
    nonce: BigNumberish;
  };