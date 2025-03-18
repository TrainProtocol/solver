import { GetFeesRequest } from "../../../lib/models/GetFeesModels/GetFeesRequest";
import { HTLCAddLockSigBuilderRequest as HTLCAddLockSigBuilderRequest } from "../../../lib/models/TransactionBuilderModels/HTLCAddLockSigBuildetRequest";
import { FunctionName, HTLCLockTransferBuilderRequest } from "../../../lib/models/TransactionBuilderModels/HTLCLockTransferBuilderRequest";
import { StarknetGetFeesActivity } from "../FeeProvider/StarknetGetFees";
import { StarknetTransactionBuilderActivity } from "./StarknetTransactionBuilderActivity";

describe("Test for transactionBuilder", () => { 
    it('returns correct balance for valid input', async () => {
        // Arrange
        let request: HTLCAddLockSigBuilderRequest = {
            Id: "0x8551b5a9d09887c6d8785243ce7e6d392b652cebcc931b349b214beb5ab629d3",
            Hashlock: "0x6522de2d983e19d1f4d0b05d6e42dd6b3e698863ab4ebed4f20103ee7de375e8",
            Timelock: "1740247695",
            ChainId: "0x534e5f5345504f4c4941",
            NodeUrl: "https://starknet-sepolia.blastapi.io/b80cc803-ddc6-4582-9e56-481ec38ec039/rpc/v0_7",
            SignerAddress: "0x0762ad0b96a23016c83322d04d3b48df94ea3d4c9dc9dabab63cb2fd7bb8f3a5",
            FunctionName: FunctionName.AddLockSig,
            HTLCContractAddress: "0x02ae9eb99f49ef9807e4984e882cd9611e486f386237b4af2766d248d935f974",
            CorrelationId: "null",
            ReferenceId: "null",
            IsErc20: true,
            SignatureArray: [
                "1",
                "1550963859147513828717540871994831972681427894690049861484113669910776063310",
                "2371233011532634937676462112833305267820468687941122482305947885676614402340"
            ]
        };
        
        const response = await StarknetTransactionBuilderActivity(request);

        let getFeesRequest: GetFeesRequest = {
            CorrelationId: "0xdc1622cc6420e564842cd0e3543f8235943609b9f57c0d0a00f8e972872d17cf",
            Decimals: 18,
            FromAddress: "0x03Bd42Ec6bc6fA63F376dB9Da3DF346bFCA470Bf88a66FdD5389695860497FA2",
            NodeUrl: "https://starknet-sepolia.blastapi.io/b80cc803-ddc6-4582-9e56-481ec38ec039/rpc/v0_7",
            Symbol: "ETH",
            TokenContract: "0x049d36570d4e46f48e99674bd3fcc84644ddd6b96f7c741b1562b82f9e004dc7",
            CallData: response.Data
        };

        const feeResponse = await StarknetGetFeesActivity(getFeesRequest);

    
        // Assert
      });
});