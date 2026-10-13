import {
  Button,
  CircularProgress,
  Dialog,
  DialogActions,
  DialogContent,
  DialogContentText,
  DialogTitle,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableRow,
  TextField,
} from '@mui/material'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useEffect, useState } from 'react'
import { api } from '../../api/client'
import type { StockTransferDto } from '../../api/types'
import { useSnackbar } from '../../contexts/SnackbarContext'

interface Props {
  open: boolean
  onClose: () => void
  stockTransfer: StockTransferDto
}

// Received quantity defaults to the dispatched quantity purely as a convenient starting point in
// the input — it is never submitted unless the user explicitly confirms it (the backend never
// defaults ReceivedQuantity itself; see StockTransferLine.ReceivedQuantity doc comment).
export function ReceiveStockTransferDialog({ open, onClose, stockTransfer }: Props) {
  const { showSuccess, showError } = useSnackbar()
  const queryClient = useQueryClient()

  const [quantities, setQuantities] = useState<Record<string, string>>({})

  useEffect(() => {
    if (open) {
      setQuantities(
        Object.fromEntries(stockTransfer.lines.map((l) => [l.id, String(l.dispatchedQuantity)])),
      )
    }
  }, [open, stockTransfer.lines])

  const mutation = useMutation({
    mutationFn: () =>
      api.post<StockTransferDto>(`/api/stock-transfers/${stockTransfer.id}/receive`, {
        lines: stockTransfer.lines.map((l) => ({
          lineId: l.id,
          receivedQuantity: Number(quantities[l.id] ?? 0),
        })),
      }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['stock-transfers'] })
      showSuccess('Stock Transfer received. Destination Inventory has been updated.')
      onClose()
    },
    onError: (err: unknown) => showError((err as Error).message ?? 'Failed to receive Stock Transfer.'),
  })

  const canSubmit = stockTransfer.lines.every((l) => {
    const v = quantities[l.id]
    return v !== undefined && v.trim() !== '' && Number(v) >= 0
  })

  return (
    <Dialog open={open} onClose={onClose} maxWidth="sm" fullWidth>
      <DialogTitle>Receive Stock Transfer</DialogTitle>
      <DialogContent>
        <DialogContentText sx={{ mb: 2 }}>
          Enter the actual quantity received for each line — this may be less than what was
          dispatched if stock was lost or damaged in transit.
        </DialogContentText>
        <Table size="small">
          <TableHead>
            <TableRow>
              <TableCell>SKU</TableCell>
              <TableCell align="right">Dispatched</TableCell>
              <TableCell align="right">Received</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {stockTransfer.lines.map((l) => (
              <TableRow key={l.id}>
                <TableCell>{l.sku}</TableCell>
                <TableCell align="right">{l.dispatchedQuantity}</TableCell>
                <TableCell align="right">
                  <TextField
                    size="small"
                    type="number"
                    value={quantities[l.id] ?? ''}
                    onChange={(e) =>
                      setQuantities((prev) => ({ ...prev, [l.id]: e.target.value }))
                    }
                    slotProps={{ input: { inputProps: { min: 0, step: '1' } } }}
                    sx={{ width: 100 }}
                  />
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>Cancel</Button>
        <Button
          variant="contained"
          color="success"
          onClick={() => mutation.mutate()}
          disabled={!canSubmit || mutation.isPending}
          startIcon={mutation.isPending ? <CircularProgress size={16} /> : undefined}
        >
          Confirm Receive
        </Button>
      </DialogActions>
    </Dialog>
  )
}
