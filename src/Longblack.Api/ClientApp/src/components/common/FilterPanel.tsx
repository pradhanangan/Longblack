import FilterListIcon from "@mui/icons-material/FilterList";
import {
  Badge,
  Box,
  Button,
  Chip,
  Collapse,
  FormControl,
  InputLabel,
  MenuItem,
  Select,
  TextField,
} from "@mui/material";
import { useState, type ReactNode } from "react";

export interface FilterField {
  key: string;
  label: string;
  kind: "select" | "text" | "date";
  options?: { value: string; label: string }[];
  /** Menu items for selects whose options are not a flat list (e.g. the category tree). */
  renderOptions?: () => ReactNode;
  /** Label for the empty-value option of a select. Omit when the field has no "all" option. */
  allLabel?: string;
  /** Value meaning "no filter". Defaults to "". */
  emptyValue?: string;
  /** Maps a raw value to the text shown in the chip; defaults to the option label. */
  formatValue?: (value: string) => string;
  /** Required fields always have a value, so they show a chip that cannot be removed. */
  required?: boolean;
  placeholder?: string;
  minWidth?: number;
}

interface Props {
  fields: FilterField[];
  values: Record<string, string>;
  onApply: (values: Record<string, string>) => void;
}

function emptyOf(field: FilterField) {
  return field.emptyValue ?? "";
}

function displayValue(field: FilterField, value: string) {
  if (field.formatValue) return field.formatValue(value);
  return field.options?.find((o) => o.value === value)?.label ?? value;
}

export function FilterPanel({ fields, values, onApply }: Props) {
  const [open, setOpen] = useState(false);
  const [draft, setDraft] = useState<Record<string, string>>(values);

  const active = fields.filter((f) => (values[f.key] ?? "") !== emptyOf(f));

  function toggle() {
    if (!open) setDraft(values);
    setOpen(!open);
  }

  function apply() {
    onApply(draft);
    setOpen(false);
  }

  function clear() {
    const cleared: Record<string, string> = {};
    for (const f of fields) {
      cleared[f.key] = f.required ? (values[f.key] ?? "") : emptyOf(f);
    }
    onApply(cleared);
    setOpen(false);
  }

  function removeFilter(field: FilterField) {
    onApply({ ...values, [field.key]: emptyOf(field) });
  }

  function renderControl(field: FilterField) {
    const value = draft[field.key] ?? "";
    const onChange = (v: string) => setDraft({ ...draft, [field.key]: v });
    const minWidth = field.minWidth ?? 160;

    if (field.kind === "select") {
      return (
        <FormControl key={field.key} size="small" sx={{ minWidth }}>
          <InputLabel>{field.label}</InputLabel>
          <Select
            value={value}
            label={field.label}
            onChange={(e) => onChange(e.target.value)}
          >
            {field.allLabel !== undefined && (
              <MenuItem value={emptyOf(field)}>{field.allLabel}</MenuItem>
            )}
            {field.renderOptions
              ? field.renderOptions()
              : field.options?.map((o) => (
                  <MenuItem key={o.value} value={o.value}>
                    {o.label}
                  </MenuItem>
                ))}
          </Select>
        </FormControl>
      );
    }

    return (
      <TextField
        key={field.key}
        size="small"
        label={field.label}
        type={field.kind === "date" ? "date" : "text"}
        value={value}
        placeholder={field.placeholder}
        onChange={(e) => onChange(e.target.value)}
        onKeyDown={(e) => {
          if (e.key === "Enter") apply();
        }}
        slotProps={
          field.kind === "date" ? { inputLabel: { shrink: true } } : undefined
        }
        sx={{ minWidth: field.kind === "text" ? 220 : undefined }}
      />
    );
  }

  return (
    <Box sx={{ mb: 3 }}>
      <Box
        sx={{ display: "flex", alignItems: "center", gap: 1, flexWrap: "wrap" }}
      >
        <Badge color="primary" badgeContent={active.length}>
          <Button
            variant={open ? "contained" : "outlined"}
            startIcon={<FilterListIcon />}
            onClick={toggle}
            aria-expanded={open}
          >
            Filters
          </Button>
        </Badge>
        {active.map((f) => {
          const value = values[f.key] ?? "";
          return (
            <Chip
              key={f.key}
              label={`${f.label}: ${displayValue(f, value)}`}
              onDelete={f.required ? undefined : () => removeFilter(f)}
            />
          );
        })}
      </Box>

      <Collapse in={open} unmountOnExit>
        <Box
          sx={{
            display: "flex",
            gap: 2,
            mt: 2,
            flexWrap: "wrap",
            alignItems: "center",
          }}
        >
          {fields.map(renderControl)}
          <Button variant="contained" onClick={apply}>
            Apply
          </Button>
          <Button onClick={clear}>Clear</Button>
        </Box>
      </Collapse>
    </Box>
  );
}
