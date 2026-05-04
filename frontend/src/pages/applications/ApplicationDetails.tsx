import { useParams, useNavigate } from "react-router-dom";
import { useState, useEffect, useCallback } from "react";
import { FiCopy, FiEye, FiEyeOff, FiRefreshCw, FiTrash2, FiChevronDown, FiChevronRight } from "react-icons/fi";
import TabBar from "../../components/common/TabBar";
import {
  getApplicationDetails, updateApplicationSettings, regenerateSecret, revokeKey,
  getMicroserviceRoutes, updateRouteAccess,
  type ApplicationDetails as AppDetailsType, type MicroserviceDto,
  type MicroserviceWithRoutes, type RouteDto,
} from "../../services/api";
import { useAuth } from "../../context/AuthContext";

const ENVIRONMENTS = ["Development", "Pre-Production", "Production"];
const METHOD_COLORS: Record<string, { bg: string; color: string }> = {
  GET:    { bg: "#e0f2fe", color: "#0369a1" },
  POST:   { bg: "#dcfce7", color: "#166534" },
  PUT:    { bg: "#fef9c3", color: "#854d0e" },
  PATCH:  { bg: "#ede9fe", color: "#6d28d9" },
  DELETE: { bg: "#fee2e2", color: "#dc2626" },
};

