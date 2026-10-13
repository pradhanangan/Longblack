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
import { Controller, useForm } from 'react-hook-form'
import { useNavigate } from 'react-router-dom'
import { z } from 'zod'
import { api } from '../../api/client'
import type { GoodsReceiptDto, SupplierDto } from '../../api/types'
import { useSnackbar } from '../../contexts/SnackbarContext'

const schema = z.object({
  supplierId: z.string().min(1, 'Supplier is required'),
  receivedDate: z.string().min(1, 'Received Date is required'),
})

type FormValues = z.infer<typeof schema>

interface Props {
  open: boolean
  onClose: () => void
  goodsReceipt?: GoodsReceiptDto
}

function toDateInputValue(iso?: string): string {
  if (!iso) return new Date().toISOString().slice(0, 10)
  return iso.slice(0, 10)
}

export function GoodsReceiptFormDialog({ open, onClose, goodsReceipt }: Props) {
  const isEdit = !!goodsReceipt
  const { showSuccess, showError } = useSnackbar()
  const queryClient = useQueryClient()
  const navigate = useNavigate()

  const { data: suppliers } = useQuery<SupplierDto[]>({
    queryKey: ['suppliers', 'Active'],
    queryFn: () => api.get('/api/suppliers?status=Active'),
    staleTime: 5 * 60_000,
  })

  const {
    handleSubmit,
    control,
    register,
    reset,
    formState: { errors },
  } = useForm<FormValues>({ resolver: zodResolver(schema) })

  useEffect(() => {
    if (open) {
      reset({
        supplierId: goodsReceipt?.supplierId ?? '',
        receivedDate: toDateInputValue(goodsReceipt?.receivedDate),
      })
    }
  }, [open, goodsReceipt, reset])

  const mutation = useMutation({
    mutationFn: (values: FormValues) => {
      const body = { supplierId: values.supplierId, receivedDate: new Date(values.receivedDate).toISOString() }
      return isEdit
        ? api.put<GoodsReceiptDto>(`/api/goods-receipts/${goodsReceipt!.id}`, body)
        : api.post<GoodsReceiptDto>('/api/goods-receipts', body)
    },
    onSuccess: (data: GoodsReceiptDto) => {
      queryClient.invalidateQueries({ queryKey: ['goods-receipts'] })
      showSuccess(isEdit ? 'Goods receipt updated.' : 'Goods receipt created.')
      onClose()
      if (!isEdit) navigate(`/goods-receipts/${data.id}`)
    },
    onError: (err: unknown) => {
      const error = err as Error
      showError(error.message ?? 'Failed to save goods receipt.')
    },
  })

  return (
    <Dialog open={open} onClose={onClose} maxWidth="sm" fullWidth>
      <DialogTitle>{isEdit ? 'Edit Goods Receipt' : 'New Goods Receipt'}</DialogTitle>
      <DialogContent>
        <Controller
          name="supplierId"
          control={control}
          render={({ field }) => (
            <FormControl fullWidth margin="normal" error={!!errors.supplierId}>
              <InputLabel>Supplier</InputLabel>
              <Select {...field} label="Supplier">
                {suppliers?.map((s) => (
                  <MenuItem key={s.id} value={s.id}>
                    {s.name}
                  </MenuItem>
                ))}
              </Select>
              {errors.supplierId && <FormHelperText>{errors.supplierId.message}</FormHelperText>}
            </FormControl>
          )}
        />
        <TextField
          label="Received Date"
          type="date"
          fullWidth
          margin="normal"
          slotProps={{ inputLabel: { shrink: true } }}
          {...register('receivedDate')}
          error={!!errors.receivedDate}
          helperText={errors.receivedDate?.message}
        />
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>Cancel</Button>
        <Button
          variant="contained"
          onClick={handleSubmit((v) => mutation.mutate(v))}
          disabled={mutation.isPending}
          startIcon={mutation.isPending ? <CircularProgress size={16} /> : undefined}
        >
          {isEdit ? 'Save' : 'Create'}
        </Button>
      </DialogActions>
    </Dialog>
  )
}
