'use client';

type CookiePreferencesProps = {
  // TODO: Add props here.
};

export default function CookiePreferences({}: CookiePreferencesProps) {
  return (
    <form>
      <button type={'reset'}>Reset to Default</button>
      <button type={'submit'}>Save Preferences</button>
    </form>
  );
}
