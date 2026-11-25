export class ApiError extends Error {
  constructor(public status: number, message: string) { super(message); }
}

export type CurrentUser = { id: string; email: string; roles: string[] };
export type PendingDevice = { deviceId: string; computerName: string; status: string; createdAtUtc: string };
export type EmployeeOption = { id: string; displayName: string; group: string };

async function request(path: string, init?: RequestInit) {
  const response = await fetch(path, { credentials: 'include', ...init });
  if (!response.ok) throw new ApiError(response.status, response.status === 401 ? 'Требуется вход' : response.status === 403 ? 'Недостаточно прав' : `Ошибка API: ${response.status}`);
  return response;
}

async function csrfToken() {
  const response = await request('/api/auth/csrf');
  return (await response.json() as { token: string }).token;
}

export async function postWithCsrf(path: string, body?: unknown) {
  const token = await csrfToken();
  return request(path, {
    method: 'POST', headers: { 'Content-Type': 'application/json', 'X-CSRF-TOKEN': token },
    body: body === undefined ? undefined : JSON.stringify(body)
  });
}

export async function login(email: string, password: string) { await postWithCsrf('/api/auth/login', { email, password }); }
export async function logout() { await postWithCsrf('/api/auth/logout'); }
export async function getCurrentUser(): Promise<CurrentUser> { return request('/api/auth/me').then(r => r.json()); }
export async function createEnrollment(): Promise<{ code: string; expiresAtUtc: string }> { return postWithCsrf('/api/admin/enrollments').then(r => r.json()); }
export async function getPendingDevices(): Promise<PendingDevice[]> { return request('/api/admin/devices/pending').then(r => r.json()); }
export async function getEmployees(): Promise<EmployeeOption[]> { return request('/api/admin/employees').then(r => r.json()); }
export async function approveDevice(deviceId: string, employeeId: string) { await postWithCsrf(`/api/admin/devices/${deviceId}/approve`, { employeeId }); }
export async function revokeDevice(deviceId: string) { await postWithCsrf(`/api/admin/devices/${deviceId}/revoke`); }
