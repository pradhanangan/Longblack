import {
  Button,
  CircularProgress,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  TextField,
} from '@mui/material'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useEffect, useState } from 'react'
import { api } from '../../api/client'
import type { PurchaseOrderDto } from '../../api/types'
import { useSnackbar } from '../../contexts/SnackbarContext'

interface Props {
  open: boolean
  onClose: () => void
  purchaseOrder: PurchaseOrderDto
}

export function SendPurchaseOrderDialog({ open, onClose, purchaseOrder }: Props) {
  const { showSuccess, showError } = useSnackbar()
  const queryClient = useQueryClient()
  const [recipient, setRecipient] = useState('')
  const [cc, setCc] = useState('')

  useEffect(() => {
    if (open) {
      setRecipient(purchaseOrder.supplierEmail)
      setCc('')
    }
  }, [open, purchaseOrder.supplierEmail])

  const mutation = useMutation({
    mutationFn: () =>
      api.post<PurchaseOrderDto>(`/api/purchase-orders/${purchaseOrder.id}/send`, {
        recipient: recipient.trim() || null,
        cc: cc.trim() || null,
      }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['purchase-orders'] })
      showSuccess('Purchase order emailed.')
      onClose()
    },
    onError: (err: unknown) => {
      // The failed attempt is logged server-side, so refresh the history too.
      queryClient.invalidateQueries({ queryKey: ['purchase-orders'] })
      showError((err as Error).message ?? 'Failed to send email.')
    },
  })

  return (
    <Dialog open={open} onClose={onClose} maxWidth="sm" fullWidth>
      <DialogTitle>Email {purchaseOrder.poNumber}</DialogTitle>
      <DialogContent>
        <TextField
          label="To"
          fullWidth
          margin="normal"
          value={recipient}
          onChange={(e) => setRecipient(e.target.value)}
        />
        <TextField
          label="CC (optional)"
          fullWidth
          margin="normal"
          value={cc}
          onChange={(e) => setCc(e.target.value)}
        />
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>Cancel</Button>
        <Button
          variant="contained"
          onClick={() => mutation.mutate()}
          disabled={mutation.isPending || recipient.trim() === ''}
          startIcon={mutation.isPending ? <CircularProgress size={16} /> : undefined}
        >
          Send
        </Button>
      </DialogActions>
    </Dialog>
  )
}
