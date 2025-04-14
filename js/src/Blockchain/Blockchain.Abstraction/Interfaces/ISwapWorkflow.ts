import { defineSignal, defineUpdate } from '@temporalio/workflow';
import { HTLCCommitEventMessage, HTLCLockEventMessage } from '../Models/EventModels/HTLCBlockEventResposne';
import { AddLockSignatureRequest } from '../Models/TransactionBuilderModels/AddLockSignatureRequest';

export const lockCommitedSignal = defineSignal<[HTLCLockEventMessage]>('LockCommited');

export const setAddLockSigUpdate = defineUpdate<boolean, [AddLockSignatureRequest]>('SetAddLockSig');

export interface ISwapWorkflow {
  LockCommited: typeof lockCommitedSignal;
  SetAddLockSig: typeof setAddLockSigUpdate;
  RunAsync(message: HTLCCommitEventMessage): Promise<void>;
}
