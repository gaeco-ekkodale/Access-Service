// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

import { JwtPayload } from 'jwt-decode'

export interface JwtDTO extends JwtPayload {
	allowed_origins?: string[]
	realm_access?: {
		roles: string[]
	}
	scope?: string
	email_verified?: boolean
	groups?: string[]
	preferred_username?: string
	resource_access?: {
		[clientId: string]: {
			roles: string[]
		}
	}
}
