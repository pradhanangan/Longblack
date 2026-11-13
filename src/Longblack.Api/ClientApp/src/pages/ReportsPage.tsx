import {
  Alert,
  Box,
  Button,
  Card,
  CardContent,
  Chip,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  FormControlLabel,
  MenuItem,
  Stack,
  Switch,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TablePagination,
  TableRow,
  TextField,
  Typography,
} from "@mui/material";
import DownloadIcon from "@mui/icons-material/Download";
import { useQuery } from "@tanstack/react-query";
import { useDeferredValue, useState } from "react";
import { api } from "../api/client";
import type { BrandDto, CategoryDto, PagedResult, StoreDto } from "../api/types";
import { useSnackbar } from "../contexts/SnackbarContext";

type ReportType = "stock-on-hand" | "stock-movement" | "sales-summary" | "stock-take-variance";

const REPORTS: { value: ReportType; label: string }[] = [
  { value: "stock-on-hand", label: "Stock on Hand" },
  { value: "stock-movement", label: "Stock Movement" },
  { value: "sales-summary", label: "Sales Summary" },
  { value: "stock-take-variance", label: "Stock Take Variance" },
];

const MOVEMENT_TYPES = [
  "GoodsReceived",
  "StockTakeAdjustment",
  "ManualAdjustment",
  "TransferOut",
  "TransferIn",
  "Sale",
  "SaleVoid",
];

const GROUP_BY = ["day", "product", "category", "brand", "store"];

interface StockOnHandRow {
  productVariantId: string;
  sku: string;
  productName: string;
  colourName: string | null;
  sizeName: string | null;
  storeId: string;
  storeName: string;
  quantity: number;
  isLowStock: boolean;
}
interface StockOnHandReport {
  lowStockThreshold: number;
  rows: PagedResult<StockOnHandRow>;
}
interface MovementRow {
  id: string;
  createdAt: string;
  sku: string;
  productName: string;
  colourName: string | null;
  sizeName: string | null;
  storeName: string;
  type: string;
  quantityDelta: number;
  createdBy: string;
}
interface SalesRow {
  key: string;
  label: string;
  saleCount: number;
  units: number;
  total: number;
  gst: number;
  exGst: number;
}
interface SalesSummary {
  totals: { saleCount: number; units: number; total: number; gst: number; exGst: number };
  voided: { saleCount: number; total: number };
  rows: SalesRow[];
  byDay: SalesRow[];
}
interface VarianceRow {
  stockTakeId: string;
  referenceNumber: string;
  storeName: string;
  status: string;
  completionDate: string | null;
  approvedDate: string | null;
  countedItems: number;
  shrinkage: number;
  overage: number;
}
interface VarianceItem {
  productVariantId: string;
  sku: string;
  productName: string;
  colourName: string | null;
  sizeName: string | null;
  expectedQuantity: number;
  countedQuantity: number;
  variance: number;
}
interface VarianceDetail {
  stockTake: VarianceRow;
  items: VarianceItem[];
}

const money = (n: number) => n.toLocaleString("en-NZ", { style: "currency", currency: "NZD" });
const fmtDate = (s: string | null) => (s ? new Date(s).toLocaleDateString("en-NZ") : "—");
const fmtDateTime = (s: string) => new Date(s).toLocaleString("en-NZ");
const variantLabel = (c: string | null, s: string | null) => [c, s].filter(Boolean).join(" / ") || "—";

function isoDate(d: Date) {
  const p = (n: number) => String(n).padStart(2, "0");
  return `${d.getFullYear()}-${p(d.getMonth() + 1)}-${p(d.getDate())}`;
}
function defaultRange() {
  const to = new Date();
  const from = new Date();
  from.setDate(from.getDate() - 30);
  return { from: isoDate(from), to: isoDate(to) };
}

function qs(params: Record<string, string | number | boolean | undefined>) {
  const sp = new URLSearchParams();
  for (const [k, v] of Object.entries(params)) {
    if (v !== undefined && v !== "" && v !== false) sp.set(k, String(v));
  }
  const s = sp.toString();
  return s ? `?${s}` : "";
}