export default function ApplicationDetails() {
  const { id }   = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { user } = useAuth();
  const appId    = parseInt(id ?? "0");
  const isAdmin  = user?.roleId === 2 || user?.roleId === 3;

  const [tabIndex, setTabIndex]     = useState(0);
  const [details, setDetails]       = useState<AppDetailsType | null>(null);
  const [loading, setLoading]       = useState(true);
  const [saving, setSaving]         = useState(false);
  const [error, setError]           = useState("");
  const [successMsg, setSuccessMsg] = useState("");
  const [showSecret, setShowSecret] = useState(false);
  const [localMicroservices, setLocalMicroservices] = useState<MicroserviceDto[]>([]);

  // Route panel state
  const [expandedMs, setExpandedMs]   = useState<number | null>(null);
  const [routeLoading, setRouteLoading] = useState<Record<number, boolean>>({});
  const [routeSaving, setRouteSaving]   = useState<Record<number, boolean>>({});
  const [localRoutes, setLocalRoutes]   = useState<Record<number, RouteDto[]>>({});

  const fetchDetails = useCallback(async () => {
    setLoading(true); setError("");
    try {
      const data = await getApplicationDetails(appId);
      setDetails(data);
      setLocalMicroservices(data.microservices ?? []);
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : "Failed to load details");
    } finally { setLoading(false); }
  }, [appId]);

  useEffect(() => { fetchDetails(); }, [fetchDetails]);

  const currentEnvName = ENVIRONMENTS[tabIndex];
  const currentEnv     = details?.environments?.find(
    (e) => e.environment.toLowerCase() === currentEnvName.toLowerCase()) ?? null;
  const isEnabled = currentEnv?.isEnabled ?? false;

  const handleToggleEnv = async (enabled: boolean) => {
    if (!details || !currentEnv) return;
    setSaving(true); setError(""); setSuccessMsg("");
    try {
      const envUpdates = (details.environments ?? []).map((e) => ({
        name: e.environment,
        isEnabled: e.environment.toLowerCase() === currentEnvName.toLowerCase() ? enabled : e.isEnabled,
      }));
      await updateApplicationSettings(appId, envUpdates,
        localMicroservices.map((m) => ({ id: m.id, isEnabled: m.isEnabled })));
      await fetchDetails();
      setSuccessMsg("Environment updated.");
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : "Update failed");
    } finally { setSaving(false); }
  };

  const handleToggleMicroservice = (index: number, value: boolean) => {
    const updated = [...localMicroservices];
    updated[index] = { ...updated[index], isEnabled: value };
    setLocalMicroservices(updated);
    if (value && isAdmin) loadRoutes(updated[index].id);
  };

  const handleUpdate = async () => {
    if (!details) return;
    setSaving(true); setError(""); setSuccessMsg("");
    try {
      const envUpdates = (details.environments ?? []).map((e) => ({ name: e.environment, isEnabled: e.isEnabled }));
      await updateApplicationSettings(appId, envUpdates,
        localMicroservices.map((m) => ({ id: m.id, isEnabled: m.isEnabled })));
      await fetchDetails();
      setSuccessMsg("Application updated successfully!");
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : "Update failed");
    } finally { setSaving(false); }
  };

  const loadRoutes = async (msId: number) => {
    if (localRoutes[msId]) return;
    setRouteLoading((p) => ({ ...p, [msId]: true }));
    try {
      const data = await getMicroserviceRoutes(appId, msId);
      setLocalRoutes((p) => ({ ...p, [msId]: data.routes }));
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : "Failed to load routes");
    } finally { setRouteLoading((p) => ({ ...p, [msId]: false })); }
  };

  const handleExpandRoutes = (msId: number, msEnabled: boolean) => {
    if (expandedMs === msId) { setExpandedMs(null); return; }
    setExpandedMs(msId);
    if (msEnabled && isAdmin) loadRoutes(msId);
  };

  const handleRouteToggle = (msId: number, routeId: string, checked: boolean) =>
    setLocalRoutes((prev) => ({
      ...prev,
      [msId]: (prev[msId] ?? []).map((r) => r.routeId === routeId ? { ...r, isEnabled: checked } : r),
    }));

  const handleSelectAllRoutes = (msId: number, checked: boolean) =>
    setLocalRoutes((prev) => ({
      ...prev,
      [msId]: (prev[msId] ?? []).map((r) => ({ ...r, isEnabled: checked })),
    }));

  const handleSaveRoutes = async (msId: number) => {
    const routes = localRoutes[msId];
    if (!routes) return;
    setRouteSaving((p) => ({ ...p, [msId]: true }));
    setError(""); setSuccessMsg("");
    try {
      await updateRouteAccess(appId, msId, routes.map((r) => ({ routeId: r.routeId, isEnabled: r.isEnabled })));
      const data = await getMicroserviceRoutes(appId, msId);
      setLocalRoutes((p) => ({ ...p, [msId]: data.routes }));
      setSuccessMsg("Route access updated & synced to API Gateway!");
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : "Failed to save routes");
    } finally { setRouteSaving((p) => ({ ...p, [msId]: false })); }
  };

  const handleRegenerate = async () => {
    if (!currentEnv?.id) return;
    setSaving(true); setError("");
    try { await regenerateSecret(appId, currentEnv.id); await fetchDetails(); setSuccessMsg("Credentials regenerated!"); }
    catch (err: unknown) { setError(err instanceof Error ? err.message : "Regenerate failed"); }
    finally { setSaving(false); }
  };

  const handleRevoke = async () => {
    if (!currentEnv?.id) return;
    if (!window.confirm("Revoke access? This will disable credentials for this environment.")) return;
    setSaving(true); setError("");
    try { await revokeKey(appId, currentEnv.id); await fetchDetails(); setSuccessMsg("Access revoked."); }
    catch (err: unknown) { setError(err instanceof Error ? err.message : "Revoke failed"); }
    finally { setSaving(false); }
  };

  const handleCopy = (text: string) => {
    navigator.clipboard.writeText(text);
    setSuccessMsg("Copied!"); setTimeout(() => setSuccessMsg(""), 2000);
  };

  if (loading) return <div className="container mt-5 text-muted text-center">Loading...</div>;
  if (error && !details) return (
    <div className="container mt-5">
      <div className="alert alert-danger">{error}</div>
      <button onClick={() => navigate(-1)} className="btn btn-secondary">Back</button>
    </div>
  );

  return (
    <div className="container mt-4 mb-5">
      <button onClick={() => navigate(-1)} style={{ border: "none", background: "none" }}>← Back</button>
      <h3 className="fw-bold mt-2">{details?.title ?? "Application"}</h3>
      <p className="text-muted">Configure microservice access and manage credentials</p>

      {error      && <div className="alert alert-danger py-2">{error}</div>}
      {successMsg && <div className="alert alert-success py-2">{successMsg}</div>}

      {/* Environment */}
      <div className="card p-3 mb-4 shadow-sm" style={{ borderRadius: 12 }}>
        <h6 className="fw-semibold">Environment</h6>
        <TabBar tabs={ENVIRONMENTS.map((e) => ({ label: e }))} value={tabIndex}
          onChange={(i) => { setTabIndex(i); setError(""); setSuccessMsg(""); }} />
        <div className="mt-3 d-flex justify-content-between align-items-center">
          <div>
            <strong>{currentEnvName}</strong>
            <p className="text-muted mb-0">{isEnabled ? "Enabled — Credentials active" : "Disabled"}</p>
          </div>
          {isAdmin && (
            <div onClick={() => !saving && handleToggleEnv(!isEnabled)} style={{
              width: 50, height: 26, borderRadius: 20,
              background: isEnabled ? "linear-gradient(135deg,#4c7df0,#2d6cdf)" : "#ccc",
              display: "flex", alignItems: "center", padding: 3,
              cursor: saving ? "not-allowed" : "pointer", transition: "0.3s",
            }}>
              <div style={{ width: 20, height: 20, borderRadius: "50%", background: "#fff",
                transform: isEnabled ? "translateX(24px)" : "translateX(0)", transition: "0.3s" }} />
            </div>
          )}
        </div>
      </div>

      {isEnabled && currentEnv ? (
        <>
          {/* Credentials */}
          <div className="card p-3 mb-4 shadow-sm" style={{ borderRadius: 12 }}>
            <h6 className="fw-semibold">Application Credentials — {currentEnvName}</h6>

            <label className="mb-1">App Key</label>
            <div className="d-flex align-items-center justify-content-between mb-3"
              style={{ border: "1px solid #d0d5dd", borderRadius: 10, padding: "6px 10px" }}>
              <input type="text" readOnly value={currentEnv.apiKey ?? ""}
                style={{ border: "none", outline: "none", flex: 1, fontSize: 14 }} />
              <FiCopy onClick={() => handleCopy(currentEnv.apiKey)} style={{ cursor: "pointer", color: "#667085" }} />
            </div>

            <label className="mb-1">App Secret</label>
            <div className="d-flex align-items-center justify-content-between mb-3"
              style={{ border: "1px solid #d0d5dd", borderRadius: 10, padding: "6px 10px", gap: 10 }}>
              <input type={showSecret ? "text" : "password"} readOnly value={currentEnv.apiSecret ?? ""}
                style={{ border: "none", outline: "none", flex: 1, fontSize: 14 }} />
              {showSecret
                ? <FiEye onClick={() => setShowSecret(false)} style={{ cursor: "pointer", color: "#667085" }} />
                : <FiEyeOff onClick={() => setShowSecret(true)} style={{ cursor: "pointer", color: "#667085" }} />}
              <FiCopy onClick={() => handleCopy(currentEnv.apiSecret)} style={{ cursor: "pointer", color: "#667085" }} />
            </div>

            {isAdmin && (
              <div className="d-flex gap-2 mt-2">
                <button onClick={handleRegenerate} disabled={saving} className="btn d-flex align-items-center gap-2"
                  style={{ border: "1px solid #d0d5dd", background: "#fff", borderRadius: 8, padding: "6px 12px" }}>
                  <FiRefreshCw /> Regenerate
                </button>
                <button onClick={handleRevoke} disabled={saving} className="btn text-white d-flex align-items-center gap-2"
                  style={{ background: "#ef4444", borderRadius: 8, padding: "6px 12px", border: "none" }}>
                  <FiTrash2 /> Revoke
                </button>
              </div>
            )}
          </div>

          {/* Microservices + Routes */}
          <div className="card p-3 shadow-sm" style={{ borderRadius: 12 }}>
            <h6 className="fw-semibold">Microservices Access</h6>
            <p className="text-muted mb-3" style={{ fontSize: 14 }}>
              Enable microservices, then expand each to control individual route access.
              Route changes sync to the API Gateway immediately.
            </p>

            <div className="d-flex justify-content-between border-bottom pb-2 mb-1 fw-semibold" style={{ fontSize: 14 }}>
              <span>Microservice</span><span>Status</span>
            </div>

            {localMicroservices.map((svc, i) => {
              const isExpanded = expandedMs === svc.id;
              const routes     = localRoutes[svc.id] ?? [];
              const isLoadingR = routeLoading[svc.id] ?? false;
              const isSavingR  = routeSaving[svc.id]  ?? false;
              const enabledCount = routes.filter((r) => r.isEnabled).length;

              return (
                <div key={svc.id} style={{ borderBottom: "1px solid #f0f0f0" }}>
                  <div className="d-flex justify-content-between align-items-center py-3">
                    <div className="d-flex align-items-center gap-2"
                      style={{ cursor: svc.isEnabled && isAdmin ? "pointer" : "default" }}
                      onClick={() => svc.isEnabled && isAdmin && handleExpandRoutes(svc.id, svc.isEnabled)}>
                      {svc.isEnabled && isAdmin && (
                        isExpanded
                          ? <FiChevronDown style={{ color: "#4c7df0" }} />
                          : <FiChevronRight style={{ color: "#aaa" }} />
                      )}
                      <span className="fw-medium">{svc.name}</span>
                      {svc.isEnabled && routes.length > 0 && (
                        <span style={{
                          fontSize: 11, padding: "2px 8px", borderRadius: 20,
                          background: enabledCount > 0 ? "#e0f2fe" : "#fee2e2",
                          color:      enabledCount > 0 ? "#0369a1" : "#dc2626",
                        }}>
                          {enabledCount}/{routes.length} routes
                        </span>
                      )}
                    </div>

                    {isAdmin ? (
                      <div className="d-flex gap-3">
                        <label className="d-flex align-items-center gap-1" style={{ fontSize: 14 }}>
                          <input type="radio" checked={svc.isEnabled} onChange={() => handleToggleMicroservice(i, true)} /> Enabled
                        </label>
                        <label className="d-flex align-items-center gap-1" style={{ fontSize: 14 }}>
                          <input type="radio" checked={!svc.isEnabled} onChange={() => handleToggleMicroservice(i, false)} /> Disabled
                        </label>
                      </div>
                    ) : (
                      <span style={{
                        padding: "3px 12px", borderRadius: 20, fontSize: 12,
                        background: svc.isEnabled ? "#dcfce7" : "#fee2e2",
                        color:      svc.isEnabled ? "#166534" : "#dc2626",
                      }}>{svc.isEnabled ? "Enabled" : "Disabled"}</span>
                    )}
                  </div>

                  {/* Routes panel */}
                  {isExpanded && svc.isEnabled && isAdmin && (
                    <div style={{ margin: "0 0 16px 28px", background: "#f9fafb",
                      borderRadius: 10, padding: "14px 16px", border: "1px solid #e5e7eb" }}>
                      {isLoadingR ? (
                        <p className="text-muted mb-0" style={{ fontSize: 13 }}>Loading routes...</p>
                      ) : routes.length === 0 ? (
                        <p className="text-muted mb-0" style={{ fontSize: 13 }}>No routes defined.</p>
                      ) : (
                        <>
                          <div className="d-flex align-items-center justify-content-between mb-3">
                            <span style={{ fontSize: 12, fontWeight: 600, color: "#374151", letterSpacing: "0.05em" }}>
                              ROUTES — {enabledCount} of {routes.length} enabled
                            </span>
                            <div className="d-flex gap-2">
                              {["All","None"].map((label) => (
                                <button key={label} onClick={() => handleSelectAllRoutes(svc.id, label === "All")}
                                  style={{ fontSize: 12, padding: "2px 10px", borderRadius: 6,
                                    border: "1px solid #d0d5dd", background: "#fff", cursor: "pointer" }}>
                                  {label}
                                </button>
                              ))}
                            </div>
                          </div>

                          {routes.map((route) => {
                            const mc = METHOD_COLORS[route.method] ?? { bg: "#f3f4f6", color: "#374151" };
                            return (
                              <div key={route.routeId} className="d-flex align-items-center gap-3 py-2"
                                style={{ borderBottom: "1px solid #e5e7eb" }}>
                                <input type="checkbox" checked={route.isEnabled}
                                  onChange={(e) => handleRouteToggle(svc.id, route.routeId, e.target.checked)}
                                  style={{ width: 16, height: 16, cursor: "pointer" }} />
                                <span style={{ fontSize: 11, fontWeight: 700, padding: "2px 8px",
                                  borderRadius: 4, minWidth: 50, textAlign: "center",
                                  background: mc.bg, color: mc.color }}>
                                  {route.method}
                                </span>
                                <div style={{ flex: 1 }}>
                                  <code style={{ fontSize: 12, color: "#374151" }}>{route.path}</code>
                                  {route.description && (
                                    <span style={{ fontSize: 11, color: "#9ca3af", marginLeft: 8 }}>
                                      — {route.description}
                                    </span>
                                  )}
                                </div>
                              </div>
                            );
                          })}

                          <div className="d-flex justify-content-end mt-3">
                            <button onClick={() => handleSaveRoutes(svc.id)} disabled={isSavingR}
                              style={{ background: "linear-gradient(135deg,#4c7df0,#2d6cdf)",
                                color: "#fff", border: "none", borderRadius: 8,
                                padding: "7px 18px", fontSize: 13,
                                cursor: isSavingR ? "not-allowed" : "pointer" }}>
                              {isSavingR ? "Saving..." : "Save Route Access"}
                            </button>
                          </div>
                        </>
                      )}
                    </div>
                  )}
                </div>
              );
            })}
          </div>
        </>
      ) : (
        <div className="card p-4 text-center shadow-sm" style={{ borderRadius: 12 }}>
          <h6 className="fw-semibold" style={{ textAlign: "left" }}>Microservices Access</h6>
          <p className="text-muted mt-5 mb-5">Enable the environment above to manage microservice and route access</p>
        </div>
      )}

      {isEnabled && isAdmin && (
        <div className="d-flex justify-content-end gap-2 mt-4">
          <button onClick={() => navigate(-1)} className="btn"
            style={{ borderRadius: 10, padding: "10px 20px", border: "1px solid #d0d5dd", background: "#fff" }}>
            Close
          </button>
          <button onClick={handleUpdate} disabled={saving}
            className="d-flex align-items-center justify-content-center text-white"
            style={{ borderRadius: 10, background: "linear-gradient(135deg,#4c7df0,#2d6cdf)",
              border: "none", padding: "10px 20px", fontWeight: 500 }}>
            {saving ? "Saving..." : "Update Microservices"}
          </button>
        </div>
      )}
    </div>
  );
}