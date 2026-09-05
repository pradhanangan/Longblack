import ArrowBackIcon from '@mui/icons-material/ArrowBack'
import {
  Box,
  Button,
  Chip,
  CircularProgress,
  Divider,
  IconButton,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableRow,
  Typography,
} from '@mui/material'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { api } from '../api/client'
import type { GoodsReceiptDto } from '../api/types'
import { ConfirmDialog } from '../components/products/ConfirmDialog'
import { AddLineRow } from '../components/receiving/AddLineRow'
import { GoodsReceiptFormDialog } from '../components/receiving/GoodsReceiptFormDialog'
import { GoodsReceiptLineRow } from '../components/receiving/GoodsReceiptLineRow'
import { useAuth } from '../contexts/AuthContext'
import { useSnackbar } from '../contexts/SnackbarContext'

function statusColor(status: string): 'default' | 'success' | 'error' {
  if (status === 'Received') return 'success'
  if (status === 'Cancelled') return 'error'
  return 'default'
}

function useGoodsReceipt(id: string) {
  return useQuery<GoodsReceiptDto>({
    queryKey: ['goods-receipts', id],
    queryFn: () => api.get(`/api/goods-receipts/${id}`),
  })
}

export function GoodsReceiptDetailPage() {
  const { id } = useParams<{ id: string }>()
  const { user } = useAuth()
  const { showSuccess, showError } = useSnackbar()
  const queryClient = useQueryClient()

  const canWrite = user?.roles.some((r) => r === 'Staff' || r === 'Manager' || r === 'Admin') ?? false
  const canCancel = user?.roles.some((r) => r === 'Manager' || r === 'Admin') ?? false

  const [editOpen, setEditOpen] = useState(false)
  const [receiveConfirmOpen, setReceiveConfirmOpen] = useState(false)
  const [cancelConfirmOpen, setCancelConfirmOpen] = useState(false)

  const { data: receipt, isLoading, isError } = useGoodsReceipt(id!)

  const receiveMutation = useMutation({
    mutationFn: () => api.post<GoodsReceiptDto>(`/api/goods-receipts/${id}/receive`, {}),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['goods-receipts'] })
      showSuccess('Goods receipt marked as Received. Inventory has been updated.')
    },
    onError: (err: unknown) => showError((err as Error).message ?? 'Failed to mark as received.'),
  })

  const cancelMutation = useMutation({
    mutationFn: () => api.post<GoodsReceiptDto>(`/api/goods-receipts/${id}/cancel`, {}),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['goods-receipts'] })
      showSuccess('Goods receipt cancelled.')
    },
    onError: (err: unknown) => showError((err as Error).message ?? 'Failed to cancel.'),
  })

  if (isLoading) return <CircularProgress />
  if (isError || !receipt) return <Typography color="error">Goods receipt not found.</Typography>

  const isDraft = receipt.status === 'Draft'
  const canEditLines = isDraft && canWrite

  return (
    <Box>
      <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 3 }}>
        <IconButton component={Link} to="/goods-receipts" size="small">
          <ArrowBackIcon />
        </IconButton>
        <Typography variant="h5">{receipt.receiptNumber}</Typography>
        <Chip label={receipt.status} size="small" color={statusColor(receipt.status)} sx={{ ml: 1 }} />
      </Box>

      {/* Header fields */}
      <Box sx={{ display: 'grid', gridTemplateColumns: '160px 1fr', gap: 1, mb: 3, maxWidth: 600 }}>
        <Typography color="text.secondary">Supplier Code</Typography>
        <Typography>{receipt.supplierCode}</Typography>
        <Typography color="text.secondary">Received Date</Typography>
        <Typography>{new Date(receipt.receivedDate).toLocaleDateString()}</Typography>
        <Typography color="text.secondary">Received By</Typography>
        <Typography>{receipt.receivedBy ?? '—'}</Typography>
      </Box>

      {/* Action buttons */}
      {isDraft && canWrite && (
        <Box sx={{ display: 'flex', gap: 1, mb: 3 }}>
          <Button variant="outlined" onClick={() => setEditOpen(true)}>Edit Header</Button>
          <Button
            variant="contained"
            color="success"
            onClick={() => setReceiveConfirmOpen(true)}
            disabled={receiveMutation.isPending}
          >
            Mark Received
          </Button>
          {canCancel && (
            <Button
              variant="outlined"
              color="error"
              onClick={() => setCancelConfirmOpen(true)}
              disabled={cancelMutation.isPending}
            >
              Cancel Receipt
            </Button>
          )}
        </Box>
      )}

      <GoodsReceiptFormDialog open={editOpen} onClose={() => setEditOpen(false)} goodsReceipt={receipt} />
      <ConfirmDialog
        open={receiveConfirmOpen}
        onClose={() => setReceiveConfirmOpen(false)}
        title="Mark as Received"
        message="This will permanently update inventory for every line on this receipt and lock it from further edits. Continue?"
        confirmLabel="Mark Received"
        onConfirm={() => receiveMutation.mutate()}
      />
      <ConfirmDialog
        open={cancelConfirmOpen}
        onClose={() => setCancelConfirmOpen(false)}
        title="Cancel Goods Receipt"
        message={`Are you sure you want to cancel receipt "${receipt.receiptNumber}"? This cannot be undone.`}
        confirmLabel="Cancel Receipt"
        onConfirm={() => cancelMutation.mutate()}
      />

      <Divider sx={{ my: 3 }} />

      {/* Lines section */}
      <Typography variant="h6" sx={{ mb: 2 }}>Lines</Typography>

      {receipt.lines.length === 0 && !canEditLines && (
        <Typography color="text.secondary">No lines on this receipt.</Typography>
      )}

      {(receipt.lines.length > 0 || canEditLines) && (
        <Table size="small">
          <TableHead>
            <TableRow>
              <TableCell>SKU</TableCell>
              <TableCell>Barcode</TableCell>
              <TableCell align="right">Quantity</TableCell>
              <TableCell align="right">Unit Cost</TableCell>
              {canEditLines && <TableCell align="right">Actions</TableCell>}
            </TableRow>
          </TableHead>
          <TableBody>
            {receipt.lines.map((line) => (
              <GoodsReceiptLineRow key={line.id} goodsReceiptId={receipt.id} line={line} canWrite={canEditLines} />
            ))}
            {canEditLines && <AddLineRow goodsReceiptId={receipt.id} />}
          </TableBody>
        </Table>
      )}
    </Box>
  )
}
