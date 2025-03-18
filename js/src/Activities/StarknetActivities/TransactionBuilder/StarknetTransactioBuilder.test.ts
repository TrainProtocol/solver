import { GetFeesRequest } from "../../../lib/models/GetFeesModels/GetFeesRequest";
import { HTLCLockTransferBuilderRequest } from "../../../lib/models/TransactionBuilderModels/HTLCLockTransferBuilderRequest";
import { TransferBuilderRequest } from "../../../lib/models/TransactionBuilderModels/TransferBuilderRequest";
import { FunctionName } from "../../../lib/models/TransactionBuilderModels/TransferBuilderRequestBase";
import { WithdrawalRequest } from "../../../lib/models/WithdrawalModels/WithdrawalRequest";
import { StarknetGetFeesActivity } from "../FeeProvider/StarknetGetFees";
import { StarknetWithdrawalActivity } from "../TransferService/StarknetTransferService";
import { StarknetTransactionBuilderActivity } from "./StarknetTransactionBuilderActivity";
import * as dotenv from 'dotenv';

describe("Test for transactionBuilder", () => {
    it('', async () => {
        dotenv.config();

        // Arrange
        // let request: HTLCLockTransferBuilderRequest = {
        //     Amount: 0.000318,
        //     AmountInWei: "318000000000000",
        //     Reward: 0,
        //     RewardInWei: "0",
        //     RewardTimelock: "1741353529",
        //     Hashlock: "0x9bd53479a6d2b34b83028944dedf6d030be76bb2929f48e4bfac915d55c5dbcd",
        //     Timelock: "1741357129",
        //     Receiver: "0x0762ad0b96a23016c83322d04d3b48df94ea3d4c9dc9dabab63cb2fd7bb8f3a5",
        //     SourceAsset: "ETH",
        //     DestinationChain: "OPTIMISM_SEPOLIA",
        //     DestinationAddress: "0x2330bc7d79f670f51546dcf5fd0eca6889a7ceb9",
        //     DestinationAsset: "ETH",
        //     Id: "0x3e6c6797acc9d1eb73f5014193a54da30016d6040098f67750f90217e897af8e",
        //     TokenContract: "0x049d36570d4e46f48e99674bd3fcc84644ddd6b96f7c741b1562b82f9e004dc7",
        //     CorrelationId: "null",
        //     ReferenceId: "null",
        //     ContractAddress: "0x02dc7ec536415e3482342120b64f95530012135ddd7538c9c3ae2905e011049c",
        //     IsErc20: true,
        //     FunctionName: FunctionName.Lock
        // };


        // const response = await StarknetTransactionBuilderActivity(request);

        // let getFeesRequest: GetFeesRequest = {
        //     CorrelationId: "0xdc1622cc6420e564842cd0e3543f8235943609b9f57c0d0a00f8e972872d17cf",
        //     Decimals: 18,
        //     FromAddress: "0x03Bd42Ec6bc6fA63F376dB9Da3DF346bFCA470Bf88a66FdD5389695860497FA2",
        //     NodeUrl: "https://starknet-sepolia.blastapi.io/b80cc803-ddc6-4582-9e56-481ec38ec039/rpc/v0_7",
        //     Symbol: "ETH",
        //     TokenContract: "0x049d36570d4e46f48e99674bd3fcc84644ddd6b96f7c741b1562b82f9e004dc7",
        //     CallData: response.Data
        // };

        // const feeResponse = await StarknetGetFeesActivity(getFeesRequest);

        // let transferRequest: TransferBuilderRequest = {
        //     CorrelationId: "asd",
        //     ReferenceId: "asd",
        //     ContractAddress: "0x049d36570d4e46f48e99674bd3fcc84644ddd6b96f7c741b1562b82f9e004dc7",
        //     ToAddress: "0x03bd42ec6bc6fa63f376db9da3df346bfca470bf88a66fdd5389695860497fa2",
        //     AmountInWei: "0",
        //     IsErc20: true,
        //     FunctionName: FunctionName.Transfer
        // };

        // const transferResponse = await StarknetTransactionBuilderActivity(transferRequest);

        // let withdrawalRequest: WithdrawalRequest = {
        //     NodeUrl: "https://starknet-sepolia.blastapi.io/b80cc803-ddc6-4582-9e56-481ec38ec039/rpc/v0_7",
        //     FromAddress: "0x03bd42ec6bc6fa63f376db9da3df346bfca470bf88a66fdd5389695860497fa2",
        //     TokenContract: "0x049d36570d4e46f48e99674bd3fcc84644ddd6b96f7c741b1562b82f9e004dc7",
        //     Decimals: 18,
        //     CorrelationId: "fasdf",
        //     ReferenceId: "fasdf",
        //     Network: "STARKNET",
        //     ChainId: "0x534e5f5345504f4c4941",
        //     Nonce: "253",
        //     CallData: transferResponse.Data,
        //     Fee: {
        //         FeeInWei: "1072487878512",
        //     },
        // };

        // const withdrawaResponse = await StarknetWithdrawalActivity(withdrawalRequest);


        // Assert
    });
});