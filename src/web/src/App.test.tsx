import { cleanup, fireEvent, render, screen, within } from '@testing-library/react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import App from './App';

describe('сводка учёта времени', () => {
  beforeEach(() => { vi.stubGlobal('fetch', vi.fn().mockRejectedValue(new Error('offline'))); });
  afterEach(() => { cleanup(); vi.unstubAllGlobals(); });
  it('показывает заголовок и демонстрационное предупреждение', async () => {
    render(<App />);
    expect(screen.getByRole('heading', { name: 'Учёт рабочего времени' })).toBeInTheDocument();
    expect(await screen.findByText(/Показаны вымышленные демонстрационные данные/)).toBeInTheDocument();
  });

  it('переключает раздел и фильтрует сотрудников', async () => {
    render(<App />);
    await screen.findByText(/Показаны вымышленные демонстрационные данные/);
    fireEvent.click(within(screen.getByRole('navigation', { name: 'Разделы отчётов' })).getByRole('button', { name: 'Продуктивность' }));
    expect(screen.getByRole('heading', { name: 'Продуктивность' })).toBeInTheDocument();
    fireEvent.change(screen.getByRole('textbox', { name: 'Поиск сотрудника' }), { target: { value: 'Илья' } });
    expect(screen.getByText('Илья Тестовый')).toBeInTheDocument();
    expect(screen.queryByText('Анна Демонстрационная')).not.toBeInTheDocument();
  });
});
