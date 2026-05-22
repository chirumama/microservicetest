import { useState, useEffect } from "react";
import { useNavigate } from "react-router-dom";
import { useAuth } from "../../context/AuthContext";
import CreateApplicationModal from "./CreateApplication";
import { FiSettings, FiPlus } from "react-icons/fi";
import NavBar from "../../components/common/NavBar";
import { getApplications, type ApplicationSummary } from "../../services/api";

export default function ManageApplications() {
  const navigate = useNavigate();
  const [showModal, setShowModal] = useState(false);
  const [apps, setApps] = useState<ApplicationSummary[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  const { user } = useAuth();
  const username = user?.email?.split("@")[0] || "User";

  const fetchApps = async () => {
    setLoading(true);
    setError("");

    try {
      const data = await getApplications();
      setApps(data);
    } catch (err: unknown) {
      setError(
        err instanceof Error
          ? err.message
          : "Failed to load applications"
      );
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchApps();
  }, []);

  return (
    <>
      <NavBar username={username} />

      <div className="container py-3 py-md-4">

        {/* Header */}
        <div className="d-flex flex-column flex-md-row justify-content-between align-items-start align-items-md-center gap-3 mb-4">
          <div>
            <button
              className="p-0 mb-3"
              style={{
                border: "none",
                background: "none",
              }}
              onClick={() => navigate("/dashboard")}
            >
              ← Back to Dashboard
            </button>

            <h3 className="fw-bold mb-1">
              Manage Applications
            </h3>

            <p className="text-muted mb-0">
              View and manage all applications consuming your microservices
            </p>
          </div>

          <button
            className="btn text-white d-flex align-items-center justify-content-center w-100 w-md-auto"
            style={{
              borderRadius: "10px",
              padding: "10px 15px",
              background: "#667eea",
              border: "none",
              gap: "6px",
              maxWidth: "260px",
            }}
            onClick={() => setShowModal(true)}
          >
            <FiPlus /> Create New Application
          </button>
        </div>

        {error && (
          <div className="alert alert-danger">
            {error}
          </div>
        )}

        {loading ? (
          <div className="text-center mt-5 text-muted">
            Loading applications...
          </div>
        ) : apps.length === 0 ? (
          <div className="d-flex justify-content-center mt-5">
            <div
              className="card text-center shadow-sm"
              style={{
                width: "100%",
                borderRadius: "14px",
                padding: "30px 20px",
                border: "1px solid #eee",
              }}
            >
              <h5 className="fw-semibold mb-2">
                No Applications Yet
              </h5>

              <p
                className="text-muted mb-3"
                style={{ fontSize: "14px" }}
              >
                You haven't created any applications yet. Start by creating your first application.
              </p>

              <button
                className="d-flex align-items-center justify-content-center text-white mx-auto w-100"
                style={{
                  borderRadius: "10px",
                  background: "#667eea",
                  border: "none",
                  padding: "10px 18px",
                  fontWeight: "400",
                  gap: "6px",
                  maxWidth: "280px",
                }}
                onClick={() => setShowModal(true)}
              >
                <FiPlus /> Create Your First Application
              </button>
            </div>
          </div>
        ) : (
          <div className="row g-4">
            {apps.map((app) => (
              <div
                key={app.id}
                className="col-12 col-sm-6 col-lg-4"
              >
                <div
                  className="card h-100 shadow-sm"
                  style={{
                    borderRadius: "14px",
                    overflow: "hidden",
                    border: "1px solid #eee",
                  }}
                >
                  <div className="p-3">
                    <h5 className="fw-semibold mb-2">
                      {app.title}
                    </h5>

                    <p className="text-muted mb-1">
                      {app.description}
                    </p>

                    {app.ownerEmail && (
                      <p
                        className="text-muted mb-2"
                        style={{
                          fontSize: 12,
                          wordBreak: "break-word",
                        }}
                      >
                        Owner: {app.ownerEmail}
                      </p>
                    )}

                    <span
                      style={{
                        backgroundColor: "#d1f7dc",
                        color: "#1e7e34",
                        padding: "6px 12px",
                        borderRadius: "20px",
                        fontSize: "13px",
                        fontWeight: "500",
                        display: "inline-block",
                      }}
                    >
                      Active
                    </span>
                  </div>

                  <hr className="m-0" style={{ color: "grey" }} />

                  <div className="p-3 mt-auto">
                    <button
                      onClick={() => navigate(`/manage/${app.id}`)}
                      className="w-100 d-flex align-items-center justify-content-center text-white"
                      style={{
                        borderRadius: "10px",
                        background: "#667eea",
                        border: "none",
                        padding: "10px",
                        fontWeight: "500",
                        gap: "6px",
                      }}
                    >
                      <FiSettings /> Manage
                    </button>
                  </div>
                </div>
              </div>
            ))}
          </div>
        )}

        <CreateApplicationModal
          show={showModal}
          onClose={() => setShowModal(false)}
          onCreated={() => {
            setShowModal(false);
            fetchApps();
          }}
        />
      </div>
    </>
  );
}