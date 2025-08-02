import { FuelSignTransactionRequestModel } from "../../../Blockchain.Fuel/Activities/Models/FuelSignTransactionModel";
import { TresurySignTransactionRequestModel, TreasuryGenerateAddressResponseModel, TreasurySignTransactionResponseModel } from "./Models";
import axios from "axios";

const apiClient = axios.create({
  baseURL: `${process.env.TrainSolver__TreasuryUrl}/api/treasury/`,
  timeout: process.env.TrainSolver__TreasuryTimeout ?
    parseInt(process.env.TrainSolver__TreasuryTimeout) : 30000
});


export class TreasuryClient { 
  private apiClient;

  constructor() {
    this.apiClient = axios.create({
      baseURL: `${process.env.TrainSolver__TreasuryUrl}/api/treasury/`,
      timeout: process.env.TrainSolver__TreasuryTimeout
        ? parseInt(process.env.TrainSolver__TreasuryTimeout)
        : 30000,
    });
  }

  async signTransaction(
    networkType: string,
    request: TresurySignTransactionRequestModel | FuelSignTransactionRequestModel
  ): Promise<TreasurySignTransactionResponseModel> {

    const res = await this.apiClient.post(
      `${networkType.toLocaleLowerCase()}/sign`,
      request
    );

    return res.data;
  }

  async generateAddress(networkType: string): Promise<TreasuryGenerateAddressResponseModel> {
    const res = await this.apiClient.post(
      `${networkType.toLocaleLowerCase()}/generate`
    );

    return res.data;
  }
}