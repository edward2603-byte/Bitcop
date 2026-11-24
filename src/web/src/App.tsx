import { useEffect, useMemo, useState } from 'react';
import { loadDashboard, type DashboardRow } from './api/dashboard';
import { TimeBar } from './components/TimeBar';
import './styles.css';

const menu = ['Учёт времени', 'Продуктивность', 'Программы', 'Структура дня', 'Хронометраж дня', 'Табели', 'Системные события'];
const periods = ['Сегодня, 00:00 — 23:59', 'Вчера, 00:00 — 23:59', 'Текущая неделя'];
const formatLastSeen = (value: string | null) => value ? new Intl.DateTimeFormat('ru-RU', { dateStyle: 'short', timeStyle: 'short' }).format(new Date(value)) : 'Нет данных';
const formatDuration = (seconds: number) => {
  if (seconds <= 0) return '—';
  const hours = Math.floor(seconds / 3600);
  const minutes = Math.floor((seconds % 3600) / 60);
  return `${hours} ч ${String(minutes).padStart(2, '0')} мин`;
};

export default function App() {
  const [rows, setRows] = useState<DashboardRow[]>([]);
  const [isDemo, setIsDemo] = useState(false);
  const [filter, setFilter] = useState('');
  const [activeReport, setActiveReport] = useState(menu[0]);
  const [periodIndex, setPeriodIndex] = useState(0);
  const [onlyOnline, setOnlyOnline] = useState(false);
  const [allTime, setAllTime] = useState(false);
  const [groupSelected, setGroupSelected] = useState(true);
  const [lastRefresh, setLastRefresh] = useState<Date | null>(null);

  const refresh = () => loadDashboard().then(({ rows, isDemo }) => {
    setRows(rows);
    setIsDemo(isDemo);
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
    return groupSelected && matchesSearch && matchesStatus;
  }), [rows, filter, onlyOnline, groupSelected]);

  const reportHint = activeReport === 'Учёт времени'
    ? 'Сводка по рабочему времени'
    : `Раздел «${activeReport}» открыт в демонстрационном режиме. Фильтры и выбор сотрудников применяются к данным ниже.`;
  const reportTitle = activeReport === 'Учёт времени' ? 'Учёт рабочего времени' : activeReport;

  return <div className="app-shell">
    <aside>
      <div className="brand"><span>●</span> WorkControl</div>
      <nav aria-label="Разделы отчётов">
        {menu.map(item => <button className={item === activeReport ? 'selected' : ''} aria-pressed={item === activeReport} onClick={() => setActiveReport(item)} key={item}>{item}</button>)}
      </nav>
      <section className="groups"><b>Группы</b>
        <label><input type="checkbox" checked={groupSelected} onChange={event => setGroupSelected(event.target.checked)} /> Проектный отдел</label>
        <label><input type="checkbox" checked={groupSelected} onChange={event => setGroupSelected(event.target.checked)} /> Все сотрудники</label>
      </section>
    </aside>
    <main>
      <header><div><p className="eyebrow">Отчёты / {activeReport}</p><h1>{reportTitle}</h1></div><div className="user">Руководитель <span>●</span></div></header>
      <section className="controls" aria-label="Фильтры отчёта">
        <button className="period" onClick={() => setPeriodIndex(index => (index + 1) % periods.length)} aria-label="Изменить период">▣ {periods[periodIndex]}</button>
        <button className={onlyOnline ? 'active-control' : ''} onClick={() => setOnlyOnline(value => !value)}>{onlyOnline ? 'На связи' : '⌄ Сотрудники'}</button>
        <button className={allTime ? 'active-control' : ''} onClick={() => setAllTime(value => !value)}>{allTime ? 'Всё время' : 'Рабочее время'}</button>
        <input value={filter} onChange={event => setFilter(event.target.value)} placeholder="Поиск сотрудника" aria-label="Поиск сотрудника" />
      </section>
      {isDemo && <p className="notice">Показаны вымышленные демонстрационные данные: сервер пока не настроен.</p>}
      <section className="summary"><div><span>Устройств</span><strong>{visibleRows.length}</strong></div><div><span>На связи</span><strong>{visibleRows.filter(row => row.status === 'На связи').length}</strong></div><div><span>Период</span><strong>{periodIndex === 2 ? 'Неделя' : 'День'}</strong></div></section>
      <section className="table-card">
        <div className="table-heading"><div><h2>{reportHint}</h2>{lastRefresh && <p className="updated">Обновлено: {lastRefresh.toLocaleTimeString('ru-RU')}</p>}</div><button onClick={() => void refresh()}>Обновить</button></div>
        <table><thead><tr><th>Сотрудник</th><th>Группа</th><th>Компьютер</th><th>{activeReport === 'Продуктивность' ? 'Показатель' : 'Наблюдаемо'}</th><th>Активное</th><th>Последняя связь</th><th>Активность</th><th>Статус</th></tr></thead>
          <tbody>{visibleRows.map(row => {
            const activePercent = row.observedSeconds > 0 ? Math.round((row.activeSeconds / row.observedSeconds) * 100) : 0;
            return <tr key={row.computer}><td><b>{row.employee}</b></td><td>{row.group}</td><td>{row.computer}</td><td><div className="worked"><span>{formatDuration(row.observedSeconds)}</span><TimeBar active={activePercent} neutral={Math.max(0, 100 - activePercent)} /></div></td><td>{row.observedSeconds > 0 ? `${formatDuration(row.activeSeconds)} (${activePercent}%)` : '—'}</td><td>{formatLastSeen(row.lastSeenAtUtc)}</td><td><span className={`activity ${row.activityState === 'Активен' ? 'active' : ''}`}>{row.activityState}</span></td><td><span className={`status ${row.status === 'На связи' ? 'online' : ''}`}>{row.status}</span></td></tr>;
          })}</tbody>
        </table>
        {visibleRows.length === 0 && <p className="empty">По выбранным фильтрам сотрудники не найдены.</p>}
      </section>
      <p className="footnote">Демонстрационный режим: интерфейс работает локально, без передачи данных и без наблюдения за сотрудниками.</p>
    </main>
  </div>;
}
