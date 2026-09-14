import Box from '@mui/material/Box';
import {
  DataGrid,
  type DataGridProps,
  type GridColDef,
  type GridRowsProp,
} from '@mui/x-data-grid';

export interface DataTableProps extends Partial<DataGridProps> {
  rows: GridRowsProp;
  columns: GridColDef[];
  loading?: boolean;
  height?: number | string;
}

/**
 * Thin wrapper around MUI X DataGrid with sensible defaults for the app.
 * Supports both client-side and server-side (paginationMode="server") usage.
 */
export default function DataTable({
  rows,
  columns,
  loading,
  height = 560,
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
