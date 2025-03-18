import { WithdrawalRequest } from "../../../lib/models/WithdrawalModels/WithdrawalRequest";
import { StarknetWithdrawalActivity } from "./StarknetTransferService";

describe("Test for withdrawal", () => { 
    it('returnstx id for valid input', async () => {
        // Arrange
        let request: WithdrawalRequest = {
            NodeUrl: "https://starknet-sepolia.blastapi.io/b80cc803-ddc6-4582-9e56-481ec38ec039/rpc/v0_7",
            FromAddress: "0x03bd42ec6bc6fa63f376db9da3df346bfca470bf88a66fdd5389695860497fa2",
            TokenContract: "0x049d36570d4e46f48e99674bd3fcc84644ddd6b96f7c741b1562b82f9e004dc7",
            Decimals: 18,
            CorrelationId: "fasdf",
            ReferenceId: "fasdf",
            Network: "STARKNET",
            ChainId: "0x534e5f5345504f4c4941",
            Nonce: "210",
            Fee: {
                FeeInWei: "1000600823816",
            },
        };   
        
        const response = await StarknetWithdrawalActivity(request);
    
        // Assert
      });
});

