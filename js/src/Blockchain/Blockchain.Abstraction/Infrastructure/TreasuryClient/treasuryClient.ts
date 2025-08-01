import { generateAddressResponseModel, signTransactionRequestModel, signTransactionResponseModel } from "./Models";
import axios from "axios";

const apiClient = axios.create({
  baseURL: `/api/treasury/${process.env.TrainSolver__TreasuryUrl}`,
  timeout: process.env.TrainSolver__TreasuryTimeout ?
   parseInt(process.env.TrainSolver__TreasuryTimeout) : 30000
});


export class TreasuryClient {

  async signTransaction(
    networkType: string,
    request: signTransactionRequestModel
  ): Promise<signTransactionResponseModel> {
    const res = await apiClient.post<ApiResponse<signTransactionResponseModel>>(
      `${networkType}/sign`,
      request
    );
    return res.data.data;
  }

  async generateAddress(networkType: string): Promise<generateAddressResponseModel> {
    const res = await apiClient.post<ApiResponse<generateAddressResponseModel>>(
      `${networkType}/generate`
    );
    return res.data.data;
  }
}

interface ApiResponse<T> {
  data: T;
  success: boolean;
  message?: string;
}