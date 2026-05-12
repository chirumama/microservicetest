import { useNavigate } from "react-router-dom";
// import { IoList } from "react-icons/io5";
import { FaCogs } from "react-icons/fa";

import { useAuth } from "../../context/AuthContext";
import NavBar from "../../components/common/NavBar";

export default function Dashboard() {
  const navigate = useNavigate();

  const { user } = useAuth();

  // Username from email
  const username =
    user?.email?.split("@")[0] || "User";

  // Dummy Microservices Data
  const microservices = [
    {
      id: 1,
      name: "Pan Service",
      status: "Healthy",
    },
    {
      id: 2,
      name: "Passport Service",
      status: "Healthy",
    },
    {
      id: 3,
      name: "GST Service",
      status: "Down",
    },
    {
      id: 4,
      name: "IP Lookup Service",
      status: "Healthy",
    },
  ];

  return (
    <>
      {/* Navbar */}
      <NavBar username={username} />

      {/* Dashboard Content */}
      <div className="dashboard-container p-4">
        {/* Header */}
        <div className="d-flex justify-content-between align-items-center mb-4">
          <div>
            <h2 className="mb-1">
              Microservices
            </h2>

          </div>

          {/* Separate Manage Button */}
          <button
            className="btn btn-dark d-flex align-items-center"
            onClick={() =>
              navigate("/manage")
            }
            style={{
              borderRadius: "10px",
              padding: "10px 18px",
              fontWeight: 500,
            }}
          >
            <FaCogs className="me-2" />
            Manage Applications
          </button>
        </div>

        {/* Microservice Cards */}
        <div className="row g-4">
          {microservices.map((service) => (
            <div
              className="col-md-6 col-lg-4"
              key={service.id}
            >
              <div
                className="card border-0 shadow-sm h-100"
                style={{
                  borderRadius: "18px",
                }}
              >
                <div className="card-body d-flex flex-column justify-content-between">
                  {/* Top Section */}
                  <div>
                    {/* Service Name */}
                    <h5
                      className="card-title mb-3"
                      style={{
                        fontWeight: 600,
                      }}
                    >
                      {service.name}
                    </h5>

                    {/* Health Indicator */}
                    <div className="d-flex align-items-center gap-2 mb-4">
                      <div
                        style={{
                          width: "12px",
                          height: "12px",
                          borderRadius: "50%",
                          backgroundColor:
                            service.status ===
                            "Healthy"
                              ? "#28a745"
                              : "#dc3545",
                        }}
                      />

                      <span
                        style={{
                          fontSize: "14px",
                          fontWeight: 500,
                          color:
                            service.status ===
                            "Healthy"
                              ? "#28a745"
                              : "#dc3545",
                        }}
                      >
                        {service.status}
                      </span>
                    </div>
                  </div>

                  {/* View Button */}
                  <button
                    className="btn btn-primary w-100"
                    onClick={() =>
                      navigate(
                        `/microservice/${service.id}`
                      )
                    }
                    style={{
                      background: "#667eea",
                      borderRadius: "10px",
                      fontWeight: 500,
                    }}
                  >
                    {/* <IoList className="me-2" /> */}
                    View Details
                  </button>
                </div>
              </div>
            </div>
          ))}
        </div>
      </div>
    </>
  );
}