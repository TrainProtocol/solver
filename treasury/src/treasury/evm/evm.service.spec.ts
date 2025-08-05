import { Test, TestingModule } from '@nestjs/testing';
import { EvmTreasuryService } from './evm.service';
import { EVMSignRequest, EVMSignResponse } from './evm.dto';
import { PrivateKeyService } from '../../kv/vault.service';
import { BadRequestException } from '@nestjs/common';

describe('EVM Signer Service', () => {
  let signer: EvmTreasuryService;

  const privateKey = '0x4cf6b3e772a8042d68700ecbea4e2a3ece01117439585cbc46fea98bbfedceab';
  const fromAddress = '0xe5a124ecc494aa00a969b48c79364037a4a2dfeb';
  const signedTxn = '0x02f87583aa36a728850649534e0985064961898282520894169da96eef4ce602e8101cf5261553a127a4a21d865af3107a400080c001a0b946b3c9fc98fdf3777a0db7554949582acbbb32b5af855088fbf69d0a97678da068cbc10e897beb343dd1ed600d94c16b7a62cc0223762257eee33991fccddd55';
  
  const mockEvmSignRequest: EVMSignRequest = {
    address: fromAddress,
    unsignedTxn: '02f283aa36a728850649534e0985064961898282520894169da96eef4ce602e8101cf5261553a127a4a21d865af3107a400080c0',
  };

  let mockPrivateKeyService: any;

  const network  = 'evm';

  beforeEach(async () => {
     mockPrivateKeyService = {
      getAsync: jest
        .fn()
        .mockImplementation((address: string) =>
          Promise.resolve(privateKey),
        ),
      setAsync: jest
        .fn()
        .mockImplementation((address: string, privateKey: string) => 
          Promise.resolve()
        ),
    };

    const app: TestingModule = await Test.createTestingModule({
      providers: [
        EvmTreasuryService,
        {
          provide: PrivateKeyService,
          useValue: mockPrivateKeyService
        },
      ],
    })
    .compile();

    signer = app.get<EvmTreasuryService>(EvmTreasuryService);
  });

  it('should have network property set to evm', () => {
    expect(signer.network).toBe(network);
  });

  describe('sign()', () => {
    it.each([
      {
        description: 'correct request',
        request: { ...mockEvmSignRequest, unsignedTxn: `0x${mockEvmSignRequest.unsignedTxn}` }
      },
      {
        description: 'non-hex message', 
        request: { ...mockEvmSignRequest, address: mockEvmSignRequest.address.slice(2) }
      },
      {
        description: 'uppercase address',
        request: { ...mockEvmSignRequest, address: mockEvmSignRequest.address.toUpperCase() }
      }
    ])('should sign a valid transaction successfully', async ({ request }) => {
      const response = await signer.sign(request);
      const expectedResponse: EVMSignResponse = { signedTxn: signedTxn }

      expect(response).toEqual(expectedResponse);
    });

    it('should throw bad request on invalid address', async () => {
      await expect(signer.sign({...mockEvmSignRequest, address: 'invalid_address' }))
        .rejects.toThrow(BadRequestException);
    });

    it('should throw bad request on invalid unsigned transaction', async () => {
      await expect(signer.sign({...mockEvmSignRequest, unsignedTxn: 'invalid_unsigned_transaction' }))
        .rejects.toThrow(BadRequestException);
    });
  });

  describe('generate()', () => {
    it('should generate a lowercase address and store private key in vault', async () => {
      const response = await signer.generate();
      
      expect(response.address).toBe(response.address.toLowerCase());
      
      expect(mockPrivateKeyService.setAsync).toHaveBeenCalledWith(
        response.address,
        expect.any(String)
      );
    });
  });
});