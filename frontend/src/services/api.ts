// In development, Vite proxies /v1.0.1/* -> http://localhost:5266
// In production set VITE_API_BASE_URL env var

const BASE_URL = "http://192.168.17.129:30081/v1.0.1";
function getToken(): string | null {
  return localStorage.getItem("token");
}

async function request<T>(path: string, options: RequestInit = {}): Promise<T> {
  const token = getToken();
  const headers: Record<string, string> = {
    "Content-Type": "application/json",
    ...(options.headers as Record<string, string>),
  };
  if (token) headers["Authorization"] = `Bearer ${token}`;

  const res = await fetch(`${BASE_URL}${path}`, { ...options, headers });

  if (!res.ok) {
    // Try to parse a JSON error body, else use status text
    let msg = `HTTP ${res.status}`;
    try {
      const body = await res.json();
      msg = body?.error ?? body?.message ?? msg;
    } catch {
      try { msg = await res.text() || msg; } catch { /* ignore */ }
    }
    throw new Error(msg);
  }

  const contentType = res.headers.get("content-type");
  if (!contentType?.includes("application/json")) return null as T;
  return res.json();
}

// ─── Auth ─────────────────────────────────────────────────────────────────

export interface LoginResponse {
  token: string;
  role: string;   // "User" | "Admin" | "SuperAdmin"
}

export async function login(email: string, password: string): Promise<LoginResponse> {
  return request<LoginResponse>("/auth/login", {
    method: "POST",
    body: JSON.stringify({ email, password }),
  });
}

export async function createUser(email: string, password: string, roleId: number): Promise<void> {
  return request<void>("/auth/create-user", {
    method: "POST",
    body: JSON.stringify({ email, password, roleId }),
  });
}

// ─── Applications ─────────────────────────────────────────────────────────

export interface ApplicationSummary {
  id: number;
  title: string;
  description: string;
  ownerEmail: string;
}

export interface CreateApplicationResponse {
  applicationId: number;
  appKey: string;
  appSecret: string;
}

export interface EnvironmentDto {
  id: number;           // ApiKeys.Id — used for regenerate/revoke
  environment: string;
  apiKey: string;
  apiSecret: string;
  isEnabled: boolean;
}

export interface MicroserviceDto {
  id: number;
  name: string;
  isEnabled: boolean;
}

export interface ApplicationDetails {
  applicationId: number;
  title: string;
  environments: EnvironmentDto[];
  microservices: MicroserviceDto[];
}

export async function getApplications(): Promise<ApplicationSummary[]> {
  return request<ApplicationSummary[]>("/Application");
}

export async function createApplication(
  title: string,
  description: string
): Promise<CreateApplicationResponse> {
  return request<CreateApplicationResponse>("/Application", {
    method: "POST",
    body: JSON.stringify({ title, description }),
  });
}

export async function getApplicationDetails(appId: number): Promise<ApplicationDetails> {
  return request<ApplicationDetails>(`/Application/${appId}/details`);
}

export interface EnvironmentUpdateDto {
  name: string;
  isEnabled: boolean;
}

export interface MicroserviceUpdateDto {
  id: number;
  isEnabled: boolean;
}

export async function updateApplicationSettings(
  appId: number,
  environments: EnvironmentUpdateDto[],
  microservices: MicroserviceUpdateDto[]
): Promise<void> {
  return request<void>(`/Application/${appId}/settings`, {
    method: "PUT",
    body: JSON.stringify({ environments, microservices }),
  });
}

// keyId is the ApiKeys.Id row returned in EnvironmentDto.id
export async function regenerateSecret(appId: number, keyId: number): Promise<void> {
  return request<void>(`/Application/${appId}/keys/${keyId}/regenerate`, { method: "POST" });
}

export async function revokeKey(appId: number, keyId: number): Promise<void> {
  return request<void>(`/Application/${appId}/keys/${keyId}/revoke`, { method: "PATCH" });
}

export async function getMicroservices(): Promise<MicroserviceDto[]> {
  return request<MicroserviceDto[]>("/Application/microservices");
}
export interface UserSummary {
  id: number;
  email: string;
  role: string;
  isActive: boolean;
  createdAt: string;
}

export async function getUsers(): Promise<UserSummary[]> {
  return request<UserSummary[]>("/auth/users");
}
