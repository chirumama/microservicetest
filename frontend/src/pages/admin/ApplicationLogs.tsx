import React, { useState } from "react";
import { useNavigate } from "react-router-dom";
import {
  FaChevronDown,
  FaChevronRight,
  FaSearch,
  FaSyncAlt,
} from "react-icons/fa";

const applicationLogs = [
  {
    id: 1,
    timestamp: "15 May 2026, 12:11:10",
    event: "PAN_VERIFY_SUCCESS",
    appId: "lending-app-v1",
    refId: "PAN-9ba16131-aee4",
    flow: "PanService.VerifyPanAsync",
    thread: 21,
    status: "SUCCESS",
    payload: {
      request_id: "PAN-REQ-782191",
      pan_number: "ABCDE1234F",
      full_name: "Rahul Sharma",
      category: "Individual",
      status: "VALID",
      dob: "1998-02-14",
      linked_aadhaar: true,
      response_time: "200ms",
    },
  },

  {
    id: 2,
    timestamp: "15 May 2026, 12:15:48",
    event: "PASSPORT_VERIFY_SUCCESS",
    appId: "travel-app-v2",
    refId: "PASS-19ab22bc44",
    flow: "PassportService.VerifyPassportAsync",
    thread: 16,
    status: "SUCCESS",
    payload: {
      passport_number: "P4587612",
      file_number: "BO1065733511221",
      full_name: "Shreya Raut",
      nationality: "Indian",
      dob: "2000-12-29",
      expiry_date: "2032-01-10",
      verification_status: "VERIFIED",
    },
  },

  {
    id: 3,
    timestamp: "15 May 2026, 12:19:03",
    event: "GST_VERIFY_SUCCESS",
    appId: "gst-portal-v1",
    refId: "GST-8829XX",
    flow: "GSTService.VerifyGSTAsync",
    thread: 31,
    status: "SUCCESS",
    payload: {
      gstin: "22AAAAA0000A1Z5",
      business_name: "ABC Technologies Pvt Ltd",
      registration_type: "Regular",
      taxpayer_status: "Active",
      state: "Maharashtra",
      pan_number: "AAAAA0000A",
    },
  },

  {
    id: 4,
    timestamp: "15 May 2026, 12:24:27",
    event: "IP_LOOKUP_SUCCESS",
    appId: "security-monitor",
    refId: "IP-9911X",
    flow: "IpLookupService.LookupAsync",
    thread: 9,
    status: "SUCCESS",
    payload: {
      ip: "8.8.8.8",
      city: "Mumbai",
      region: "Maharashtra",
      country: "India",
      isp: "Google LLC",
      timezone: "Asia/Kolkata",
    },
  },

  {
    id: 5,
    timestamp: "15 May 2026, 12:28:44",
    event: "PAN_VERIFY_FAILED",
    appId: "lending-app-v1",
    refId: "PAN-ERR-9292",
    flow: "PanService.VerifyPanAsync",
    thread: 42,
    status: "ERROR",
    payload: {
      error: "Invalid PAN number format",
      status_code: 400,
      failed_pan: "ABCDE12",
      timestamp: "2026-05-14T12:28:44",
    },
  },

  {
    id: 6,
    timestamp: "15 May 2026, 12:32:18",
    event: "PASSPORT_DB_HEALTH_SUCCESS",
    appId: "passport-monitor",
    refId: "PASS-DB-781",
    flow: "PassportService.DatabaseHealthAsync",
    thread: 12,
    status: "SUCCESS",
    payload: {
      database: "MongoDB",
      status: "CONNECTED",
      response_time: "81ms",
      checked_at: "2026-05-14T12:32:18",
    },
  },

  {
    id: 7,
    timestamp: "15 May 2026, 12:37:52",
    event: "GST_VERIFY_FAILED",
    appId: "gst-portal-v1",
    refId: "GST-ERR-001",
    flow: "GSTService.VerifyGSTAsync",
    thread: 27,
    status: "ERROR",
    payload: {
      error: "GST number not found",
      status_code: 404,
      gstin: "27BBBBB1111B2Z6",
      timestamp: "2026-05-14T12:37:52",
    },
  },

  {
    id: 8,
    timestamp: "15 May 2026, 12:40:11",
    event: "PAN_HEALTH_SUCCESS",
    appId: "health-monitor",
    refId: "PAN-HEALTH-11",
    flow: "PanService.HealthCheckAsync",
    thread: 7,
    status: "SUCCESS",
    payload: {
      service: "Pan Service",
      status: "Healthy",
      uptime: "99.98%",
      response_time: "54ms",
    },
  },
];

