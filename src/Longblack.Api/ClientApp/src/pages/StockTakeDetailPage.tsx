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
import type { StockTakeDto } from '../api/types'
import { ConfirmDialog } from '../components/products/ConfirmDialog'
import { QuickCountEntry } from '../components/stockTake/QuickCountEntry'
import { StockTakeItemRow } from '../components/stockTake/StockTakeItemRow'
import { useAuth } from '../contexts/AuthContext'
import { useSnackbar } from '../contexts/SnackbarContext'

function statusColor(status: string): 'default' | 'success' | 'error' | 'warning' {
  if (status === 'Approved') return 'success'
  if (status === 'Cancelled') return 'error'
  if (status === 'InProgress') return 'warning'
  return 'default'
}

function useStockTake(id: string) {
  return useQuery<StockTakeDto>({
    queryKey: ['stock-takes', id],
    queryFn: () => api.get(`/api/stock-takes/${id}`),
  })
}

export function StockTakeDetailPage() {
  const { id } = useParams<{ id: string }>()
  const { user } = useAuth()
  const { showSuccess, showError } = useSnackbar()
  const queryClient = useQueryClient()

  const canWrite = user?.roles.some((r) => r === 'Staff' || r === 'Manager' || r === 'Admin') ?? false
  const canApprove = user?.roles.some((r) => r === 'Manager' || r === 'Admin') ?? false

  const [cancelConfirmOpen, setCancelConfirmOpen] = useState(false)
  const [approveConfirmOpen, setApproveConfirmOpen] = useState(false)

  const { data: stockTake, isLoading, isError } = useStockTake(id!)

  const invalidate = () => queryClient.invalidateQueries({ queryKey: ['stock-takes'] })

  const startMutation = useMutation({
    mutationFn: () => api.post<StockTakeDto>(`/api/stock-takes/${id}/start`, {}),
    onSuccess: () => { invalidate(); showSuccess('Counting started.') },
    onError: (err: unknown) => showError((err as Error).message ?? 'Failed to start counting.'),
  })

  const completeMutation = useMutation({
    mutationFn: () => api.post<StockTakeDto>(`/api/stock-takes/${id}/complete`, {}),
    onSuccess: () => { invalidate(); showSuccess('Stock Take marked as Completed.') },
    onError: (err: unknown) => showError((err as Error).message ?? 'Failed to complete Stock Take.'),
  })

  const reopenMutation = useMutation({
    mutationFn: () => api.post<StockTakeDto>(`/api/stock-takes/${id}/reopen`, {}),
    onSuccess: () => { invalidate(); showSuccess('Stock Take reopened for counting.') },
    onError: (err: unknown) => showError((err as Error).message ?? 'Failed to reopen Stock Take.'),
  })

  const approveMutation = useMutation({
    mutationFn: () => api.post<StockTakeDto>(`/api/stock-takes/${id}/approve`, {}),
    onSuccess: () => { invalidate(); showSuccess('Stock Take approved. Inventory has been adjusted.') },
    onError: (err: unknown) => showError((err as Error).message ?? 'Failed to approve Stock Take.'),
  })

  const cancelMutation = useMutation({
    mutationFn: () => api.post<StockTakeDto>(`/api/stock-takes/${id}/cancel`, {}),
    onSuccess: () => { invalidate(); showSuccess('Stock Take cancelled.') },
    onError: (err: unknown) => showError((err as Error).message ?? 'Failed to cancel Stock Take.'),
  })

  if (isLoading) return <CircularProgress />
  if (isError || !stockTake) return <Typography color="error">Stock Take not found.</Typography>

  const isDraft = stockTake.status === 'Draft'
  const isInProgress = stockTake.status === 'InProgress'
  const isCompleted = stockTake.status === 'Completed'
  const canEditItems = isInProgress && canWrite

  const allCounted = stockTake.items.length > 0 && stockTake.items.every((i) => i.status === 'Counted')

  return (
    <Box>
      <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 3 }}>
        <IconButton component={Link} to="/stock-takes" size="small">
          <ArrowBackIcon />
        </IconButton>
        <Typography variant="h5">{stockTake.referenceNumber}</Typography>
        <Chip label={stockTake.status} size="small" color={statusColor(stockTake.status)} sx={{ ml: 1 }} />
      </Box>

      <Box sx={{ display: 'grid', gridTemplateColumns: '160px 1fr', gap: 1, mb: 3, maxWidth: 600 }}>
        <Typography color="text.secondary">Brand Scope</Typography>
        <Typography>{stockTake.brandName ?? 'Any'}</Typography>
        <Typography color="text.secondary">Category Scope</Typography>
        <Typography>{stockTake.categoryName ?? 'Any'}</Typography>
        <Typography color="text.secondary">Start Date</Typography>
        <Typography>{stockTake.startDate ? new Date(stockTake.startDate).toLocaleString() : '—'}</Typography>
        <Typography color="text.secondary">Completion Date</Typography>
        <Typography>{stockTake.completionDate ? new Date(stockTake.completionDate).toLocaleString() : '—'}</Typography>
        {stockTake.completedBy && (
          <>
            <Typography color="text.secondary">Completed By</Typography>
            <Typography>{stockTake.completedBy}</Typography>
          </>
        )}
        {stockTake.approvedBy && (
          <>
            <Typography color="text.secondary">Approved By</Typography>
            <Typography>{stockTake.approvedBy} on {new Date(stockTake.approvedDate!).toLocaleString()}</Typography>
          </>
        )}
      </Box>

      {/* Lifecycle actions */}
      <Box sx={{ display: 'flex', gap: 1, mb: 3, flexWrap: 'wrap' }}>
        {isDraft && canWrite && (
          <Button variant="contained" onClick={() => startMutation.mutate()} disabled={startMutation.isPending}>
            Start Counting
          </Button>
        )}
        {isDraft && canApprove && (
          <Button variant="outlined" color="error" onClick={() => setCancelConfirmOpen(true)}>
            Cancel
          </Button>
        )}
        {isInProgress && canWrite && (
          <Button
            variant="contained"
            color="success"
            onClick={() => completeMutation.mutate()}
            disabled={completeMutation.isPending}
          >
            Complete
          </Button>
        )}
        {isInProgress && !allCounted && (
          <Typography color="text.secondary" sx={{ alignSelf: 'center' }}>
            {stockTake.items.filter((i) => i.status === 'Counted').length} of {stockTake.items.length} counted
          </Typography>
        )}
        {isInProgress && canApprove && (
          <Button variant="outlined" color="error" onClick={() => setCancelConfirmOpen(true)}>
            Cancel
          </Button>
        )}
        {isCompleted && canApprove && (
          <>
            <Button
              variant="contained"
              color="success"
              onClick={() => setApproveConfirmOpen(true)}
              disabled={approveMutation.isPending}
            >
              Approve
            </Button>
            <Button
              variant="outlined"
              onClick={() => reopenMutation.mutate()}
              disabled={reopenMutation.isPending}
            >
              Reopen
            </Button>
          </>
        )}
      </Box>

      <ConfirmDialog
        open={cancelConfirmOpen}
        onClose={() => setCancelConfirmOpen(false)}
        title="Cancel Stock Take"
        message={`Are you sure you want to cancel "${stockTake.referenceNumber}"? This cannot be undone.`}
        confirmLabel="Cancel Stock Take"
        onConfirm={() => cancelMutation.mutate()}
      />
      <ConfirmDialog
        open={approveConfirmOpen}
        onClose={() => setApproveConfirmOpen(false)}
        title="Approve Stock Take"
        message="This will permanently adjust Inventory for every item with a nonzero variance and lock this Stock Take from further changes. Continue?"
        confirmLabel="Approve"
        onConfirm={() => approveMutation.mutate()}
      />

      <Divider sx={{ my: 3 }} />

      {isDraft && (
        <Typography color="text.secondary">
          Items will be generated from this Stock Take's scope once counting starts.
        </Typography>
      )}

      {!isDraft && (
        <>
          <Typography variant="h6" sx={{ mb: 2 }}>Items</Typography>

          {canEditItems && <QuickCountEntry stockTakeId={stockTake.id} items={stockTake.items} />}

          {stockTake.items.length === 0 ? (
            <Typography color="text.secondary">No items in this Stock Take.</Typography>
          ) : (
            <Table size="small">
              <TableHead>
                <TableRow>
                  <TableCell>SKU</TableCell>
                  <TableCell>Barcode</TableCell>
                  <TableCell>Product</TableCell>
                  <TableCell>Colour</TableCell>
                  <TableCell>Size</TableCell>
                  <TableCell align="right">Expected</TableCell>
                  <TableCell align="right">Counted</TableCell>
                  <TableCell align="right">Variance</TableCell>
                  <TableCell>Status</TableCell>
                  {canEditItems && <TableCell align="right">Actions</TableCell>}
                </TableRow>
              </TableHead>
              <TableBody>
                {stockTake.items.map((item) => (
                  <StockTakeItemRow key={item.id} stockTakeId={stockTake.id} item={item} canEdit={canEditItems} />
                ))}
              </TableBody>
            </Table>
          )}
        </>
      )}
    </Box>
  )
}
