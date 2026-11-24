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
  { employee: 'Анна Демонстрационная', group: 'Проектный отдел', computer: 'DESIGN-01', lastSeenAtUtc: new Date().toISOString(), status: 'На связи', observedSeconds: 27_600, activeSeconds: 24_300, activityState: 'Активен' },
  { employee: 'Илья Тестовый', group: 'Проектный отдел', computer: 'PROJECT-02', lastSeenAtUtc: null, status: 'Нет данных', observedSeconds: 0, activeSeconds: 0, activityState: 'Нет данных' }
];

export async function loadDashboard(): Promise<{ rows: DashboardRow[]; isDemo: boolean }> {
  try {
    const response = await fetch('/api/dashboard');
    if (!response.ok) throw new Error('API unavailable');
    return { rows: await response.json() as DashboardRow[], isDemo: false };
  } catch {
    return { rows: demoRows, isDemo: true };
  }
}
