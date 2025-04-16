import 'reflect-metadata';
import { DataSource, Repository } from 'typeorm';
import { Contracts } from './Entities/Contracts';
import { Networks } from './Entities/Networks';
import { ManagedAccounts } from './Entities/ManagedAccounts';
import { Nodes } from './Entities/Nodes';
import { Tokens } from './Entities/Tokens';

export class SolverContext {
  private dataSource: DataSource;

  private nodesRepo: Repository<Nodes>;
  private tokensRepo: Repository<Tokens>;
  private contracts: Repository<Contracts>;
  private networks: Repository<Networks>;
  private managedAccounts: Repository<ManagedAccounts>;

  constructor(connectionString: string) {
    this.dataSource = new DataSource({
      type: 'postgres',
      url: convertAdoNetToPostgresUri(connectionString),
      entities: [Nodes, Tokens, Contracts, Networks, ManagedAccounts],
      synchronize: false,
      logging: false,
    });

    this.init();
  }

  private async init(): Promise<void> {
    await this.dataSource.initialize();
    this.nodesRepo = this.dataSource.getRepository(Nodes);
    this.tokensRepo = this.dataSource.getRepository(Tokens);
    this.contracts = this.dataSource.getRepository(Contracts);
    this.networks = this.dataSource.getRepository(Networks);
    this.managedAccounts = this.dataSource.getRepository(ManagedAccounts);
  }

  public get Nodes() {
    return this.nodesRepo;
  }

  public get Tokens() {
    return this.tokensRepo;
  }

  public get Contracts() {
    return this.contracts;
  }

  public get Networks() {
    return this.networks;
  }

  public get ManagedAccounts() {
    return this.managedAccounts;
  }
}


function convertAdoNetToPostgresUri(adoNetConnectionString: string): string {
  const keyValuePairs = adoNetConnectionString
    .split(';')
    .map(pair => pair.trim())
    .filter(pair => pair.length > 0)
    .map(pair => {
      const [key, ...valueParts] = pair.split('=');
      return { key: key.trim().toLowerCase(), value: valueParts.join('=').trim() };
    });

  const params: Record<string, string> = {};
  keyValuePairs.forEach(({ key, value }) => {
    params[key] = value;
  });

  const username = params['user id'];
  const password = params['password'];
  const host = params['server'];
  const port = params['port'];
  const database = params['database'];

  if (!username) throw new Error('Missing "User Id" in connection string');
  if (!password) throw new Error('Missing "Password" in connection string');
  if (!host) throw new Error('Missing "Server" in connection string');
  if (!port) throw new Error('Missing "Port" in connection string');
  if (!database) throw new Error('Missing "Database" in connection string');

  const queryParams = [];

  if (params['ssl mode']?.toLowerCase() === 'require') {
    queryParams.push('sslmode=require');
  }

  if (params['trust server certificate']?.toLowerCase() === 'true') {
    queryParams.push('trustServerCertificate=true');
  }

  const queryString = queryParams.length > 0 ? `?${queryParams.join('&')}` : '';

  return `postgres://${encodeURIComponent(username)}:${encodeURIComponent(password)}@${host}:${port}/${database}${queryString}`;
}
