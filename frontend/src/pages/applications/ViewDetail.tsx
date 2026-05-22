import { useState } from "react";
import { useParams, useNavigate } from "react-router-dom";
import { FaChevronDown, FaArrowLeft, FaCheckCircle } from "react-icons/fa";

import NavBar from "../../components/common/NavBar";
import { useAuth } from "../../context/AuthContext";
import { MICROSERVICES, type EndpointConfig } from "../../config/microservices";
import { getGatewayToken } from "../../services/api";

// ── Per-endpoint state ────────────────────────────────────────────────────────
interface EndpointState {
  isOpen: boolean;
  url: string;
  isEditingUrl: boolean;
  body: string;
  response: any;
  loading: boolean;
  error: string;
}

// Colour per HTTP method badge
function methodColor(method: string): string {
  switch (method) {
    case "GET": return "#28a745";
    case "POST": return "#667eea";
    case "PUT": return "#fd7e14";
    case "DELETE": return "#dc3545";
    default: return "#6c757d";
  }
}

export default function ViewDetail() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { user } = useAuth();
  const username = user?.email?.split("@")[0] || "User";

  // ── Gateway auth state ──────────────────────────────────────────────────────
  const [gatewayToken, setGatewayToken] = useState<string | null>(
    () => localStorage.getItem("gatewayToken")
  );
  const [clientId, setClientId] = useState("");
  const [clientSecret, setClientSecret] = useState("");
  const [tokenLoading, setTokenLoading] = useState(false);
  const [tokenError, setTokenError] = useState("");

  function persistToken(token: string | null) {
    if (token) localStorage.setItem("gatewayToken", token);
    else localStorage.removeItem("gatewayToken");
    setGatewayToken(token);
  }

  async function handleFetchToken() {
    if (!clientId.trim() || !clientSecret.trim()) return;
    setTokenLoading(true);
    setTokenError("");
    try {
      const res = await getGatewayToken(clientId.trim(), clientSecret.trim());
      persistToken(res.access_token);
      setClientId("");
      setClientSecret("");
    } catch (err: any) {
      setTokenError(err.message || "Failed to get gateway token");
    } finally {
      setTokenLoading(false);
    }
  }

  // ── Service config ──────────────────────────────────────────────────────────
  const service = MICROSERVICES.find((s) => s.id === Number(id));

  // ── Per-endpoint state initialised from config ──────────────────────────────
  const [endpointStates, setEndpointStates] = useState<EndpointState[]>(() =>
    (service?.endpoints ?? []).map((ep) => ({
      isOpen: false,
      url: `${service!.gatewayBaseUrl}${ep.path}`,
      isEditingUrl: false,
      body: ep.defaultBody ?? "",
      response: null,
      loading: false,
      error: "",
    }))
  );

  // ── 404 guard ───────────────────────────────────────────────────────────────
  if (!service) {
    return (
      <>
        <NavBar username={username} />
        <div className="container py-5 text-center">
          <h4 className="text-muted">Microservice not found.</h4>
          <button
            className="btn btn-primary mt-3"
            style={{ background: "#667eea", border: "none", borderRadius: "10px" }}
            onClick={() => navigate("/dashboard")}
          >
            Back to Dashboard
          </button>
        </div>
      </>
    );
  }

  // ── Helpers ─────────────────────────────────────────────────────────────────
  function updateEndpoint(index: number, patch: Partial<EndpointState>) {
    setEndpointStates((prev) =>
      prev.map((s, i) => (i === index ? { ...s, ...patch } : s))
    );
  }

  async function handleTest(index: number, ep: EndpointConfig) {
    const state = endpointStates[index];
    updateEndpoint(index, { loading: true, error: "", response: null });

    try {
      const headers: Record<string, string> = {
        "Content-Type": "application/json",
      };

      // Always use the API gateway token (has the "key" claim APISIX jwt-auth needs)
      if (gatewayToken) {
        headers["Authorization"] = `Bearer ${gatewayToken}`;
      }

      const res = await fetch(state.url, {
        method: ep.method,
        headers,
        body:
          ep.method !== "GET" && state.body.trim()
            ? state.body
            : undefined,
      });

      let data: any;
      const ct = res.headers.get("content-type") ?? "";
      if (ct.includes("application/json")) {
        data = await res.json();
      } else {
        data = { raw: await res.text() };
      }

      updateEndpoint(index, {
        response: { status: res.status, ok: res.ok, data },
        loading: false,
      });
    } catch (err: any) {
      updateEndpoint(index, {
        error: err.message || "Request failed",
        loading: false,
      });
    }
  }

  // ── Render ──────────────────────────────────────────────────────────────────
  return (
    <>
      <NavBar username={username} />

      <div className="container py-4">

        {/* Page header */}
        <div className="d-flex align-items-center gap-3 mb-4">
          <button
            className="btn btn-outline-secondary"
            onClick={() => navigate("/dashboard")}
            style={{
              borderRadius: "10px",
              width: "42px",
              height: "42px",
              padding: 0,
            }}
          >
            <FaArrowLeft />
          </button>

          <div>
            <h2 className="mb-0" style={{ fontWeight: 700 }}>
              {service.name}
            </h2>

            <p className="text-muted mb-0" style={{ fontSize: "13px" }}>
              Gateway:{" "}
              <code style={{ color: "#667eea" }}>
                {service.gatewayBaseUrl}
              </code>
            </p>
          </div>

          {/* Move button to right side */}
          <div className="ms-auto">
            <button
              className="btn btn-dark"
              style={{
                borderRadius: "10px",
                padding: "10px 18px",
                fontWeight: 600,
              }}
              onClick={() => navigate("/health")}
            >
              Check Health
            </button>
          </div>
        </div>

        {/* ── Gateway Auth Banner ──────────────────────────────────────────── */}
        <div
          className="card mb-4"
          style={{
            borderRadius: "14px",
            border: `1.5px solid ${gatewayToken ? "#28a745" : "#f59e0b"}`,
            background: gatewayToken ? "#f0fdf4" : "#fffbeb",
          }}
        >
          <div className="card-body">
            {gatewayToken ? (
              /* ── Authenticated state ── */
              <div className="d-flex align-items-center justify-content-between">
                <div className="d-flex align-items-center gap-2">
                  <FaCheckCircle size={18} color="#28a745" />
                  <span style={{ fontWeight: 600, color: "#15803d" }}>
                    Gateway Authenticated
                  </span>
                  <span className="text-muted" style={{ fontSize: "13px" }}>
                    — Token is active. All API calls will use it automatically.
                  </span>
                </div>
                <button
                  className="btn btn-sm btn-outline-danger"
                  style={{ borderRadius: "8px" }}
                  onClick={() => persistToken(null)}
                >
                  Clear Token
                </button>
              </div>
            ) : (
              /* ── Unauthenticated state ── */
              <>
                <div className="d-flex align-items-center gap-2 mb-2">
                  <span style={{ fontWeight: 600, color: "#000000" }}>
                    Gateway Authentication Required
                  </span>
                </div>
                <p className="text-muted mb-3" style={{ fontSize: "13px" }}>
                  Enter your Application credentials to call the API Gateway.
                  Find them under{" "}
                  <strong>Manage Applications → your app → API Keys</strong>.
                </p>

                <div className="row g-2 align-items-start">
                  <div className="col-md-4">
                    <input
                      type="text"
                      className="form-control"
                      placeholder="Client ID (App Key)"
                      value={clientId}
                      onChange={(e) => setClientId(e.target.value)}
                      style={{ borderRadius: "10px", height: "46px" }}
                    />
                  </div>
                  <div className="col-md-4">
                    <input
                      type="password"
                      className="form-control"
                      placeholder="Client Secret (App Secret)"
                      value={clientSecret}
                      onChange={(e) => setClientSecret(e.target.value)}
                      onKeyDown={(e) => e.key === "Enter" && handleFetchToken()}
                      style={{ borderRadius: "10px", height: "46px" }}
                    />
                  </div>
                  <div className="col-md-4">
                    <button
                      className="btn w-100 text-white"
                      onClick={handleFetchToken}
                      disabled={tokenLoading || !clientId.trim() || !clientSecret.trim()}
                      style={{
                        background: "#667eea",
                        border: "none",
                        borderRadius: "10px",
                        height: "46px",
                        fontWeight: 600,
                      }}
                    >
                      {tokenLoading ? "Authenticating..." : "Get Gateway Token"}
                    </button>
                  </div>
                </div>

                {tokenError && (
                  <div className="alert alert-danger mt-2 mb-0 py-2" style={{ fontSize: "13px" }}>
                    {tokenError}
                  </div>
                )}
              </>
            )}
          </div>
        </div>

        {/* ── Endpoint Cards ───────────────────────────────────────────────── */}
        {service.endpoints.map((ep, i) => {
          const state = endpointStates[i];
          const color = methodColor(ep.method);

          return (
            <div
              key={i}
              className="card border mb-3"
              style={{ borderRadius: "12px", overflow: "hidden" }}
            >
              {/* Accordion header */}
              <div
                className="d-flex align-items-center justify-content-between px-3 py-2"
                style={{ background: "#eaf3ff", cursor: "pointer", userSelect: "none" }}
                onClick={() => updateEndpoint(i, { isOpen: !state.isOpen })}
              >
                <div className="d-flex align-items-center gap-3 flex-wrap">
                  <span
                    style={{
                      background: color,
                      color: "white",
                      padding: "5px 16px",
                      borderRadius: "8px",
                      fontWeight: 700,
                      fontSize: "13px",
                      minWidth: "60px",
                      textAlign: "center",
                    }}
                  >
                    {ep.method}
                  </span>
                  <span style={{ fontSize: "16px", fontWeight: 500 }}>
                    {ep.path}
                  </span>
                  {ep.description && (
                    <span
                      className="text-muted d-none d-md-inline"
                      style={{ fontSize: "13px" }}
                    >
                      — {ep.description}
                    </span>
                  )}
                </div>
                <FaChevronDown
                  size={15}
                  style={{
                    transition: "transform 0.2s",
                    transform: state.isOpen ? "rotate(180deg)" : "rotate(0deg)",
                    flexShrink: 0,
                  }}
                />
              </div>

              {/* Expanded body */}
              {state.isOpen && (
                <div className="card-body">
                  {/* URL row */}
                  <div className="row g-2 align-items-center mb-3">
                    {/* Method badge */}
                    <div className="col-md-2">
                      <div
                        className="d-flex align-items-center justify-content-center fw-bold text-white"
                        style={{
                          height: "50px",
                          background: color,
                          borderRadius: "10px",
                          fontSize: "14px",
                        }}
                      >
                        {ep.method}
                      </div>
                    </div>

                    {/* URL input + Edit/Save */}
                    <div className="col-md-8">
                      <div className="d-flex gap-2">
                        <input
                          type="text"
                          className="form-control"
                          value={state.url}
                          disabled={!state.isEditingUrl}
                          onChange={(e) =>
                            updateEndpoint(i, { url: e.target.value })
                          }
                          style={{
                            height: "50px",
                            borderRadius: "10px",
                            background: state.isEditingUrl ? "#ffffff" : "#f1f5f9",
                            fontWeight: 500,
                            fontSize: "14px",
                          }}
                        />
                        <button
                          className="btn"
                          onClick={() =>
                            updateEndpoint(i, { isEditingUrl: !state.isEditingUrl })
                          }
                          style={{
                            minWidth: "80px",
                            height: "50px",
                            borderRadius: "10px",
                            border: `1px solid ${color}`,
                            color: state.isEditingUrl ? "white" : color,
                            background: state.isEditingUrl ? color : "white",
                            fontWeight: 600,
                            transition: "all 0.15s",
                          }}
                        >
                          {state.isEditingUrl ? "Save" : "Edit"}
                        </button>
                      </div>
                    </div>

                    {/* Test button */}
                    <div className="col-md-2">
                      <button
                        className="btn w-100 text-white"
                        onClick={() => handleTest(i, ep)}
                        disabled={state.loading || !gatewayToken}
                        title={!gatewayToken ? "Authenticate first using the banner above" : ""}
                        style={{
                          height: "50px",
                          background: gatewayToken ? color : "#adb5bd",
                          border: "none",
                          borderRadius: "10px",
                          fontWeight: 600,
                          cursor: !gatewayToken ? "not-allowed" : "pointer",
                        }}
                      >
                        {state.loading ? "Testing…" : "Test API"}
                      </button>
                    </div>
                  </div>

                  {/* No-token warning inline */}
                  {!gatewayToken && (
                    <div
                      className="alert mb-3 py-2"
                      style={{
                        background: "#fff8e1",
                        border: "1px solid #f59e0b",
                        borderRadius: "10px",
                        fontSize: "13px",
                        color: "#92400e",
                      }}
                    >
                      Authenticate via the banner above before testing.
                    </div>
                  )}

                  {/* Request body (POST / PUT only) */}
                  {(ep.method === "POST" || ep.method === "PUT") && (
                    <div className="mb-3">
                      <label className="form-label fw-semibold mb-2">
                        Request Body
                      </label>
                      <textarea
                        rows={6}
                        className="form-control"
                        value={state.body}
                        onChange={(e) =>
                          updateEndpoint(i, { body: e.target.value })
                        }
                        style={{
                          borderRadius: "12px",
                          fontFamily: "monospace",
                          fontSize: "14px",
                        }}
                      />
                    </div>
                  )}

                  {/* Error */}
                  {state.error && (
                    <div
                      className="alert alert-danger py-2 mb-3"
                      style={{ borderRadius: "10px", fontSize: "13px" }}
                    >
                      {state.error}
                    </div>
                  )}

                  {/* Response */}
                  <div>
                    <div className="d-flex align-items-center justify-content-between mb-2">
                      <label className="form-label fw-semibold mb-0">
                        API Response
                      </label>
                      {state.response && (
                        <span
                          className="badge"
                          style={{
                            background: state.response.ok ? "#d1fae5" : "#fee2e2",
                            color: state.response.ok ? "#065f46" : "#991b1b",
                            padding: "5px 10px",
                            borderRadius: "8px",
                            fontSize: "12px",
                            fontWeight: 700,
                          }}
                        >
                          {state.response.status}{" "}
                          {state.response.ok ? "OK" : "ERROR"}
                        </span>
                      )}
                    </div>
                    <pre
                      style={{
                        background: "#0f172a",
                        color: state.response && !state.response.ok
                          ? "#f87171"
                          : "#f8fafc",
                        padding: "20px",
                        borderRadius: "12px",
                        minHeight: "160px",
                        overflowX: "auto",
                        fontSize: "13px",
                        lineHeight: "1.6",
                      }}
                    >
                      {state.response
                        ? JSON.stringify(state.response.data, null, 2)
                        : `{\n  "message": "Response will appear here..."\n}`}
                    </pre>
                  </div>
                </div>
              )}
            </div>
          );
        })}
      </div>
    </>
  );
}