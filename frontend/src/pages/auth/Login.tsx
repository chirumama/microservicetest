import { useState, type ChangeEvent } from "react";
import { useNavigate } from "react-router-dom";
import { useAuth } from "../../context/AuthContext";
import { FaEnvelope, FaLock } from "react-icons/fa";
import { IoEyeOutline, IoEyeOffOutline } from "react-icons/io5";
import InputField from "../../components/common/InputField";
import { login as apiLogin } from "../../services/api";

export default function Login() {
  const [email, setEmail]       = useState("");
  const [password, setPassword] = useState("");
  const [showPassword, setShowPassword] = useState(false);
  const [loading, setLoading]   = useState(false);
  const [errors, setErrors]     = useState<{ email?: string; password?: string; api?: string }>({});

  const { login } = useAuth();
  const navigate  = useNavigate();

  const handleLogin = async () => {
    const newErrors: typeof errors = {};
    if (!email.trim())    newErrors.email    = "Email is required";
    if (!password.trim()) newErrors.password = "Password is required";
    if (Object.keys(newErrors).length) { setErrors(newErrors); return; }

    setErrors({});
    setLoading(true);
    try {
      const result = await apiLogin(email, password);

      
      // ✅ Store JWT
localStorage.setItem("token", result.accessToken);

// ✅ Update auth context (no userId/roleId needed)
login(0, 0, result.role, result.email);

      if (result.role === "SuperAdmin") {
        navigate("/superadmin-dashboard");
      } else {
        navigate("/dashboard");
      }
    } catch (err: unknown) {
      setErrors({ api: err instanceof Error ? err.message : "Login failed" });
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

        <InputField
          label="Email"
          type="email"
          value={email}
          onChange={(e: ChangeEvent<HTMLInputElement>) => {
            setEmail(e.target.value);
            if (errors.email) setErrors((p) => ({ ...p, email: "" }));
          }}
          placeholder="Enter email..."
          fullWidth
          error={!!errors.email} helperText={errors.email}
          startIcon={<FaEnvelope style={{ color: "gray" }} />}
        />
        <br /><br />

        <InputField
          label="Password"
          type={showPassword ? "text" : "password"}
          value={password}
          onChange={(e: ChangeEvent<HTMLInputElement>) => {
            setPassword(e.target.value);
            if (errors.password) setErrors((p) => ({ ...p, password: "" }));
          }}
          placeholder="Enter password..."
          fullWidth
          error={!!errors.password} helperText={errors.password}
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
      </div>
    </div>
  );
}