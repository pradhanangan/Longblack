import AddIcon from '@mui/icons-material/Add'
import { IconButton, TableCell, TableRow, TextField } from '@mui/material'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useState } from 'react'
import { api } from '../../api/client'
import type { GoodsReceiptDto } from '../../api/types'
import { useSnackbar } from '../../contexts/SnackbarContext'
import { VariantSearchAutocomplete, type VariantOption } from './VariantSearchAutocomplete'

interface Props {
  goodsReceiptId: string
}

// Stays open after a successful add so the next carton can be entered immediately (Q1/Q8).
export function AddLineRow({ goodsReceiptId }: Props) {
  const { showError } = useSnackbar()
  const queryClient = useQueryClient()

  const [variant, setVariant] = useState<VariantOption | null>(null)
  const [quantity, setQuantity] = useState('1')
  const [unitCost, setUnitCost] = useState('')

  const mutation = useMutation({
    mutationFn: () =>
      api.post<GoodsReceiptDto>(`/api/goods-receipts/${goodsReceiptId}/lines`, {
        productVariantId: variant!.id,
        quantity: Number(quantity),
        unitCost: Number(unitCost),
      }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['goods-receipts', goodsReceiptId] })
      setVariant(null)
      setQuantity('1')
      setUnitCost('')
    },
    onError: (err: unknown) => showError((err as Error).message ?? 'Failed to add line.'),
  })

  const canAdd =
    !!variant &&
    Number(quantity) > 0 &&
    unitCost.trim() !== '' &&
    Number(unitCost) >= 0 &&
    !mutation.isPending

  return (
    <TableRow>
      <TableCell colSpan={2}>
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
        <TextField
          size="small"
          type="number"
          placeholder="Unit cost"
          value={unitCost}
          onChange={(e) => setUnitCost(e.target.value)}
          slotProps={{ input: { inputProps: { min: 0, step: '0.01' } } }}
          sx={{ width: 120 }}
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
