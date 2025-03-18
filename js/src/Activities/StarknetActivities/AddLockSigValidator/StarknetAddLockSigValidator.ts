import { HTLCAddLockSigBuilderRequest } from "../../../lib/models/TransactionBuilderModels/HTLCAddLockSigBuildetRequest";
import { cairo, RpcProvider, shortString, TypedData, TypedDataRevision } from "starknet";

export async function StarknetAddLockSigValidatorActivity(request: HTLCAddLockSigBuilderRequest): Promise<boolean>
{
  try
  {
    
    const provider = new RpcProvider({
        nodeUrl: request.NodeUrl
    });

    
    const addlockData: TypedData = {
        domain: {
            name: 'Train',
            version: shortString.encodeShortString("v1"),
            chainId: request.ChainId,
            revision: TypedDataRevision.ACTIVE,
        },
        primaryType: 'AddLockMsg',
        types: {
            StarknetDomain: [
                {
                    name: 'name',
                    type: 'shortstring',
                },
                {
                    name: 'version',
                    type: 'shortstring',
                },
                {
                    name: 'chainId',
                    type: 'shortstring',
                },
                {
                    name: 'revision',
                    type: 'shortstring'
                }
            ],
            AddLockMsg: [
                { name: 'Id', type: 'u256' },
                { name: 'hashlock', type: 'u256' },
                { name: 'timelock', type: 'u256' }
            ],
        },
        message: {
            Id: cairo.uint256(request.Id),
            hashlock: cairo.uint256(request.Hashlock),
            timelock: cairo.uint256(request.Timelock),
        },
    }
    
    return await provider.verifyMessageInStarknet(addlockData, request.SignatureArray, request.SignerAddress);
  }
  catch (error)
  {
    throw error;
  }
}
