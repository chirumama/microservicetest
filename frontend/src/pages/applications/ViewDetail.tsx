import { useState } from "react";
import { useParams } from "react-router-dom";
import { FaChevronDown } from "react-icons/fa";

import NavBar from "../../components/common/NavBar";
import { useAuth } from "../../context/AuthContext";

export default function ViewDetails() {
    const { id } = useParams();

    const { user } = useAuth();

    const username =
        user?.email?.split("@")[0] || "User";

    const [
        verifyApiUrl,
        setVerifyApiUrl,
    ] = useState(
        "https://localhost:7251/api/passport/verify"
    );

    const [
        healthApiUrl,
        setHealthApiUrl,
    ] = useState(
        "https://localhost:7251/api/passport/health"
    );

    const [
        dbHealthApiUrl,
        setDbHealthApiUrl,
    ] = useState(
        "https://localhost:7251/api/passport/health/db"
    );

    const [
        isEditingVerifyUrl,
        setIsEditingVerifyUrl,
    ] = useState(false);

    const [
        isEditingHealthUrl,
        setIsEditingHealthUrl,
    ] = useState(false);

    const [
        isEditingDbHealthUrl,
        setIsEditingDbHealthUrl,
    ] = useState(false);

    const [loading, setLoading] =
        useState(false);

    const [error, setError] =
        useState("");

    const [
        showVerifyDetails,
        setShowVerifyDetails,
    ] = useState(false);

    const [
        showHealthDetails,
        setShowHealthDetails,
    ] = useState(false);

    const [
        showDbHealthDetails,
        setShowDbHealthDetails,
    ] = useState(false);

    const [
        verifyResponse,
        setVerifyResponse,
    ] = useState<any>(null);

    const [
        healthResponse,
        setHealthResponse,
    ] = useState<any>(null);

    const [
        dbHealthResponse,
        setDbHealthResponse,
    ] = useState<any>(null);

    const [
        verifyRequestBody,
        setVerifyRequestBody,
    ] = useState(`{
  "file_number": "BO1065733511221",
  "date_of_birth": "2000-12-29",
  "consent": "Y"
}`);

    const handleVerifyApi =
        async () => {
            try {
                setLoading(true);
                setError("");

                const response =
                    await fetch(
                        verifyApiUrl,
                        {
                            method: "POST",
                            headers: {
                                "Content-Type":
                                    "application/json",
                            },
                            body: JSON.stringify(
                                JSON.parse(
                                    verifyRequestBody
                                )
                            ),
                        }
                    );

                const data =
                    await response.json();

                setVerifyResponse(data);
            } catch (err: any) {
                setError(
                    err.message ||
                    "Verify API Failed"
                );
            } finally {
                setLoading(false);
            }
        };

    const handleHealthApi =
        async () => {
            try {
                setLoading(true);
                setError("");

                const response =
                    await fetch(
                        healthApiUrl
                    );

                const data =
                    await response.json();

                setHealthResponse(data);
            } catch (err: any) {
                setError(
                    err.message ||
                    "Health API Failed"
                );
            } finally {
                setLoading(false);
            }
        };

    const handleDbHealthApi =
        async () => {
            try {
                setLoading(true);
                setError("");

                const response =
                    await fetch(
                        dbHealthApiUrl
                    );

                const data =
                    await response.json();

                setDbHealthResponse(data);
            } catch (err: any) {
                setError(
                    err.message ||
                    "DB Health API Failed"
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
                        Passport API's
                    </h2>

                    <p
                        style={{
                            color: "#6c757d",
                        }}
                    >
                        Test your microservice APIs
                    </p>
                </div>

                <div
                    className="card border mb-4"
                    style={{
                        borderRadius: "12px",
                        overflow: "hidden",
                    }}
                >
                    <div
                        className="d-flex align-items-center justify-content-between p-2"
                        style={{
                            background: "#eaf3ff",
                            cursor: "pointer",
                        }}
                        onClick={() =>
                            setShowVerifyDetails(
                                !showVerifyDetails
                            )
                        }
                    >
                        <div className="d-flex align-items-center gap-3">
                            <div
                                style={{
                                    background: "#667eea",
                                    color: "white",
                                    padding:
                                        "6px 22px",
                                    borderRadius: "8px",
                                    fontWeight: 700,
                                }}
                            >
                                POST
                            </div>

                            <span
                                style={{
                                    fontSize: "19px",
                                    fontWeight: 500,
                                }}
                            >
                                /api/passport/verify
                            </span>
                        </div>

                        <FaChevronDown
                            size={20}
                            style={{
                                transform:
                                    showVerifyDetails
                                        ? "rotate(180deg)"
                                        : "rotate(0deg)",
                            }}
                        />
                    </div>

                    {showVerifyDetails && (
                        <div className="card-body">
                            <div className="row g-3 align-items-center mb-4">
                                <div className="col-md-2">
                                    <input
                                        type="text"
                                        value="POST"
                                        disabled
                                        className="form-control"
                                        style={{
                                            height:
                                                "50px",
                                            borderRadius:
                                                "10px",
                                            background:
                                                "#f1f5f9",
                                            textAlign:
                                                "center",
                                            fontWeight: 600,
                                        }}
                                    />
                                </div>

                                <div className="col-md-8">
                                    <div className="d-flex gap-2">
                                        <input
                                            type="text"
                                            value={
                                                verifyApiUrl
                                            }
                                            disabled={
                                                !isEditingVerifyUrl
                                            }
                                            onChange={(e) =>
                                                setVerifyApiUrl(
                                                    e.target.value
                                                )
                                            }
                                            className="form-control"
                                            style={{
                                                height:
                                                    "50px",
                                                borderRadius:
                                                    "10px",
                                                background:
                                                    isEditingVerifyUrl
                                                        ? "#ffffff"
                                                        : "#f1f5f9",
                                                fontWeight: 500,
                                            }}
                                        />

                                        <button
                                            type="button"
                                            className="btn"
                                            onClick={() =>
                                                setIsEditingVerifyUrl(
                                                    !isEditingVerifyUrl
                                                )
                                            }
                                            style={{
                                                minWidth: "90px",
                                                borderRadius: "10px",
                                                border: "1px solid #667eea",
                                                color: "#667eea",
                                                backgroundColor:
                                                    isEditingVerifyUrl
                                                        ? "#667eea"
                                                        : "white",
                                            }}
                                            onMouseEnter={(e) => {
                                                e.currentTarget.style.backgroundColor =
                                                    "#667eea";
                                                e.currentTarget.style.color =
                                                    "white";
                                            }}
                                            onMouseLeave={(e) => {
                                                e.currentTarget.style.backgroundColor =
                                                    isEditingVerifyUrl
                                                        ? "#667eea"
                                                        : "white";
                                                e.currentTarget.style.color =
                                                    isEditingVerifyUrl
                                                        ? "white"
                                                        : "#667eea";
                                            }}
                                        >
                                            {isEditingVerifyUrl
                                                ? "Save"
                                                : "Edit"}
                                        </button>
                                    </div>
                                </div>

                                <div className="col-md-2">
                                    <button
                                        className="btn w-100 text-white"
                                        onClick={
                                            handleVerifyApi
                                        }
                                        disabled={
                                            loading
                                        }
                                        style={{
                                            height:
                                                "50px",
                                            background:
                                                "#667eea",
                                            border:
                                                "none",
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

                            <div className="mb-4">
                                <label className="form-label fw-semibold mb-2">
                                    Request Body
                                </label>

                                <textarea
                                    rows={8}
                                    className="form-control"
                                    value={
                                        verifyRequestBody
                                    }
                                    onChange={(e) =>
                                        setVerifyRequestBody(
                                            e.target
                                                .value
                                        )
                                    }
                                    style={{
                                        borderRadius:
                                            "12px",
                                        fontFamily:
                                            "monospace",
                                    }}
                                />
                            </div>

                            <div>
                                <label className="form-label fw-semibold mb-2">
                                    API Response
                                </label>

                                <pre
                                    style={{
                                        background:
                                            "#0f172a",
                                        color:
                                            "#f8fafc",
                                        padding:
                                            "20px",
                                        borderRadius:
                                            "12px",
                                        minHeight:
                                            "250px",
                                        overflowX:
                                            "auto",
                                        fontSize:
                                            "14px",
                                    }}
                                >
                                    {verifyResponse
                                        ? JSON.stringify(
                                            verifyResponse,
                                            null,
                                            2
                                        )
                                        : `{
  "message": "Verify API response will appear here..."
}`}
                                </pre>
                            </div>
                        </div>
                    )}
                </div>

                {/* HEALTH API */}
                <div
                    className="card border mb-4"
                    style={{
                        borderRadius: "12px",
                        overflow: "hidden",
                    }}
                >
                    <div
                        className="d-flex align-items-center justify-content-between p-2"
                        style={{
                            background: "#eaf3ff",
                            cursor: "pointer",
                        }}
                        onClick={() =>
                            setShowHealthDetails(
                                !showHealthDetails
                            )
                        }
                    >
                        <div className="d-flex align-items-center gap-3">
                            <div
                                style={{
                                    background: "#667eea",
                                    color: "white",
                                    padding:
                                        "6px 22px",
                                    borderRadius: "8px",
                                    fontWeight: 700,
                                }}
                            >
                                GET
                            </div>

                            <span
                                style={{
                                    fontSize: "19px",
                                    fontWeight: 500,
                                }}
                            >
                                /api/passport/health
                            </span>
                        </div>

                        <FaChevronDown
                            size={20}
                            style={{
                                transform:
                                    showHealthDetails
                                        ? "rotate(180deg)"
                                        : "rotate(0deg)",
                            }}
                        />
                    </div>

                    {showHealthDetails && (
                        <div className="card-body">
                            <div className="row g-3 align-items-center mb-4">
                                <div className="col-md-2">
                                    <input
                                        type="text"
                                        value="GET"
                                        disabled
                                        className="form-control"
                                    />
                                </div>

                                <div className="col-md-8">
                                    <div className="d-flex gap-2">
                                        <input
                                            type="text"
                                            value={
                                                healthApiUrl
                                            }
                                            disabled={
                                                !isEditingHealthUrl
                                            }
                                            onChange={(e) =>
                                                setHealthApiUrl(
                                                    e.target.value
                                                )
                                            }
                                            className="form-control"
                                            style={{
                                                height: "50px",
                                                borderRadius: "10px",
                                                background:
                                                    isEditingHealthUrl
                                                        ? "#ffffff"
                                                        : "#f1f5f9",
                                                fontWeight: 500,
                                            }}
                                        />

                                        <button
                                            type="button"
                                            className="btn"
                                            onClick={() =>
                                                setIsEditingHealthUrl(
                                                    !isEditingHealthUrl
                                                )
                                            }
                                            style={{
                                                minWidth: "90px",
                                                borderRadius: "10px",
                                                border:
                                                    "1px solid #667eea",
                                                color:
                                                    isEditingHealthUrl
                                                        ? "white"
                                                        : "#667eea",
                                                backgroundColor:
                                                    isEditingHealthUrl
                                                        ? "#667eea"
                                                        : "white",
                                            }}
                                            onMouseEnter={(e) => {
                                                e.currentTarget.style.backgroundColor =
                                                    "#667eea";
                                                e.currentTarget.style.color =
                                                    "white";
                                            }}
                                            onMouseLeave={(e) => {
                                                e.currentTarget.style.backgroundColor =
                                                    isEditingHealthUrl
                                                        ? "#667eea"
                                                        : "white";

                                                e.currentTarget.style.color =
                                                    isEditingHealthUrl
                                                        ? "white"
                                                        : "#667eea";
                                            }}
                                        >
                                            {isEditingHealthUrl
                                                ? "Save"
                                                : "Edit"}
                                        </button>
                                    </div>
                                </div>

                                <div className="col-md-2">
                                    <button
                                        className="btn w-100 text-white"
                                        onClick={
                                            handleHealthApi
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
                                        Test API
                                    </button>
                                </div>
                            </div>

                            <pre
                                style={{
                                    background:
                                        "#0f172a",
                                    color:
                                        "#f8fafc",
                                    padding:
                                        "20px",
                                    borderRadius:
                                        "12px",
                                }}
                            >
                                {healthResponse
                                    ? JSON.stringify(
                                        healthResponse,
                                        null,
                                        2
                                    )
                                    : `{
  "message": "Health API response will appear here..."
}`}
                            </pre>
                        </div>
                    )}
                </div>

                {/* DB HEALTH API */}
                <div
                    className="card border"
                    style={{
                        borderRadius: "12px",
                        overflow: "hidden",
                    }}
                >
                    <div
                        className="d-flex align-items-center justify-content-between p-2"
                        style={{
                            background: "#eaf3ff",
                            cursor: "pointer",
                        }}
                        onClick={() =>
                            setShowDbHealthDetails(
                                !showDbHealthDetails
                            )
                        }
                    >
                        <div className="d-flex align-items-center gap-3">
                            <div
                                style={{
                                    background: "#667eea",
                                    color: "white",
                                    padding:
                                        "6px 22px",
                                    borderRadius: "8px",
                                    fontWeight: 700,
                                }}
                            >
                                GET
                            </div>

                            <span
                                style={{
                                    fontSize: "19px",
                                    fontWeight: 500,
                                }}
                            >
                                /api/passport/health/db
                            </span>
                        </div>

                        <FaChevronDown
                            size={20}
                            style={{
                                transform:
                                    showVerifyDetails
                                        ? "rotate(180deg)"
                                        : "rotate(0deg)",
                            }}
                        />
                    </div>

                    {showDbHealthDetails && (
                        <div className="card-body">
                            <div className="row g-3 align-items-center mb-4">
                                <div className="col-md-2">
                                    <input
                                        type="text"
                                        value="GET"
                                        disabled
                                        className="form-control"
                                    />
                                </div>

                                <div className="col-md-8">
                                    <div className="d-flex gap-2">
                                        <input
                                            type="text"
                                            value={
                                                dbHealthApiUrl
                                            }
                                            disabled={
                                                !isEditingDbHealthUrl
                                            }
                                            onChange={(e) =>
                                                setDbHealthApiUrl(
                                                    e.target.value
                                                )
                                            }
                                            className="form-control"
                                            style={{
                                                height: "50px",
                                                borderRadius: "10px",
                                                background:
                                                    isEditingDbHealthUrl
                                                        ? "#ffffff"
                                                        : "#f1f5f9",
                                                fontWeight: 500,
                                            }}
                                        />

                                        <button
                                            type="button"
                                            className="btn"
                                            onClick={() =>
                                                setIsEditingDbHealthUrl(
                                                    !isEditingDbHealthUrl
                                                )
                                            }
                                            style={{
                                                minWidth: "90px",
                                                borderRadius: "10px",
                                                border:
                                                    "1px solid #667eea",
                                                color:
                                                    isEditingDbHealthUrl
                                                        ? "white"
                                                        : "#667eea",
                                                backgroundColor:
                                                    isEditingDbHealthUrl
                                                        ? "#667eea"
                                                        : "white",
                                            }}
                                            onMouseEnter={(e) => {
                                                e.currentTarget.style.backgroundColor =
                                                    "#667eea";
                                                e.currentTarget.style.color =
                                                    "white";
                                            }}
                                            onMouseLeave={(e) => {
                                                e.currentTarget.style.backgroundColor =
                                                    isEditingDbHealthUrl
                                                        ? "#667eea"
                                                        : "white";

                                                e.currentTarget.style.color =
                                                    isEditingDbHealthUrl
                                                        ? "white"
                                                        : "#667eea";
                                            }}
                                        >
                                            {isEditingDbHealthUrl
                                                ? "Save"
                                                : "Edit"}
                                        </button>
                                    </div>
                                </div>

                                <div className="col-md-2">
                                    <button
                                        className="btn w-100 text-white"
                                        onClick={
                                            handleDbHealthApi
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
                                        Test API
                                    </button>
                                </div>
                            </div>

                            <pre
                                style={{
                                    background:
                                        "#0f172a",
                                    color:
                                        "#f8fafc",
                                    padding:
                                        "20px",
                                    borderRadius:
                                        "12px",
                                }}
                            >
                                {dbHealthResponse
                                    ? JSON.stringify(
                                        dbHealthResponse,
                                        null,
                                        2
                                    )
                                    : `{
  "message": "DB Health API response will appear here..."
}`}
                            </pre>
                        </div>
                    )}
                </div>
            </div>
        </>
    );
}