import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { FaPlus } from "react-icons/fa";
import { IoList } from "react-icons/io5";
import { useAuth } from "../../context/AuthContext";
import CreateApplicationModal from "../applications/CreateApplication";

export default function Dashboard(): JSX.Element {
  const navigate = useNavigate();
  const [showModal, setShowModal] = useState(false);
  const { user, logout } = useAuth();

  return (
    <div className="dashboard-container">
      <div className="d-flex justify-content-between align-items-center mb-4">
        <h2 className="mb-0">Microservices Dashboard</h2>
        <div className="d-flex align-items-center gap-3">
          <span style={{ fontSize: 14, opacity: 0.85 }}>{user?.email}</span>
          <button
            className="btn btn-sm btn-light"
            onClick={() => { logout(); navigate("/"); }}
          >
            Logout
          </button>
        </div>
      </div>

      <div className="action-card">
        <h5>Applications</h5>
        <p>Create and manage your applications</p>

        <div className="btn-group-custom">
          <button className="btn-create" onClick={() => setShowModal(true)}>
            <FaPlus className="me-2" /> Create New
          </button>
          <button className="btn-manage" onClick={() => navigate("/manage")}>
            <IoList className="me-2" /> Manage
          </button>
        </div>
      </div>

      <CreateApplicationModal
        show={showModal}
        onClose={() => setShowModal(false)}
        onCreated={() => { setShowModal(false); navigate("/manage"); }}
      />
    </div>
  );
}
