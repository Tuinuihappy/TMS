import { useState } from 'react'
import { OrderList } from './components/OrderList'
import { CreateOrderForm } from './components/CreateOrderForm'
import { OrderDetail } from './components/OrderDetail'
import { ImportOrders } from './components/ImportOrders'
import { PlanningPage } from './components/PlanningPage'
import { ExecutionPage } from './components/ExecutionPage'
import type { OrderDto } from './types/order'

type Page = 'orders' | 'planning' | 'execution'
type Modal = 'create' | 'import' | null

export default function App() {
  const [page, setPage] = useState<Page>('orders')
  const [selectedOrder, setSelectedOrder] = useState<OrderDto | null>(null)
  const [modal, setModal] = useState<Modal>(null)
  const [refreshKey, setRefreshKey] = useState(0)

  const refresh = () => setRefreshKey(k => k + 1)

  return (
    <div style={{ minHeight: '100vh', background: '#f3f4f6', fontFamily: '-apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif' }}>
      {/* Top Nav */}
      <nav style={navStyle}>
        <div style={{ display: 'flex', alignItems: 'center', gap: '4px' }}>
          <span style={{ fontWeight: 700, fontSize: '16px', color: '#111827', marginRight: '12px' }}>TMS</span>
          {([
            ['orders',    '📦 Orders'],
            ['planning',  '🗺 Planning'],
            ['execution', '🚛 Execution'],
          ] as [Page, string][]).map(([key, label]) => (
            <button
              key={key}
              onClick={() => setPage(key)}
              style={{
                padding: '6px 14px',
                borderRadius: '6px',
                border: 'none',
                background: page === key ? '#eff6ff' : 'transparent',
                color: page === key ? '#2563eb' : '#6b7280',
                fontWeight: page === key ? 600 : 400,
                cursor: 'pointer',
                fontSize: '13px',
              }}
            >
              {label}
            </button>
          ))}
        </div>
        <div style={{ fontSize: '11px', color: '#9ca3af' }}>
          API: localhost:5080 · Dev Auth
        </div>
      </nav>

      {/* Pages */}
      <main style={{ maxWidth: '1280px', margin: '0 auto', padding: '0 16px' }}>
        {page === 'orders' && (
          <OrderList
            onSelect={setSelectedOrder}
            onCreateNew={() => setModal('create')}
            onImport={() => setModal('import')}
            refreshKey={refreshKey}
          />
        )}
        {page === 'planning'  && <PlanningPage />}
        {page === 'execution' && <ExecutionPage />}
      </main>

      {/* Order modals */}
      {modal === 'create' && (
        <CreateOrderForm
          onSuccess={() => { setModal(null); refresh() }}
          onCancel={() => setModal(null)}
        />
      )}
      {modal === 'import' && (
        <ImportOrders
          onClose={() => setModal(null)}
          onSuccess={() => { setModal(null); refresh() }}
        />
      )}
      {selectedOrder && (
        <OrderDetail
          order={selectedOrder}
          onClose={() => setSelectedOrder(null)}
          onRefresh={() => { setSelectedOrder(null); refresh() }}
        />
      )}
    </div>
  )
}

const navStyle: React.CSSProperties = {
  background: '#fff',
  borderBottom: '1px solid #e5e7eb',
  padding: '10px 24px',
  display: 'flex',
  justifyContent: 'space-between',
  alignItems: 'center',
}
