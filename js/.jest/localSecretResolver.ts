export default function getLocalSecret(secretKey: string): Promise<string> {
    let jsonConf = require('../local.settings.json');

    return Promise.resolve(jsonConf.KeyVault.DefaultAccount);
}

export function getStarkLocalSecret(secretKey: string): Promise<string> {
    let jsonConf = require('../local.settings.json');
   
    return Promise.resolve(jsonConf.KeyVault.DefaultAccountSTARK);
}

export function getSeedLocalSecret(secretKey: string): Promise<string> {
    let jsonConf = require('../local.settings.json');
    
    return Promise.resolve(jsonConf.KeyVault.DefaultAccountSeed);
}

export function getPasswordLocalSecret(): Promise<string> {
    let jsonConf = require('../local.settings.json');
    
    return Promise.resolve(jsonConf.KeyVault.DefaultAccountPassword);
}

export function setLocalSecret(publicKey: string, privateKey: string): Promise<string> {
    let jsonConf = require('../local.settings.json');
    return Promise.resolve(jsonConf.KeyVault.DefaultAccountSTARK);
}