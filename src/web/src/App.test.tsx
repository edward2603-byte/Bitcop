import { cleanup, render, screen, within } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import App from './App';

const response = (status: number, data?: unknown) => ({ ok: status >= 200 && status < 300, status, json: async () => data }) as Response;

describe('защищённая панель WorkControl', () => {
  afterEach(() => { cleanup(); vi.unstubAllGlobals(); });

  it('показывает вход при отсутствии cookie', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(response(401)));
    render(<App />);
    expect(await screen.findByRole('heading', { name: 'Вход в панель' })).toBeInTheDocument();
    expect(screen.getByText(/Общедоступной регистрации нет/)).toBeInTheDocument();
  });

  it('показывает только реальные данные защищённого API', async () => {
    vi.stubGlobal('fetch', vi.fn(async (input: RequestInfo | URL) => {
      const path = String(input);
      if (path.endsWith('/api/auth/me')) return response(200, { id: '1', email: 'user@test.local', roles: ['MAIN_USER'] });
      if (path.endsWith('/api/dashboard')) return response(200, [{ employee: 'Тестовый Пользователь', group: 'Проектный отдел', computer: 'TEST-PC', lastSeenAtUtc: '2026-11-24T06:00:00Z', status: 'Нет связи', observedSeconds: 60, activeSeconds: 60, activityState: 'Активен' }]);
      return response(404);
    }));
    render(<App />);
    expect(await screen.findByText(/Источник: защищённый API/)).toBeInTheDocument();
    const row = screen.getByText('Тестовый Пользователь').closest('tr');
    expect(row).not.toBeNull(); expect(within(row!).getByText('Нет связи')).toBeInTheDocument(); expect(within(row!).getByText('Активен')).toBeInTheDocument();
    expect(screen.queryByText(/ДЕМОНСТРАЦИОННЫЕ ДАННЫЕ/)).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Табели' })).not.toBeInTheDocument();
  });

  it('не подставляет демо-строку при отказе API', async () => {
    vi.stubGlobal('fetch', vi.fn(async (input: RequestInfo | URL) => String(input).endsWith('/api/auth/me')
      ? response(200, { id: '1', email: 'user@test.local', roles: ['MAIN_USER'] })
      : Promise.reject(new Error('offline'))));
    render(<App />);
    expect(await screen.findByText(/Данные не загружены/)).toBeInTheDocument();
    expect(screen.getByText(/Демонстрационные данные не подставляются/)).toBeInTheDocument();
    expect(screen.queryByText('DEMO-PC')).not.toBeInTheDocument();
  });
});
