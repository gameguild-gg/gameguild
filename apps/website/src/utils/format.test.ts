import { formatName, validateEmail } from '@/utils/format';

describe('formatName', () => {
  it('should format first and last name correctly', () => {
    expect(formatName('John', 'Doe')).toBe('John Doe');
  });

  it('should handle empty strings', () => {
    expect(formatName('', 'Doe')).toBe('Doe');
    expect(formatName('John', '')).toBe('John');
  });

  it('should trim whitespace', () => {
    expect(formatName(' John ', ' Doe ')).toBe('John Doe');
  });
});

describe('validateEmail', () => {
  it('should validate correct email addresses', () => {
    expect(validateEmail('test@example.com')).toBe(true);
    expect(validateEmail('user.name@domain.co.uk')).toBe(true);
  });

  it('should reject invalid email addresses', () => {
    expect(validateEmail('invalid-email')).toBe(false);
    expect(validateEmail('test@')).toBe(false);
    expect(validateEmail('@example.com')).toBe(false);
  });
});
