import { cleanup, fireEvent, render, screen, within } from '@testing-library/react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import App from './App';

describe('сводка учёта времени', () => {
  beforeEach(() => { vi.stubGlobal('fetch', vi.fn().mockRejectedValue(new Error('offline'))); });
  afterEach(() => { cleanup(); vi.unstubAllGlobals(); });
  it('показывает заголовок и демонстрационное предупреждение', async () => {
    render(<App />);
    expect(screen.getByRole('heading', { name: 'Учёт рабочего времени' })).toBeInTheDocument();
    expect(await screen.findByText(/ДЕМОНСТРАЦИОННЫЕ ДАННЫЕ/)).toBeInTheDocument();
    expect(screen.getByText('ДЕМО')).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Табели' })).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Системные события' })).not.toBeInTheDocument();
    expect(screen.getByRole('button', { name: /Продуктивность/ })).toBeDisabled();
  });

  it('явно показывает данные API и независимые статусы связи и активности', async () => {
    vi.mocked(fetch).mockResolvedValue({
      ok: true,
      json: async () => [{
        employee: 'Тестовый Пользователь', group: 'Проектный отдел', computer: 'TEST-PC',
        lastSeenAtUtc: '2026-11-24T06:00:00Z', status: 'Нет связи', observedSeconds: 60,
        activeSeconds: 60, activityState: 'Активен'
      }]
    } as Response);
    render(<App />);
    expect(await screen.findByText(/Источник: локальный API/)).toBeInTheDocument();
    const row = screen.getByText('Тестовый Пользователь').closest('tr');
    expect(row).not.toBeNull();
    expect(within(row!).getByText('Нет связи')).toBeInTheDocument();
    expect(within(row!).getByText('Активен')).toBeInTheDocument();
    expect(screen.queryByText('ДЕМО')).not.toBeInTheDocument();
  });

  it('фильтрует данные API по поиску и подключению', async () => {
    vi.mocked(fetch).mockResolvedValue({
      ok: true,
      json: async () => [
        { employee: 'Анна', group: 'Проектный отдел', computer: 'PC-1', lastSeenAtUtc: null, status: 'На связи', observedSeconds: 0, activeSeconds: 0, activityState: 'Активен' },
        { employee: 'Илья', group: 'Проектный отдел', computer: 'PC-2', lastSeenAtUtc: null, status: 'Нет связи', observedSeconds: 0, activeSeconds: 0, activityState: 'Неактивен' }
      ]
    } as Response);
    render(<App />);
    await screen.findByText('Анна');
    fireEvent.click(screen.getByRole('button', { name: 'Все компьютеры' }));
    expect(screen.getByText('Анна')).toBeInTheDocument();
    expect(screen.queryByText('Илья')).not.toBeInTheDocument();
    fireEvent.change(screen.getByRole('textbox', { name: 'Поиск сотрудника' }), { target: { value: 'нет совпадений' } });
    expect(screen.getByText(/API доступен, но тестовые события ещё не получены/)).toBeInTheDocument();
  });
});
