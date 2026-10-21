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
import type { PurchaseOrderDto, SupplierDto } from '../../api/types'
import { useSnackbar } from '../../contexts/SnackbarContext'

const schema = z.object({
  supplierId: z.string().min(1, 'Supplier is required'),
  expectedDeliveryDate: z.string(),
  notes: z.string(),
})

type FormValues = z.infer<typeof schema>

interface Props {
  open: boolean
  onClose: () => void
  purchaseOrder?: PurchaseOrderDto
}

export function PurchaseOrderFormDialog({ open, onClose, purchaseOrder }: Props) {
  const isEdit = !!purchaseOrder
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
        supplierId: purchaseOrder?.supplierId ?? '',
        expectedDeliveryDate: purchaseOrder?.expectedDeliveryDate?.slice(0, 10) ?? '',
        notes: purchaseOrder?.notes ?? '',
      })
    }
  }, [open, purchaseOrder, reset])

  const mutation = useMutation({
    mutationFn: (values: FormValues) => {
      const body = {
        supplierId: values.supplierId,
        expectedDeliveryDate: values.expectedDeliveryDate
          ? new Date(values.expectedDeliveryDate).toISOString()
          : null,
        notes: values.notes.trim() || null,
      }
      return isEdit
        ? api.put<PurchaseOrderDto>(`/api/purchase-orders/${purchaseOrder!.id}`, body)
        : api.post<PurchaseOrderDto>('/api/purchase-orders', body)
    },
    onSuccess: (data: PurchaseOrderDto) => {
      queryClient.invalidateQueries({ queryKey: ['purchase-orders'] })
      showSuccess(isEdit ? 'Purchase order updated.' : 'Purchase order created.')
      onClose()
      if (!isEdit) navigate(`/purchase-orders/${data.id}`)
    },
    onError: (err: unknown) => showError((err as Error).message ?? 'Failed to save purchase order.'),
  })

  return (
    <Dialog open={open} onClose={onClose} maxWidth="sm" fullWidth>
      <DialogTitle>{isEdit ? 'Edit Purchase Order' : 'New Purchase Order'}</DialogTitle>
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
          label="Expected Delivery Date"
          type="date"
          fullWidth
          margin="normal"
          slotProps={{ inputLabel: { shrink: true } }}
          {...register('expectedDeliveryDate')}
        />
        <TextField label="Notes" fullWidth multiline minRows={2} margin="normal" {...register('notes')} />
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
