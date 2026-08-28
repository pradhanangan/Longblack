import { createContext, useCallback, useContext, useState } from 'react'
import { Alert, Snackbar } from '@mui/material'

type Severity = 'success' | 'error'

interface SnackbarMessage {
  message: string
  severity: Severity
}

interface SnackbarContextValue {
  showSuccess: (message: string) => void
  showError: (message: string) => void
}

const SnackbarContext = createContext<SnackbarContextValue | null>(null)

export function SnackbarProvider({ children }: { children: React.ReactNode }) {
  const [snack, setSnack] = useState<SnackbarMessage | null>(null)
  const [open, setOpen] = useState(false)

  const showSuccess = useCallback((message: string) => {
    setSnack({ message, severity: 'success' })
    setOpen(true)
  }, [])

  const showError = useCallback((message: string) => {
    setSnack({ message, severity: 'error' })
    setOpen(true)
  }, [])

  return (
    <SnackbarContext.Provider value={{ showSuccess, showError }}>
      {children}
      <Snackbar
        open={open}
        autoHideDuration={snack?.severity === 'success' ? 4000 : null}
        onClose={() => setOpen(false)}
        anchorOrigin={{ vertical: 'bottom', horizontal: 'center' }}
      >
        <Alert
          severity={snack?.severity ?? 'success'}
          onClose={() => setOpen(false)}
          sx={{ width: '100%' }}
        >
          {snack?.message}
        </Alert>
      </Snackbar>
    </SnackbarContext.Provider>
  )
}

export function useSnackbar() {
  const ctx = useContext(SnackbarContext)
  if (!ctx) throw new Error('useSnackbar must be used inside SnackbarProvider')
  return ctx
}
