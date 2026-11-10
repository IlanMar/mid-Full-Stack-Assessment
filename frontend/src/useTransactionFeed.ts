import { HubConnectionBuilder, HttpTransportType } from '@microsoft/signalr';
import { useEffect, useState } from 'react';
import { mergeFeed, type FeedEntry } from './feed';

type Connection = 'Connecting' | 'Live' | 'Reconnecting';

export function useTransactionFeed() {
  const [entries, setEntries] = useState<FeedEntry[]>([]);
  const [connection, setConnection] = useState<Connection>('Connecting');
  const [error, setError] = useState('');

  useEffect(() => {
    const hub = new HubConnectionBuilder()
      .withUrl('/hubs/transactions', {
        transport: HttpTransportType.WebSockets,
        skipNegotiation: true,
      })
      .withAutomaticReconnect({ nextRetryDelayInMilliseconds: () => 2000 })
      .build();

    let disposed = false;
    let retry: ReturnType<typeof setTimeout> | undefined;
    let request: AbortController | undefined;
    let buffered: FeedEntry[] | null = [];

    hub.on('TransactionAdded', (entry: FeedEntry) => {
      if (disposed) return;
      if (buffered) buffered = mergeFeed(buffered, [entry]);
      else setEntries(current => mergeFeed(current, [entry]));
    });

    async function synchronize() {
      const controller = new AbortController();
      request = controller;
      setError('');

      try {
        const response = await fetch('/api/transactions', { signal: controller.signal, cache: 'no-store' });
        if (!response.ok) throw new Error('Snapshot request failed');
        const snapshot: FeedEntry[] = await response.json();
        if (disposed || controller.signal.aborted) return;

        setEntries(mergeFeed(snapshot, buffered ?? []));
        buffered = null;
        setConnection('Live');
      } catch {
        if (disposed || controller.signal.aborted) return;
        setError('Could not load recent transactions. Retrying…');
        retry = setTimeout(() => void synchronize(), 2000);
      }
    }

    function resetSnapshot() {
      clearTimeout(retry);
      request?.abort();
      buffered = [];
    }

    hub.onreconnecting(() => {
      if (disposed) return;
      resetSnapshot();
      setError('');
      setConnection('Reconnecting');
    });
    hub.onreconnected(() => {
      if (!disposed) void synchronize();
    });

    async function connect() {
      try {
        await hub.start();
        if (!disposed) void synchronize();
      } catch {
        if (disposed) return;
        setError('Could not connect to the monitor. Retrying…');
        retry = setTimeout(() => void connect(), 2000);
      }
    }

    void connect();
    return () => {
      disposed = true;
      resetSnapshot();
      hub.off('TransactionAdded');
      void hub.stop();
    };
  }, []);

  return { entries, connection, error };
}
