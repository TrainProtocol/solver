import {
  BaseSignTransactionRequestModel,
  TreasuryGenerateAddressResponseModel,
  TreasurySignTransactionResponseModel,
  FuelSignTransactionRequestModel,
  StarknetSignTransactionRequestModel,
} from "./Models";
import axios from "axios";
import { AztecSignTransactionRequest } from "./Models/AztecSignTransactionRequest";

export class TreasuryClient {
  private apiClient;

  constructor(signerAgentUrl: string) {
    this.apiClient = axios.create({
      baseURL: `${signerAgentUrl}/api/treasury/`,
      timeout: 600000
    });
  }

  async signTransaction(
    networkType: string,
    request: BaseSignTransactionRequestModel | FuelSignTransactionRequestModel | StarknetSignTransactionRequestModel | AztecSignTransactionRequest
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
