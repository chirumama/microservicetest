import { useState } from "react";
import { createApplication, type CreateApplicationResponse } from "../../services/api";

type Props = {
  show: boolean;
  onClose: () => void;
  onCreated?: (app: CreateApplicationResponse & { title: string }) => void;
};

export default function CreateApplicationModal({ show, onClose, onCreated }: Props) {
  const [title, setTitle] = useState("");
  const [desc, setDesc] = useState("");
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");
  const [created, setCreated] = useState<(CreateApplicationResponse & { title: string }) | null>(null);

  const handleSave = async () => {
    if (!title.trim() || !desc.trim()) {
      setError("Name and description are required.");
      return;
    }
    setError("");
    setLoading(true);
    try {
      const result = await createApplication(title, desc);
      const appData = { ...result, title };
      setCreated(appData);
      onCreated?.(appData);
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : "Failed to create application");
    } finally {
      setLoading(false);
    }
  };

  const handleClose = () => {
    setTitle("");
    setDesc("");
    setError("");
    setCreated(null);
    onClose();
  };

  if (!show) return null;

  return (
    <div className="custom-modal-overlay">
      <div className="custom-modal-card">
        {!created ? (
          <>
            <h3 className="mb-4">Create New Application</h3>
            {error && <div className="alert alert-danger py-2 mb-3" style={{ fontSize: 14 }}>{error}</div>}

            <div className="mb-3">
              <label className="form-label">Application Name</label>
              <input
                className="form-control"
                placeholder="Enter name"
                value={title}
                onChange={(e) => setTitle(e.target.value)}
              />
            </div>

            <div className="mb-3">
              <label className="form-label">Description</label>
              <textarea
                className="form-control"
                placeholder="Enter description"
                rows={3}
                value={desc}
                onChange={(e) => setDesc(e.target.value)}
              />
            </div>

            <div className="d-flex justify-content-end mt-4">
              <button className="btn btn-secondary me-2" onClick={handleClose}>Cancel</button>
              <button className="btn btn-primary" onClick={handleSave} disabled={loading}>
                {loading ? "Creating..." : "Create Application"}
              </button>
            </div>
          </>
        ) : (
          <>
            <h3 className="mb-3">Application Created!</h3>
            <p className="text-muted mb-4">
              Save these credentials — the secret will not be shown again.
            </p>

            <div className="mb-3">
              <label className="form-label fw-semibold">Application</label>
              <div className="form-control bg-light">{created.title}</div>
            </div>
            <div className="mb-3">
              <label className="form-label fw-semibold">App Key</label>
              <div
                className="d-flex align-items-center justify-content-between"
                style={{ border: "1px solid #d0d5dd", borderRadius: 10, padding: "6px 10px" }}
              >
                <span style={{ fontFamily: "monospace", fontSize: 13 }}>{created.appKey}</span>
                <button
                  className="btn btn-sm btn-outline-secondary ms-2"
                  onClick={() => navigator.clipboard.writeText(created.appKey)}
                >Copy</button>
              </div>
            </div>
            <div className="mb-3">
              <label className="form-label fw-semibold">App Secret</label>
              <div
                className="d-flex align-items-center justify-content-between"
                style={{ border: "1px solid #d0d5dd", borderRadius: 10, padding: "6px 10px" }}
              >
                <span style={{ fontFamily: "monospace", fontSize: 13 }}>{created.appSecret}</span>
                <button
                  className="btn btn-sm btn-outline-secondary ms-2"
                  onClick={() => navigator.clipboard.writeText(created.appSecret)}
                >Copy</button>
              </div>
            </div>

            <div className="d-flex justify-content-end mt-4">
              <button className="btn btn-primary" onClick={handleClose}>Done</button>
            </div>
          </>
        )}
      </div>
    </div>
  );
}
