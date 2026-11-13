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
import type { CategoryDto } from '../../api/types'
import { useSnackbar } from '../../contexts/SnackbarContext'

const schema = z.object({
  code: z.string().trim().min(1, 'Code is required'),
  name: z.string().trim().min(1, 'Name is required'),
  parentCategoryId: z.string(),
})

type FormValues = z.infer<typeof schema>

interface Props {
  open: boolean
  onClose: () => void
  category?: CategoryDto
  // Every category (any status), used to offer top-level parents and detect sub-categories.
  allCategories: CategoryDto[]
}

export function CategoryFormDialog({ open, onClose, category, allCategories }: Props) {
  const isEdit = !!category
  const { showSuccess, showError } = useSnackbar()
  const queryClient = useQueryClient()

  const hasChildren =
    isEdit && allCategories.some((c) => c.parentCategoryId === category.id)
  const parentOptions = allCategories
    .filter((c) => !c.parentCategoryId && c.id !== category?.id)
    .sort((a, b) => a.name.localeCompare(b.name))

  const {
    register,
    control,
    handleSubmit,
    reset,
    setError,
    formState: { errors },
  } = useForm<FormValues>({ resolver: zodResolver(schema) })

  useEffect(() => {
    if (open) {
      reset({
        code: category?.code ?? '',
        name: category?.name ?? '',
        parentCategoryId: category?.parentCategoryId ?? '',
      })
    }
  }, [open, category, reset])

  const mutation = useMutation({
    mutationFn: (values: FormValues) => {
      const body = {
        code: isEdit ? category.code : values.code,
        name: values.name,
        parentCategoryId: values.parentCategoryId || null,
      }
      return isEdit
        ? api.put<CategoryDto>(`/api/categories/${category.id}`, body)
        : api.post<CategoryDto>('/api/categories', body)
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['categories'] })
      showSuccess(isEdit ? 'Category updated.' : 'Category created.')
      onClose()
    },
    onError: (err: unknown) => {
      const error = err as Error & { status?: number }
      if (error.status === 409) {
        setError(error.message.toLowerCase().includes('code') ? 'code' : 'name', {
          message: error.message,
        })
      } else {
        showError(error.message ?? 'Failed to save category.')
      }
    },
  })

  return (
    <Dialog open={open} onClose={onClose} maxWidth="sm" fullWidth>
      <DialogTitle>{isEdit ? 'Edit Category' : 'New Category'}</DialogTitle>
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
        <Controller
          name="parentCategoryId"
          control={control}
          render={({ field }) => (
            <FormControl fullWidth margin="normal" disabled={hasChildren}>
              <InputLabel shrink>Parent category</InputLabel>
              <Select {...field} label="Parent category" displayEmpty notched>
                <MenuItem value="">None (top-level)</MenuItem>
                {parentOptions.map((p) => (
                  <MenuItem key={p.id} value={p.id}>
                    {p.name}
                  </MenuItem>
                ))}
              </Select>
              {hasChildren && (
                <FormHelperText>
                  This category has sub-categories, so it must stay top-level.
                </FormHelperText>
              )}
            </FormControl>
          )}
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
