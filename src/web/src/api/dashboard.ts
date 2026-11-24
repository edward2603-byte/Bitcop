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

const demoRows: DashboardRow[] = [
  { employee: 'Анна Демонстрационная', group: 'Проектный отдел', computer: 'DEMO-PC', lastSeenAtUtc: null, status: 'Демо', observedSeconds: 3_600, activeSeconds: 2_400, activityState: 'Демо' }
];

export type DashboardResult = {
  rows: DashboardRow[];
  source: 'api' | 'demo';
};

export async function loadDashboard(): Promise<DashboardResult> {
  try {
    const response = await fetch('/api/dashboard');
    if (!response.ok) throw new Error('API недоступен');
    return { rows: await response.json() as DashboardRow[], source: 'api' };
  } catch {
    return { rows: demoRows, source: 'demo' };
  }
}
