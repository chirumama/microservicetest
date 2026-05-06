import { request } from "./api"; // reuse your existing request helper

export interface RouteSyncResult {
  success:     boolean;
  totalRoutes: number;
  synced:      number;
  skipped:     number;
  added:       string[];
  updated:     string[];
  skipReasons: string[];
  syncedAt:    string;
  error?:      string;
}

export async function triggerRouteSync(): Promise<RouteSyncResult> {
  return request<RouteSyncResult>("/admin/sync-routes", { method: "POST" });
}