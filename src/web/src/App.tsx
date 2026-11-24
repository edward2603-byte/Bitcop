import { useEffect, useMemo, useState } from 'react';
import { loadDashboard, type DashboardResult, type DashboardRow } from './api/dashboard';
import { TimeBar } from './components/TimeBar';
import './styles.css';

const menu = [
  { label: 'Учёт времени', enabled: true },
  { label: 'Продуктивность', enabled: false },
  { label: 'Программы', enabled: false },
  { label: 'Структура дня', enabled: false },
  { label: 'Хронометраж дня', enabled: false }
];
const formatLastSeen = (value: string | null) => value ? new Intl.DateTimeFormat('ru-RU', { dateStyle: 'short', timeStyle: 'short' }).format(new Date(value)) : 'Нет данных';
const formatDuration = (seconds: number) => {
  if (seconds <= 0) return '—';
  const hours = Math.floor(seconds / 3600);
  const minutes = Math.floor((seconds % 3600) / 60);
  return `${hours} ч ${String(minutes).padStart(2, '0')} мин`;
};

export default function App() {
  const [rows, setRows] = useState<DashboardRow[]>([]);
  const [dataSource, setDataSource] = useState<DashboardResult['source'] | 'loading'>('loading');
  const [filter, setFilter] = useState('');
  const [onlyOnline, setOnlyOnline] = useState(false);
  const [lastRefresh, setLastRefresh] = useState<Date | null>(null);

  const refresh = () => loadDashboard().then(({ rows, source }) => {
    setRows(rows);
    setDataSource(source);
    setLastRefresh(new Date());
  });

  useEffect(() => {
    void refresh();
    const timer = window.setInterval(() => void refresh(), 10_000);
    return () => window.clearInterval(timer);
  }, []);

  const visibleRows = useMemo(() => rows.filter(row => {
    const matchesSearch = `${row.employee} ${row.group} ${row.computer}`.toLowerCase().includes(filter.toLowerCase());
    const matchesStatus = !onlyOnline || row.status === 'На связи';
    return matchesSearch && matchesStatus;
  }), [rows, filter, onlyOnline]);

  return <div className="app-shell">
    <aside>
      <div className="brand"><span>●</span> WorkControl</div>
      <nav aria-label="Разделы отчётов">
        {menu.map(item => <button className={item.enabled ? 'selected' : ''} aria-pressed={item.enabled} disabled={!item.enabled} key={item.label}>{item.label}{!item.enabled && <small>не реализовано</small>}</button>)}
      </nav>
      <section className="groups"><b>Группы</b><span>Фильтр групп пока не реализован</span></section>
    </aside>
    <main>
      <header><div><p className="eyebrow">Отчёты / Учёт времени</p><h1>Учёт рабочего времени</h1></div><div className="user">Локальный режим <span>●</span></div></header>
      <section className="controls" aria-label="Фильтры отчёта">
        <button className="period" disabled title="Выбор периода пока не реализован">▣ Сегодня</button>
        <button className={onlyOnline ? 'active-control' : ''} onClick={() => setOnlyOnline(value => !value)}>{onlyOnline ? 'Только на связи' : 'Все компьютеры'}</button>
        <button disabled title="Фильтр рабочего времени пока не реализован">Рабочее время — не реализовано</button>
        <input value={filter} onChange={event => setFilter(event.target.value)} placeholder="Поиск сотрудника" aria-label="Поиск сотрудника" />
      </section>
      {dataSource === 'demo' && <p className="notice"><b>ДЕМОНСТРАЦИОННЫЕ ДАННЫЕ.</b> Локальный API недоступен; строка ниже вымышлена и не является отчётом.</p>}
      {dataSource === 'api' && <p className="source-api"><b>Источник: локальный API.</b> Показаны данные текущего тестового запуска.</p>}
      {dataSource === 'loading' && <p className="source-loading">Подключение к локальному API…</p>}
      <section className="summary"><div><span>Компьютеров</span><strong>{visibleRows.length}</strong></div><div><span>На связи</span><strong>{visibleRows.filter(row => row.status === 'На связи').length}</strong></div><div><span>Период</span><strong>Сегодня</strong></div></section>
      <section className="table-card">
        <div className="table-heading"><div><h2>Техническая сводка активности</h2>{lastRefresh && <p className="updated">Обновлено: {lastRefresh.toLocaleTimeString('ru-RU')}</p>}</div><button onClick={() => void refresh()}>Обновить</button></div>
        <table><thead><tr><th>Сотрудник</th><th>Группа</th><th>Компьютер</th><th>Наблюдаемо</th><th>Активное</th><th>Последняя связь</th><th>Активность</th><th>Статус</th></tr></thead>
          <tbody>{visibleRows.map(row => {
            const activePercent = row.observedSeconds > 0 ? Math.round((row.activeSeconds / row.observedSeconds) * 100) : 0;
            return <tr className={dataSource === 'demo' ? 'demo-row' : ''} key={row.computer}><td><b>{row.employee}</b>{dataSource === 'demo' && <span className="demo-badge">ДЕМО</span>}</td><td>{row.group}</td><td>{row.computer}</td><td><div className="worked"><span>{formatDuration(row.observedSeconds)}</span><TimeBar active={activePercent} neutral={Math.max(0, 100 - activePercent)} /></div></td><td>{row.observedSeconds > 0 ? `${formatDuration(row.activeSeconds)} (${activePercent}%)` : '—'}</td><td>{formatLastSeen(row.lastSeenAtUtc)}</td><td><span className={`activity ${row.activityState === 'Активен' ? 'active' : ''}`}>{row.activityState}</span></td><td><span className={`status ${row.status === 'На связи' ? 'online' : ''}`}>{row.status}</span></td></tr>;
          })}</tbody>
        </table>
        {visibleRows.length === 0 && <p className="empty">{dataSource === 'api' ? 'API доступен, но тестовые события ещё не получены или не соответствуют фильтру.' : 'По выбранным фильтрам строки не найдены.'}</p>}
      </section>
      <p className="footnote">Техническая сводка не является утверждённым табелем и не оценивает качество работы сотрудника.</p>
    </main>
  </div>;
}
