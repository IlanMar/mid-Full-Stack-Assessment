export type TransactionStatus = 'Pending' | 'Completed' | 'Failed';

export type Transaction = {
  transactionId: string;
  amount: number;
  currency: string;
  status: TransactionStatus;
  timestamp: string;
};

export type FeedEntry = {
  sequence: number;
  transaction: Transaction;
};

export function mergeFeed(current: FeedEntry[], incoming: FeedEntry[]): FeedEntry[] {
  const byId = new Map<string, FeedEntry>();
  for (const entry of [...current, ...incoming]) {
    const id = entry.transaction.transactionId;
    const previous = byId.get(id);
    if (!previous || entry.sequence > previous.sequence) byId.set(id, entry);
  }

  return [...byId.values()].sort((a, b) => b.sequence - a.sequence).slice(0, 200);
}
