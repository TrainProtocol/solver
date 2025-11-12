export async function sendInvocation(rpcUrl: string, invocation: object): Promise<string> {

    const res = await fetch(
        rpcUrl,
        {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({
                jsonrpc: "2.0",
                id: 1,
                method: "starknet_addInvokeTransaction",
                params: [invocation],
            }),
        });

    const body = await res.json();

    if (body.error) {
        throw new Error(`RPC error ${body.error.code}: ${body.error.message}`);
    }

    if (body.result.transaction_hash) {
        return body.result.transaction_hash;
    }
}
