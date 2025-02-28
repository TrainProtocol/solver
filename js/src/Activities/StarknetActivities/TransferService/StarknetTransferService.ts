import { WithdrawalRequest } from "../../../lib/models/WithdrawalModels/WithdrawalRequest";
import { utils } from "ethers";
import { Account, CallData, cairo, RpcProvider } from 'starknet';
import { PrivateKeyRepository } from "../../../lib/PrivateKeyRepository";

const FEE_ESTIMATE_MULTIPLIER = BigInt(4);

export async function StarknetWithdrawalActivity(request: WithdrawalRequest): Promise<string>
{
  try
  {
    let result: string;

    const amountToWithdraw = utils.parseUnits(request.Amount.toString(), request.Decimals);

    const privateKey = await new PrivateKeyRepository().getAsync(request.FromAddress);

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

    const executeResponse = await account.execute(
        transferCall,
        undefined,
        {
            maxFee: feeEstimateResponse.suggestedMaxFee * FEE_ESTIMATE_MULTIPLIER,
            nonce: request.Nonce
        },
    );

    result = executeResponse.transaction_hash;

    if (!result || !result.startsWith("0x")) {
        throw new Error(`Withdrawal response didn't contain a correct transaction hash. Response: ${JSON.stringify(executeResponse)}`);
    }

    return result;    
  }
  catch (error)
  {
    throw error;
  }
}