function usePaging(resetKey: string) {
  const [page, setPage] = useState(0);
  const [pageSize, setPageSize] = useState(25);
  const [lastKey, setLastKey] = useState(resetKey);
  if (lastKey !== resetKey) {
    setLastKey(resetKey);
    setPage(0);
  }
  return { page, setPage, pageSize, setPageSize };
}

function useExport() {
  const { showError } = useSnackbar();
  return async (path: string) => {
    try {
      const { blob, fileName } = await api.getBlob(path);
      const url = URL.createObjectURL(blob);
      const a = document.createElement("a");
      a.href = url;
      a.download = fileName;
      a.click();
      URL.revokeObjectURL(url);
    } catch (e) {
      showError((e as Error).message);
    }
  };
}

function ExportButton({ path }: { path: string }) {
  const exportCsv = useExport();
  return (
    <Button variant="outlined" startIcon={<DownloadIcon />} onClick={() => exportCsv(path)}>
      Export CSV
    </Button>
  );
}

function Pager({
  total,
  paging,
}: {
  total: number;
  paging: ReturnType<typeof usePaging>;
}) {
  return (
    <TablePagination
      component="div"
      count={total}
      page={paging.page}
      rowsPerPage={paging.pageSize}
      onPageChange={(_, p) => paging.setPage(p)}
      onRowsPerPageChange={(e) => {
        paging.setPageSize(Number(e.target.value));
        paging.setPage(0);
      }}
      rowsPerPageOptions={[10, 25, 50, 100]}
    />
  );
}

function EmptyRow({ cols }: { cols: number }) {
  return (
    <TableRow>
      <TableCell colSpan={cols} align="center">
        No data for the selected filters.
      </TableCell>
    </TableRow>
  );
}

interface Filters {
  storeId: string;
  brandId: string;
  categoryId: string;
  from: string;
  to: string;
}

function DateFields({ f, set }: { f: Filters; set: (p: Partial<Filters>) => void }) {
  return (
    <>
      <TextField
        size="small"
        type="date"
        label="From"
        value={f.from}
        onChange={(e) => set({ from: e.target.value })}
        slotProps={{ inputLabel: { shrink: true } }}
      />
      <TextField
        size="small"
        type="date"
        label="To"
        value={f.to}
        onChange={(e) => set({ to: e.target.value })}
        slotProps={{ inputLabel: { shrink: true } }}
      />
    </>
  );
}

function StockOnHand({ f }: { f: Filters }) {
  const [lowOnly, setLowOnly] = useState(false);
  const base = { storeId: f.storeId, brandId: f.brandId, categoryId: f.categoryId, lowStockOnly: lowOnly };
  const paging = usePaging(JSON.stringify(base));
  const { data } = useQuery<StockOnHandReport>({
    queryKey: ["report", "soh", base, paging.page, paging.pageSize],
    queryFn: () =>
      api.get(`/api/reports/stock-on-hand${qs({ ...base, page: paging.page + 1, pageSize: paging.pageSize })}`),
  });
  return (
    <>
      <Stack direction="row" spacing={2} sx={{ mb: 2, alignItems: "center" }}>
        <FormControlLabel
          control={<Switch checked={lowOnly} onChange={(e) => setLowOnly(e.target.checked)} />}
          label="Low stock only"
        />
        {data && (
          <Typography variant="body2" color="text.secondary">
            Low stock = quantity at or below {data.lowStockThreshold}
          </Typography>
        )}
        <Box sx={{ flexGrow: 1 }} />
        <ExportButton path={`/api/reports/stock-on-hand/export${qs(base)}`} />
      </Stack>
      <Table size="small" data-no-stack>
        <TableHead>
          <TableRow>
            <TableCell>SKU</TableCell>
            <TableCell>Product</TableCell>
            <TableCell>Variant</TableCell>
            <TableCell>Store</TableCell>
            <TableCell align="right">Quantity</TableCell>
            <TableCell />
          </TableRow>
        </TableHead>
        <TableBody>
          {data?.rows.items.map((r) => (
            <TableRow key={`${r.productVariantId}-${r.storeId}`}>
              <TableCell>{r.sku}</TableCell>
              <TableCell>{r.productName}</TableCell>
              <TableCell>{variantLabel(r.colourName, r.sizeName)}</TableCell>
              <TableCell>{r.storeName}</TableCell>
              <TableCell align="right">{r.quantity}</TableCell>
              <TableCell>{r.isLowStock && <Chip size="small" color="warning" label="Low" />}</TableCell>
            </TableRow>
          ))}
          {data && data.rows.items.length === 0 && <EmptyRow cols={6} />}
        </TableBody>
      </Table>
      <Pager total={data?.rows.total ?? 0} paging={paging} />
    </>
  );
}

