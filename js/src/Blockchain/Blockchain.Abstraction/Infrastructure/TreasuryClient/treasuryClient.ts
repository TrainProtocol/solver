import {
  BaseSignTransactionRequestModel,
  TreasuryGenerateAddressResponseModel,
  TreasurySignTransactionResponseModel,
  FuelSignTransactionRequest
} from "./Models";
import axios from "axios";

export class TreasuryClient {
  private apiClient

  constructor(signerAgentUrl: string) {
    this.apiClient = axios.create({
      baseURL: `${signerAgentUrl}/api/treasury/`,
      timeout: process.env.TrainSolver__TreasuryTimeout ?
        parseInt(process.env.TrainSolver__TreasuryTimeout) : 30000
    });
  }

  async signTransaction(
    networkType: string,
    request: BaseSignTransactionRequestModel | FuelSignTransactionRequest
  ): Promise<TreasurySignTransactionResponseModel> {
    const res = await this.apiClient.post(
      `${networkType.toLowerCase()}/sign`,
      request
    );

    return res.data;
  }

  async generateAddress(networkType: string): Promise<TreasuryGenerateAddressResponseModel> {
    const res = await this.apiClient.post(
      `${networkType.toLowerCase()}/generate`
    );

    return res.data;
  }
}
