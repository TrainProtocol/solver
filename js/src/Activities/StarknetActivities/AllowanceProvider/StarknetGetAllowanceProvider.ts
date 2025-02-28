import { GetAllowanceRequest } from "../../../lib/models/GetAllowanceModels/GetAllowanceRequest";
import { Contract, RpcProvider } from "starknet";
import { utils } from "ethers";

export async function StarknetGetAllowanceActivity(request: GetAllowanceRequest): Promise<number>
{
  try
  {    
    const provider = new RpcProvider({ nodeUrl: request.NodeUrl });
    const { abi: tokenAbi } = await provider.getClassAt(request.TokenContract);
    const ercContract = new Contract(tokenAbi, request.TokenContract, provider);
    var response : BigInt = await ercContract.allowance(request.OwnerAddress, request.SpenderAddress);

    return Number(utils.formatUnits(response.toString(), request.Decimals))
  }
  catch (error)
  {
    throw error;
  }
}