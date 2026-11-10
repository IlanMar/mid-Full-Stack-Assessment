import { useState } from 'react';
import { createRoot } from 'react-dom/client';
import type { Transaction, TransactionStatus } from './feed';
import { useTransactionFeed } from './useTransactionFeed';
import './styles.css';

const isSimulator = window.location.pathname === '/add';
const statuses: TransactionStatus[] = ['Completed', 'Pending', 'Failed'];
const amountFormat = new Intl.NumberFormat('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 8 });
const timeFormat = new Intl.DateTimeFormat('en-GB', {
  dateStyle: 'short',
  timeStyle: 'medium',
  timeZone: 'UTC',
});

function makeTransaction(index: number): Transaction {
  return {
    transactionId: crypto.randomUUID(),
    amount: Math.round((5 + Math.random() * 1995) * 100) / 100,
    currency: 'USD',
    status: statuses[index % statuses.length],
    timestamp: new Date().toISOString(),
  };
}

function Simulator() {
  const [sending, setSending] = useState(false);
  const [result, setResult] = useState<{ sent: number; failed: number }>();

  async function send(count: number) {
    setSending(true);
    setResult(undefined);
    let sent = 0;

    for (let offset = 0; offset < count; offset += 10) {
      const batch = Array.from({ length: Math.min(10, count - offset) }, async (_, index) => {
        const transaction = makeTransaction(count === 1 ? Math.floor(Math.random() * 3) : offset + index);
        const response = await fetch('/api/transactions', {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify(transaction),
        });
        return response.ok;
      });
      const outcomes = await Promise.allSettled(batch);
      sent += outcomes.filter(outcome => outcome.status === 'fulfilled' && outcome.value).length;
    }

    setResult({ sent, failed: count - sent });
    setSending(false);
  }

  return (
    <>
      <div className="heading">
        <h1>Transaction simulator</h1>
        <p>Generate sample transactions to see how they appear in the live monitor.</p>
      </div>
      <section className="panel simulator" aria-label="Generate transactions">
        <div className="actions">
          <button className="primary" disabled={sending} onClick={() => void send(1)}>Send one</button>
          <button disabled={sending} onClick={() => void send(100)}>Send 100</button>
        </div>
        <p className="feedback" role="status">
          {sending ? 'Sending…' : result ? `Sent ${result.sent} · Failed ${result.failed}` : 'Ready to send.'}
        </p>
        <a href="/monitor" target="_blank" rel="noopener">Open live monitor in a new tab ↗</a>
      </section>
    </>
  );
}

function Monitor() {
  const { entries, connection, error } = useTransactionFeed();
  const [failedOnly, setFailedOnly] = useState(false);
  const visible = failedOnly
    ? entries.filter(entry => entry.transaction.status === 'Failed')
    : entries;

  return (
    <>
      <div className="heading monitor-heading">
        <div>
          <h1>Live monitor</h1>
          <p>The latest transactions as they arrive.</p>
        </div>
        <span className={connection === 'Live' ? 'connection live' : 'connection'} role="status">{connection}</span>
      </div>
      {error && <p className="notice" role="status">{error}</p>}
      <section className="panel" aria-label="Recent transactions">
        <div className="toolbar">
          <span>{visible.length} {visible.length === 1 ? 'transaction' : 'transactions'}</span>
          <label><input type="checkbox" checked={failedOnly} onChange={e => setFailedOnly(e.target.checked)} /> Errors only</label>
        </div>
        <div className="table-scroll">
          <table>
            <thead>
              <tr>
                <th scope="col">Transaction ID</th>
                <th scope="col" className="number">Amount</th>
                <th scope="col">Status</th>
                <th scope="col">Time (UTC)</th>
              </tr>
            </thead>
            <tbody>
              {visible.map(({ sequence, transaction }) => (
                <tr key={transaction.transactionId + ':' + sequence}>
                  <td className="transaction-id">{transaction.transactionId}</td>
                  <td className="number">{amountFormat.format(transaction.amount)} <span className="muted">{transaction.currency}</span></td>
                  <td><span className={`badge ${transaction.status.toLowerCase()}`}>{transaction.status}</span></td>
                  <td><time dateTime={transaction.timestamp}>{timeFormat.format(new Date(transaction.timestamp))}</time></td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
        {!visible.length && (
          <p className="empty">
            {failedOnly ? 'No failed transactions.' : connection === 'Live' ? 'No transactions yet. Send one from the simulator.' : 'Waiting for the transaction feed…'}
          </p>
        )}
      </section>
    </>
  );
}

function App() {
  return (
    <>
      <header className="site-header">
        <div className="shell header-content">
          <a className="brand" href="/monitor">Transaction Monitor</a>
          <nav aria-label="Main navigation">
            <a href="/monitor" aria-current={!isSimulator ? 'page' : undefined}>Monitor</a>
            <a href="/add" aria-current={isSimulator ? 'page' : undefined}>Simulator</a>
          </nav>
        </div>
      </header>
      <main className="shell">{isSimulator ? <Simulator /> : <Monitor />}</main>
    </>
  );
}

createRoot(document.getElementById('root')!).render(<App />);
