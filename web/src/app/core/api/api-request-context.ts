import { HttpContextToken } from '@angular/common/http';

export const SKIP_ACCESS_TOKEN = new HttpContextToken<boolean>(() => false);
export const SEND_BROWSER_CREDENTIALS = new HttpContextToken<boolean>(() => false);
export const SEND_CSRF_TOKEN = new HttpContextToken<boolean>(() => false);
