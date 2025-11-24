import { render } from '@testing-library/react';
import { SignInForm } from '../sign-in-form';

describe('SignInForm', () => {
  it('renders sign in form', () => {
    const { container } = render(<SignInForm />);
    const form = container.querySelector('form');
    expect(form).toBeTruthy();
  });

  it('should render without crashing', () => {
    const { container } = render(<SignInForm />);
    expect(container.firstChild).toBeTruthy();
  });
});
