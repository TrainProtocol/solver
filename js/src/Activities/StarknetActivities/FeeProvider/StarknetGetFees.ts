import { InvalidTimelockException } from "../../../Exceptions/InvalidTimelockException";
import { GetFeesRequest } from "../../../lib/models/GetFeesModels/GetFeesRequest";
import { BigNumber } from "ethers";
import { Account, cairo, CallData, RpcProvider } from "starknet";
import { Fee, FixedFeeData, GetFeesResponse } from "../../../lib/models/GetFeesModels/GetFeesResponse";
import { PrivateKeyRepository } from "../../../lib/PrivateKeyRepository";

const FeeSymbol = "ETH";
const FeeDecimals = 18;
const FEE_ESTIMATE_MULTIPLIER = BigInt(4);

export async function StarknetGetFeesActivity(request: GetFeesRequest): Promise<GetFeesResponse>
{
  try
  {
    var result: any = new Map<string, Fee>();

    const privateKey = await new PrivateKeyRepository().getAsync(request.FromAddress);
    const amountToWithdraw = BigNumber.from(1);        

    const provider = new RpcProvider({
        nodeUrl: request.NodeUrl
    });

    const account = new Account(provider, request.FromAddress, privateKey, '1');

    let transferCall = request.CallData 
            ? JSON.parse(request.CallData) 
            : {
                contractAddress: request.TokenContract.toLowerCase(),
                entrypoint: "transfer",
                calldata: CallData.compile({
                    recipient: request.FromAddress,
                    amount: cairo.uint256(amountToWithdraw.toHexString())
                })
            };    

    let feeEstimateResponse = await account.estimateFee(transferCall);
    if (!feeEstimateResponse?.suggestedMaxFee) {
        throw new Error(`Couldn't get fee estimation for the transfer. Response: ${JSON.stringify(feeEstimateResponse)}`);
    };
     
    const feeInWei = (feeEstimateResponse.suggestedMaxFee * FEE_ESTIMATE_MULTIPLIER).toString();

    const fixedfeeData: FixedFeeData = {
        FeeInWei: feeInWei,
    };
    
    result[FeeSymbol] = {
        Asset: FeeSymbol,
        Decimals: FeeDecimals,
        FixedFeeData : fixedfeeData
    };

    return result;
  }
  catch (error: any) {
    if (error?.message && error.message.includes("Invalid TimeLock")) 
    {
      throw new InvalidTimelockException("Invalid TimeLock error encountered");
    }
    throw error;
  }
}