import {
  Column,
  Entity,
  Index,
  JoinColumn,
  ManyToOne,
  PrimaryGeneratedColumn,
} from "typeorm";
import { Networks } from "./Networks";

@Index("IX_ManagedAccounts_Address", ["address"], {})
@Index("PK_ManagedAccounts", ["id"], { unique: true })
@Index("IX_ManagedAccounts_NetworkId", ["networkId"], {})
@Entity("ManagedAccounts", { schema: "public" })
export class ManagedAccounts {
  @PrimaryGeneratedColumn({ type: "integer", name: "Id" })
  id: number;

  @Column("text", { name: "Address" })
  address: string;

  @Column("integer", { name: "Type" })
  type: number;

  @Column("integer", { name: "NetworkId" })
  networkId: number;

  @Column("timestamp with time zone", {
    name: "CreatedDate",
    default: () => "now()",
  })
  createdDate: Date;

  @ManyToOne(() => Networks, (networks) => networks.managedAccounts, {
    onDelete: "CASCADE",
  })
  @JoinColumn([{ name: "NetworkId", referencedColumnName: "id" }])
  network: Networks;
}

export enum AccountType {
  LP ,
  Charging,
}
