import { Autocomplete, TextField } from '@mui/material'
import { useQuery } from '@tanstack/react-query'
import { useState } from 'react'
import { api } from '../../api/client'
import type { ProductDto, ProductVariantDto } from '../../api/types'

export interface VariantOption extends ProductVariantDto {
  productName: string
}

interface Props {
  value: VariantOption | null
  onChange: (variant: VariantOption | null) => void
  // Variants already on the receipt aren't excluded — duplicates are allowed (different cost per carton).
  disabled?: boolean
  error?: boolean
  helperText?: string
}

// Reuses the product search endpoint (GET /api/products?q=) rather than a dedicated variant-search
// endpoint: it already matches on SKU/barcode/name and only loads matched results.
function useVariantSearch(query: string) {
  return useQuery<ProductDto[]>({
    queryKey: ['products', 'variant-search', query],
    queryFn: () => api.get(`/api/products?q=${encodeURIComponent(query)}`),
    enabled: query.trim().length >= 2,
    staleTime: 30_000,
  })
}

export function VariantSearchAutocomplete({ value, onChange, disabled, error, helperText }: Props) {
  const [inputValue, setInputValue] = useState('')
  const { data: products, isFetching } = useVariantSearch(inputValue)

  const options: VariantOption[] = (products ?? []).flatMap((p) =>
    (p.variants ?? [])
      .filter((v) => v.status === 'Active')
      .map((v) => ({ ...v, productName: p.name })),
  )

  return (
    <Autocomplete
      size="small"
      sx={{ minWidth: 260 }}
      disabled={disabled}
      options={options}
      value={value}
      inputValue={inputValue}
      onInputChange={(_, newInputValue) => setInputValue(newInputValue)}
      onChange={(_, newValue) => onChange(newValue)}
      loading={isFetching}
      filterOptions={(opts) => opts}
      getOptionLabel={(v) => `${v.sku} — ${v.productName} (${v.colourName ?? '—'}/${v.sizeName ?? '—'})`}
      isOptionEqualToValue={(a, b) => a.id === b.id}
      noOptionsText={inputValue.trim().length < 2 ? 'Type at least 2 characters…' : 'No matching variants'}
      renderInput={(params) => (
        <TextField
          {...params}
          label="Variant (SKU / barcode / product)"
          placeholder="Scan or search…"
          error={error}
          helperText={helperText}
        />
      )}
    />
  )
}
