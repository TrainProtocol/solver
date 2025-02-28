import { SufficientBalanceRequest } from "../../../lib/models/GetBalanceModels/GetBalanceRequest";
import { Abi, Contract, RpcProvider, uint256 } from "starknet";
import { BigNumber } from "@ethersproject/bignumber";
import { utils } from "ethers";
import erc20Json from '../ABIs/ERC20.json';

export async function StarknetEnsureSufficientBalanceActivity(request: SufficientBalanceRequest): Promise<void> {
  try 
  {
    const provider = new RpcProvider({
        nodeUrl: request.NodeUrl
    });

    const erc20 = new Contract(erc20Json as Abi, request.TokenContract, provider);

    const balanceResult = await erc20.balanceOf(request.Address);
    const balanceInWei = BigNumber.from(uint256.uint256ToBN(balanceResult.balance as any).toString()).toString();
    const balance = Number(utils.formatUnits(balanceInWei, request.Decimals));

    if(balance <= request.Amount)
    {
      throw new Error(`Insufficient balance on ${request.Address}. Balance is less than ${request.Amount}`);
    }
  }
  catch (error) 
  {
    throw error;
  }
}
