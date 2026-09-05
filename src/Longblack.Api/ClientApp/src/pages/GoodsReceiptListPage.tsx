import AddIcon from '@mui/icons-material/Add'
import {
  Box,
  Button,
  Chip,
  CircularProgress,
  FormControl,
  InputLabel,
  MenuItem,
  Select,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableRow,
  TextField,
  Typography,
} from '@mui/material'
import { useQuery } from '@tanstack/react-query'
import { useDeferredValue, useState } from 'react'
import { Link } from 'react-router-dom'
import { api } from '../api/client'
import type { GoodsReceiptDto } from '../api/types'
import { GoodsReceiptFormDialog } from '../components/receiving/GoodsReceiptFormDialog'
import { useAuth } from '../contexts/AuthContext'

function statusColor(status: string): 'default' | 'success' | 'error' {
  if (status === 'Received') return 'success'
  if (status === 'Cancelled') return 'error'
  return 'default'
}

function useGoodsReceipts(status: string, supplierCode: string) {
  const params = new URLSearchParams()
  if (status) params.set('status', status)
  if (supplierCode) params.set('supplierCode', supplierCode)
  const qs = params.toString()
  return useQuery<GoodsReceiptDto[]>({
    queryKey: ['goods-receipts', status, supplierCode],
    queryFn: () => api.get(`/api/goods-receipts${qs ? `?${qs}` : ''}`),
  })
}

export function GoodsReceiptListPage() {
  const { user } = useAuth()
  const canCreate = user?.roles.some((r) => r === 'Staff' || r === 'Manager' || r === 'Admin') ?? false

  const [status, setStatus] = useState('')
  const [supplierCodeInput, setSupplierCodeInput] = useState('')
  const [createOpen, setCreateOpen] = useState(false)

  const deferredSupplierCode = useDeferredValue(supplierCodeInput)
  const { data: receipts, isLoading, isError } = useGoodsReceipts(status, deferredSupplierCode)

  return (
    <Box>
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 3 }}>
        <Typography variant="h5">Goods Receipts</Typography>
        {canCreate && (
          <Button variant="contained" startIcon={<AddIcon />} onClick={() => setCreateOpen(true)}>
            New Receipt
          </Button>
        )}
      </Box>

      <GoodsReceiptFormDialog open={createOpen} onClose={() => setCreateOpen(false)} />

      <Box sx={{ display: 'flex', gap: 2, mb: 3, flexWrap: 'wrap' }}>
        <TextField
          label="Supplier Code"
          size="small"
          value={supplierCodeInput}
          onChange={(e) => setSupplierCodeInput(e.target.value)}
          sx={{ minWidth: 200 }}
        />
        <FormControl size="small" sx={{ minWidth: 140 }}>
          <InputLabel>Status</InputLabel>
          <Select value={status} label="Status" onChange={(e) => setStatus(e.target.value)}>
            <MenuItem value="">All</MenuItem>
            <MenuItem value="Draft">Draft</MenuItem>
            <MenuItem value="Received">Received</MenuItem>
            <MenuItem value="Cancelled">Cancelled</MenuItem>
          </Select>
        </FormControl>
      </Box>

      {isLoading && <CircularProgress />}
      {isError && <Typography color="error">Failed to load goods receipts.</Typography>}

      {!isLoading && !isError && receipts && (
        receipts.length === 0 ? (
          <Typography color="text.secondary">No goods receipts match the current filters.</Typography>
        ) : (
          <Table size="small">
            <TableHead>
              <TableRow>
                <TableCell>Receipt #</TableCell>
                <TableCell>Supplier Code</TableCell>
                <TableCell>Received Date</TableCell>
                <TableCell>Status</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {receipts.map((r) => (
                <TableRow key={r.id} hover>
                  <TableCell>
                    <Link to={`/goods-receipts/${r.id}`} style={{ textDecoration: 'none', color: 'inherit', fontWeight: 500 }}>
                      {r.receiptNumber}
                    </Link>
                  </TableCell>
                  <TableCell>{r.supplierCode}</TableCell>
                  <TableCell>{new Date(r.receivedDate).toLocaleDateString()}</TableCell>
                  <TableCell>
                    <Chip label={r.status} size="small" color={statusColor(r.status)} />
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        )
      )}
    </Box>
  )
}
