// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

import { createTheme } from '@mui/material'
import { colors } from '@mui/material'

export const baseTheme = createTheme({
	palette: {
		primary: {
			main: colors.blue['A400'],
			light: colors.blue[300],
		},
		secondary: {
			main: colors.blueGrey['A400'],
			light: colors.red[300],
		},
		background: { default: '#F7F7F7' },
	},

	typography: {
		fontFamily: ['Roboto', '"Helvetica Neue"', 'Arial', 'sans-serif'].join(','),
		h1: {
			fontSize: '2.5rem',
			fontWeight: 500,
		},
		h2: {
			fontSize: '2rem',
			fontWeight: 500,
		},
		body1: {
			fontSize: '1rem',
			fontWeight: 400,
		},
		body2: {
			fontSize: '0.875rem',
			fontWeight: 400,
		},
	},
})
