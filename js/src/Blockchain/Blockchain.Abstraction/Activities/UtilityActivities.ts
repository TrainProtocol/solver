import { TransactionType } from "../Models/TransacitonModels/TransactionType";
import { v4 as uuidv4 } from 'uuid';

export function BuildProcessorId(networkName: string, type: TransactionType): string {
   return `${networkName}-${type}-${uuidv4()}`;
}
