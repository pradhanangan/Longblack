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
import { z } from 'zod'
import { api } from '../../api/client'
import type { SupplierDto } from '../../api/types'
import { useSnackbar } from '../../contexts/SnackbarContext'

const schema = z.object({
  code: z.string().min(1, 'Code is required'),
  name: z.string().min(1, 'Name is required'),
  email: z.string().min(1, 'Email is required').email('Enter a valid email'),
  phone: z.string().min(1, 'Phone is required'),
})

type FormValues = z.infer<typeof schema>

interface Props {
  open: boolean
  onClose: () => void
  supplier?: SupplierDto
}

export function SupplierFormDialog({ open, onClose, supplier }: Props) {
  const isEdit = !!supplier
  const { showSuccess, showError } = useSnackbar()
  const queryClient = useQueryClient()

  const {
    register,
    handleSubmit,
    reset,
    setError,
    formState: { errors },
  } = useForm<FormValues>({ resolver: zodResolver(schema) })

  useEffect(() => {
    if (open) {
      reset({
        code: supplier?.code ?? '',
        name: supplier?.name ?? '',
        email: supplier?.email ?? '',
        phone: supplier?.phone ?? '',
      })
    }
  }, [open, supplier, reset])

  const mutation = useMutation({
    mutationFn: (values: FormValues) =>
      isEdit
        ? api.put<SupplierDto>(`/api/suppliers/${supplier!.id}`, values)
        : api.post<SupplierDto>('/api/suppliers', values),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['suppliers'] })
      showSuccess(isEdit ? 'Supplier updated.' : 'Supplier created.')
      onClose()
    },
    onError: (err: unknown) => {
      const error = err as Error & { status?: number }
      if (error.status === 409) {
        setError('code', { message: 'Code already exists.' })
      } else {
        showError(error.message ?? 'Failed to save supplier.')
      }
    },
  })

  return (
    <Dialog open={open} onClose={onClose} maxWidth="sm" fullWidth>
      <DialogTitle>{isEdit ? 'Edit Supplier' : 'New Supplier'}</DialogTitle>
      <DialogContent>
        <TextField
          label="Code"
          fullWidth
          margin="normal"
          {...register('code')}
          error={!!errors.code}
          helperText={errors.code?.message}
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
          label="Email"
          fullWidth
          margin="normal"
          {...register('email')}
          error={!!errors.email}
          helperText={errors.email?.message}
        />
        <TextField
          label="Phone"
          fullWidth
          margin="normal"
          {...register('phone')}
          error={!!errors.phone}
          helperText={errors.phone?.message}
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
