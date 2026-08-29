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
import type { BrandDto, CategoryDto, ProductDto } from '../../api/types'
import { useSnackbar } from '../../contexts/SnackbarContext'

const schema = z.object({
  productCode: z.string().min(1, 'Product Code is required'),
  name: z.string().min(1, 'Name is required'),
  description: z.string().optional(),
  brandId: z.string().optional(),
  categoryId: z.string().optional(),
})

type FormValues = z.infer<typeof schema>

interface Props {
  open: boolean
  onClose: () => void
  product?: ProductDto
}

export function ProductFormDialog({ open, onClose, product }: Props) {
  const isEdit = !!product
  const { showSuccess, showError } = useSnackbar()
  const queryClient = useQueryClient()

  const { data: brands } = useQuery<BrandDto[]>({
    queryKey: ['brands'],
    queryFn: () => api.get('/api/brands'),
    staleTime: 5 * 60_000,
  })

  const { data: categories } = useQuery<CategoryDto[]>({
    queryKey: ['categories'],
    queryFn: () => api.get('/api/categories'),
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

  const watchedBrandId = useWatch({ control, name: 'brandId' })
  const watchedCategoryId = useWatch({ control, name: 'categoryId' })

  // Fetch suggestion whenever brand+category changes (Add mode only)
  const { data: suggestion } = useQuery<{ suggestedCode: string }>({
    queryKey: ['products', 'suggest-code', watchedBrandId, watchedCategoryId],
    queryFn: () => api.get(`/api/products/suggest-code?brandId=${watchedBrandId}&categoryId=${watchedCategoryId}`),
    enabled: !isEdit && !!watchedBrandId && !!watchedCategoryId,
    staleTime: 0,
  })

  // Pre-fill productCode only when field is still empty
  useEffect(() => {
    if (!isEdit && suggestion?.suggestedCode) {
      const current = getValues('productCode')
      if (!current) setValue('productCode', suggestion.suggestedCode)
    }
  }, [suggestion, isEdit, getValues, setValue])

  useEffect(() => {
    if (open) {
      reset({
        productCode: product?.productCode ?? '',
        name: product?.name ?? '',
        description: product?.description ?? '',
        brandId: product?.brandId ?? '',
        categoryId: product?.categoryId ?? '',
      })
    }
  }, [open, product, reset])

  const mutation = useMutation({
    mutationFn: (values: FormValues) => {
      const body = {
        ...values,
        brandId: values.brandId || null,
        categoryId: values.categoryId || null,
      }
      return isEdit
        ? api.put<ProductDto>(`/api/products/${product!.id}`, body)
        : api.post<ProductDto>('/api/products', body)
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['products'] })
      showSuccess(isEdit ? 'Product updated.' : 'Product created.')
      onClose()
    },
    onError: (err: unknown) => {
      const error = err as Error & { status?: number }
      if (error.status === 409) {
        setError('productCode', { message: 'Product Code already exists.' })
      } else {
        showError(error.message ?? 'Failed to save product.')
      }
    },
  })

  return (
    <Dialog open={open} onClose={onClose} maxWidth="sm" fullWidth>
      <DialogTitle>{isEdit ? 'Edit Product' : 'Add Product'}</DialogTitle>
      <DialogContent>
        <TextField
          label="Product Code"
          fullWidth
          margin="normal"
          {...register('productCode')}
          error={!!errors.productCode}
          helperText={errors.productCode?.message}
          disabled={isEdit}
          slotProps={isEdit ? { input: { readOnly: true } } : undefined}
        />
        <TextField
          label="Name"
          fullWidth
          margin="normal"
          {...register('name')}
          error={!!errors.name}
          helperText={errors.name?.message}
        />
        <TextField
          label="Description"
          fullWidth
          margin="normal"
          multiline
          rows={3}
          {...register('description')}
        />
        <Controller
          name="brandId"
          control={control}
          render={({ field }) => (
            <FormControl fullWidth margin="normal" error={!!errors.brandId}>
              <InputLabel>Brand</InputLabel>
              <Select {...field} label="Brand">
                <MenuItem value="">None</MenuItem>
                {brands?.map((b) => <MenuItem key={b.id} value={b.id}>{b.name}</MenuItem>)}
              </Select>
              {errors.brandId && <FormHelperText>{errors.brandId.message}</FormHelperText>}
            </FormControl>
          )}
        />
        <Controller
          name="categoryId"
          control={control}
          render={({ field }) => (
            <FormControl fullWidth margin="normal" error={!!errors.categoryId}>
              <InputLabel>Category</InputLabel>
              <Select {...field} label="Category">
                <MenuItem value="">None</MenuItem>
                {categories?.map((c) => <MenuItem key={c.id} value={c.id}>{c.name}</MenuItem>)}
              </Select>
              {errors.categoryId && <FormHelperText>{errors.categoryId.message}</FormHelperText>}
            </FormControl>
          )}
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
