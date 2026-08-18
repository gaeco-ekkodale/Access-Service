// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

import { OpenAPI as AccessAPI } from './api/AccessService/core/OpenAPI'
import { OpenAPI as UsecaseAPI } from './api/UseCaseService/core/OpenAPI'
import { useAuth } from 'react-oidc-context'
import { Route, Routes } from 'react-router-dom'
import { useEffect } from 'react'
import { ThemeProvider } from '@emotion/react'
import { CssBaseline } from '@mui/material'
import { Toaster } from 'sonner'
import { baseTheme } from './styles/muiThemes'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import ProjectModule from './features/ProjectOverview/components/ProjectModule'
import './index.css'

// Query Client setzen
const queryClient = new QueryClient()

// API-Basis-URL setzen
AccessAPI.BASE = import.meta.env.VITE_ACCESS_SERVICE_API_URL
UsecaseAPI.BASE = import.meta.env.VITE_USECASE_SERVICE_API_URL

function App() {
	const auth = useAuth()
	const setTokens = (value: string | undefined) => {
		AccessAPI.TOKEN = value
		UsecaseAPI.TOKEN = value
	}

	setTokens(auth.user?.access_token)

	useEffect(() => {
		if (auth.user?.access_token) {
			setTokens(auth.user?.access_token)
		} else {
			setTokens(undefined)
		}
	}, [auth])

	return (
		<div className='h-full'>
			<Routes>
				<Route
					path='/*'
					element={
						<ThemeProvider theme={baseTheme}>
							<CssBaseline />
							<Toaster richColors />
							<QueryClientProvider client={queryClient}>
								<ProjectModule />
							</QueryClientProvider>
						</ThemeProvider>
					}
				/>
			</Routes>
		</div>
	)
}

export default App
