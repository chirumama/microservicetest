import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { FaCogs, FaSyncAlt } from "react-icons/fa";

import { useAuth } from "../../context/AuthContext";
import NavBar from "../../components/common/NavBar";
import { MICROSERVICES } from "../../config/microservices";

type HealthStatus = "Checking" | "Healthy" | "Down";

interface ServiceHealth {
  id: number;
  name: string;
  status: HealthStatus;
}

async function checkHealth(
  gatewayBaseUrl: string,
  healthPath: string
): Promise<HealthStatus> {
  try {
    const token = localStorage.getItem("gatewayToken");

    const res = await fetch(`${gatewayBaseUrl}${healthPath}`, {
      method: "GET",
      headers: token ? { Authorization: `Bearer ${token}` } : {},
      signal: AbortSignal.timeout(5000),
    });

    if (!res.ok) return "Down";

    const data = await res.json();
    const s = (data?.status ?? data?.Status ?? "").toLowerCase();

    return s === "" || s === "healthy" ? "Healthy" : "Down";
  } catch {
    return "Down";
  }
}

export default function Dashboard() {
  const navigate = useNavigate();
  const { user } = useAuth();
  const username = user?.email?.split("@")[0] || "User";

  const [services, setServices] = useState<ServiceHealth[]>(
    MICROSERVICES.map((s) => ({
      id: s.id,
      name: s.name,
      status: "Checking",
    }))
  );

  const [refreshing, setRefreshing] = useState(false);

  async function runHealthChecks() {
    setRefreshing(true);

    setServices(
      MICROSERVICES.map((s) => ({
        id: s.id,
        name: s.name,
        status: "Checking",
      }))
    );

    await Promise.all(
      MICROSERVICES.map(async (s) => {
        const status = await checkHealth(
          s.gatewayBaseUrl,
          s.healthPath
        );

        setServices((prev) =>
          prev.map((svc) =>
            svc.id === s.id
              ? { ...svc, status }
              : svc
          )
        );
      })
    );

    setRefreshing(false);
  }

  useEffect(() => {
    runHealthChecks();
  }, []);

  function statusColor(status: HealthStatus) {
    if (status === "Checking") return "#f59e0b";
    return status === "Healthy"
      ? "#28a745"
      : "#dc3545";
  }

  return (
    <>
      <NavBar username={username} />

      <div className="dashboard-container p-3 p-md-4">

        {/* Header */}
        <div className="d-flex flex-column flex-xl-row justify-content-between align-items-start align-items-xl-center gap-3 mb-4">

          <h2 className="mb-0">Microservices</h2>

          {/* Buttons */}
          <div
  className="d-flex flex-column flex-sm-row flex-wrap gap-2 ms-auto"
>

            {/* Refresh */}
            <button
              className="btn btn-outline-secondary d-flex align-items-center justify-content-center"
              onClick={runHealthChecks}
              disabled={refreshing}
              style={{
                borderRadius: "10px",
                padding: "10px 18px",
                fontWeight: 500,
              }}
            >
              <FaSyncAlt
                className="me-2"
                style={{
                  animation: refreshing
                    ? "spin 1s linear infinite"
                    : "none",
                }}
              />

              {refreshing
                ? "Checking..."
                : "Refresh"}
            </button>

            {/* Manage Applications */}
            <button
              className="btn btn-dark d-flex align-items-center justify-content-center"
              onClick={() => navigate("/manage")}
              style={{
                borderRadius: "10px",
                padding: "10px 18px",
                fontWeight: 500,
              }}
            >
              <FaCogs className="me-2" />
              Manage Applications
            </button>

            {/* Logs */}
            <button
              className="btn btn-dark d-flex align-items-center justify-content-center"
              onClick={() => navigate("/application-logs")}
              style={{
                borderRadius: "10px",
                padding: "10px 18px",
                fontWeight: 500,
              }}
            >
              Application Logs
            </button>
          </div>
        </div>

        {/* Cards */}
        <div className="row g-4">
          {services.map((service) => (
            <div
              className="col-12 col-sm-6 col-lg-4"
              key={service.id}
            >
              <div
                className="card border-0 shadow-sm h-100"
                style={{ borderRadius: "18px" }}
              >
                <div className="card-body d-flex flex-column justify-content-between">

                  <div>

                    {/* Service Name */}
                    <h5
                      className="card-title mb-2"
                      style={{ fontWeight: 600 }}
                    >
                      {service.name}
                    </h5>

                    {/* Status */}
                    <div className="d-flex align-items-center gap-2 mb-4">
                      <div
                        style={{
                          width: "12px",
                          height: "12px",
                          borderRadius: "50%",
                          backgroundColor: statusColor(service.status),
                          animation:
                            service.status === "Checking"
                              ? "pulse 1.2s ease-in-out infinite"
                              : "none",
                          flexShrink: 0,
                        }}
                      />

                      <span
                        style={{
                          fontSize: "14px",
                          fontWeight: 500,
                          color: statusColor(service.status),
                          wordBreak: "break-word",
                        }}
                      >
                        {service.status}
                      </span>
                    </div>

                    {/* Down Since + Uptime */}
                    <div className="row g-3 mb-4">

                      {/* Down Since */}
                      <div className="col-6">
                        <div
                          style={{
                            fontSize: "13px",
                            color: "#6b7280",
                            marginBottom: "4px",
                          }}
                        >
                          Down since
                        </div>

                        <div
                          style={{
                            fontSize: "clamp(18px, 2vw, 22px)",
                            fontWeight: 700,
                            lineHeight: 1.2,
                            wordBreak: "break-word",
                          }}
                        >
                          10:32 AM
                        </div>
                      </div>

                      {/* Uptime */}
                      <div className="col-6 text-end">
                        <div
                          style={{
                            fontSize: "13px",
                            color: "#6b7280",
                            marginBottom: "4px",
                          }}
                        >
                          Uptime (24h)
                        </div>

                        <div
                          style={{
                            fontSize: "clamp(18px, 2vw, 22px)",
                            fontWeight: 700,
                            lineHeight: 1.2,
                          }}
                        >
                          32.5%
                        </div>
                      </div>
                    </div>
                  </div>

                  {/* Button */}
                  <button
                    className="btn w-100 text-white"
                    onClick={() =>
                      navigate(`/microservice/${service.id}`)
                    }
                    style={{
                      background: "#667eea",
                      borderRadius: "10px",
                      fontWeight: 500,
                      border: "none",
                    }}
                  >
                    View Details
                  </button>
                </div>
              </div>
            </div>
          ))}
        </div>
      </div>

      {/* Animations */}
      <style>{`
        @keyframes pulse {
          0%, 100% {
            opacity: 1;
            transform: scale(1);
          }

          50% {
            opacity: 0.35;
            transform: scale(0.8);
          }
        }

        @keyframes spin {
          from {
            transform: rotate(0deg);
          }

          to {
            transform: rotate(360deg);
          }
        }
      `}</style>
    </>
  );
}