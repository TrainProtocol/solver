export interface AztecFunctionInteractionModel {
    interactionAddress: string,
    functionName: string,
    args: any[],
    callerAddress?: string,
    authwiths?: AztecFunctionInteractionModel[],
}