import { useNavigate } from "react-router-dom";
import { FaArrowLeft } from "react-icons/fa";

import NavBar from "../../components/common/NavBar";
import { useAuth } from "../../context/AuthContext";

export default function HealthPage() {
  const navigate = useNavigate();
  const { user } = useAuth();

  const username = user?.email?.split("@")[0] || "User";

  const timeline = [
    "up", "up", "up", "up", "up",
    "down", "down", "up", "up", "up",
    "down", "down", "down", "up", "up",
    "up", "down", "up", "up", "up",
    "up", "up", "down", "up", "up",
    "up", "down", "down", "up", "up",
  ];

  const logs = [
    {
      time: "10:45:15 AM",
      type: "ERROR",
      message: "Connection refused to downstream service",
    },
    {
      time: "10:45:14 AM",
      type: "ERROR",
      message: "Request timeout after 5000ms",
    },
    {
      time: "10:45:13 AM",
      type: "WARN",
      message: "Retry attempt 3 failed",
    },
    {
      time: "10:45:12 AM",
      type: "ERROR",
      message: "Unable to connect to database",
    },
    {
      time: "10:45:11 AM",
      type: "INFO",
      message: "Health check failed",
    },
  ];

  return (
    <>
      <NavBar username={username} />

      <div
        className="container-fluid py-4 px-4"
        style={{
          background: "#f8fafc",
          minHeight: "100vh",
        }}
      >
        {/* Header */}
        <div className="d-flex align-items-center gap-3 mb-4">
          <button
            className="btn btn-outline-secondary"
            onClick={() => navigate(-1)}
            style={{
              borderRadius: "12px",
              width: "44px",
              height: "44px",
              padding: 0,
            }}
          >
            <FaArrowLeft />
          </button>

          <div>
            <h3
              className="mb-1"
              style={{
                fontWeight: 700,
                color: "#0f172a",
              }}
            >
              Pan Service Health
            </h3>

            <p
              className="mb-0"
              style={{
                color: "#64748b",
                fontSize: "14px",
              }}
            >
              Real-time service monitoring
            </p>
          </div>
        </div>

        {/* Top Cards */}
        <div className="row g-3 mb-4">
          {[
            {
              title: "Status",
              value: "Down",
              color: "#dc2626",
              sub: "Service unavailable",
            },
            {
              title: "Down Since",
              value: "10:32:15 AM",
              sub: "13 mins ago",
            },
            {
              title: "Uptime (24h)",
              value: "32.5%",
              sub: "Poor availability",
            },
            {
              title: "Response Time",
              value: "N/A",
              sub: "No response",
            },
            {
              title: "Last Check",
              value: "10:45:15 AM",
              sub: "30 sec ago",
            },
          ].map((item, index) => (
            <div className="col-lg col-md-4 col-sm-6" key={index}>
              <div
                className="card border-0 h-100"
                style={{
                  borderRadius: "18px",
                  boxShadow: "0 2px 10px rgba(15,23,42,0.05)",
                }}
              >
                <div className="card-body">
                  <small
                    style={{
                      color: "#94a3b8",
                      fontWeight: 600,
                      fontSize: "12px",
                    }}
                  >
                    {item.title}
                  </small>

                  <h4
                    className="mt-2 mb-1"
                    style={{
                      fontWeight: 700,
                      color: item.color || "#0f172a",
                    }}
                  >
                    {item.value}
                  </h4>

                  <small style={{ color: "#64748b" }}>
                    {item.sub}
                  </small>
                </div>
              </div>
            </div>
          ))}
        </div>

        {/* Timeline + Quick Info */}
        <div className="row g-4 mb-4">
          {/* Timeline */}
          <div className="col-xl-8">
            <div
              className="card border-0 h-100"
              style={{
                borderRadius: "20px",
                boxShadow: "0 2px 10px rgba(15,23,42,0.05)",
              }}
            >
              <div className="card-body p-4">
                <div className="d-flex justify-content-between align-items-center mb-4">
                  <div>
                    <h5
                      className="mb-1"
                      style={{
                        fontWeight: 700,
                        color: "#0f172a",
                      }}
                    >
                      Status Timeline
                    </h5>

                    <small style={{ color: "#64748b" }}>
                      Live uptime history
                    </small>
                  </div>

                  <small
                    style={{
                      color: "#64748b",
                      fontWeight: 600,
                    }}
                  >
                    Today
                  </small>
                </div>

                <div
                  className="d-flex flex-wrap gap-2"
                  style={{
                    padding: "10px 0",
                  }}
                >
                  {timeline.map((item, index) => (
                    <div
                      key={index}
                      title={item}
                      style={{
                        width: "11px",
                        height: "28px",
                        borderRadius: "20px",
                        background:
                          item === "up"
                            ? "#22c55e"
                            : "#ef4444",
                      }}
                    />
                  ))}
                </div>

                <div className="d-flex gap-4 mt-4">
                  <div className="d-flex align-items-center gap-2">
                    <div
                      style={{
                        width: "10px",
                        height: "10px",
                        borderRadius: "50%",
                        background: "#22c55e",
                      }}
                    />
                    <small style={{ color: "#64748b" }}>Up</small>
                  </div>

                  <div className="d-flex align-items-center gap-2">
                    <div
                      style={{
                        width: "10px",
                        height: "10px",
                        borderRadius: "50%",
                        background: "#ef4444",
                      }}
                    />
                    <small style={{ color: "#64748b" }}>Down</small>
                  </div>
                </div>
              </div>
            </div>
          </div>

          {/* Quick Info */}
          <div className="col-xl-4">
            <div
              className="card border-0 h-100"
              style={{
                borderRadius: "20px",
                boxShadow: "0 2px 10px rgba(15,23,42,0.05)",
              }}
            >
              <div className="card-body p-4">
                <h5
                  className="mb-4"
                  style={{
                    fontWeight: 700,
                    color: "#0f172a",
                  }}
                >
                  Quick Info
                </h5>

                {[
                  ["Service Name", "Pan Service"],
                  ["Service Type", "Internal Microservice"],
                  ["Environment", "Production"],
                  ["Base URL", "http://pan-service:8080"],
                  ["Health API", "/api/v1/pan/health"],
                ].map(([label, value], index) => (
                  <div
                    key={index}
                    className="d-flex justify-content-between align-items-start mb-3"
                  >
                    <small
                      style={{
                        color: "#64748b",
                        fontWeight: 500,
                      }}
                    >
                      {label}
                    </small>

                    <small
                      style={{
                        color: "#0f172a",
                        fontWeight: 600,
                        textAlign: "right",
                        flex: 1,
                        whiteSpace: "nowrap",
                        overflow: "hidden",
                        textOverflow: "ellipsis"
                      }}
                    >
                      {value}
                    </small>
                  </div>
                ))}
              </div>
            </div>
          </div>
        </div>

        {/* Bottom Cards */}
        <div className="row g-4">
          {/* Status Changes */}
          <div className="col-xl-6">
            <div
              className="card border-0 h-100"
              style={{
                borderRadius: "20px",
                boxShadow: "0 2px 10px rgba(15,23,42,0.05)",
              }}
            >
              <div className="card-body p-4">
                <div className="d-flex justify-content-between align-items-center mb-4">
                  <h5
                    className="mb-0"
                    style={{
                      fontWeight: 700,
                      color: "#0f172a",
                    }}
                  >
                    Status Changes
                  </h5>

                  <small
                    style={{
                      color: "#667eea",
                      fontWeight: 600,
                      cursor: "pointer",
                    }}
                  >
                    View History →
                  </small>
                </div>

                <div className="table-responsive">
                  <table className="table align-middle">
                    <thead>
                      <tr>
                        <th>Status</th>
                        <th>Time</th>
                        <th>Duration</th>
                      </tr>
                    </thead>

                    <tbody>
                      <tr>
                        <td>
                          <span className="badge bg-danger">
                            Down
                          </span>
                        </td>
                        <td>10:32:15 AM</td>
                        <td>13m 15s</td>
                      </tr>

                      <tr>
                        <td>
                          <span className="badge bg-success">
                            Up
                          </span>
                        </td>
                        <td>10:21:08 AM</td>
                        <td>11m 07s</td>
                      </tr>

                      <tr>
                        <td>
                          <span className="badge bg-danger">
                            Down
                          </span>
                        </td>
                        <td>10:10:14 AM</td>
                        <td>10m 54s</td>
                      </tr>
                    </tbody>
                  </table>
                </div>
              </div>
            </div>
          </div>

          {/* Recent Logs */}
          <div className="col-xl-6">
            <div
              className="card border-0 h-100"
              style={{
                borderRadius: "20px",
                boxShadow: "0 2px 10px rgba(15,23,42,0.05)",
              }}
            >
              <div className="card-body p-4">
                <div className="d-flex justify-content-between align-items-center mb-4">
                  <h5
                    className="mb-0"
                    style={{
                      fontWeight: 700,
                      color: "#0f172a",
                    }}
                  >
                    Recent Logs
                  </h5>

                  <small
                    style={{
                      color: "#667eea",
                      cursor: "pointer",
                      fontWeight: 600,
                    }}
                  >
                    View All →
                  </small>
                </div>

                {logs.map((log, index) => (
                  <div
                    key={index}
                    className="d-flex align-items-center justify-content-between py-3 border-bottom"
                  >
                    <small
                      style={{
                        color: "#64748b",
                        minWidth: "90px",
                      }}
                    >
                      {log.time}
                    </small>

                    <span
                      className="badge"
                      style={{
                        minWidth: "70px",
                        background:
                          log.type === "ERROR"
                            ? "#fee2e2"
                            : log.type === "WARN"
                            ? "#fef3c7"
                            : "#dbeafe",

                        color:
                          log.type === "ERROR"
                            ? "#dc2626"
                            : log.type === "WARN"
                            ? "#d97706"
                            : "#2563eb",
                      }}
                    >
                      {log.type}
                    </span>

                    <small
                      style={{
                        color: "#0f172a",
                        fontWeight: 500,
                        width: "60%",
                        textAlign: "right",
                      }}
                    >
                      {log.message}
                    </small>
                  </div>
                ))}
              </div>
            </div>
          </div>
        </div>
      </div>
    </>
  );
}