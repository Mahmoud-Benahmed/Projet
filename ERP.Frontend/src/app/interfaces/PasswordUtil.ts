interface PasswordValidationResult {
  isValid: boolean;
  strength: "weak" | "fair" | "strong" | "very strong";
  errors: string[];
  score: number;
}

interface PasswordRules {
  minLength?: number;
  maxLength?: number;
  requireUppercase?: boolean;
  requireLowercase?: boolean;
  requireNumbers?: boolean;
  requireSpecialChars?: boolean;
  disallowSpaces?: boolean;
}

const DEFAULT_RULES: PasswordRules = {
  minLength: 8,
  maxLength: 32,
  requireUppercase: true,
  requireLowercase: true,
  requireNumbers: true,
  requireSpecialChars: true,
  disallowSpaces: true,
};

function checkPassword(
  password: string,
  rules: PasswordRules  = DEFAULT_RULES
): PasswordValidationResult {
  const errors: string[] = [];
  let score = 0;

  const minLength= rules.minLength!;
  const maxLength= rules.maxLength!;
  const requireUppercase= rules.requireUppercase!;
  const requireLowercase= rules.requireLowercase!;
  const requireNumbers= rules.requireNumbers!;
  const requireSpecialChars= rules.requireSpecialChars!;
  const disallowSpaces= rules.disallowSpaces!;

  // --- Length checks ---
  if (password.length < minLength) {
    errors.push(`Password must be at least ${minLength} characters long.`);
  } else {
    score += 1;
    if (password.length >= 12) score += 1;
    if (password.length >= 16) score += 1;
  }

  if (password.length > maxLength) {
    errors.push(`Password must be no more than ${maxLength} characters long.`);
  }

  // --- Character type checks ---
  if (requireUppercase && !/[A-Z]/.test(password)) {
    errors.push("Password must contain at least one uppercase letter.");
  } else if (/[A-Z]/.test(password)) {
    score += 1;
  }

  if (requireLowercase && !/[a-z]/.test(password)) {
    errors.push("Password must contain at least one lowercase letter.");
  } else if (/[a-z]/.test(password)) {
    score += 1;
  }

  if (requireNumbers && !/[0-9]/.test(password)) {
    errors.push("Password must contain at least one number.");
  } else if (/[0-9]/.test(password)) {
    score += 1;
  }

  if (requireSpecialChars && !/[!@#$%^&*()_+\-=\[\]{};':"\\|,.<>\/?]/.test(password)) {
    errors.push("Password must contain at least one special character.");
  } else if (/[!@#$%^&*()_+\-=\[\]{};':"\\|,.<>\/?]/.test(password)) {
    score += 1;
  }

  if (disallowSpaces && /\s/.test(password)) {
    errors.push("Password must not contain spaces.");
  }

  // --- Common pattern penalties ---
  if (/^(.)\1+$/.test(password)) {
    errors.push("Password cannot be all the same character.");
    score = Math.max(0, score - 2);
  }

  if (/^(012|123|234|345|456|567|678|789|890|abc|bcd|cde|def)/i.test(password)) {
    score = Math.max(0, score - 1); // Penalize sequential patterns
  }

  // --- Strength rating ---
  const strength: PasswordValidationResult["strength"] =
    score <= 2 ? "weak" :
    score <= 4 ? "fair" :
    score <= 6 ? "strong" :
    "very strong";

  return {
    isValid: errors.length === 0,
    strength,
    errors,
    score,
  };
}

function generatePassword(): string {
    const chars = {
      upper: 'ABCDEFGHIJKLMNOPQRSTUVWXYZ',
      lower: 'abcdefghijklmnopqrstuvwxyz',
      numbers: '0123456789',
      symbols: '!@#$%^&*'
    };

    const allChars = chars.upper + chars.lower + chars.numbers + chars.symbols;

    // random length between 8 and 20
    const length = Math.floor(Math.random() * (DEFAULT_RULES.maxLength! - DEFAULT_RULES.minLength! + 1)) + DEFAULT_RULES.minLength!;

    // guarantee at least one of each type
    const password = [
      chars.upper[Math.floor(Math.random() * chars.upper.length)],
      chars.lower[Math.floor(Math.random() * chars.lower.length)],
      chars.numbers[Math.floor(Math.random() * chars.numbers.length)],
      chars.symbols[Math.floor(Math.random() * chars.symbols.length)],
      // fill remaining characters randomly
      ...Array.from({ length: length - 4 }, () => allChars[Math.floor(Math.random() * allChars.length)])
    ]
    // shuffle so the guaranteed chars aren't always at the start
    .sort(() => Math.random() - 0.5)
    .join('');
    return password;
}
