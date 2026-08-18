// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

import { UserManager, WebStorageStateStore } from 'oidc-client-ts'
import React, { useEffect, useState } from 'react'
import { AuthProvider, useAuth } from 'react-oidc-context'
import { BrowserRouter, Routes, Route } from 'react-router-dom'
import App from './App'
import MockHostNavigation from './components/MockHostNavigation'

const test_with_auth = true // TODO: Set to true to enable authentication if your Backend is secured with Keycloak
/**
 * StandaloneApp - Entry point for the microfrontend plugin application
 *
 * This component configures:
 * 1. Authentication via OpenID Connect (OIDC)
 * 2. Top-level routing with the configured mount path
 * 3. The browser router for client-side navigation
 * 4. A simulated host navigation bar
 *
 * In a microfrontend architecture, this is the container that encapsulates the plugin
 * and enables integration into the host application.
 */

// Configuration of the OIDC UserManager for authentication
const userManager = new UserManager({
	// Keycloak authentication server URL
	authority: import.meta.env.VITE_KEYCLOAK_AUTHORITY,

	// Client ID for authentication
	client_id: import.meta.env.VITE_KEYCLOAK_CLIENT_ID,

	// Requested permissions
	scope: 'groups openid profile email',

	// URL to which the user is redirected after successful login
	// Uses the configured mount path for the plugin
	redirect_uri: `${window.location.origin}/${import.meta.env.VITE_MOUNT_PATH}`,

	// URL to which the user is redirected after logout
	post_logout_redirect_uri: window.location.origin,

	// Storage location for authentication data (SessionStorage)
	userStore: new WebStorageStateStore({ store: window.sessionStorage }),

	// Enable session monitoring
	monitorSession: true,
})

/**
 * AuthenticatedApp - Component for authenticated users
 *
 * This component:
 * 1. Checks the user's authentication status
 * 2. Redirects to login if necessary
 * 3. Renders the main application only for authenticated users
 * 4. Configures routing with the correct mount path
 * 5. Displays a mock host navigation bar to simulate the host environment
 */
const AuthenticatedApp = () => {
	const auth = useAuth()
	const [hasTriedSignin, setHasTriedSignin] = useState(false)

	// Effect to check authentication status
	useEffect(() => {
		// If the user is not authenticated and no login attempt has been made yet
		if (
			test_with_auth &&
			!(
				auth.isAuthenticated || // Not authenticated
				auth.activeNavigator || // No active navigation
				auth.isLoading || // Not in loading state
				hasTriedSignin // No login attempt yet
			)
		) {
			// Redirect to login
			auth.signinRedirect()
			setHasTriedSignin(true)
		}
	}, [auth.isAuthenticated, auth.activeNavigator, auth.isLoading, auth, hasTriedSignin])

	return (
		<Routes>
			{/* Root path to redirect to the plugin mount path */}
			<Route
				path='/'
				element={
					<MockHostLayout>
						<div className='flex h-64 items-center justify-center'>
							<div className='text-center'>
								<h2 className='mb-4 text-2xl font-semibold'>Welcome to Plugin Demo</h2>
								<p className='mb-4 text-gray-600'>You are viewing the standalone version of the plugin.</p>
								<a href={`/${import.meta.env.VITE_MOUNT_PATH}`} className='rounded bg-blue-600 px-4 py-2 text-white hover:bg-blue-700'>
									Go to Plugin
								</a>
							</div>
						</div>
					</MockHostLayout>
				}
			/>

			{/* 
          Main route for the plugin
          - path: Uses the configured mount path environment variable
          - The * is important for forwarding all sub-routes
        */}
			<Route
				path={`/${import.meta.env.VITE_MOUNT_PATH}/*`}
				element={
					// Render the App only if the user is authenticated and has a token
					(auth.isAuthenticated && auth.user?.access_token) || !test_with_auth ? (
						<MockHostLayout>
							<App />
						</MockHostLayout>
					) : (
						// Otherwise show a loading indicator
						<div className='flex h-64 items-center justify-center'>
							<div className='text-center'>
								<div className='spinner-border inline-block h-8 w-8 animate-spin rounded-full border-4 border-blue-500 border-r-transparent' role='status'></div>
								<h2 className='mt-4 text-xl font-semibold'>Loading...</h2>
								<p className='text-gray-500'>Authenticating user...</p>
							</div>
						</div>
					)
				}
			/>
		</Routes>
	)
}

const StandaloneApp = () => {
	return (
		<BrowserRouter>
			<AuthProvider
				userManager={userManager}
				onSigninCallback={() => {
					window.history.replaceState({}, document.title, window.location.pathname)
				}}
			>
				<AuthenticatedApp />
			</AuthProvider>
		</BrowserRouter>
	)
}

const MockHostLayout = ({ children }: { children: React.ReactNode }) => {
	return (
		<div className='flex h-screen flex-col overflow-hidden'>
			<MockHostNavigation />
			<div className='w-full flex-1 overflow-auto'>{children}</div>
		</div>
	)
}

export default StandaloneApp