function StockMovement({ f }: { f: Filters }) {
  const [search, setSearch] = useState("");
  const [type, setType] = useState("");
  const [user, setUser] = useState("");
  const q = useDeferredValue(search);
  const u = useDeferredValue(user);
  const base = { storeId: f.storeId, from: f.from, to: f.to, q, type, user: u };
  const paging = usePaging(JSON.stringify(base));
  const { data } = useQuery<PagedResult<MovementRow>>({
    queryKey: ["report", "movement", base, paging.page, paging.pageSize],
    queryFn: () =>
      api.get(`/api/reports/stock-movement${qs({ ...base, page: paging.page + 1, pageSize: paging.pageSize })}`),
  });
  return (
    <>
      <Stack direction="row" spacing={2} useFlexGap sx={{ mb: 2, flexWrap: "wrap" }}>
        <TextField size="small" label="SKU / product" value={search} onChange={(e) => setSearch(e.target.value)} />
        <TextField size="small" select label="Type" value={type} onChange={(e) => setType(e.target.value)} sx={{ minWidth: 200 }}>
          <MenuItem value="">All</MenuItem>
          {MOVEMENT_TYPES.map((t) => (
            <MenuItem key={t} value={t}>
              {t}
            </MenuItem>
          ))}
        </TextField>
        <TextField size="small" label="User" value={user} onChange={(e) => setUser(e.target.value)} />
        <Box sx={{ flexGrow: 1 }} />
        <ExportButton path={`/api/reports/stock-movement/export${qs(base)}`} />
      </Stack>
      <Table size="small" data-no-stack>
        <TableHead>
          <TableRow>
            <TableCell>Date</TableCell>
            <TableCell>SKU</TableCell>
            <TableCell>Product</TableCell>
            <TableCell>Variant</TableCell>
            <TableCell>Store</TableCell>
            <TableCell>Type</TableCell>
            <TableCell align="right">Change</TableCell>
            <TableCell>User</TableCell>
          </TableRow>
        </TableHead>
        <TableBody>
          {data?.items.map((r) => (
            <TableRow key={r.id}>
              <TableCell>{fmtDateTime(r.createdAt)}</TableCell>
              <TableCell>{r.sku}</TableCell>
              <TableCell>{r.productName}</TableCell>
              <TableCell>{variantLabel(r.colourName, r.sizeName)}</TableCell>
              <TableCell>{r.storeName}</TableCell>
              <TableCell>{r.type}</TableCell>
              <TableCell align="right" sx={{ color: r.quantityDelta < 0 ? "error.main" : "success.main" }}>
                {r.quantityDelta > 0 ? `+${r.quantityDelta}` : r.quantityDelta}
              </TableCell>
              <TableCell>{r.createdBy}</TableCell>
            </TableRow>
          ))}
          {data && data.items.length === 0 && <EmptyRow cols={8} />}
        </TableBody>
      </Table>
      <Pager total={data?.total ?? 0} paging={paging} />
    </>
  );
}

