import AddIcon from '@mui/icons-material/Add'
import { Autocomplete, IconButton, TableCell, TableRow, TextField } from '@mui/material'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useState } from 'react'
import { api } from '../../api/client'
import type { InventoryDto, SaleDto } from '../../api/types'
import { useSnackbar } from '../../contexts/SnackbarContext'

interface Props {
  saleId: string
  storeId: string
}

// Only variants with stock at the Sale's Store are offered; the server enforces this too.
function useInStockVariants(storeId: string, query: string) {
  return useQuery<InventoryDto[]>({
    queryKey: ['inventory', 'sale-search', storeId, query],
    queryFn: () => api.get(`/api/inventory?storeId=${storeId}&q=${encodeURIComponent(query)}`),
    enabled: query.trim().length >= 2,
    staleTime: 10_000,
    select: (items) => items.filter((i) => i.quantity > 0 && i.status === 'Active'),
  })
}

// Stays open after a successful add so the next variant can be entered immediately.
export function AddSaleLineRow({ saleId, storeId }: Props) {
  const { showError } = useSnackbar()
  const queryClient = useQueryClient()

  const [variant, setVariant] = useState<InventoryDto | null>(null)
  const [inputValue, setInputValue] = useState('')
  const [quantity, setQuantity] = useState('1')
  const [discount, setDiscount] = useState('0')

  const { data: options = [], isFetching } = useInStockVariants(storeId, inputValue)

  const mutation = useMutation({
    mutationFn: () =>
      api.post<SaleDto>(`/api/sales/${saleId}/lines`, {
        productVariantId: variant!.productVariantId,
        quantity: Number(quantity),
        discount: Number(discount) || 0,
      }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['sales', saleId] })
      queryClient.invalidateQueries({ queryKey: ['inventory', 'sale-search'] })
      setVariant(null)
      setQuantity('1')
      setDiscount('0')
    },
    onError: (err: unknown) => showError((err as Error).message ?? 'Failed to add line.'),
  })

  const canAdd =
    !!variant &&
    Number(quantity) > 0 &&
    Number(quantity) <= variant.quantity &&
    Number(discount) >= 0 &&
    !mutation.isPending

  return (
    <TableRow>
      <TableCell colSpan={3}>
        <Autocomplete
          size="small"
          sx={{ minWidth: 260 }}
          options={options}
          value={variant}
          inputValue={inputValue}
          onInputChange={(_, v) => setInputValue(v)}
          onChange={(_, v) => setVariant(v)}
          loading={isFetching}
          filterOptions={(opts) => opts}
          getOptionLabel={(v) =>
            `${v.sku} — ${v.productName} (${v.colourName ?? '—'}/${v.sizeName ?? '—'}) · ${v.quantity} available`
          }
          isOptionEqualToValue={(a, b) => a.productVariantId === b.productVariantId}
          noOptionsText={
            inputValue.trim().length < 2 ? 'Type at least 2 characters…' : 'No matching variants in stock'
          }
          renderInput={(params) => (
            <TextField {...params} label="In-stock variant (SKU / barcode / product)" placeholder="Scan or search…" />
          )}
        />
      </TableCell>
      <TableCell align="right">
        <TextField
          size="small"
          type="number"
          value={quantity}
          onChange={(e) => setQuantity(e.target.value)}
          slotProps={{ input: { inputProps: { min: 1, max: variant?.quantity, step: '1' } } }}
          sx={{ width: 90 }}
        />
      </TableCell>
      <TableCell />
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
      <TableCell />
      <TableCell align="right">
        <IconButton color="primary" disabled={!canAdd} onClick={() => mutation.mutate()} aria-label="Add line">
          <AddIcon />
        </IconButton>
      </TableCell>
    </TableRow>
  )
}
