import {
  Button,
  CircularProgress,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  FormControl,
  InputLabel,
  MenuItem,
  Select,
  TextField,
} from '@mui/material'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useState } from 'react'
import { api } from '../../api/client'
import type { SaleDto } from '../../api/types'
import { useSnackbar } from '../../contexts/SnackbarContext'

interface Props {
  open: boolean
  onClose: () => void
  sale: SaleDto
}

const PAYMENT_TYPES = [
  { value: 'Cash', label: 'Cash' },
  { value: 'Card', label: 'Card' },
  { value: 'BankTransfer', label: 'Bank transfer' },
  { value: 'Other', label: 'Other' },
]

// Payment amount must equal the Sale total; it is prefilled but editable so a mismatch surfaces
// the server's validation message rather than being silently corrected.
export function CompleteSaleDialog({ open, onClose, sale }: Props) {
  const { showSuccess, showError } = useSnackbar()
  const queryClient = useQueryClient()

  const [paymentType, setPaymentType] = useState('Card')
  const [paymentAmount, setPaymentAmount] = useState(sale.total.toFixed(2))

  const mutation = useMutation({
    mutationFn: () =>
      api.post<SaleDto>(`/api/sales/${sale.id}/complete`, {
        paymentType,
        paymentAmount: Number(paymentAmount),
      }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['sales'] })
      queryClient.invalidateQueries({ queryKey: ['dashboard'] })
      showSuccess('Sale completed. Store Inventory has been reduced.')
      onClose()
    },
    onError: (err: unknown) => showError((err as Error).message ?? 'Failed to complete Sale.'),
  })

  return (
    <Dialog
      open={open}
      onClose={onClose}
      maxWidth="xs"
      fullWidth
      slotProps={{ transition: { onEnter: () => setPaymentAmount(sale.total.toFixed(2)) } }}
    >
      <DialogTitle>Complete Sale</DialogTitle>
      <DialogContent>
        <FormControl fullWidth margin="normal">
          <InputLabel>Payment type</InputLabel>
          <Select value={paymentType} label="Payment type" onChange={(e) => setPaymentType(e.target.value)}>
            {PAYMENT_TYPES.map((p) => (
              <MenuItem key={p.value} value={p.value}>
                {p.label}
              </MenuItem>
            ))}
          </Select>
        </FormControl>
        <TextField
          fullWidth
          margin="normal"
          label="Payment amount"
          type="number"
          value={paymentAmount}
          onChange={(e) => setPaymentAmount(e.target.value)}
          helperText={`Sale total: $${sale.total.toFixed(2)} (GST inclusive)`}
          slotProps={{ input: { inputProps: { min: 0, step: '0.01' } } }}
        />
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>Cancel</Button>
        <Button
          variant="contained"
          color="success"
          onClick={() => mutation.mutate()}
          disabled={mutation.isPending}
          startIcon={mutation.isPending ? <CircularProgress size={16} /> : undefined}
        >
          Complete
        </Button>
      </DialogActions>
    </Dialog>
  )
}
