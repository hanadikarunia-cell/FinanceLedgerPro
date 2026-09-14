import Box from '@mui/material/Box';
import {
  DataGrid,
  GridToolbarContainer,
  GridToolbarColumnsButton,
  GridToolbarFilterButton,
  GridToolbarDensitySelector,
  GridToolbarExport,
  type DataGridProps,
  type GridColDef,
  type GridRowsProp,
} from '@mui/x-data-grid';

export interface DataTableProps extends Partial<DataGridProps> {
  rows: GridRowsProp;
  columns: GridColDef[];
  loading?: boolean;
  height?: number | string;
  /** Show the toolbar (Columns/Filters/Density/Export) so viewers can add fields
   * that aren't shown by default instead of waiting for a code change. On by default. */
  showToolbar?: boolean;
}

function Toolbar() {
  return (
    <GridToolbarContainer sx={{ p: 1 }}>
      <GridToolbarColumnsButton />
      <GridToolbarFilterButton />
      <GridToolbarDensitySelector />
      <GridToolbarExport />
    </GridToolbarContainer>
  );
}

/**
 * Thin wrapper around MUI X DataGrid with sensible defaults for the app.
 * Supports both client-side and server-side (paginationMode="server") usage.
 * Includes a toolbar with a "Columns" picker so any hidden field can be shown
 * on demand without a code change.
 */
export default function DataTable({
  rows,
  columns,
  loading,
  height = 560,
  showToolbar = true,
  ...rest
}: DataTableProps) {
  return (
    <Box sx={{ width: '100%', height }}>
      <DataGrid
        rows={rows}
        columns={columns}
        loading={loading}
        disableRowSelectionOnClick
        pageSizeOptions={[10, 25, 50, 100]}
        density="standard"
        slots={showToolbar ? { toolbar: Toolbar } : undefined}
        sx={{
          border: 0,
          '& .MuiDataGrid-columnHeaders': { fontWeight: 700 },
          '& .MuiDataGrid-cell:focus, & .MuiDataGrid-cell:focus-within': {
            outline: 'none',
          },
        }}
        {...rest}
      />
    </Box>
  );
}
