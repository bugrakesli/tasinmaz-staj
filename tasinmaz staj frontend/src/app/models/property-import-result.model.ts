import { PagedResult } from './property.model';
import { Property } from './property.model';

// Backend basarili importta (200): { message, importedCount, data }
// Basarisiz importta (400, PropertyImportResultDto): { success, importedCount, errors }
export interface PropertyImportResult {
  message?: string;
  success?: boolean;
  importedCount: number;
  errors?: string[];
  data?: PagedResult<Property>;
}