export default function ApplicationLogs() {
  const [expandedRow, setExpandedRow] = useState<number | null>(null);

  const totalLogs = applicationLogs.length;

  const successLogs = applicationLogs.filter(
    (l) => l.status === "SUCCESS"
  ).length;

  const errorLogs = applicationLogs.filter(
    (l) => l.status === "ERROR"
  ).length;

  const navigate = useNavigate();

  return (
    <div className="container-fluid p-4">

      {/* Header */}
      <div className="d-flex justify-content-between align-items-start mb-4">
        <div>
          <button
            className="p-0 mb-3 mt-3"
            style={{ border: "none", background: "none" }}
            onClick={() => navigate("/dashboard")}
          >
            ← Back to Dashboard
          </button>

          <h3 className="fw-bold">Application Logs</h3>

        </div>

        <button
          className="btn btn-outline-secondary d-flex align-items-center"
          style={{
            borderRadius: 12,
            padding: "10px 18px",
            fontWeight: 500,
          }}
        >
          <FaSyncAlt className="me-2" />
          Refresh
        </button>
      </div>

      {/* Stats */}
      <div className="row g-4 mb-4">

        <div className="col-md-4">
          <div
            className="card border-0 shadow-sm"
            style={{
              borderRadius: 16,
              background: "#eef2ff",
            }}
          >
            <div className="card-body">
              <h1
                style={{
                  color: "#2563eb",
                  fontWeight: 700,
                }}
              >
                {totalLogs}
              </h1>

              <p
                style={{
                  marginBottom: 0,
                  color: "#475467",
                }}
              >
                Total Logs
              </p>
            </div>
          </div>
        </div>

        <div className="col-md-4">
          <div
            className="card border-0 shadow-sm"
            style={{
              borderRadius: 16,
              background: "#ecfdf3",
            }}
          >
            <div className="card-body">
              <h1
                style={{
                  color: "#16a34a",
                  fontWeight: 700,
                }}
              >
                {successLogs}
              </h1>

              <p
                style={{
                  marginBottom: 0,
                  color: "#475467",
                }}
              >
                Success Events
              </p>
            </div>
          </div>
        </div>

        <div className="col-md-4">
          <div
            className="card border-0 shadow-sm"
            style={{
              borderRadius: 16,
              background: "#fef2f2",
            }}
          >
            <div className="card-body">
              <h1
                style={{
                  color: "#dc2626",
                  fontWeight: 700,
                }}
              >
                {errorLogs}
              </h1>

              <p
                style={{
                  marginBottom: 0,
                  color: "#475467",
                }}
              >
                Error Events
              </p>
            </div>
          </div>
        </div>
      </div>

      {/* Search */}
      <div
        className="card shadow-sm mb-4"
        style={{
          borderRadius: 16,
        }}
      >
        <div className="card-body">
          <div
            className="input-group"
            style={{
              height: 50,
            }}
          >
            <span className="input-group-text bg-white border-end-0">
              <FaSearch color="#98A2B3" />
            </span>

            <input
              type="text"
              className="form-control border-start-0"
              placeholder="Search by event, app-id, ref-id..."
              style={{
                boxShadow: "none",
              }}
            />
          </div>
        </div>
      </div>

      {/* Table */}
      <div
        className="card shadow-sm"
        style={{
          borderRadius: 16,
          overflow: "hidden",
        }}
      >
        <div
          style={{
            overflowX: "auto",
          }}
        >
          <table
            className="table mb-0"
            style={{
              minWidth: 1200,
            }}
          >
            <thead
              style={{
                background: "#f8fafc",
              }}
            >
              <tr>
                <th></th>
                <th>Timestamp</th>
                <th>Event</th>
                <th>App ID</th>
                <th>Ref ID</th>
                <th>Flow</th>
                <th>Thread</th>
                <th>Status</th>
              </tr>
            </thead>

            <tbody>
              {applicationLogs.map((log) => (
                <React.Fragment key={log.id}>

                  <tr>
                    <td
                      style={{
                        cursor: "pointer",
                        width: 40,
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
                        <FaChevronDown size={12} />
                      ) : (
                        <FaChevronRight size={12} />
                      )}
                    </td>

                    <td>{log.timestamp}</td>

                    <td>
                      <span
                        style={{
                          background:
                            log.status === "SUCCESS"
                              ? "#dbeafe"
                              : "#fee2e2",
                          color:
                            log.status === "SUCCESS"
                              ? "#1d4ed8"
                              : "#dc2626",
                          padding: "5px 12px",
                          borderRadius: 20,
                          fontSize: 12,
                          fontWeight: 600,
                        }}
                      >
                        {log.event}
                      </span>
                    </td>

                    <td>{log.appId}</td>

                    <td>{log.refId}</td>

                    <td>{log.flow}</td>

                    <td>{log.thread}</td>

                    <td>
                      <span
                        style={{
                          background:
                            log.status === "SUCCESS"
                              ? "#dcfce7"
                              : "#fee2e2",
                          color:
                            log.status === "SUCCESS"
                              ? "#15803d"
                              : "#dc2626",
                          padding: "5px 12px",
                          borderRadius: 20,
                          fontSize: 12,
                          fontWeight: 700,
                        }}
                      >
                        {log.status}
                      </span>
                    </td>
                  </tr>

                  {expandedRow === log.id && (
                    <tr>
                      <td colSpan={8}>
                        <div
                          style={{
                            background: "#07112b",
                            borderRadius: 14,
                            padding: 24,
                            margin: 20,
                            overflowX: "auto",
                          }}
                        >
                          <pre
                            style={{
                              color: "#fff",
                              margin: 0,
                              fontSize: 13,
                              lineHeight: 1.7,
                            }}
                          >
                            {JSON.stringify(
                              log.payload,
                              null,
                              2
                            )}
                          </pre>
                        </div>
                      </td>
                    </tr>
                  )}

                </React.Fragment>
              ))}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
}