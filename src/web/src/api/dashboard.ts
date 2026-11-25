import { ApiError } from './auth';

export type DashboardRow = {
  employee: string;
  group: string;
  computer: string;
  lastSeenAtUtc: string | null;
  status: string;
  observedSeconds: number;
  activeSeconds: number;
  activityState: string;
};

export async function loadDashboard(): Promise<DashboardRow[]> {
  let response: Response;
  try { response = await fetch('/api/dashboard', { credentials: 'include' }); }
  catch { throw new ApiError(0, 'API недоступен. Демонстрационные данные не подставляются.'); }
  if (!response.ok) throw new ApiError(response.status, response.status === 401 ? 'Требуется вход' : response.status === 403 ? 'Недостаточно прав' : `Ошибка API: ${response.status}`);
  return response.json();
}
