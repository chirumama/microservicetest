import { useState, type ChangeEvent } from "react";
import { useNavigate } from "react-router-dom";
import { useAuth } from "../../context/AuthContext";
import { FaEnvelope, FaLock } from "react-icons/fa";
import { IoEyeOutline, IoEyeOffOutline } from "react-icons/io5";
import InputField from "../../components/common/InputField";
import { login as apiLogin, verifyOtp, forgotPassword, resetPassword } from "../../services/api";

export default function Login() {
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [newPassword, setNewPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");

  const [showPassword, setShowPassword] = useState(false);
  const [loading, setLoading] = useState(false);
  const [errors, setErrors] = useState<{ email?: string; password?: string; api?: string }>({});

  const [step, setStep] = useState<"login" | "otp" | "forgot-email" | "forgot-otp" | "reset-password">("login");

  const [otp, setOtp] = useState<string[]>(["", "", "", "", "", ""]);

  const { login } = useAuth();
  const navigate = useNavigate();

  const getOtp = () => otp.join("");

  // ── Step 1: credential login ──────────────────────────────────────────────
  const handleLogin = async () => {
    const newErrors: typeof errors = {};
    if (!email.trim()) newErrors.email = "Email is required";
    if (!password.trim()) newErrors.password = "Password is required";
    if (Object.keys(newErrors).length) { setErrors(newErrors); return; }

    setErrors({});
    setLoading(true);
    try {
      const result = await apiLogin(email, password);

      if (result.requiresOtp) {
        localStorage.setItem("tempUser", JSON.stringify(result));
        setStep("otp");
      } else {
        login(result.userId, result.roleId, result.role, result.email);
        navigate(result.role === "SuperAdmin" ? "/superadmin-dashboard" : "/dashboard");
      }
    } catch (err: unknown) {
      setErrors({ api: err instanceof Error ? err.message : "Login failed" });
    } finally {
      setLoading(false);
    }
  };

  // ── OTP input handlers ────────────────────────────────────────────────────
  const handleOtpChange = (value: string, index: number) => {
    if (!/^\d?$/.test(value)) return;
    const newOtp = [...otp];
    newOtp[index] = value;
    setOtp(newOtp);
    if (value && index < 5) {
      document.getElementById(`otp-${index + 1}`)?.focus();
    }
  };

  const handleKeyDown = (e: React.KeyboardEvent<HTMLInputElement>, index: number) => {
    if (e.key === "Backspace" && !otp[index] && index > 0) {
      document.getElementById(`otp-${index - 1}`)?.focus();
    }
  };

  // ── Step 2: OTP verification → JWT issued ────────────────────────────────
  const handleVerifyOtp = async () => {
    const tempUser = JSON.parse(localStorage.getItem("tempUser") || "{}");

    try {
      const result = await verifyOtp(tempUser.userId, getOtp());

      localStorage.setItem("accessToken", result.accessToken);
      localStorage.removeItem("tempUser");

      login(result.userId, result.roleId, result.role, result.email);
      navigate(result.role === "SuperAdmin" ? "/superadmin-dashboard" : "/dashboard");
    } catch (err: unknown) {
      setErrors({ api: err instanceof Error ? err.message : "Invalid or expired OTP" });
    }
  };

  // ── Forgot password: send OTP to email ───────────────────────────────────
  const handleSendOtp = async () => {
    setLoading(true);
    try {
      await forgotPassword(email);
      setStep("forgot-otp");
    } catch (err: unknown) {
      setErrors({ api: err instanceof Error ? err.message : "Failed to send OTP. Check that the email is registered." });
    } finally {
      setLoading(false);
    }
  };

  // ── Forgot password: verify OTP → go to reset ────────────────────────────
  const handleVerifyForgotOtp = () => {
    // We don't verify separately — the OTP is sent along with resetPassword
    setStep("reset-password");
  };

  // ── Forgot password: reset password ──────────────────────────────────────
  const handleResetPassword = async () => {
    setLoading(true);
    try {
      await resetPassword(email, getOtp(), newPassword);
      setStep("login");
      setEmail("");
      setPassword("");
      setNewPassword("");
      setConfirmPassword("");
      setOtp(["", "", "", "", "", ""]);
      setErrors({});
    } catch (err: unknown) {
      setErrors({ api: err instanceof Error ? err.message : "Failed to reset password" });
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="d-flex vh-100 justify-content-center align-items-center bg-light">
      <div className="login-card">
        <h3 className="text-center mb-4">System Login</h3>

        {errors.api && (
          <div className="alert alert-danger py-2 mb-3" style={{ fontSize: 14 }}>
            {errors.api}
          </div>
        )}

        {/* LOGIN */}
        {step === "login" && (
          <>
            <InputField
              label="Email"
              type="email"
              value={email}
              onChange={(e: ChangeEvent<HTMLInputElement>) => setEmail(e.target.value)}
              placeholder="Enter email..."
              fullWidth
              error={!!errors.email}
              helperText={errors.email}
              startIcon={<FaEnvelope style={{ color: "gray" }} />}
            />
            <br /><br />

            <InputField
              label="Password"
              type={showPassword ? "text" : "password"}
              value={password}
              onChange={(e: ChangeEvent<HTMLInputElement>) => setPassword(e.target.value)}
              placeholder="Enter password..."
              fullWidth
              error={!!errors.password}
              helperText={errors.password}
              startIcon={<FaLock style={{ color: "gray" }} />}
              endIcon={
                <span onClick={() => setShowPassword(!showPassword)} style={{ cursor: "pointer" }}>
                  {showPassword ? <IoEyeOutline /> : <IoEyeOffOutline />}
                </span>
              }
            />
            <br /><br />

            <button className="login-btn" onClick={handleLogin} disabled={loading}>
              {loading ? "Logging in..." : "Login"}
            </button>

            <p
              className="text-center mt-3"
              style={{ fontSize: 15, color: "#2f5ec3", cursor: "pointer" }}
              onClick={() => {
                setStep("forgot-email");
                setErrors({});
              }}
            >
              Forgot password?
            </p>
          </>
        )}

        {/* OTP (login OTP + forgot-otp share same UI) */}
        {(step === "otp" || step === "forgot-otp") && (
          <>
            <p className="text-center text-muted mb-3" style={{ fontSize: 14 }}>
              Enter the 6-digit OTP sent to your registered email.
            </p>

            <div className="d-flex justify-content-center gap-2 mb-4">
              {otp.map((digit, index) => (
                <input
                  key={index}
                  id={`otp-${index}`}
                  type="text"
                  inputMode="numeric"
                  value={digit}
                  maxLength={1}
                  onChange={e => handleOtpChange(e.target.value, index)}
                  onKeyDown={e => handleKeyDown(e, index)}
                  style={{
                    width: "45px",
                    height: "52px",
                    textAlign: "center",
                    fontSize: "20px",
                    borderRadius: "8px",
                    border: "1.5px solid #ccc",
                    outline: "none",
                  }}
                />
              ))}
            </div>

            <button
              className="login-btn"
              onClick={step === "otp" ? handleVerifyOtp : handleVerifyForgotOtp}
              disabled={otp.join("").length !== 6 || loading}
            >
              {loading ? "Verifying..." : "Verify OTP"}
            </button>

            <p
              className="text-center mt-3"
              style={{ fontSize: 15, color: "#2f5ec3", cursor: "pointer" }}
              onClick={() => {
                setStep("login");
                setOtp(["", "", "", "", "", ""]);
                setErrors({});
              }}
            >
              ← Back to Login
            </p>
          </>
        )}

        {/* FORGOT EMAIL */}
        {step === "forgot-email" && (
          <>
            <InputField
              label="Email"
              type="email"
              value={email}
              onChange={(e: ChangeEvent<HTMLInputElement>) => {
                setEmail(e.target.value);
                if (errors.email) setErrors(p => ({ ...p, email: "" }));
              }}
              placeholder="Enter email..."
              fullWidth
              error={!!errors.email}
              helperText={errors.email}
              startIcon={<FaEnvelope style={{ color: "gray" }} />}
            />
            <br /><br />

            <button
              className="login-btn"
              onClick={() => {
                if (!email.trim()) {
                  setErrors({ email: "Email is required" });
                  return;
                }
                setErrors({});
                handleSendOtp();
              }}
              disabled={loading}
            >
              {loading ? "Sending OTP..." : "Send OTP"}
            </button>

            <p
              className="text-center mt-3"
              style={{ fontSize: 15, color: "#2f5ec3", cursor: "pointer" }}
              onClick={() => setStep("login")}
            >
              ← Back to Login
            </p>
          </>
        )}

        {/* RESET PASSWORD */}
        {step === "reset-password" && (
          <>
            <InputField
              label="New Password"
              type={showPassword ? "text" : "password"}
              value={newPassword}
              onChange={(e: ChangeEvent<HTMLInputElement>) => {
                setNewPassword(e.target.value);
                if (errors.password) setErrors(p => ({ ...p, password: "" }));
              }}
              fullWidth
              error={!!errors.password}
              helperText={errors.password}
              startIcon={<FaLock style={{ color: "gray" }} />}
              endIcon={
                <span onClick={() => setShowPassword(!showPassword)} style={{ cursor: "pointer" }}>
                  {showPassword ? <IoEyeOutline /> : <IoEyeOffOutline />}
                </span>
              }
            />
            <br /><br />

            <InputField
              label="Confirm Password"
              type={showPassword ? "text" : "password"}
              value={confirmPassword}
              onChange={(e: ChangeEvent<HTMLInputElement>) => {
                setConfirmPassword(e.target.value);
                if (errors.api) setErrors(p => ({ ...p, api: "" }));
              }}
              fullWidth
              error={!!errors.api}
              helperText={errors.api}
              startIcon={<FaLock style={{ color: "gray" }} />}
              endIcon={
                <span onClick={() => setShowPassword(!showPassword)} style={{ cursor: "pointer" }}>
                  {showPassword ? <IoEyeOutline /> : <IoEyeOffOutline />}
                </span>
              }
            />
            <br /><br />

            <button
              className="login-btn"
              onClick={() => {
                if (!newPassword.trim()) {
                  setErrors({ password: "New password is required" });
                  return;
                }
                if (!confirmPassword.trim()) {
                  setErrors({ api: "Confirm password is required" });
                  return;
                }
                if (newPassword !== confirmPassword) {
                  setErrors({ api: "Passwords do not match" });
                  return;
                }
                setErrors({});
                handleResetPassword();
              }}
              disabled={loading}
            >
              {loading ? "Resetting..." : "Set New Password"}
            </button>
          </>
        )}
      </div>
    </div>
  );
}