import { zodResolver } from '@hookform/resolvers/zod'
import {
  Button,
  CircularProgress,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  FormControl,
  FormHelperText,
  InputLabel,
  MenuItem,
  Select,
  TextField,
} from '@mui/material'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useEffect } from 'react'
import { Controller, useForm, useWatch } from 'react-hook-form'
import { z } from 'zod'
import { api } from '../../api/client'
import type { ColourDto, ProductVariantDto, SizeDto } from '../../api/types'
import { useSnackbar } from '../../contexts/SnackbarContext'

const schema = z.object({
  sku: z.string().min(1, 'SKU is required'),
  barcode: z.string().optional(),
  colourId: z.string().min(1, 'Colour is required'),
  sizeId: z.string().min(1, 'Size is required'),
  sellingPrice: z
    .number({ message: 'Enter a valid price' })
    .positive('Selling Price must be greater than 0'),
})

type FormValues = z.infer<typeof schema>

interface Props {
  open: boolean
  onClose: () => void
  productId: string
  variant?: ProductVariantDto
  brandCode?: string
  categoryCode?: string
}

export function VariantFormDialog({ open, onClose, productId, variant, brandCode, categoryCode }: Props) {
  const isEdit = !!variant
  const { showSuccess, showError } = useSnackbar()
  const queryClient = useQueryClient()

  const { data: colours } = useQuery<ColourDto[]>({
    queryKey: ['colours'],
    queryFn: () => api.get('/api/colours'),
    staleTime: 5 * 60_000,
  })

  const { data: sizes } = useQuery<SizeDto[]>({
    queryKey: ['sizes'],
    queryFn: () => api.get('/api/sizes'),
    staleTime: 5 * 60_000,
  })

  const {
    register,
    handleSubmit,
    control,
    reset,
    setValue,
    getValues,
    setError,
    formState: { errors, isSubmitting },
  } = useForm<FormValues>({ resolver: zodResolver(schema) })

  const watchedColourId = useWatch({ control, name: 'colourId' })
  const watchedSizeId = useWatch({ control, name: 'sizeId' })

  // SKU suggestion: compute client-side from brand/category/colour/size codes
  useEffect(() => {
    if (isEdit || !brandCode || !categoryCode) return
    const colour = colours?.find((c) => c.id === watchedColourId)
    const size = sizes?.find((s) => s.id === watchedSizeId)
    if (!colour || !size) return
    const suggested = `${brandCode}-${categoryCode}-${colour.code}-${size.code}`.toUpperCase()
    const current = getValues('sku')
    if (!current) setValue('sku', suggested)
  }, [watchedColourId, watchedSizeId, brandCode, categoryCode, colours, sizes, isEdit, getValues, setValue])

  useEffect(() => {
    if (open) {
      reset({
        sku: variant?.sku ?? '',
        barcode: variant?.barcode ?? '',
        colourId: variant?.colourId ?? '',
        sizeId: variant?.sizeId ?? '',
        sellingPrice: variant?.sellingPrice ?? ('' as unknown as number),
      })
    }
  }, [open, variant, reset])

  const mutation = useMutation({
    mutationFn: (values: FormValues) => {
      const body = { ...values, barcode: values.barcode || null }
      return isEdit
        ? api.put<ProductVariantDto>(`/api/products/${productId}/variants/${variant!.id}`, body)
        : api.post<ProductVariantDto>(`/api/products/${productId}/variants`, body)
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['products', productId, 'variants'] })
      showSuccess(isEdit ? 'Variant updated.' : 'Variant created.')
      onClose()
    },
    onError: (err: unknown) => {
      const error = err as Error & { status?: number }
      if (error.status === 409) {
        const msg = error.message ?? ''
        if (msg.toLowerCase().includes('barcode')) {
          setError('barcode', { message: 'Barcode already exists.' })
        } else {
          setError('sku', { message: 'SKU already exists.' })
        }
      } else {
        showError(error.message ?? 'Failed to save variant.')
      }
    },
  })

  return (
    <Dialog open={open} onClose={onClose} maxWidth="sm" fullWidth>
      <DialogTitle>{isEdit ? 'Edit Variant' : 'Add Variant'}</DialogTitle>
      <DialogContent>
        <TextField
          label="SKU"
          fullWidth
          margin="normal"
          {...register('sku')}
          error={!!errors.sku}
          helperText={errors.sku?.message}
          disabled={isEdit}
          slotProps={isEdit ? { input: { readOnly: true } } : undefined}
        />
        <TextField
          label="Barcode (optional)"
          fullWidth
          margin="normal"
          {...register('barcode')}
          error={!!errors.barcode}
          helperText={errors.barcode?.message}
        />
        <Controller
          name="colourId"
          control={control}
          render={({ field }) => (
            <FormControl fullWidth margin="normal" error={!!errors.colourId}>
              <InputLabel>Colour</InputLabel>
              <Select {...field} label="Colour">
                <MenuItem value="">Select colour</MenuItem>
                {colours?.map((c) => <MenuItem key={c.id} value={c.id}>{c.name}</MenuItem>)}
              </Select>
              {errors.colourId && <FormHelperText>{errors.colourId.message}</FormHelperText>}
            </FormControl>
          )}
        />
        <Controller
          name="sizeId"
          control={control}
          render={({ field }) => (
            <FormControl fullWidth margin="normal" error={!!errors.sizeId}>
              <InputLabel>Size</InputLabel>
              <Select {...field} label="Size">
                <MenuItem value="">Select size</MenuItem>
                {sizes?.map((s) => <MenuItem key={s.id} value={s.id}>{s.name}</MenuItem>)}
              </Select>
              {errors.sizeId && <FormHelperText>{errors.sizeId.message}</FormHelperText>}
            </FormControl>
          )}
        />
        <TextField
          label="Selling Price"
          type="number"
          fullWidth
          margin="normal"
          {...register('sellingPrice', { valueAsNumber: true })}
          error={!!errors.sellingPrice}
          helperText={errors.sellingPrice?.message}
          slotProps={{ input: { inputProps: { min: 0, step: '0.01' } } }}
        />
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>Cancel</Button>
        <Button
          variant="contained"
          onClick={handleSubmit((v) => mutation.mutate(v))}
          disabled={isSubmitting || mutation.isPending}
          startIcon={mutation.isPending ? <CircularProgress size={16} /> : undefined}
        >
          {isEdit ? 'Save' : 'Create'}
        </Button>
      </DialogActions>
    </Dialog>
  )
}
