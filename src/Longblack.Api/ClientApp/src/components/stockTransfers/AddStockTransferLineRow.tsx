import AddIcon from '@mui/icons-material/Add'
import { IconButton, TableCell, TableRow, TextField } from '@mui/material'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useState } from 'react'
import { api } from '../../api/client'
import type { StockTransferDto } from '../../api/types'
import { useSnackbar } from '../../contexts/SnackbarContext'
import { VariantSearchAutocomplete, type VariantOption } from '../receiving/VariantSearchAutocomplete'

interface Props {
  stockTransferId: string
}

// Stays open after a successful add so the next variant can be entered immediately, mirroring
// AddLineRow's Goods Receipt pattern.
export function AddStockTransferLineRow({ stockTransferId }: Props) {
  const { showError } = useSnackbar()
  const queryClient = useQueryClient()

  const [variant, setVariant] = useState<VariantOption | null>(null)
  const [quantity, setQuantity] = useState('1')

  const mutation = useMutation({
    mutationFn: () =>
      api.post<StockTransferDto>(`/api/stock-transfers/${stockTransferId}/lines`, {
        productVariantId: variant!.id,
        quantity: Number(quantity),
      }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['stock-transfers', stockTransferId] })
      setVariant(null)
      setQuantity('1')
    },
    onError: (err: unknown) => showError((err as Error).message ?? 'Failed to add line.'),
  })

  const canAdd = !!variant && Number(quantity) > 0 && !mutation.isPending

  return (
    <TableRow>
      <TableCell colSpan={3}>
        <VariantSearchAutocomplete value={variant} onChange={setVariant} />
      </TableCell>
      <TableCell align="right">
        <TextField
          size="small"
          type="number"
          value={quantity}
          onChange={(e) => setQuantity(e.target.value)}
          slotProps={{ input: { inputProps: { min: 1, step: '1' } } }}
          sx={{ width: 100 }}
        />
      </TableCell>
      <TableCell align="right">
        <IconButton
          color="primary"
          disabled={!canAdd}
          onClick={() => mutation.mutate()}
          aria-label="Add line"
        >
          <AddIcon />
        </IconButton>
      </TableCell>
    </TableRow>
  )
}