function SalesChart({ rows }: { rows: SalesRow[] }) {
  if (rows.length === 0) return null;
  const w = 800;
  const h = 180;
  const pad = 24;
  const max = Math.max(...rows.map((r) => r.total), 1);
  const bw = (w - pad * 2) / rows.length;
  return (
    <Box sx={{ mb: 3 }}>
      <Typography variant="subtitle2" sx={{ mb: 1 }}>
        Sales over time (GST inclusive)
      </Typography>
      <svg viewBox={`0 0 ${w} ${h + 20}`} width="100%" role="img" aria-label="Sales over time">
        <line x1={pad} y1={h} x2={w - pad} y2={h} stroke="#999" />
        {rows.map((r, i) => {
          const bh = (r.total / max) * (h - 10);
          return (
            <rect
              key={r.key}
              x={pad + i * bw + bw * 0.1}
              y={h - bh}
              width={bw * 0.8}
              height={bh}
              fill="#1976d2"
            >
              <title>{`${r.label}: ${money(r.total)}`}</title>
            </rect>
          );
        })}
        <text x={pad} y={h + 15} fontSize="11" fill="#666">
          {rows[0].label}
        </text>
        <text x={w - pad} y={h + 15} fontSize="11" fill="#666" textAnchor="end">
          {rows[rows.length - 1].label}
        </text>
        <text x={w - pad} y={12} fontSize="11" fill="#666" textAnchor="end">
          Peak {money(max)}
        </text>
      </svg>
    </Box>
  );
}

function SummaryCard({ title, value, sub }: { title: string; value: string; sub?: string }) {
  return (
    <Card variant="outlined" sx={{ minWidth: 160 }}>
      <CardContent>
        <Typography variant="caption" color="text.secondary">
          {title}
        </Typography>
        <Typography variant="h6">{value}</Typography>
        {sub && (
          <Typography variant="caption" color="text.secondary">
            {sub}
          </Typography>
        )}
      </CardContent>
    </Card>
  );
}

function SalesSummaryReport({ f }: { f: Filters }) {
  const [groupBy, setGroupBy] = useState("day");
  const base = { storeId: f.storeId, brandId: f.brandId, categoryId: f.categoryId, from: f.from, to: f.to, groupBy };
  const { data } = useQuery<SalesSummary>({
    queryKey: ["report", "sales", base],
    queryFn: () => api.get(`/api/reports/sales-summary${qs(base)}`),
  });
  return (
    <>
      <Stack direction="row" spacing={2} sx={{ mb: 2 }}>
        <TextField size="small" select label="Group by" value={groupBy} onChange={(e) => setGroupBy(e.target.value)} sx={{ minWidth: 160 }}>
          {GROUP_BY.map((g) => (
            <MenuItem key={g} value={g}>
              {g.charAt(0).toUpperCase() + g.slice(1)}
            </MenuItem>
          ))}
        </TextField>
        <Box sx={{ flexGrow: 1 }} />
        <ExportButton path={`/api/reports/sales-summary/export${qs(base)}`} />
      </Stack>
      {data && (
        <>
          <Stack direction="row" spacing={2} useFlexGap sx={{ mb: 3, flexWrap: "wrap" }}>
            <SummaryCard title="Total (incl. GST)" value={money(data.totals.total)} sub={`${data.totals.saleCount} sales`} />
            <SummaryCard title="GST" value={money(data.totals.gst)} />
            <SummaryCard title="Total (ex. GST)" value={money(data.totals.exGst)} />
            <SummaryCard title="Units sold" value={String(data.totals.units)} />
            <SummaryCard title="Voided" value={money(data.voided.total)} sub={`${data.voided.saleCount} sales (excluded above)`} />
          </Stack>
          <SalesChart rows={data.byDay} />
          <Table size="small" data-no-stack>
            <TableHead>
              <TableRow>
                <TableCell>{groupBy.charAt(0).toUpperCase() + groupBy.slice(1)}</TableCell>
                <TableCell align="right">Sales</TableCell>
                <TableCell align="right">Units</TableCell>
                <TableCell align="right">Total</TableCell>
                <TableCell align="right">GST</TableCell>
                <TableCell align="right">Ex. GST</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {data.rows.map((r) => (
                <TableRow key={r.key}>
                  <TableCell>{r.label}</TableCell>
                  <TableCell align="right">{r.saleCount}</TableCell>
                  <TableCell align="right">{r.units}</TableCell>
                  <TableCell align="right">{money(r.total)}</TableCell>
                  <TableCell align="right">{money(r.gst)}</TableCell>
                  <TableCell align="right">{money(r.exGst)}</TableCell>
                </TableRow>
              ))}
              {data.rows.length === 0 && <EmptyRow cols={6} />}
            </TableBody>
          </Table>
        </>
      )}
    </>
  );
}

