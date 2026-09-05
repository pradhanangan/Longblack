import { zodResolver } from '@hookform/resolvers/zod'
import {
  Button,
  CircularProgress,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  TextField,
} from '@mui/material'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useEffect } from 'react'
import { useForm } from 'react-hook-form'
import { useNavigate } from 'react-router-dom'
import { z } from 'zod'
import { api } from '../../api/client'
import type { GoodsReceiptDto } from '../../api/types'
import { useSnackbar } from '../../contexts/SnackbarContext'

const schema = z.object({
  supplierCode: z.string().min(1, 'Supplier Code is required').max(50, 'Max 50 characters'),
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

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<FormValues>({ resolver: zodResolver(schema) })

  useEffect(() => {
    if (open) {
      reset({
        supplierCode: goodsReceipt?.supplierCode ?? '',
        receivedDate: toDateInputValue(goodsReceipt?.receivedDate),
      })
    }
  }, [open, goodsReceipt, reset])

  const mutation = useMutation({
    mutationFn: (values: FormValues) => {
      const body = { supplierCode: values.supplierCode, receivedDate: new Date(values.receivedDate).toISOString() }
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
        <TextField
          label="Supplier Code"
          fullWidth
          margin="normal"
          {...register('supplierCode')}
          error={!!errors.supplierCode}
          helperText={errors.supplierCode?.message}
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
