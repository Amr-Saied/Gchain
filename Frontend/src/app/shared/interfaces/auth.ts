export interface GuestUserInfo {
  id: string;
  email: string;
  name: string;
  firstName: string;
  lastName: string;
  picture: string;
  emailVerified: boolean;
  locale?: string | null;
}

export interface UserSessionInfo {
  // shape unknown from snippet, keep flexible
  [key: string]: unknown;
}

export interface UserProfileData {
  // shape unknown from snippet, keep flexible
  [key: string]: unknown;
}

export interface GuestAuthResponse {
  accessToken: string;
  refreshToken: string;
  expiresAt: string; // Date serialized
  user: GuestUserInfo;
  isNewUser: boolean;
  databaseUserId: string;
  userSession?: UserSessionInfo | null;
  userProfile?: UserProfileData | null;
}

// Google OAuth
export interface GoogleOAuthRequest {
  token: string;
  tokenType: 'code' | 'id_token';
}

export interface GoogleUserInfo {
  id: string;
  email: string;
  name: string;
  firstName: string;
  lastName: string;
  picture: string;
  emailVerified: boolean;
  locale?: string | null;
}

export interface GoogleOAuthResponse {
  accessToken: string;
  refreshToken: string;
  expiresAt: string;
  user: GoogleUserInfo;
  isNewUser: boolean;
  databaseUserId: string;
  userSession?: UserSessionInfo | null;
  userProfile?: UserProfileData | null;
}

export interface RefreshTokenRequest {
  refreshToken: string;
}

export interface RefreshTokenResponse {
  accessToken: string;
  refreshToken: string;
  expiresAt: string; // Date serialized
}
