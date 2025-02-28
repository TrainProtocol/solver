import { WithdrawalRequest } from "../../../lib/models/WithdrawalModels/WithdrawalRequest";
import { StarknetWithdrawalActivity } from "./StarknetTransferService";

describe("Test for withdrawal", () => { 
    it('returnstx id for valid input', async () => {
        // Arrange
        let request: WithdrawalRequest = {
            NodeUrl: "https://starknet-sepolia.blastapi.io/b80cc803-ddc6-4582-9e56-481ec38ec039/rpc/v0_7",
            TokenContract: "0x049d36570d4e46f48e99674bd3fcc84644ddd6b96f7c741b1562b82f9e004dc7",
            FromAddress: "0x03Bd42Ec6bc6fA63F376dB9Da3DF346bFCA470Bf88a66FdD5389695860497FA2",
            ToAddress: "0x05990362a9dda6da80bfed7f3fee94d28595f5669db802a651ab7007f36b0bd7",
            Decimals: 18,
            Amount: 0,
            CorrelationId: "fasdf",
            ReferenceId: "fasdf",
            Asset: "ETH",
            Network: "STARKNET"
        };   
        
        const response = await StarknetWithdrawalActivity(request);
    
        // Assert
      });
});