import { useState } from "react";
import { useParams } from "react-router-dom";

import NavBar from "../../components/common/NavBar";
import { useAuth } from "../../context/AuthContext";

export default function ViewDetails() {
  const { id } = useParams();

  const { user } = useAuth();

  const username =
    user?.email?.split("@")[0] || "User";

  // Default API URL
  const [apiUrl, setApiUrl] =
    useState(
      "https://localhost:7251/api/passport/verify"
    );

  const [method, setMethod] =
    useState("POST");

  const [loading, setLoading] =
    useState(false);

  const [responseData, setResponseData] =
    useState<any>(null);

  const [error, setError] =
    useState("");

  // Request Body
  const [requestBody, setRequestBody] =
    useState(`{
  "file_number": "BO1065733511221",
  "date_of_birth": "2000-12-29",
  "consent": "Y"
}`);

  const handleTestApi = async () => {
    try {
      setLoading(true);
      setError("");
      setResponseData(null);

      const response = await fetch(apiUrl, {
        method,
        headers: {
          "Content-Type":
            "application/json",
        },
        body:
          method !== "GET"
            ? requestBody
            : undefined,
      });

      // Handle non-success response
      if (!response.ok) {
        throw new Error(
          `HTTP Error: ${response.status}`
        );
      }

      const data = await response.json();

      setResponseData(data);
    } catch (err: any) {
      console.error(err);

      setError(
        err.message ||
          "Failed to fetch API"
      );
    } finally {
      setLoading(false);
    }
  };

  return (
    <>
      <NavBar username={username} />

      <div className="container py-4">
        {/* Header */}
        <div className="mb-4">
          <h2 className="mb-1">
            Passport API Tester
          </h2>

          <p
            style={{
              color: "#6c757d",
            }}
          >
            Test your microservice APIs
          </p>
        </div>

        {/* API Request Card */}
        <div
          className="card border-0 shadow-sm mb-4"
          style={{
            borderRadius: "18px",
          }}
        >
          <div className="card-body">
            {/* Top Request Row */}
            <div className="row g-3 align-items-center mb-4">
              {/* Method Dropdown */}
              <div className="col-md-2">
                <select
                  className="form-select"
                  value={method}
                  onChange={(e) =>
                    setMethod(
                      e.target.value
                    )
                  }
                  style={{
                    height: "50px",
                    borderRadius: "10px",
                    fontWeight: 600,
                  }}
                >
                  <option value="GET">
                    GET
                  </option>

                  <option value="POST">
                    POST
                  </option>

                  <option value="PUT">
                    PUT
                  </option>

                  <option value="DELETE">
                    DELETE
                  </option>
                </select>
              </div>

              {/* API URL */}
              <div className="col-md-8">
                <input
                  type="text"
                  className="form-control"
                  placeholder="Enter API URL"
                  value={apiUrl}
                  onChange={(e) =>
                    setApiUrl(
                      e.target.value
                    )
                  }
                  style={{
                    height: "50px",
                    borderRadius: "10px",
                  }}
                />
              </div>

              {/* Send Button */}
              <div className="col-md-2">
                <button
                  className="btn w-100 text-white"
                  onClick={
                    handleTestApi
                  }
                  disabled={
                    loading ||
                    !apiUrl
                  }
                  style={{
                    height: "50px",
                    background:
                      "#667eea",
                    border: "none",
                    borderRadius:
                      "10px",
                    fontWeight: 600,
                  }}
                >
                  {loading
                    ? "Testing..."
                    : "Test API"}
                </button>
              </div>
            </div>

            {/* Request Body */}
            <div>
              <label
                className="form-label fw-semibold mb-2"
              >
                Request Body
              </label>

              <textarea
                className="form-control"
                rows={10}
                value={requestBody}
                onChange={(e) =>
                  setRequestBody(
                    e.target.value
                  )
                }
                style={{
                  borderRadius: "12px",
                  fontFamily:
                    "monospace",
                  fontSize: "14px",
                }}
              />
            </div>
          </div>
        </div>

        {/* Response Section */}
        <div
          className="card border-0 shadow-sm"
          style={{
            borderRadius: "18px",
          }}
        >
          <div className="card-body">
            <div className="d-flex justify-content-between align-items-center mb-3">
              <h5
                className="mb-0"
                style={{
                  fontWeight: 600,
                }}
              >
                API Response
              </h5>

              <span
                className="badge"
                style={{
                  background:
                    "#e8f5e9",
                  color: "#2e7d32",
                  padding:
                    "8px 12px",
                }}
              >
                Service ID: {id}
              </span>
            </div>

            {/* Error */}
            {error && (
              <div className="alert alert-danger">
                {error}
              </div>
            )}

            {/* Response */}
            <pre
              style={{
                background:
                  "#0f172a",
                color: "#f8fafc",
                padding: "20px",
                borderRadius:
                  "12px",
                minHeight: "300px",
                overflowX: "auto",
                fontSize: "14px",
              }}
            >
              {responseData
                ? JSON.stringify(
                    responseData,
                    null,
                    2
                  )
                : `{
  "message": "API response will appear here..."
}`}
            </pre>
          </div>
        </div>
      </div>
    </>
  );
}