import React, { useState, useEffect, type ChangeEvent } from "react";
import { FaEnvelope, FaLock, FaSearch, FaChevronDown, FaChevronRight } from "react-icons/fa";
import { IoEyeOutline, IoEyeOffOutline } from "react-icons/io5";
import {
  FiRefreshCw,
  FiDownload,
  FiTrash2,
} from "react-icons/fi";

import InputField from "../../components/common/InputField";
import { createUser, getUsers, type UserSummary } from "../../services/api";
import { useAuth } from "../../context/AuthContext";
import { useNavigate } from "react-router-dom";

import {
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  FormHelperText,
} from "@mui/material";

// Password regex
const PASSWORD_REGEX =
  /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$/;

const PASSWORD_HINT =
  "Must be 8+ chars and include an uppercase letter, lowercase letter, number, and special character (@$!%*?&).";

export default function SuperAdminDashboard() {
  const [tab, setTab] = useState<"create" | "manage" | "logs">("create");

  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [roleId, setRoleId] = useState("");
  const [showPassword, setShowPassword] = useState(false);
  const [loading, setLoading] = useState(false);
  const [successMsg, setSuccessMsg] = useState("");

  const [errors, setErrors] = useState<{
    email?: string;
    password?: string;
    roleId?: string;
    api?: string;
  }>({});

  const [users, setUsers] = useState<UserSummary[]>([]);
  const [usersLoading, setUsersLoading] = useState(false);

  const [expandedRow, setExpandedRow] = useState<number | null>(null);

  const { logout } = useAuth();
  const navigate = useNavigate();

  const fetchUsers = async () => {
    setUsersLoading(true);

    try {
      const data = await getUsers();
      setUsers(data);
    } catch {
      //
    } finally {
      setUsersLoading(false);
    }
  };

  useEffect(() => {
    if (tab === "manage") {
      fetchUsers();
    }
  }, [tab]);

  const handleCreateUser = async () => {
    const newErrors: typeof errors = {};

    if (!email.trim()) {
      newErrors.email = "Email is required";
    }

    if (!password.trim()) {
      newErrors.password = "Password is required";
    } else if (!PASSWORD_REGEX.test(password)) {
      newErrors.password = PASSWORD_HINT;
    }

    if (!roleId) {
      newErrors.roleId = "Role is required";
    }

    if (Object.keys(newErrors).length) {
      setErrors(newErrors);
      return;
    }

    setErrors({});
    setLoading(true);
    setSuccessMsg("");

    try {
      await createUser(email, password, parseInt(roleId));

      setEmail("");
      setPassword("");
      setRoleId("");

      setSuccessMsg("User created successfully!");
    } catch (err: unknown) {
      setErrors({
        api:
          err instanceof Error
            ? err.message
            : "Failed to create user",
      });
    } finally {
      setLoading(false);
    }
  };

  const tabBtn = (active: boolean) =>
    ({
      borderRadius: 10,
      padding: "8px 28px",
      background: active
        ? "linear-gradient(135deg,#4c7df0,#2d6cdf)"
        : "#f3f3f3",
      color: active ? "#fff" : "#333",
      border: "none",
      fontWeight: 500,
      cursor: "pointer",
    } as React.CSSProperties);

  // Dummy access logs data
  const accessLogs = [
  {
    id: 1,
    timestamp: "14 May 2026, 12:11:10",
    actor: "shreyaraut061@gmail.com",
    role: "Admin",
    service: "Pan Service",
    method: "POST",
    endpoint: "/api/v1/pan/verify",
    appId: "lending-app-v1",
    status: "200",
    outcome: "SUCCESS",
    latency: "200ms",
    fields: "4 fields",
    type: "success",
    responseFields: [
      "id_number",
      "full_name",
      "pan_status",
      "timestamp",
    ],
  },

  {
    id: 2,
    timestamp: "14 May 2026, 12:09:48",
    actor: "shreyaraut061@gmail.com",
    role: "Admin",
    service: "Pan Service",
    method: "GET",
    endpoint: "/api/v1/pan/health",
    appId: "gateway-v1",
    status: "200",
    outcome: "SUCCESS",
    latency: "74ms",
    fields: "2 fields",
    type: "success",
    responseFields: [
      "service_status",
      "timestamp",
    ],
  },

  {
    id: 3,
    timestamp: "14 May 2026, 12:08:31",
    actor: "admin@example.com",
    role: "Admin",
    service: "Passport Service",
    method: "POST",
    endpoint: "/api/passport/verify",
    appId: "passport-app-v1",
    status: "201",
    outcome: "SUCCESS",
    latency: "371ms",
    fields: "5 fields",
    type: "success",
    responseFields: [
      "file_number",
      "passport_status",
      "citizenship",
      "full_name",
      "timestamp",
    ],
  },

  {
    id: 4,
    timestamp: "14 May 2026, 12:07:20",
    actor: "admin@example.com",
    role: "Admin",
    service: "Passport Service",
    method: "GET",
    endpoint: "/api/passport/health/db",
    appId: "passport-app-v1",
    status: "200",
    outcome: "SUCCESS",
    latency: "98ms",
    fields: "2 fields",
    type: "success",
    responseFields: [
      "database_status",
      "timestamp",
    ],
  },

  {
    id: 5,
    timestamp: "14 May 2026, 12:05:53",
    actor: "admin@example.com",
    role: "Admin",
    service: "GST Service",
    method: "POST",
    endpoint: "/api/gst/verify",
    appId: "gst-app-v1",
    status: "200",
    outcome: "SUCCESS",
    latency: "240ms",
    fields: "4 fields",
    type: "success",
    responseFields: [
      "gstin",
      "business_name",
      "gst_status",
      "timestamp",
    ],
  },

  {
    id: 6,
    timestamp: "14 May 2026, 12:04:12",
    actor: "user@example.com",
    role: "User",
    service: "GST Service",
    method: "GET",
    endpoint: "/api/gst/health",
    appId: "gst-app-v1",
    status: "200",
    outcome: "SUCCESS",
    latency: "61ms",
    fields: "2 fields",
    type: "success",
    responseFields: [
      "service_status",
      "timestamp",
    ],
  },

  {
    id: 7,
    timestamp: "14 May 2026, 12:03:40",
    actor: "user@example.com",
    role: "User",
    service: "IP Lookup Service",
    method: "GET",
    endpoint: "/v1/iplookup/8.8.8.8",
    appId: "network-app-v1",
    status: "200",
    outcome: "SUCCESS",
    latency: "110ms",
    fields: "5 fields",
    type: "success",
    responseFields: [
      "ip",
      "city",
      "country",
      "isp",
      "timestamp",
    ],
  },

  {
    id: 8,
    timestamp: "14 May 2026, 12:02:10",
    actor: "shreyaraut061@gmail.com",
    role: "Admin",
    service: "Pan Service",
    method: "POST",
    endpoint: "/api/v1/pan/verify",
    appId: "lending-app-v1",
    status: "--",
    outcome: "ERROR",
    latency: "2738ms",
    fields: "0",
    type: "error",
    errorMessage:
      "PAN verification failed due to invalid PAN number.",
  },

  {
    id: 9,
    timestamp: "14 May 2026, 12:01:36",
    actor: "admin@example.com",
    role: "Admin",
    service: "Passport Service",
    method: "POST",
    endpoint: "/api/passport/verify",
    appId: "passport-app-v1",
    status: "--",
    outcome: "ERROR",
    latency: "2330ms",
    fields: "0",
    type: "error",
    errorMessage:
      "Passport service timeout. Please try again later.",
  },

  {
    id: 10,
    timestamp: "14 May 2026, 11:59:58",
    actor: "user@example.com",
    role: "User",
    service: "GST Service",
    method: "POST",
    endpoint: "/api/gst/verify",
    appId: "gst-app-v1",
    status: "--",
    outcome: "ERROR",
    latency: "1840ms",
    fields: "0",
    type: "error",
    errorMessage:
      "GSTIN not found in government records.",
  },

  {
    id: 11,
    timestamp: "14 May 2026, 11:58:42",
    actor: "user@example.com",
    role: "User",
    service: "IP Lookup Service",
    method: "GET",
    endpoint: "/v1/iplookup/256.256.256.256",
    appId: "network-app-v1",
    status: "--",
    outcome: "ERROR",
    latency: "430ms",
    fields: "0",
    type: "error",
    errorMessage:
      "Invalid IP address format.",
  },
];

  return (
    <div className="container mt-4">

      {/* Header */}
      <div className="d-flex justify-content-between align-items-center mb-4">
        <h3 className="fw-bold mb-0">SuperAdmin Panel</h3>

        <button
          className="btn btn-sm btn-outline-danger"
          onClick={() => {
            logout();
            navigate("/");
          }}
        >
          Logout
        </button>
      </div>

      {/* Tabs */}
      <div className="d-flex gap-2 mb-4">
        <button
          style={tabBtn(tab === "create")}
          onClick={() => setTab("create")}
        >
          Create User
        </button>

        <button
          style={tabBtn(tab === "manage")}
          onClick={() => setTab("manage")}
        >
          All Users
        </button>

        <button
          style={tabBtn(tab === "logs")}
          onClick={() => setTab("logs")}
        >
          Access Logs
        </button>
      </div>

      {/* CREATE USER */}
      {tab === "create" && (
        <div style={{ maxWidth: 460 }}>
          {errors.api && (
            <div
              className="alert alert-danger py-2 mb-3"
              style={{ fontSize: 14 }}
            >
              {errors.api}
            </div>
          )}

          {successMsg && (
            <div
              className="alert alert-success py-2 mb-3"
              style={{ fontSize: 14 }}
            >
              {successMsg}
            </div>
          )}

          <InputField
            label="Email"
            type="email"
            value={email}
            onChange={(e: ChangeEvent<HTMLInputElement>) => {
              setEmail(e.target.value);

              if (errors.email) {
                setErrors((p) => ({
                  ...p,
                  email: "",
                }));
              }
            }}
            placeholder="Enter email..."
            fullWidth
            required
            error={!!errors.email}
            helperText={errors.email}
            startIcon={<FaEnvelope style={{ color: "gray" }} />}
          />

          <br />
          <br />

          <InputField
            label="Password"
            type={showPassword ? "text" : "password"}
            value={password}
            onChange={(e: ChangeEvent<HTMLInputElement>) => {
              const val = e.target.value;

              setPassword(val);

              if (val && !PASSWORD_REGEX.test(val)) {
                setErrors((p) => ({
                  ...p,
                  password: PASSWORD_HINT,
                }));
              } else {
                setErrors((p) => ({
                  ...p,
                  password: "",
                }));
              }
            }}
            placeholder="Enter password..."
            fullWidth
            required
            error={!!errors.password}
            helperText={errors.password}
            startIcon={<FaLock style={{ color: "gray" }} />}
            endIcon={
              <span
                onClick={() =>
                  setShowPassword(!showPassword)
                }
                style={{ cursor: "pointer" }}
              >
                {showPassword ? (
                  <IoEyeOutline />
                ) : (
                  <IoEyeOffOutline />
                )}
              </span>
            }
          />

          <br />
          <br />

          <FormControl
            fullWidth
            variant="outlined"
            error={!!errors.roleId}
          >
            <InputLabel id="role-label">
              Role
            </InputLabel>

            <Select
              labelId="role-label"
              label="Role"
              value={roleId}
              onChange={(e) => {
                setRoleId(e.target.value);

                if (errors.roleId) {
                  setErrors((p) => ({
                    ...p,
                    roleId: "",
                  }));
                }
              }}
            >
              <MenuItem value="1">User</MenuItem>
              <MenuItem value="2">Admin</MenuItem>
            </Select>

            {errors.roleId && (
              <FormHelperText>
                {errors.roleId}
              </FormHelperText>
            )}
          </FormControl>

          <br />
          <br />

          <button
            className="login-btn"
            onClick={handleCreateUser}
            disabled={loading}
          >
            {loading ? "Creating..." : "Create User"}
          </button>
        </div>
      )}

      {/* ALL USERS */}
      {tab === "manage" && (
        <div>
          {usersLoading ? (
            <p className="text-muted">
              Loading users...
            </p>
          ) : users.length === 0 ? (
            <p className="text-muted">
              No users found.
            </p>
          ) : (
            <div
              className="card shadow-sm"
              style={{
                borderRadius: 12,
                overflow: "hidden",
              }}
            >
              <table className="table mb-0">
                <thead
                  style={{ background: "#f8f9fa" }}
                >
                  <tr>
                    <th style={{ padding: "12px 16px" }}>
                      #
                    </th>
                    <th style={{ padding: "12px 16px" }}>
                      Email
                    </th>
                    <th style={{ padding: "12px 16px" }}>
                      Role
                    </th>
                    <th style={{ padding: "12px 16px" }}>
                      Status
                    </th>
                    <th style={{ padding: "12px 16px" }}>
                      Created
                    </th>
                  </tr>
                </thead>

                <tbody>
                  {users.map((u, i) => (
                    <tr key={u.id}>
                      <td
                        style={{
                          padding: "12px 16px",
                          verticalAlign: "middle",
                        }}
                      >
                        {i + 1}
                      </td>

                      <td
                        style={{
                          padding: "12px 16px",
                          verticalAlign: "middle",
                        }}
                      >
                        {u.email}
                      </td>

                      <td
                        style={{
                          padding: "12px 16px",
                          verticalAlign: "middle",
                        }}
                      >
                        <span
                          style={{
                            padding: "4px 12px",
                            borderRadius: 20,
                            fontSize: 12,
                            fontWeight: 500,
                            background:
                              u.role === "SuperAdmin"
                                ? "#e8d5ff"
                                : u.role === "Admin"
                                ? "#d1e8ff"
                                : "#d1f7dc",
                            color:
                              u.role === "SuperAdmin"
                                ? "#6b21a8"
                                : u.role === "Admin"
                                ? "#1e40af"
                                : "#1e7e34",
                          }}
                        >
                          {u.role}
                        </span>
                      </td>

                      <td
                        style={{
                          padding: "12px 16px",
                          verticalAlign: "middle",
                        }}
                      >
                        <span
                          style={{
                            padding: "4px 12px",
                            borderRadius: 20,
                            fontSize: 12,
                            fontWeight: 500,
                            background: u.isActive
                              ? "#d1f7dc"
                              : "#fee2e2",
                            color: u.isActive
                              ? "#1e7e34"
                              : "#dc2626",
                          }}
                        >
                          {u.isActive
                            ? "Active"
                            : "Inactive"}
                        </span>
                      </td>

                      <td
                        style={{
                          padding: "12px 16px",
                          verticalAlign: "middle",
                          fontSize: 13,
                          color: "#667085",
                        }}
                      >
                        {new Date(
                          u.createdAt
                        ).toLocaleDateString(
                          "en-IN",
                          {
                            day: "2-digit",
                            month: "short",
                            year: "numeric",
                          }
                        )}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>

              <div
                style={{
                  padding: "10px 16px",
                  background: "#f8f9fa",
                  fontSize: 13,
                  color: "#667085",
                  borderTop: "1px solid #eee",
                }}
              >
                Total users:{" "}
                <strong>{users.length}</strong>
              </div>
            </div>
          )}
        </div>
      )}

      {/* ACCESS LOGS */}
      {tab === "logs" && (
        <div>

          {/* Top */}
          <div className="d-flex justify-content-between align-items-start mb-4">
            <div>
              <h2
                style={{
                  fontWeight: 700,
                  marginBottom: 4,
                }}
              >
                Access Logs
              </h2>

              <p
                style={{
                  color: "#667085",
                  fontSize: 14,
                  marginBottom: 0,
                }}
              >
                UIDAI-compliant · field names only · no Aadhaar data stored
              </p>
            </div>

            <div className="d-flex gap-2">
              <button className="btn btn-light border">
                <FiRefreshCw /> Refresh
              </button>

              <button className="btn btn-light border">
                <FiDownload /> Export JSON
              </button>

              <button className="btn btn-danger-subtle border text-danger">
                <FiTrash2 /> Clear
              </button>
            </div>
          </div>

          {/* Cards */}
          <div className="row mb-4">
            <div className="col-md-3">
              <div
                className="card shadow-sm"
                style={{
                  borderRadius: 14,
                  padding: 20,
                  textAlign: "center",
                }}
              >
                <h1
                  style={{
                    fontWeight: 700,
                  }}
                >
                  11
                </h1>

                <p className="text-muted mb-0">
                  Total
                </p>
              </div>
            </div>

            <div className="col-md-3">
              <div
                className="card shadow-sm"
                style={{
                  borderRadius: 14,
                  padding: 20,
                  textAlign: "center",
                  background: "#eefbf3",
                }}
              >
                <h1
                  style={{
                    color: "#198754",
                    fontWeight: 700,
                  }}
                >
                  7
                </h1>

                <p className="text-muted mb-0">
                  Success
                </p>
              </div>
            </div>

            <div className="col-md-3">
              <div
                className="card shadow-sm"
                style={{
                  borderRadius: 14,
                  padding: 20,
                  textAlign: "center",
                  background: "#fff1f2",
                }}
              >
                <h1
                  style={{
                    color: "#dc2626",
                    fontWeight: 700,
                  }}
                >
                  4
                </h1>

                <p className="text-muted mb-0">
                  Errors
                </p>
              </div>
            </div>

            <div className="col-md-3">
              <div
                className="card shadow-sm"
                style={{
                  borderRadius: 14,
                  padding: 20,
                  textAlign: "center",
                  background: "#fffbea",
                }}
              >
                <h1
                  style={{
                    color: "#a16207",
                    fontWeight: 700,
                  }}
                >
                  0
                </h1>

                <p className="text-muted mb-0">
                  Warn
                </p>
              </div>
            </div>
          </div>

          {/* Filters */}
          <div
            className="card shadow-sm mb-3"
            style={{
              borderRadius: 14,
              padding: 16,
            }}
          >
            <div className="d-flex gap-3 align-items-center">
              <div
                className="d-flex align-items-center border rounded px-3"
                style={{
                  flex: 1,
                  height: 46,
                }}
              >
                <FaSearch
                  style={{
                    color: "#98A2B3",
                  }}
                />

                <input
                  type="text"
                  placeholder="Search by email, endpoint, app-id, request-id..."
                  className="form-control border-0 shadow-none"
                />
              </div>

              <select
                className="form-select"
                style={{
                  width: 160,
                  height: 46,
                }}
              >
                <option>
                  All Outcomes
                </option>
              </select>

              <select
                className="form-select"
                style={{
                  width: 160,
                  height: 46,
                }}
              >
                <option>
                  All Services
                </option>
              </select>

              <span
                style={{
                  color: "#667085",
                  fontSize: 14,
                }}
              >
                11 records
              </span>
            </div>
          </div>

          {/* Table */}
          <div
            className="card shadow-sm"
            style={{
              borderRadius: 14,
              overflow: "hidden",
            }}
          >
            <table className="table mb-0">
              <thead
                style={{
                  background: "#f8fafc",
                }}
              >
                <tr>
                  <th></th>
                  <th>Timestamp</th>
                  <th>Actor</th>
                  <th>Role</th>
                  <th>Service</th>
                  <th>Method</th>
                  <th>Endpoint</th>
                  <th>App-Id</th>
                  <th>Status</th>
                  <th>Outcome</th>
                  <th>Latency</th>
                  <th>Fields</th>
                </tr>
              </thead>

              <tbody>
                {accessLogs.map((log) => (
                  <React.Fragment key={log.id}>
                    <tr>
                      <td
                        style={{
                          width: 40,
                          cursor: "pointer",
                        }}
                        onClick={() =>
                          setExpandedRow(
                            expandedRow === log.id
                              ? null
                              : log.id
                          )
                        }
                      >
                        {expandedRow === log.id ? (
                          <FaChevronDown
                            size={12}
                            color="#667085"
                          />
                        ) : (
                          <FaChevronRight
                            size={12}
                            color="#667085"
                          />
                        )}
                      </td>

                      <td>{log.timestamp}</td>

                      <td>{log.actor}</td>

                      <td>
                        <span
                          style={{
                            padding:
                              "4px 10px",
                            borderRadius: 20,
                            background:
                              "#dbeafe",
                            color: "#1d4ed8",
                            fontSize: 12,
                            fontWeight: 600,
                          }}
                        >
                          {log.role}
                        </span>
                      </td>

                      <td
                        style={{
                          fontWeight: 600,
                        }}
                      >
                        {log.service}
                      </td>

                      <td
                        style={{
                          color: "#15803d",
                          fontWeight: 600,
                        }}
                      >
                        {log.method}
                      </td>

                      <td>{log.endpoint}</td>

                      <td>{log.appId}</td>

                      <td
                        style={{
                          color:
                            log.status ===
                            "200"
                              ? "#15803d"
                              : log.status ===
                                "201"
                              ? "#15803d"
                              : "#dc2626",
                          fontWeight: 600,
                        }}
                      >
                        {log.status}
                      </td>

                      <td>
                        <span
                          style={{
                            padding:
                              "5px 12px",
                            borderRadius: 20,
                            fontSize: 12,
                            fontWeight: 700,
                            background:
                              log.outcome ===
                              "SUCCESS"
                                ? "#dcfce7"
                                : "#fee2e2",
                            color:
                              log.outcome ===
                              "SUCCESS"
                                ? "#15803d"
                                : "#dc2626",
                          }}
                        >
                          {log.outcome}
                        </span>
                      </td>

                      <td
                        style={{
                          color:
                            log.outcome ===
                            "ERROR"
                              ? "#dc2626"
                              : "#111827",
                        }}
                      >
                        {log.latency}
                      </td>

                      <td
                        style={{
                          color:
                            log.outcome ===
                            "ERROR"
                              ? "#dc2626"
                              : "#667085",
                        }}
                      >
                        {log.fields}
                      </td>
                    </tr>

                    {expandedRow === log.id && (
                      <tr>
                        <td colSpan={12}>
                          <div className="row p-3">

                            {/* Left */}
                            <div className="col-md-6">
                              <div
                                className="border rounded p-4 h-100"
                                style={{
                                  background:
                                    "#fff",
                                }}
                              >
                                <h6
                                  style={{
                                    color:
                                      "#98A2B3",
                                    fontWeight: 700,
                                    marginBottom: 20,
                                  }}
                                >
                                  IDENTIFIERS
                                </h6>

                                <div className="mb-2">
                                  <strong>
                                    Log ID
                                  </strong>
                                  <div>
                                    92lc0222-5b81
                                  </div>
                                </div>

                                <div className="mb-2">
                                  <strong>
                                    Request ID
                                  </strong>
                                  <div>
                                    79aee03c
                                  </div>
                                </div>

                                <div className="mb-2">
                                  <strong>
                                    Session ID
                                  </strong>
                                  <div>
                                    97de576f
                                  </div>
                                </div>

                                <div className="mb-2">
                                  <strong>
                                    Environment
                                  </strong>
                                  <div>
                                    Development
                                  </div>
                                </div>

                                <div>
                                  <strong>
                                    Actor User ID
                                  </strong>
                                  <div>1</div>
                                </div>
                              </div>
                            </div>

                            {/* Right */}
                            <div className="col-md-6">
                              <div
                                className="border rounded p-4 h-100"
                                style={{
                                  background:
                                    "#fff",
                                }}
                              >
                                {log.type ===
                                "success" ? (
                                  <>
                                    <h6
                                      style={{
                                        color:
                                          "#98A2B3",
                                        fontWeight:
                                          700,
                                        marginBottom:
                                          20,
                                      }}
                                    >
                                      RESPONSE
                                      FIELDS
                                    </h6>

                                    <div className="d-flex flex-wrap gap-2">
                                      {log.responseFields?.map(
                                        (
                                          field,
                                          index
                                        ) => (
                                          <span
                                            key={
                                              index
                                            }
                                            style={{
                                              background:
                                                "#dbeafe",
                                              color:
                                                "#1d4ed8",
                                              padding:
                                                "4px 10px",
                                              borderRadius:
                                                8,
                                              fontSize: 12,
                                            }}
                                          >
                                            {field}
                                          </span>
                                        )
                                      )}
                                    </div>
                                  </>
                                ) : (
                                  <>
                                    <h6
                                      style={{
                                        color:
                                          "#dc2626",
                                        fontWeight:
                                          700,
                                        marginBottom:
                                          20,
                                      }}
                                    >
                                      ERROR
                                      DETAILS
                                    </h6>

                                    <p
                                      style={{
                                        color:
                                          "#dc2626",
                                      }}
                                    >
                                      {
                                        log.errorMessage
                                      }
                                    </p>
                                  </>
                                )}
                              </div>
                            </div>
                          </div>
                        </td>
                      </tr>
                    )}
                  </React.Fragment>
                ))}
              </tbody>
            </table>
          </div>
          <br/>
        </div>
      )}
    </div>
  );
}