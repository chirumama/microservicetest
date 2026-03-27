import { useParams, useNavigate } from "react-router-dom";
import { useState, useEffect } from "react";
import { FiCopy, FiEye, FiEyeOff, FiRefreshCw, FiTrash2 } from "react-icons/fi";
import TabBar from "../../components/common/TabBar";
import {
  getApplicationDetails,
  updateApplicationSettings,
  regenerateSecret,
  revokeKey,
  type ApplicationDetails as AppDetailsType,
  type MicroserviceDto,
} from "../../services/api";

// These must exactly match the Environment values stored in the DB / created by ApplicationService
const ENVIRONMENTS = ["Development", "Pre-Production", "Production"];

export default function ApplicationDetails() {
  const { id }  = useParams<{ id: string }>();
  const navigate = useNavigate();
  const appId    = parseInt(id ?? "0");

  const [tabIndex, setTabIndex]   = useState(0);
  const [details, setDetails]     = useState<AppDetailsType | null>(null);
  const [loading, setLoading]     = useState(true);
  const [saving, setSaving]       = useState(false);
  const [error, setError]         = useState("");
  const [successMsg, setSuccessMsg] = useState("");
  const [showSecret, setShowSecret] = useState(false);
  const [localMicroservices, setLocalMicroservices] = useState<MicroserviceDto[]>([]);

  const fetchDetails = async () => {
    setLoading(true);
    setError("");
    try {
      const data = await getApplicationDetails(appId);
      setDetails(data);
      setLocalMicroservices(data.microservices ?? []);
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : "Failed to load application details");
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { fetchDetails(); }, [appId]);

  const currentEnvName = ENVIRONMENTS[tabIndex];

  // Case-insensitive match so minor DB casing differences don't break the UI
  const currentEnv = details?.environments?.find(
    (e) => e.environment.toLowerCase() === currentEnvName.toLowerCase()
  ) ?? null;

  const isEnabled = currentEnv?.isEnabled ?? false;

  const handleToggleEnv = async (enabled: boolean) => {
    if (!details || !currentEnv) return;
    setSaving(true); setError(""); setSuccessMsg("");
    try {
      const envUpdates = (details.environments ?? []).map((e) => ({
        name:      e.environment,
        isEnabled: e.environment.toLowerCase() === currentEnvName.toLowerCase() ? enabled : e.isEnabled,
      }));
      await updateApplicationSettings(
        appId,
        envUpdates,
        localMicroservices.map((m) => ({ id: m.id, isEnabled: m.isEnabled }))
      );
      await fetchDetails();
      setSuccessMsg("Environment updated.");
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : "Update failed");
    } finally {
      setSaving(false);
    }
  };

  const handleToggleMicroservice = (index: number, value: boolean) => {
    const updated = [...localMicroservices];
    updated[index] = { ...updated[index], isEnabled: value };
    setLocalMicroservices(updated);
  };

  const handleUpdate = async () => {
    if (!details) return;
    setSaving(true); setError(""); setSuccessMsg("");
    try {
      const envUpdates = (details.environments ?? []).map((e) => ({
        name:      e.environment,
        isEnabled: e.isEnabled,
      }));
      await updateApplicationSettings(
        appId,
        envUpdates,
        localMicroservices.map((m) => ({ id: m.id, isEnabled: m.isEnabled }))
      );
      setSuccessMsg("Application updated successfully!");
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : "Update failed");
    } finally {
      setSaving(false);
    }
  };

  const handleRegenerate = async () => {
    // Use the real ApiKeys.Id from the EnvironmentDto — not the tab index
    if (!currentEnv?.id) return;
    setSaving(true); setError("");
    try {
      await regenerateSecret(appId, currentEnv.id);
      await fetchDetails();
      setSuccessMsg("Credentials regenerated!");
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : "Regenerate failed");
    } finally {
      setSaving(false);
    }
  };

  const handleRevoke = async () => {
    if (!currentEnv?.id) return;
    if (!window.confirm("Revoke access? This will disable credentials for this environment.")) return;
    setSaving(true); setError("");
    try {
      await revokeKey(appId, currentEnv.id);
      await fetchDetails();
      setSuccessMsg("Access revoked.");
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : "Revoke failed");
    } finally {
      setSaving(false);
    }
  };

  const handleCopy = (text: string) => {
    navigator.clipboard.writeText(text);
    setSuccessMsg("Copied to clipboard!");
    setTimeout(() => setSuccessMsg(""), 2000);
  };

  if (loading) return <div className="container mt-5 text-muted text-center">Loading...</div>;

  if (error && !details) return (
    <div className="container mt-5">
      <div className="alert alert-danger">{error}</div>
      <button onClick={() => navigate(-1)} className="btn btn-secondary">← Back</button>
    </div>
  );

  return (
    <div className="container mt-4 mb-5">
      <button onClick={() => navigate(-1)} style={{ border: "none", background: "none" }}>
        ← Back to Applications
      </button>

      <h3 className="fw-bold mt-2">{details?.title ?? "Manage Application"}</h3>
      <p className="text-muted">Configure microservice access and manage credentials</p>

      {error      && <div className="alert alert-danger py-2">{error}</div>}
      {successMsg && <div className="alert alert-success py-2">{successMsg}</div>}

      {/* Environment selector */}
      <div className="card p-3 mb-4 shadow-sm" style={{ borderRadius: 12 }}>
        <h6 className="fw-semibold">Environment</h6>
        <TabBar
          tabs={ENVIRONMENTS.map((e) => ({ label: e }))}
          value={tabIndex}
          onChange={(i) => { setTabIndex(i); setError(""); setSuccessMsg(""); }}
        />

        <div className="mt-3 d-flex justify-content-between align-items-center">
          <div>
            <strong>{currentEnvName} Environment</strong>
            <p className="text-muted mb-0">
              {isEnabled ? "Enabled — Credentials active" : "Disabled — No credentials active"}
            </p>
          </div>
          <div
            onClick={() => !saving && handleToggleEnv(!isEnabled)}
            style={{
              width: 50, height: 26, borderRadius: 20,
              background: isEnabled ? "linear-gradient(135deg,#4c7df0,#2d6cdf)" : "#ccc",
              display: "flex", alignItems: "center", padding: 3,
              cursor: saving ? "not-allowed" : "pointer", transition: "0.3s",
            }}
          >
            <div style={{
              width: 20, height: 20, borderRadius: "50%", background: "#fff",
              transform: isEnabled ? "translateX(24px)" : "translateX(0)", transition: "0.3s",
            }} />
          </div>
        </div>
      </div>

      {isEnabled && currentEnv ? (
        <>
          {/* Credentials */}
          <div className="card p-3 mb-4 shadow-sm" style={{ borderRadius: 12 }}>
            <h6 className="fw-semibold">Application Credentials</h6>
            <p className="text-muted">Credentials for {currentEnvName.toUpperCase()} environment</p>

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
                : <FiEyeOff onClick={() => setShowSecret(true)} style={{ cursor: "pointer", color: "#667085" }} />
              }
              <FiCopy onClick={() => handleCopy(currentEnv.apiSecret)} style={{ cursor: "pointer", color: "#667085" }} />
            </div>

            <div className="d-flex gap-2 mt-2">
              <button onClick={handleRegenerate} disabled={saving}
                className="btn d-flex align-items-center gap-2"
                style={{ border: "1px solid #d0d5dd", background: "#fff", borderRadius: 8, padding: "6px 12px" }}>
                <FiRefreshCw /> Regenerate Credentials
              </button>
              <button onClick={handleRevoke} disabled={saving}
                className="btn text-white d-flex align-items-center gap-2"
                style={{ background: "#ef4444", borderRadius: 8, padding: "6px 12px", border: "none" }}>
                <FiTrash2 /> Revoke Access
              </button>
            </div>
          </div>

          {/* Microservices */}
          <div className="card p-3 shadow-sm" style={{ borderRadius: 12 }}>
            <h6 className="fw-semibold">Microservices Access</h6>
            <p className="text-muted mb-3" style={{ fontSize: 14 }}>
              Enable or disable access to microservices in {currentEnvName.toUpperCase()} environment
            </p>

            <div className="d-flex justify-content-between border-bottom pb-2 mb-2">
              <span className="fw-semibold">Microservice Name</span>
              <span className="fw-semibold">Status</span>
            </div>

            {localMicroservices.length === 0 ? (
              <p className="text-muted mt-3" style={{ fontSize: 14 }}>No microservices available.</p>
            ) : (
              localMicroservices.map((svc, i) => (
                <div key={svc.id}
                  className="d-flex justify-content-between align-items-start border-bottom py-3">
                  <div className="fw-medium">{svc.name}</div>
                  <div className="d-flex align-items-center gap-3">
                    <label className="d-flex align-items-center gap-1">
                      <input type="radio" checked={svc.isEnabled}
                        onChange={() => handleToggleMicroservice(i, true)} /> Enabled
                    </label>
                    <label className="d-flex align-items-center gap-1">
                      <input type="radio" checked={!svc.isEnabled}
                        onChange={() => handleToggleMicroservice(i, false)} /> Disabled
                    </label>
                  </div>
                </div>
              ))
            )}
          </div>
        </>
      ) : (
        <div className="card p-4 text-center shadow-sm" style={{ borderRadius: 12 }}>
          <h6 className="fw-semibold" style={{ textAlign: "left" }}>Microservices Access</h6>
          <p className="text-muted mt-5 mb-2">Environment is disabled</p>
          <p className="text-muted mb-5" style={{ fontSize: 14 }}>
            Enable the environment to manage microservice access
          </p>
        </div>
      )}

      {isEnabled && (
        <div className="d-flex justify-content-end gap-2 mt-4">
          <button onClick={() => navigate(-1)} className="btn"
            style={{ borderRadius: 10, padding: "10px 20px", border: "1px solid #d0d5dd", background: "#fff" }}>
            Close
          </button>
          <button onClick={handleUpdate} disabled={saving}
            className="d-flex align-items-center justify-content-center text-white"
            style={{ borderRadius: 10, background: "linear-gradient(135deg,#4c7df0,#2d6cdf)",
              border: "none", padding: "10px 20px", fontWeight: 500 }}>
            {saving ? "Saving..." : "Update"}
          </button>
        </div>
      )}
    </div>
  );
}
