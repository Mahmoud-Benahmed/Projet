// password-util.spec.ts
import { checkPassword, generatePassword } from './PasswordUtil';

describe('checkPassword', () => {

  // ── Validity ──────────────────────────────────────────────────────
  it('rejects passwords shorter than 8 chars', () => {
    const r = checkPassword('abc', null);
    expect(r.isValid).toBe(false);
    expect(r.errors).toContain('Password must be at least 8 characters.');
  });

  it('rejects passwords longer than 128 chars', () => {
    const r = checkPassword('a'.repeat(129), null);
    expect(r.isValid).toBe(false);
  });

  it('rejects known breached passwords', () => {
    const r = checkPassword('password123', null);
    expect(r.isValid).toBe(false);
    expect(r.errors.some(e => e.includes('data breaches'))).toBe(true);
  });

  it('rejects all-same-character passwords', () => {
    const r = checkPassword('aaaaaaaa', null);
    expect(r.isValid).toBe(false);
  });

  it('accepts a valid passphrase', () => {
    const r = checkPassword('correct-horse-battery-staple', null);
    expect(r.isValid).toBe(true);
  });

  // ── Same-as-current ───────────────────────────────────────────────
  it('rejects new password identical to current', () => {
    const r = checkPassword('MyPassword99!', 'MyPassword99!');
    expect(r.isValid).toBe(false);
    expect(r.errors).toContain('New password must differ from the current one.');
  });

  it('accepts new password different from current', () => {
    const r = checkPassword('correct-horse-battery-staple', 'OldPassword1!');
    expect(r.isValid).toBe(true);
  });

  it('skips same-as-current check when currentPassword is null', () => {
    const r = checkPassword('correct-horse-battery-staple', null);
    expect(r.errors).not.toContain('New password must differ from the current one.');
  });

  // ── Strength scoring ──────────────────────────────────────────────
  it('scores short valid passwords as weak', () => {
    const r = checkPassword('abcdefgh', null);
    expect(r.strength).toBe('weak');
  });

  it('scores 20+ char passwords as very strong', () => {
    const r = checkPassword('correct-horse-battery-staple', null);
    expect(r.strength).toBe('very strong');
  });

  // ── generatePassword ──────────────────────────────────────────────
  it('generated password is always valid and very strong', () => {
    // Run multiple times — length is random in [20, 28]
    for (let i = 0; i < 20; i++) {
      const pwd = generatePassword();
      const r = checkPassword(pwd, null);
      expect(r.isValid).toBe(true);
      expect(r.strength).toBe('very strong');
    }
  });

  it('generated passwords are not all identical', () => {
    const passwords = new Set(Array.from({ length: 10 }, () => generatePassword()));
    expect(passwords.size).toBeGreaterThan(1);
  });
});