function VarianceDialog({ id, onClose }: { id: string | null; onClose: () => void }) {
  const { data } = useQuery<VarianceDetail>({
    queryKey: ["report", "variance", id],
    queryFn: () => api.get(`/api/reports/stock-take-variance/${id}`),
    enabled: !!id,
  });
  return (
    <Dialog open={!!id} onClose={onClose} maxWidth="md" fullWidth>
      <DialogTitle>
        {data ? `${data.stockTake.referenceNumber} — ${data.stockTake.storeName}` : "Stock take variance"}
      </DialogTitle>
      <DialogContent>
        {data && (
          <Table size="small" data-no-stack>
            <TableHead>
              <TableRow>
                <TableCell>SKU</TableCell>
                <TableCell>Product</TableCell>
                <TableCell>Variant</TableCell>
                <TableCell align="right">Expected</TableCell>
                <TableCell align="right">Counted</TableCell>
                <TableCell align="right">Variance</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {data.items.map((i) => (
                <TableRow key={i.productVariantId}>
                  <TableCell>{i.sku}</TableCell>
                  <TableCell>{i.productName}</TableCell>
                  <TableCell>{variantLabel(i.colourName, i.sizeName)}</TableCell>
                  <TableCell align="right">{i.expectedQuantity}</TableCell>
                  <TableCell align="right">{i.countedQuantity}</TableCell>
                  <TableCell align="right" sx={{ color: i.variance < 0 ? "error.main" : "success.main" }}>
                    {i.variance > 0 ? `+${i.variance}` : i.variance}
                  </TableCell>
                </TableRow>
              ))}
              {data.items.length === 0 && <EmptyRow cols={6} />}
            </TableBody>
          </Table>
        )}
      </DialogContent>
      <DialogActions>
        {id && <ExportButton path={`/api/reports/stock-take-variance/${id}/export`} />}
        <Button onClick={onClose}>Close</Button>
      </DialogActions>
    </Dialog>
  );
}

function StockTakeVariance({ f }: { f: Filters }) {
  const [openId, setOpenId] = useState<string | null>(null);
  const base = { storeId: f.storeId, from: f.from, to: f.to };
  const paging = usePaging(JSON.stringify(base));
  const { data } = useQuery<PagedResult<VarianceRow>>({
    queryKey: ["report", "variance-list", base, paging.page, paging.pageSize],
    queryFn: () =>
      api.get(`/api/reports/stock-take-variance${qs({ ...base, page: paging.page + 1, pageSize: paging.pageSize })}`),
  });
  return (
    <>
      <Stack direction="row" spacing={2} sx={{ mb: 2, alignItems: "center" }}>
        <Typography variant="body2" color="text.secondary">
          Dates filter on completion date. Click a row for item-level variances.
        </Typography>
        <Box sx={{ flexGrow: 1 }} />
        <ExportButton path={`/api/reports/stock-take-variance/export${qs(base)}`} />
      </Stack>
      <Table size="small" data-no-stack>
        <TableHead>
          <TableRow>
            <TableCell>Reference</TableCell>
            <TableCell>Store</TableCell>
            <TableCell>Status</TableCell>
            <TableCell>Completed</TableCell>
            <TableCell>Approved</TableCell>
            <TableCell align="right">Counted items</TableCell>
            <TableCell align="right">Shrinkage</TableCell>
            <TableCell align="right">Overage</TableCell>
          </TableRow>
        </TableHead>
        <TableBody>
          {data?.items.map((r) => (
            <TableRow key={r.stockTakeId} hover sx={{ cursor: "pointer" }} onClick={() => setOpenId(r.stockTakeId)}>
              <TableCell>{r.referenceNumber}</TableCell>
              <TableCell>{r.storeName}</TableCell>
              <TableCell>
                <Chip
                  size="small"
                  color={r.status === "Approved" ? "success" : "warning"}
                  label={r.status === "Approved" ? "Approved" : "Pending approval"}
                />
              </TableCell>
              <TableCell>{fmtDate(r.completionDate)}</TableCell>
              <TableCell>{fmtDate(r.approvedDate)}</TableCell>
              <TableCell align="right">{r.countedItems}</TableCell>
              <TableCell align="right">{r.shrinkage}</TableCell>
              <TableCell align="right">{r.overage}</TableCell>
            </TableRow>
          ))}
          {data && data.items.length === 0 && <EmptyRow cols={8} />}
        </TableBody>
      </Table>
      <Pager total={data?.total ?? 0} paging={paging} />
      <VarianceDialog id={openId} onClose={() => setOpenId(null)} />
    </>
  );
}

