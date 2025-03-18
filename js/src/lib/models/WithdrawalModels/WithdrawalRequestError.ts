import { WithdrawalErrorType } from "../Errors/WithdrawalErrorType";

export interface WithdrawalRequestError {
    ErrorCode: WithdrawalErrorType,
    ErrorMessage: string
}