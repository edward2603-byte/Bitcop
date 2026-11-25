import { FormEvent, useEffect, useMemo, useState } from 'react';
import { loadDashboard, type DashboardRow } from './api/dashboard';
import { ApiError, approveDevice, createEnrollment, getCurrentUser, getEmployees, getPendingDevices, login, logout, revokeDevice, type CurrentUser, type EmployeeOption, type PendingDevice } from './api/auth';
import { TimeBar } from './components/TimeBar';
import './styles.css';

const menu = [
  { label: 'Учёт времени', enabled: true }, { label: 'Продуктивность', enabled: false },
  { label: 'Программы', enabled: false }, { label: 'Структура дня', enabled: false },
  { label: 'Хронометраж дня', enabled: false }
];
const formatLastSeen = (value: string | null) => value ? new Intl.DateTimeFormat('ru-RU', { dateStyle: 'short', timeStyle: 'short' }).format(new Date(value)) : 'Нет данных';
const formatDuration = (seconds: number) => seconds <= 0 ? '—' : `${Math.floor(seconds / 3600)} ч ${String(Math.floor((seconds % 3600) / 60)).padStart(2, '0')} мин`;

function Login({ onSuccess }: { onSuccess: (user: CurrentUser) => void }) {
  const [email, setEmail] = useState(''); const [password, setPassword] = useState(''); const [error, setError] = useState(''); const [busy, setBusy] = useState(false);
  const submit = async (event: FormEvent) => {
    event.preventDefault(); setBusy(true); setError('');
    try { await login(email, password); onSuccess(await getCurrentUser()); }
    catch (e) { setError(e instanceof ApiError && e.status === 429 ? 'Слишком много попыток. Повторите позже.' : 'Неверные данные входа или учётная запись заблокирована.'); }
    finally { setBusy(false); }
  };
  return <main className="login-page"><form className="login-card" onSubmit={submit}><div className="brand"><span>●</span> WorkControl</div><h1>Вход в панель</h1><p>Общедоступной регистрации нет. Учётную запись создаёт администратор.</p><label>Электронная почта<input type="email" required value={email} onChange={e => setEmail(e.target.value)} /></label><label>Пароль<input type="password" required value={password} onChange={e => setPassword(e.target.value)} /></label>{error && <p className="error" role="alert">{error}</p>}<button disabled={busy}>{busy ? 'Проверка…' : 'Войти'}</button></form></main>;
}

function AdminPanel() {
  const [pending, setPending] = useState<PendingDevice[]>([]); const [employees, setEmployees] = useState<EmployeeOption[]>([]);
  const [code, setCode] = useState(''); const [expires, setExpires] = useState(''); const [selected, setSelected] = useState<Record<string, string>>({}); const [message, setMessage] = useState('');
  const refresh = async () => { const [devices, people] = await Promise.all([getPendingDevices(), getEmployees()]); setPending(devices); setEmployees(people); };
  useEffect(() => { void refresh().catch(() => setMessage('Не удалось загрузить заявки администратора.')); }, []);
  const makeCode = async () => { const result = await createEnrollment(); setCode(result.code); setExpires(result.expiresAtUtc); };
  const approve = async (deviceId: string) => { const employeeId = selected[deviceId]; if (!employeeId) { setMessage('Выберите сотрудника.'); return; } await approveDevice(deviceId, employeeId); setMessage('Устройство одобрено.'); await refresh(); };
  const revoke = async (deviceId: string) => { await revokeDevice(deviceId); setMessage('Устройство отозвано.'); await refresh(); };
  return <section className="admin-card"><div className="table-heading"><div><h2>Подключение агентов</h2><p className="updated">Код одноразовый; секрет после выдачи хранится только агентом в DPAPI.</p></div><button onClick={() => void makeCode()}>Создать код</button></div>{code && <div className="enrollment-code"><b>Код:</b> <code>{code}</code><span>до {new Date(expires).toLocaleTimeString('ru-RU')}</span></div>}{message && <p className="admin-message">{message}</p>}<div className="pending-list">{pending.map(device => <div key={device.deviceId}><b>{device.computerName}</b><span>ожидает одобрения</span><select aria-label={`Сотрудник для ${device.computerName}`} value={selected[device.deviceId] ?? ''} onChange={e => setSelected({ ...selected, [device.deviceId]: e.target.value })}><option value="">Выберите сотрудника</option>{employees.map(person => <option key={person.id} value={person.id}>{person.displayName} — {person.group}</option>)}</select><button onClick={() => void approve(device.deviceId)}>Одобрить</button><button className="danger" onClick={() => void revoke(device.deviceId)}>Отозвать</button></div>)}{pending.length === 0 && <p className="empty">Ожидающих устройств нет.</p>}</div></section>;
}

