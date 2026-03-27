import { useState, useEffect, type ChangeEvent } from "react";
import { FaEnvelope, FaLock } from "react-icons/fa";
import { IoEyeOutline, IoEyeOffOutline } from "react-icons/io5";
import InputField from "../../components/common/InputField";
import { createUser, getUsers, type UserSummary } from "../../services/api";
import { useAuth } from "../../context/AuthContext";
import { useNavigate } from "react-router-dom";

export default function SuperAdminDashboard() {
  const [tab, setTab] = useState<"create" | "manage">("create");

  // Create user form state
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [roleId, setRoleId] = useState("");
  const [showPassword, setShowPassword] = useState(false);
  const [loading, setLoading] = useState(false);
  const [successMsg, setSuccessMsg] = useState("");
  const [errors, setErrors] = useState<{
    email?: string; password?: string; roleId?: string; api?: string;
  }>({});

  // Users list state
  const [users, setUsers] = useState<UserSummary[]>([]);
  const [usersLoading, setUsersLoading] = useState(false);

  const { logout } = useAuth();
  const navigate = useNavigate();

  const fetchUsers = async () => {
    setUsersLoading(true);
    try {
      const data = await getUsers();
      setUsers(data);
    } catch {
      // silently fail
    } finally {
      setUsersLoading(false);
    }
  };

  useEffect(() => {
    if (tab === "manage") fetchUsers();
  }, [tab]);

  const handleCreateUser = async () => {
    const newErrors: typeof errors = {};
    if (!email.trim()) newErrors.email = "Email is required";
    if (!password.trim()) newErrors.password = "Password is required";
    if (!roleId) newErrors.roleId = "Role is required";
    if (Object.keys(newErrors).length) { setErrors(newErrors); return; }

    setErrors({}); setLoading(true); setSuccessMsg("");
    try {
      await createUser(email, password, parseInt(roleId));
      setEmail(""); setPassword(""); setRoleId("");
      setSuccessMsg("User created successfully!");
    } catch (err: unknown) {
      setErrors({ api: err instanceof Error ? err.message : "Failed to create user" });
    } finally {
      setLoading(false);
    }
  };

  const tabBtn = (active: boolean) => ({
    borderRadius: 10,
    padding: "8px 28px",
    background: active ? "linear-gradient(135deg,#4c7df0,#2d6cdf)" : "#f3f3f3",
    color: active ? "#fff" : "#333",
    border: "none",
    fontWeight: 500,
    cursor: "pointer",
  } as React.CSSProperties);

  return (
    <div className="container mt-4">

      {/* Header */}
      <div className="d-flex justify-content-between align-items-center mb-4">
        <h3 className="fw-bold mb-0">SuperAdmin Panel</h3>
        <button
          className="btn btn-sm btn-outline-danger"
          onClick={() => { logout(); navigate("/"); }}
        >
          Logout
        </button>
      </div>

      {/* Tab buttons */}
      <div className="d-flex gap-2 mb-4">
        <button style={tabBtn(tab === "create")} onClick={() => setTab("create")}>
          Create User
        </button>
        <button style={tabBtn(tab === "manage")} onClick={() => setTab("manage")}>
          All Users
        </button>
      </div>

      {/* ── Create User Tab ── */}
      {tab === "create" && (
        <div style={{ maxWidth: 460 }}>
          {errors.api && (
            <div className="alert alert-danger py-2 mb-3" style={{ fontSize: 14 }}>
              {errors.api}
            </div>
          )}
          {successMsg && (
            <div className="alert alert-success py-2 mb-3" style={{ fontSize: 14 }}>
              {successMsg}
            </div>
          )}

          <InputField
            label="Email" type="email" value={email}
            onChange={(e: ChangeEvent<HTMLInputElement>) => {
              setEmail(e.target.value);
              if (errors.email) setErrors(p => ({ ...p, email: "" }));
            }}
            placeholder="Enter email..." fullWidth required
            error={!!errors.email} helperText={errors.email}
            startIcon={<FaEnvelope style={{ color: "gray" }} />}
          />
          <br /><br />

          <InputField
            label="Password" type={showPassword ? "text" : "password"} value={password}
            onChange={(e: ChangeEvent<HTMLInputElement>) => {
              setPassword(e.target.value);
              if (errors.password) setErrors(p => ({ ...p, password: "" }));
            }}
            placeholder="Enter password..." fullWidth required
            error={!!errors.password} helperText={errors.password}
            startIcon={<FaLock style={{ color: "gray" }} />}
            endIcon={
              <span onClick={() => setShowPassword(!showPassword)} style={{ cursor: "pointer" }}>
                {showPassword ? <IoEyeOutline /> : <IoEyeOffOutline />}
              </span>
            }
          />
          <br /><br />

          <InputField
            label="Role" value={roleId}
            onChange={(e) => {
              setRoleId(e.target.value);
              if (errors.roleId) setErrors(p => ({ ...p, roleId: "" }));
            }}
            fullWidth required
            error={!!errors.roleId} helperText={errors.roleId}
            select
            options={[
              { label: "Select Role", value: "" },
              { label: "User",        value: "1" },
              { label: "Admin",       value: "2" },
            ]}
          />
          <br /><br />

          <button className="login-btn" onClick={handleCreateUser} disabled={loading}>
            {loading ? "Creating..." : "Create User"}
          </button>
        </div>
      )}

      {/* ── All Users Tab ── */}
      {tab === "manage" && (
        <div>
          {usersLoading ? (
            <p className="text-muted">Loading users...</p>
          ) : users.length === 0 ? (
            <p className="text-muted">No users found.</p>
          ) : (
            <div className="card shadow-sm" style={{ borderRadius: 12, overflow: "hidden" }}>
              <table className="table mb-0">
                <thead style={{ background: "#f8f9fa" }}>
                  <tr>
                    <th style={{ padding: "12px 16px" }}>#</th>
                    <th style={{ padding: "12px 16px" }}>Email</th>
                    <th style={{ padding: "12px 16px" }}>Role</th>
                    <th style={{ padding: "12px 16px" }}>Status</th>
                    <th style={{ padding: "12px 16px" }}>Created</th>
                  </tr>
                </thead>
                <tbody>
                  {users.map((u, i) => (
                    <tr key={u.id}>
                      <td style={{ padding: "12px 16px", verticalAlign: "middle" }}>
                        {i + 1}
                      </td>
                      <td style={{ padding: "12px 16px", verticalAlign: "middle" }}>
                        {u.email}
                      </td>
                      <td style={{ padding: "12px 16px", verticalAlign: "middle" }}>
                        <span style={{
                          padding: "4px 12px", borderRadius: 20, fontSize: 12, fontWeight: 500,
                          background:
                            u.role === "SuperAdmin" ? "#e8d5ff" :
                            u.role === "Admin"      ? "#d1e8ff" : "#d1f7dc",
                          color:
                            u.role === "SuperAdmin" ? "#6b21a8" :
                            u.role === "Admin"      ? "#1e40af" : "#1e7e34",
                        }}>
                          {u.role}
                        </span>
                      </td>
                      <td style={{ padding: "12px 16px", verticalAlign: "middle" }}>
                        <span style={{
                          padding: "4px 12px", borderRadius: 20, fontSize: 12, fontWeight: 500,
                          background: u.isActive ? "#d1f7dc" : "#fee2e2",
                          color:      u.isActive ? "#1e7e34" : "#dc2626",
                        }}>
                          {u.isActive ? "Active" : "Inactive"}
                        </span>
                      </td>
                      <td style={{ padding: "12px 16px", verticalAlign: "middle", fontSize: 13, color: "#667085" }}>
                        {new Date(u.createdAt).toLocaleDateString("en-IN", {
                          day: "2-digit", month: "short", year: "numeric"
                        })}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>

              {/* Footer count */}
              <div style={{
                padding: "10px 16px", background: "#f8f9fa",
                fontSize: 13, color: "#667085", borderTop: "1px solid #eee"
              }}>
                Total users: <strong>{users.length}</strong>
              </div>
            </div>
          )}
        </div>
      )}

    </div>
  );
}