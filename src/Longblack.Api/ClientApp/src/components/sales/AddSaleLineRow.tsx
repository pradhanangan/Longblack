import AddIcon from '@mui/icons-material/Add'
import { IconButton, TableCell, TableRow, TextField } from '@mui/material'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useState } from 'react'
import { api } from '../../api/client'
import type { SaleDto } from '../../api/types'
import { useSnackbar } from '../../contexts/SnackbarContext'
import { VariantSearchAutocomplete, type VariantOption } from '../receiving/VariantSearchAutocomplete'

interface Props {
  saleId: string
}

// Stays open after a successful add so the next variant can be entered immediately.
export function AddSaleLineRow({ saleId }: Props) {
  const { showError } = useSnackbar()
  const queryClient = useQueryClient()

  const [variant, setVariant] = useState<VariantOption | null>(null)
  const [quantity, setQuantity] = useState('1')
  const [discount, setDiscount] = useState('0')

  const mutation = useMutation({
    mutationFn: () =>
      api.post<SaleDto>(`/api/sales/${saleId}/lines`, {
        productVariantId: variant!.id,
        quantity: Number(quantity),
        discount: Number(discount) || 0,
      }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['sales', saleId] })
      setVariant(null)
      setQuantity('1')
      setDiscount('0')
    },
    onError: (err: unknown) => showError((err as Error).message ?? 'Failed to add line.'),
  })

  const canAdd = !!variant && Number(quantity) > 0 && Number(discount) >= 0 && !mutation.isPending

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
          sx={{ width: 90 }}
        />
      </TableCell>
      <TableCell align="right">{variant ? `$${variant.sellingPrice.toFixed(2)}` : '—'}</TableCell>
      <TableCell align="right">
        <TextField
          size="small"
          type="number"
          value={discount}
          onChange={(e) => setDiscount(e.target.value)}
          slotProps={{ input: { inputProps: { min: 0, step: '0.01' } } }}
          sx={{ width: 100 }}
        />
      </TableCell>
      <TableCell />
      <TableCell align="right">
        <IconButton color="primary" disabled={!canAdd} onClick={() => mutation.mutate()} aria-label="Add line">
          <AddIcon />
        </IconButton>
      </TableCell>
    </TableRow>
  )
}
