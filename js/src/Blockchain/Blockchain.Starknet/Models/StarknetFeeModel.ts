import { ResourceBounds } from "starknet";
import { Fee } from "../../Blockchain.Abstraction/Models/FeesModels/Fee";

export interface StarknetFeeModel extends Fee
{
    ResourceBounds: ResourceBounds;
}