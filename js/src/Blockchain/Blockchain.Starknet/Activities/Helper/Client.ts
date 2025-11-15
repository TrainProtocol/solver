import { uint256 } from "starknet";

export async function sendInvocation(rpcUrl: string, invocation: InvokeTransactionV3): Promise<string> {

    const requestBody = serializeWithBigInt({
                jsonrpc: "2.0",
                id: 1,
                method: "starknet_addInvokeTransaction",
                params: [ invocation ],
            });

    const res = await fetch(
        rpcUrl,
        {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: requestBody,
        });

    const body = await res.json();

    if (body.error) {
        throw new Error(`RPC error ${body.error.code}: ${body.error.message}`);
    }

    if (body.result.transaction_hash) {
        return body.result.transaction_hash;
    }
}

function serializeWithBigInt(obj: unknown): string {
    return JSON.stringify(obj, (_key, value) =>
      typeof value === 'bigint' ? uint256.bnToUint256(value) : value
    );
  }

  export interface InvokeTransactionV3 {
    type: any;
    sender_address: string;
    calldata: string[];
    version: string;
    signature: string[];
    nonce: string;
    resource_bounds: {
        l2_gas: {
        max_amount: string;
        max_price_per_unit: string;
        };
        l1_gas: {
        max_amount: string;
        max_price_per_unit: string;
        };
        l1_data_gas?: {
        max_amount: string;
        max_price_per_unit: string;
        };
    };
    tip: string;
    paymaster_data: string[];
    account_deployment_data: string[];
    nonce_data_availability_mode: 'L1' | 'L2';
    fee_data_availability_mode: 'L1' | 'L2';
    }
