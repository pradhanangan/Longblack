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
import type { BrandDto } from '../../api/types'
import { useSnackbar } from '../../contexts/SnackbarContext'

const schema = z.object({
  code: z.string().trim().min(1, 'Code is required'),
  name: z.string().trim().min(1, 'Name is required'),
})

type FormValues = z.infer<typeof schema>

interface Props {
  open: boolean
  onClose: () => void
  brand?: BrandDto
}

export function BrandFormDialog({ open, onClose, brand }: Props) {
  const isEdit = !!brand
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
      reset({ code: brand?.code ?? '', name: brand?.name ?? '' })
    }
  }, [open, brand, reset])

  const mutation = useMutation({
    mutationFn: (values: FormValues) =>
      isEdit
        ? api.put<BrandDto>(`/api/brands/${brand!.id}`, { ...values, code: brand!.code })
        : api.post<BrandDto>('/api/brands', values),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['brands'] })
      showSuccess(isEdit ? 'Brand updated.' : 'Brand created.')
      onClose()
    },
    onError: (err: unknown) => {
      const error = err as Error & { status?: number }
      if (error.status === 409) {
        setError(error.message.toLowerCase().includes('code') ? 'code' : 'name', {
          message: error.message,
        })
      } else {
        showError(error.message ?? 'Failed to save brand.')
      }
    },
  })

  return (
    <Dialog open={open} onClose={onClose} maxWidth="sm" fullWidth>
      <DialogTitle>{isEdit ? 'Edit Brand' : 'New Brand'}</DialogTitle>
      <DialogContent>
        <TextField
          label="Code"
          fullWidth
          margin="normal"
          {...register('code')}
          disabled={isEdit}
          error={!!errors.code}
          helperText={
            errors.code?.message ??
            (isEdit ? 'The code cannot be changed once created.' : undefined)
          }
        />
        <TextField
          label="Name"
          fullWidth
          margin="normal"
          {...register('name')}
          error={!!errors.name}
          helperText={errors.name?.message}
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
