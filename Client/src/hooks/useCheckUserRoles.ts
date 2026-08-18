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

const useCheckUserRoles = (requiredRoles: string[]): boolean => {
	const auth = useAuth()
	const [isAuthorised, setIsAuthorised] = useState(false)

	useEffect(() => {
		const rawToken = auth.user?.access_token
		if (auth.isAuthenticated && rawToken) {
			const token = jwtDecode<JwtDTO>(rawToken)
			const roles = token?.resource_access?.[import.meta.env.VITE_KEYCLOAK_CLIENT_ID]?.roles ?? []
			const hasRequiredRole = requiredRoles.some(role => roles.includes(role))
			setIsAuthorised(hasRequiredRole)
		} else {
			setIsAuthorised(false)
		}
	}, [requiredRoles, auth])

	return isAuthorised
}

export default useCheckUserRoles
