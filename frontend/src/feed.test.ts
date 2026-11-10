import assert from 'node:assert/strict';
import { test } from 'node:test';
import { mergeFeed, type FeedEntry } from './feed.ts';

function entry(sequence: number, transactionId = String(sequence)): FeedEntry {
  return {
    sequence,
    transaction: { transactionId, amount: 12.5, currency: 'USD', status: 'Completed', timestamp: '2025-01-01T00:00:00Z' },
  };
}

test('merges snapshot and buffered messages without duplicates', () => {
  const snapshot = [entry(3), entry(2), entry(1)];
  const buffered = [entry(4), entry(3), entry(5)];
  assert.deepEqual(mergeFeed(snapshot, buffered).map(item => item.sequence), [5, 4, 3, 2, 1]);
});

test('keeps the newest 200 even when messages arrive out of order', () => {
  const snapshot = Array.from({ length: 200 }, (_, i) => entry(i + 2));
  const result = mergeFeed(snapshot, [entry(202), entry(1)]);
  assert.equal(result.length, 200);
  assert.equal(result[0].sequence, 202);
  assert.equal(result.at(-1)?.sequence, 3);
});

test('keeps the latest acceptance of an ID reused after eviction', () => {
  assert.deepEqual(mergeFeed([entry(302, 'same')], [entry(1, 'same')]), [entry(302, 'same')]);
});
