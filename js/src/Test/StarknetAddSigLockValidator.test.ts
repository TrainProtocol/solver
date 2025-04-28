import { BigNumber, utils } from "ethers";
import { RpcProvider } from "starknet";
import 'reflect-metadata';
import { TrackBlockEventsAsync } from "../Blockchain/Blockchain.Starknet/Activities/Helper/StarknetEventTracker";

describe("Test for StarknetWithdrawal", () => {
  it('returns correct balance for valid input', async () => {
    const provider = new RpcProvider({ nodeUrl: "https://starknet-sepolia.blastapi.io/b80cc803-ddc6-4582-9e56-481ec38ec039/rpc/v0_7" });


    const a = await TrackBlockEventsAsync(
      "STARKNET_SEPOLIA",
      provider,
      [],
      "0x03Bd42Ec6bc6fA63F376dB9Da3DF346bFCA470Bf88a66FdD5389695860497FA2",
      687694,
      687694,
      "0x02dc7ec536415e3482342120b64f95530012135ddd7538c9c3ae2905e011049c"
    );


    expect(a).toEqual(true);
  });
});