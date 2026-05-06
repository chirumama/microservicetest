import { useState } from "react";
import { FiRefreshCw, FiCheck, FiAlertCircle, FiPlus, FiEdit2, FiSkipForward } from "react-icons/fi";
import { triggerRouteSync, type RouteSyncResult } from "../../services/sync-api";

export default function RouteSyncPanel() {
  const [syncing, setSyncing]   = useState(false);
  const [result, setResult]     = useState<RouteSyncResult | null>(null);
  const [error, setError]       = useState("");

  const handleSync = async () => {
    setSyncing(true); setError(""); setResult(null);
    try {
      const res = await triggerRouteSync();
      setResult(res);
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : "Sync failed");
    } finally {
      setSyncing(false);
    }
  };

  const cardStyle: React.CSSProperties = {
    background: "#fff",
    borderRadius: 12,
    border: "1px solid #e5e7eb",
    padding: "20px 24px",
    marginBottom: 16,
    boxShadow: "0 1px 3px rgba(0,0,0,0.06)",
  };

  return (
    <div style={{ maxWidth: 680 }}>
      <h5 className="fw-bold mb-1">Route Sync</h5>
      <p className="text-muted mb-4" style={{ fontSize: 14 }}>
        Routes added in APISix Dashboard are automatically synced every 30 seconds.
        Use the button below to trigger an immediate sync.
      </p>

      {/* Sync button */}
      <div style={cardStyle}>
        <div className="d-flex align-items-center justify-content-between">
          <div>
            <strong>Sync from APISix</strong>
            <p className="text-muted mb-0" style={{ fontSize: 13 }}>
              Pulls all routes from APISix Admin API and registers them in the database.
            </p>
          </div>
          <button
            onClick={handleSync}
            disabled={syncing}
            style={{
              background: syncing ? "#e5e7eb" : "linear-gradient(135deg,#4c7df0,#2d6cdf)",
              color: syncing ? "#9ca3af" : "#fff",
              border: "none", borderRadius: 8,
              padding: "9px 20px", fontWeight: 500,
              fontSize: 14, cursor: syncing ? "not-allowed" : "pointer",
              display: "flex", alignItems: "center", gap: 8,
              minWidth: 140, justifyContent: "center",
            }}
          >
            <FiRefreshCw style={{ animation: syncing ? "spin 1s linear infinite" : "none" }} />
            {syncing ? "Syncing..." : "Sync Now"}
          </button>
        </div>
      </div>

      {/* Error */}
      {error && (
        <div style={{ ...cardStyle, background: "#fef2f2", borderColor: "#fca5a5" }}>
          <div className="d-flex align-items-center gap-2" style={{ color: "#dc2626" }}>
            <FiAlertCircle /> <strong>Sync failed</strong>
          </div>
          <p className="mb-0 mt-1" style={{ fontSize: 13, color: "#dc2626" }}>{error}</p>
        </div>
      )}

      {/* Result */}
      {result && (
        <>
          {/* Summary bar */}
          <div style={{
            ...cardStyle,
            background: result.success ? "#f0fdf4" : "#fef2f2",
            borderColor: result.success ? "#86efac" : "#fca5a5",
          }}>
            <div className="d-flex align-items-center gap-2 mb-2">
              {result.success
                ? <FiCheck style={{ color: "#16a34a" }} />
                : <FiAlertCircle style={{ color: "#dc2626" }} />}
              <strong style={{ color: result.success ? "#15803d" : "#dc2626" }}>
                {result.success ? "Sync complete" : "Sync completed with errors"}
              </strong>
              <span style={{ fontSize: 12, color: "#6b7280", marginLeft: "auto" }}>
                {new Date(result.syncedAt).toLocaleTimeString()}
              </span>
            </div>

            <div className="d-flex gap-4" style={{ fontSize: 13 }}>
              <span><strong>{result.totalRoutes}</strong> routes found in APISix</span>
              <span style={{ color: "#16a34a" }}><strong>{result.synced}</strong> synced</span>
              {result.skipped > 0 && (
                <span style={{ color: "#d97706" }}><strong>{result.skipped}</strong> skipped</span>
              )}
            </div>

            {result.error && (
              <p className="mb-0 mt-2" style={{ fontSize: 12, color: "#dc2626" }}>{result.error}</p>
            )}
          </div>

          {/* Newly added routes */}
          {result.added.length > 0 && (
            <div style={cardStyle}>
              <div className="d-flex align-items-center gap-2 mb-3">
                <FiPlus style={{ color: "#16a34a" }} />
                <strong style={{ fontSize: 14 }}>
                  New routes registered ({result.added.length})
                </strong>
              </div>
              {result.added.map((r, i) => (
                <div key={i} style={{
                  padding: "8px 12px", borderRadius: 8, marginBottom: 6,
                  background: "#f0fdf4", border: "1px solid #bbf7d0",
                  fontSize: 13, color: "#15803d", fontFamily: "monospace",
                }}>
                  + {r}
                </div>
              ))}
            </div>
          )}

          {/* Updated routes */}
          {result.updated.length > 0 && (
            <div style={cardStyle}>
              <div className="d-flex align-items-center gap-2 mb-3">
                <FiEdit2 style={{ color: "#2563eb" }} />
                <strong style={{ fontSize: 14 }}>
                  Routes updated ({result.updated.length})
                </strong>
              </div>
              {result.updated.map((r, i) => (
                <div key={i} style={{
                  padding: "8px 12px", borderRadius: 8, marginBottom: 6,
                  background: "#eff6ff", border: "1px solid #bfdbfe",
                  fontSize: 13, color: "#1d4ed8", fontFamily: "monospace",
                }}>
                  ↻ {r}
                </div>
              ))}
            </div>
          )}

          {/* Skipped routes */}
          {result.skipReasons.length > 0 && (
            <div style={cardStyle}>
              <div className="d-flex align-items-center gap-2 mb-3">
                <FiSkipForward style={{ color: "#d97706" }} />
                <strong style={{ fontSize: 14 }}>
                  Skipped ({result.skipped}) — missing labels
                </strong>
              </div>
              <p style={{ fontSize: 12, color: "#6b7280", marginBottom: 12 }}>
                These routes are missing required labels in APISix. Open each route
                in APISix Dashboard → Edit → Labels tab and add the missing labels.
              </p>
              {result.skipReasons.map((r, i) => (
                <div key={i} style={{
                  padding: "8px 12px", borderRadius: 8, marginBottom: 6,
                  background: "#fffbeb", border: "1px solid #fde68a",
                  fontSize: 13, color: "#92400e",
                }}>
                  ⚠ {r}
                </div>
              ))}

              {/* Label reference */}
              <div style={{
                marginTop: 16, padding: 14, borderRadius: 8,
                background: "#f8fafc", border: "1px solid #e2e8f0",
              }}>
                <p style={{ fontSize: 12, fontWeight: 600, marginBottom: 8, color: "#374151" }}>
                  Required labels on every APISix route:
                </p>
                {[
                  ["microservice_id", "1", "integer — must match microservices.id in DB"],
                  ["method",          "GET", "HTTP method for this endpoint"],
                  ["endpoint",        "/api/passport/verify", "human-readable path shown in UI"],
                  ["description",     "Verify passport", "short description (optional)"],
                ].map(([key, example, note]) => (
                  <div key={key} style={{ display: "flex", gap: 8, marginBottom: 4, fontSize: 12 }}>
                    <code style={{
                      background: "#e2e8f0", padding: "1px 6px",
                      borderRadius: 4, minWidth: 130, color: "#1e40af",
                    }}>{key}</code>
                    <code style={{ color: "#16a34a", minWidth: 100 }}>{example}</code>
                    <span style={{ color: "#6b7280" }}>{note}</span>
                  </div>
                ))}
              </div>
            </div>
          )}

          {/* Nothing to sync */}
          {result.success && result.synced === 0 && result.skipped === 0 && (
            <div style={{ ...cardStyle, textAlign: "center", color: "#6b7280", fontSize: 14 }}>
              No routes found in APISix yet. Add routes in APISix Dashboard first.
            </div>
          )}
        </>
      )}

      {/* Spinner animation */}
      <style>{`
        @keyframes spin { from { transform: rotate(0deg); } to { transform: rotate(360deg); } }
      `}</style>
    </div>
  );
}