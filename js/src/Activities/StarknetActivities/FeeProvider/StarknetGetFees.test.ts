import { GetFeesRequest } from "../../../lib/models/GetFeesModels/GetFeesRequest";
import { StarknetGetFeesActivity } from "../FeeProvider/StarknetGetFees";

describe("Test for fee estimation", () => { 
    it('returns correct balance for valid input', async () => {
        
        let getFeesRequest: GetFeesRequest = {
            CorrelationId: "0xdc1622cc6420e564842cd0e3543f8235943609b9f57c0d0a00f8e972872d17cf",
            Decimals: 18,
            FromAddress: "0x03Bd42Ec6bc6fA63F376dB9Da3DF346bFCA470Bf88a66FdD5389695860497FA2",
            NodeUrl: "https://starknet-sepolia.blastapi.io/b80cc803-ddc6-4582-9e56-481ec38ec039/rpc/v0_7",
            Symbol: "ETH",
            TokenContract: "0x049d36570d4e46f48e99674bd3fcc84644ddd6b96f7c741b1562b82f9e004dc7",
            CallData: "{\"entrypoint\":\"lock\",\"calldata\":[{\"low\":\"118571283640518350089197437853347726\",\"high\":\"82975004904503668376835229960146079139\"},{\"low\":\"15823112893773512921210971801969351629\",\"high\":\"207137362901066165446822835564985675011\"},{\"low\":\"0\",\"high\":\"0\"},{\"low\":\"1741353529\",\"high\":\"0\"},{\"low\":\"1741357129\",\"high\":\"0\"},\"0x0762ad0b96a23016c83322d04d3b48df94ea3d4c9dc9dabab63cb2fd7bb8f3a5\",\"0x455448\",\"0x4f5054494d49534d5f5345504f4c4941\",{\"data\":[\"0x30783233333062633764373966363730663531353436646366356664306563\"],\"pending_word\":\"0x6136383839613763656239\",\"pending_word_len\":11},\"0x455448\",{\"low\":\"318000000000000\",\"high\":\"0\"},\"0x049d36570d4e46f48e99674bd3fcc84644ddd6b96f7c741b1562b82f9e004dc7\"]}"
        };

        const feeResponse = await StarknetGetFeesActivity(getFeesRequest);

    
        // Assert
      });
});