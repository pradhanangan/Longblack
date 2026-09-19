import { Autocomplete, Box, Button, TextField } from '@mui/material'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useRef, useState } from 'react'
import { api } from '../../api/client'
import type { StockTakeDto, StockTakeItemDto } from '../../api/types'
import { useSnackbar } from '../../contexts/SnackbarContext'

interface Props {
  stockTakeId: string
  items: StockTakeItemDto[]
}

// Client-side search over this Stock Take's own already-loaded item list — searching outside
// this scope makes no sense, and the list is inherently bounded (see grill session notes).
export function QuickCountEntry({ stockTakeId, items }: Props) {
  const { showError } = useSnackbar()
  const queryClient = useQueryClient()

  const [inputValue, setInputValue] = useState('')
  const [selected, setSelected] = useState<StockTakeItemDto | null>(null)
  const [quantity, setQuantity] = useState('')
  const quantityRef = useRef<HTMLInputElement>(null)
  const searchRef = useRef<HTMLInputElement>(null)

  const mutation = useMutation({
    mutationFn: () =>
      api.post<StockTakeDto>(`/api/stock-takes/${stockTakeId}/items/${selected!.id}/counts`, {
        quantity: Number(quantity),
      }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['stock-takes', stockTakeId] })
      setSelected(null)
      setInputValue('')
      setQuantity('')
      searchRef.current?.focus()
    },
    onError: (err: unknown) => showError((err as Error).message ?? 'Failed to save count.'),
  })

  function handleSelect(item: StockTakeItemDto | null) {
    setSelected(item)
    if (item) {
      // Auto-focus the quantity field so the user can type the count immediately.
      setTimeout(() => quantityRef.current?.focus(), 0)
    }
  }

  const canSave = !!selected && quantity.trim() !== '' && Number(quantity) >= 0 && !mutation.isPending

  return (
    <Box sx={{ display: 'flex', gap: 2, alignItems: 'flex-start', mb: 3, flexWrap: 'wrap' }}>
      <Autocomplete
        size="small"
        sx={{ minWidth: 320 }}
        options={items}
        value={selected}
        inputValue={inputValue}
        onInputChange={(_, newInputValue) => setInputValue(newInputValue)}
        onChange={(_, newValue) => handleSelect(newValue)}
        filterOptions={(opts, state) => {
          const q = state.inputValue.trim().toLowerCase()
          if (!q) return []
          return opts.filter((i) =>
            i.sku.toLowerCase().includes(q) ||
            i.productName.toLowerCase().includes(q) ||
            (i.barcode?.toLowerCase().includes(q) ?? false))
        }}
        getOptionLabel={(i) => i.sku}
        renderOption={(props, i) => (
          <li {...props} key={i.id}>
            {i.sku} — {i.productName} ({i.colourName ?? '—'}/{i.sizeName ?? '—'}) {i.barcode ? `[${i.barcode}]` : ''}
          </li>
        )}
        isOptionEqualToValue={(a, b) => a.id === b.id}
        noOptionsText={inputValue.trim() ? 'No matching item in this Stock Take' : 'Type a SKU or barcode…'}
        renderInput={(params) => (
          <TextField {...params} inputRef={searchRef} label="Find item (SKU / barcode / product)" placeholder="Type to search…" />
        )}
      />
      <TextField
        size="small"
        type="number"
        label="Quantity"
        inputRef={quantityRef}
        value={quantity}
        onChange={(e) => setQuantity(e.target.value)}
        onKeyDown={(e) => { if (e.key === 'Enter' && canSave) mutation.mutate() }}
        slotProps={{ input: { inputProps: { min: 0, step: '1' } } }}
        sx={{ width: 120 }}
      />
      <Button variant="contained" disabled={!canSave} onClick={() => mutation.mutate()}>
        Save Count
      </Button>
    </Box>
  )
}
