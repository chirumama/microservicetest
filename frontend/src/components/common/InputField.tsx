import React from "react";
import {
  TextField as MUITextField,
  InputAdornment,
  MenuItem,
} from "@mui/material";

interface InputFieldProps {
  label?: string;
  type?: string;
  value?: string;
  onChange?: (e: React.ChangeEvent<HTMLInputElement>) => void;
  placeholder?: string;
  fullWidth?: boolean;
  required?: boolean;        // ← add this line
  error?: boolean;
  helperText?: string;
  startIcon?: React.ReactNode;
  endIcon?: React.ReactNode;
  
  select?: boolean;
  options?: { value: string; label: string }[];
}

const InputField: React.FC<InputFieldProps> = ({
  label,
  value,
  onChange,
  type = "text",
  placeholder,
  fullWidth = false,
  error = false,
  required,
  helperText,
  startIcon,
  endIcon,
  select = false,
  options = [],
}) => {
  return (
    <MUITextField
      label={label}
      value={value}
      onChange={onChange}
      type={select ? undefined : type} // ✅ important
      placeholder={placeholder}
      fullWidth={fullWidth}
      error={error}
      required={required} 
      helperText={helperText}
      variant="outlined"
      select={select}
      SelectProps={{
        displayEmpty: true, // ✅ shows placeholder
      }}
      InputProps={{
        startAdornment: startIcon ? (
          <InputAdornment position="start">
            {startIcon}
          </InputAdornment>
        ) : undefined,
        endAdornment: endIcon ? (
          <InputAdornment position="end">
            {endIcon}
          </InputAdornment>
        ) : undefined,
      }}
    >
      {select &&
        options.map((opt) => (
          <MenuItem key={opt.value} value={opt.value}>
            {opt.label}
          </MenuItem>
        ))}
    </MUITextField>
  );
};

export default InputField;