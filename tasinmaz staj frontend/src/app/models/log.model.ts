export interface Log {
  id: number;
  userId: number;
  status: string;
  operationType: string;
  description: string;
  timestamp: string;
  userIp: string;
}

export interface LogFilter {
  id?: number;
  userId?: number;
  status?: string;
  operationType?: string;
  description?: string;
  userIp?: string;
  startDate?: string;
  endDate?: string;
  pageNumber: number;
  pageSize: number;
}

export interface LogPagedResult {
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  data: Log[];
}
