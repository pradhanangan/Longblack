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
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useEffect } from 'react'
import { Controller, useForm } from 'react-hook-form'
import { z } from 'zod'
import { api } from '../../api/client'
import type { StoreDto } from '../../api/types'
import { useSnackbar } from '../../contexts/SnackbarContext'

const schema = z.object({
  name: z.string().min(1, 'Name is required'),
  type: z.string().min(1, 'Type is required'),
})

type FormValues = z.infer<typeof schema>

interface Props {
  open: boolean
  onClose: () => void
  store?: StoreDto
}

// Type is only set at creation — editing a Store only ever changes its Name (see spec: Admins can
// edit a Store's name, not its Type). The Warehouse type itself is excluded from the create list
// since exactly one is seeded automatically and a second can never be created.
const CREATABLE_TYPES = ['LongStanding', 'OneOff', 'Online']

export function StoreFormDialog({ open, onClose, store }: Props) {
  const isEdit = !!store
  const { showSuccess, showError } = useSnackbar()
  const queryClient = useQueryClient()

  const {
    register,
    handleSubmit,
    control,
    reset,
    formState: { errors },
  } = useForm<FormValues>({ resolver: zodResolver(schema) })

  useEffect(() => {
    if (open) {
      reset({
        name: store?.name ?? '',
        type: store?.type ?? '',
      })
    }
  }, [open, store, reset])

  const mutation = useMutation({
    mutationFn: (values: FormValues) =>
      isEdit
        ? api.put<StoreDto>(`/api/stores/${store!.id}`, { name: values.name })
        : api.post<StoreDto>('/api/stores', values),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['stores'] })
      showSuccess(isEdit ? 'Store updated.' : 'Store created.')
      onClose()
    },
    onError: (err: unknown) => showError((err as Error).message ?? 'Failed to save store.'),
  })

  return (
    <Dialog open={open} onClose={onClose} maxWidth="sm" fullWidth>
      <DialogTitle>{isEdit ? 'Edit Store' : 'New Store'}</DialogTitle>
      <DialogContent>
        <TextField
          label="Name"
          fullWidth
          margin="normal"
          {...register('name')}
          error={!!errors.name}
          helperText={errors.name?.message}
        />
        {!isEdit && (
          <Controller
            name="type"
            control={control}
            render={({ field }) => (
              <FormControl fullWidth margin="normal" error={!!errors.type}>
                <InputLabel>Type</InputLabel>
                <Select {...field} label="Type">
                  {CREATABLE_TYPES.map((t) => (
                    <MenuItem key={t} value={t}>
                      {t}
                    </MenuItem>
                  ))}
                </Select>
                {errors.type && <FormHelperText>{errors.type.message}</FormHelperText>}
              </FormControl>
            )}
          />
        )}
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