export default function App() {
  const [user, setUser] = useState<CurrentUser | null>(null); const [checking, setChecking] = useState(true); const [rows, setRows] = useState<DashboardRow[]>([]);
  const [state, setState] = useState<'loading' | 'api' | 'error'>('loading'); const [error, setError] = useState(''); const [filter, setFilter] = useState(''); const [onlyOnline, setOnlyOnline] = useState(false); const [lastRefresh, setLastRefresh] = useState<Date | null>(null);
  useEffect(() => { void getCurrentUser().then(setUser).catch(() => setUser(null)).finally(() => setChecking(false)); }, []);
  const refresh = async () => { setState('loading'); try { setRows(await loadDashboard()); setState('api'); setLastRefresh(new Date()); } catch (e) { setRows([]); setError(e instanceof Error ? e.message : 'Ошибка API'); setState('error'); if (e instanceof ApiError && e.status === 401) setUser(null); } };
  useEffect(() => { if (!user) return; void refresh(); const timer = window.setInterval(() => void refresh(), 10_000); return () => window.clearInterval(timer); }, [user]);
  const visibleRows = useMemo(() => rows.filter(row => `${row.employee} ${row.group} ${row.computer}`.toLowerCase().includes(filter.toLowerCase()) && (!onlyOnline || row.status === 'На связи')), [rows, filter, onlyOnline]);
  if (checking) return <main className="login-page">Проверка сеанса…</main>;
  if (!user) return <Login onSuccess={setUser} />;
  return <div className="app-shell"><aside><div className="brand"><span>●</span> WorkControl</div><nav aria-label="Разделы отчётов">{menu.map(item => <button className={item.enabled ? 'selected' : ''} disabled={!item.enabled} key={item.label}>{item.label}{!item.enabled && <small>не реализовано</small>}</button>)}</nav><section className="groups"><b>Доступ</b><span>{user.roles.join(', ')}</span></section></aside><main><header><div><p className="eyebrow">Отчёты / Учёт времени</p><h1>Учёт рабочего времени</h1></div><div className="user">{user.email} <button onClick={() => void logout().then(() => setUser(null))}>Выйти</button></div></header>
    {user.roles.includes('ADMIN') && <AdminPanel />}
    <section className="controls" aria-label="Фильтры отчёта"><button className="period" disabled>▣ Сегодня</button><button className={onlyOnline ? 'active-control' : ''} onClick={() => setOnlyOnline(v => !v)}>{onlyOnline ? 'Только на связи' : 'Все компьютеры'}</button><button disabled>Рабочее время — не реализовано</button><input value={filter} onChange={e => setFilter(e.target.value)} placeholder="Поиск сотрудника" aria-label="Поиск сотрудника" /></section>
    {state === 'api' && <p className="source-api"><b>Источник: защищённый API.</b> Показаны данные, разрешённые текущей роли.</p>}{state === 'loading' && <p className="source-loading">Загрузка данных API…</p>}{state === 'error' && <p className="notice" role="alert"><b>Данные не загружены.</b> {error}</p>}
    <section className="summary"><div><span>Компьютеров</span><strong>{visibleRows.length}</strong></div><div><span>На связи</span><strong>{visibleRows.filter(row => row.status === 'На связи').length}</strong></div><div><span>Период</span><strong>Сегодня</strong></div></section>
    <section className="table-card"><div className="table-heading"><div><h2>Техническая сводка активности</h2>{lastRefresh && <p className="updated">Обновлено: {lastRefresh.toLocaleTimeString('ru-RU')}</p>}</div><button onClick={() => void refresh()}>Обновить</button></div><table><thead><tr><th>Сотрудник</th><th>Группа</th><th>Компьютер</th><th>Наблюдаемо</th><th>Активное</th><th>Последняя связь</th><th>Активность</th><th>Статус</th></tr></thead><tbody>{visibleRows.map(row => { const percent = row.observedSeconds > 0 ? Math.round(row.activeSeconds / row.observedSeconds * 100) : 0; return <tr key={row.computer}><td><b>{row.employee}</b></td><td>{row.group}</td><td>{row.computer}</td><td><div className="worked"><span>{formatDuration(row.observedSeconds)}</span><TimeBar active={percent} neutral={Math.max(0, 100 - percent)} /></div></td><td>{row.observedSeconds > 0 ? `${formatDuration(row.activeSeconds)} (${percent}%)` : '—'}</td><td>{formatLastSeen(row.lastSeenAtUtc)}</td><td><span className={`activity ${row.activityState === 'Активен' ? 'active' : ''}`}>{row.activityState}</span></td><td><span className={`status ${row.status === 'На связи' ? 'online' : ''}`}>{row.status}</span></td></tr>; })}</tbody></table>{visibleRows.length === 0 && state === 'api' && <p className="empty">Доступных данных пока нет.</p>}</section><p className="footnote">Техническая сводка не является утверждённым табелем и не оценивает качество работы сотрудника.</p></main></div>;
}