export function ReportsPage() {
  const [report, setReport] = useState<ReportType>("stock-on-hand");
  const [filters, setFilters] = useState<Filters>({ storeId: "", brandId: "", categoryId: "", ...defaultRange() });
  const set = (p: Partial<Filters>) => setFilters((prev) => ({ ...prev, ...p }));

  const { data: stores } = useQuery<StoreDto[]>({
    queryKey: ["stores", "Active"],
    queryFn: () => api.get("/api/stores?status=Active"),
  });
  const { data: brands } = useQuery<BrandDto[]>({ queryKey: ["brands"], queryFn: () => api.get("/api/brands") });
  const { data: categories } = useQuery<CategoryDto[]>({
    queryKey: ["categories"],
    queryFn: () => api.get("/api/categories"),
  });

  const usesProductFilters = report === "stock-on-hand" || report === "sales-summary";
  const usesDates = report !== "stock-on-hand";
  const dateInvalid = usesDates && filters.from && filters.to && filters.from > filters.to;

  return (
    <Box>
      <Typography variant="h5" sx={{ mb: 2 }}>
        Reports
      </Typography>
      <Stack direction="row" spacing={2} useFlexGap sx={{ mb: 3, flexWrap: "wrap" }}>
        <TextField
          size="small"
          select
          label="Report"
          value={report}
          onChange={(e) => setReport(e.target.value as ReportType)}
          sx={{ minWidth: 220 }}
        >
          {REPORTS.map((r) => (
            <MenuItem key={r.value} value={r.value}>
              {r.label}
            </MenuItem>
          ))}
        </TextField>
        <TextField size="small" select label="Store" value={filters.storeId} onChange={(e) => set({ storeId: e.target.value })} sx={{ minWidth: 180 }}>
          <MenuItem value="">All stores</MenuItem>
          {stores?.map((s) => (
            <MenuItem key={s.id} value={s.id}>
              {s.name}
            </MenuItem>
          ))}
        </TextField>
        {usesProductFilters && (
          <>
            <TextField size="small" select label="Brand" value={filters.brandId} onChange={(e) => set({ brandId: e.target.value })} sx={{ minWidth: 160 }}>
              <MenuItem value="">All brands</MenuItem>
              {brands?.map((b) => (
                <MenuItem key={b.id} value={b.id}>
                  {b.name}
                </MenuItem>
              ))}
            </TextField>
            <TextField size="small" select label="Category" value={filters.categoryId} onChange={(e) => set({ categoryId: e.target.value })} sx={{ minWidth: 160 }}>
              <MenuItem value="">All categories</MenuItem>
              {categories?.map((c) => (
                <MenuItem key={c.id} value={c.id}>
                  {c.name}
                </MenuItem>
              ))}
            </TextField>
          </>
        )}
        {usesDates && <DateFields f={filters} set={set} />}
      </Stack>
      {dateInvalid ? (
        <Alert severity="warning">The From date must not be after the To date.</Alert>
      ) : (
        <>
          {report === "stock-on-hand" && <StockOnHand f={filters} />}
          {report === "stock-movement" && <StockMovement f={filters} />}
          {report === "sales-summary" && <SalesSummaryReport f={filters} />}
          {report === "stock-take-variance" && <StockTakeVariance f={filters} />}
        </>
      )}
    </Box>
  );
}
