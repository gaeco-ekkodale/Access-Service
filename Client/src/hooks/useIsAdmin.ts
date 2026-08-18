// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

import { useEffect, useState } from 'react'
import { jwtDecode } from 'jwt-decode'
import { JwtDTO } from '../models/JwtDTO'
import { useAuth } from 'react-oidc-context'

const useIsAdmin = (): boolean => {
	const auth = useAuth()
	const [isAdmin, setIsAdmin] = useState(false)

	useEffect(() => {
		const rawToken = auth.user?.access_token
		if (auth.isAuthenticated && rawToken) {
			const token = jwtDecode<JwtDTO>(rawToken)
			const clientId = import.meta.env.VITE_KEYCLOAK_CLIENT_ID
			const roles = token?.resource_access?.[clientId]?.roles || []
			setIsAdmin(roles.includes(import.meta.env.VITE_ADMIN_ROLE_NAME))
		} else {
			setIsAdmin(false)
		}
	}, [auth])
	return isAdmin
}

export default useIsAdmin
