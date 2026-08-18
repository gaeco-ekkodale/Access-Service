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
import {
	Box,
	Button,
	Fab,
	IconButton,
	LinearProgress,
	Paper,
	ThemeProvider,
	Tooltip,
	Typography,
	createTheme,
} from '@mui/material'
import HelpOutlineIcon from '@mui/icons-material/HelpOutline'
import CloseRoundedIcon from '@mui/icons-material/CloseRounded'
import ArrowBackRoundedIcon from '@mui/icons-material/ArrowBackRounded'
import ArrowForwardRoundedIcon from '@mui/icons-material/ArrowForwardRounded'

import { useTourState } from './useTourState'

export type TourPanel = {
	title: string
	body: string
}

export type TourProps = {
	/** Stable key for this module's tour, used for the storage key. */
	tourKey: string
	/** Module name, shown in the panel header. */
	moduleName: string
	panels: TourPanel[]
}

/**
 * The tour carries its own theme on purpose.
 *
 * Each module themes MUI differently - one forces a colour on every Typography variant,
 * another restyles buttons and tooltips. Inheriting that would make the same tutorial look
 * different in every module. A local ThemeProvider pins the appearance so the tour is
 * recognisably the same thing everywhere.
 */
const tourTheme = createTheme({
	palette: {
		primary: { main: '#1d4ed8' },
		text: { primary: '#0f172a', secondary: '#475569' },
		background: { paper: '#ffffff' },
		divider: '#e2e8f0',
	},
	typography: {
		fontFamily: ['Inter', 'Roboto', '"Helvetica Neue"', 'Arial', 'sans-serif'].join(','),
	},
	components: {
		MuiButton: { styleOverrides: { root: { textTransform: 'none', fontWeight: 600 } } },
		MuiTooltip: { styleOverrides: { tooltip: { fontSize: 12 } } },
	},
})

/**
 * Short tutorial for this module, shown as a floating panel in the bottom right corner,
 * plus the "?" button that reopens it.
 *
 * Deliberately NOT a modal: the panel sits next to the real interface so the steps can be
 * followed while actually clicking through them. Nothing behind it is blocked.
 *
 * Placement is fixed to the viewport corner rather than to the module's own layout, so the
 * button sits in the same spot in every module and cannot collide with local toolbars.
 */
const Tour = ({ tourKey, moduleName, panels }: TourProps) => {
	const { status, markCompleted, markSkipped } = useTourState(tourKey)
	const [open, setOpen] = useState(false)
	const [index, setIndex] = useState(0)

	// Opens itself on the very first visit; afterwards only via the "?" button.
	useEffect(() => {
		if (status === 'unseen') setOpen(true)
	}, [status])

	const isFirst = index === 0
	const isLast = index === panels.length - 1
	const panel = panels[index]

	const close = (completed: boolean) => {
		setOpen(false)
		setIndex(0)
		if (completed) markCompleted()
		else markSkipped()
	}

	if (!open) {
		return (
			<ThemeProvider theme={tourTheme}>
				<Tooltip title={`How ${moduleName} works`} placement='left'>
					<Fab
						size='small'
						aria-label={`How ${moduleName} works`}
						onClick={() => {
							setIndex(0)
							setOpen(true)
						}}
						sx={{
							position: 'fixed',
							right: 24,
							bottom: 24,
							zIndex: 1250,
							bgcolor: '#ffffff',
							color: '#475569',
							'&:hover': { bgcolor: '#ffffff', color: '#1d4ed8' },
						}}
					>
						<HelpOutlineIcon />
					</Fab>
				</Tooltip>
			</ThemeProvider>
		)
	}

	return (
		<ThemeProvider theme={tourTheme}>
			<Paper
				elevation={10}
				role='dialog'
				aria-modal='false'
				aria-label={`${moduleName} tutorial`}
				sx={{
					position: 'fixed',
					right: 24,
					bottom: 24,
					zIndex: 1250,
					width: 420,
					maxWidth: 'calc(100vw - 32px)',
					maxHeight: 'calc(100vh - 120px)',
					borderRadius: 3,
					overflow: 'hidden',
					display: 'flex',
					flexDirection: 'column',
					bgcolor: '#ffffff',
				}}
			>
				<LinearProgress
					variant='determinate'
					value={((index + 1) / panels.length) * 100}
					sx={{ height: 4, bgcolor: '#e2e8f0' }}
				/>

				{/* Header: which module, how far along, and a way out. */}
				<Box
					sx={{
						display: 'flex',
						alignItems: 'center',
						gap: 1,
						px: 2.5,
						py: 1.5,
						bgcolor: '#f8fafc',
						borderBottom: '1px solid #e2e8f0',
					}}
				>
					<HelpOutlineIcon fontSize='small' sx={{ color: '#1d4ed8' }} />
					<Typography
						variant='caption'
						sx={{
							flex: 1,
							color: '#334155',
							fontWeight: 700,
							letterSpacing: '0.08em',
							textTransform: 'uppercase',
						}}
					>
						{moduleName}
					</Typography>
					<Typography
						variant='caption'
						sx={{ color: '#64748b', fontVariantNumeric: 'tabular-nums' }}
					>
						{index + 1} / {panels.length}
					</Typography>
					<IconButton
						size='small'
						aria-label='Close tutorial'
						onClick={() => close(isLast)}
						sx={{ ml: 0.5, color: '#64748b' }}
					>
						<CloseRoundedIcon fontSize='small' />
					</IconButton>
				</Box>

				<Box sx={{ px: 2.5, py: 2, overflowY: 'auto' }}>
					<Typography
						sx={{ color: '#0f172a', fontSize: '1rem', fontWeight: 700, lineHeight: 1.35 }}
					>
						{panel?.title}
					</Typography>
					<Typography
						sx={{ mt: 1, color: '#475569', fontSize: '0.875rem', lineHeight: 1.65 }}
					>
						{panel?.body}
					</Typography>
				</Box>

				<Box
					sx={{
						display: 'flex',
						alignItems: 'center',
						justifyContent: 'space-between',
						gap: 1,
						px: 2,
						py: 1.25,
						borderTop: '1px solid #e2e8f0',
					}}
				>
					{isFirst ? (
						<Button size='small' onClick={() => close(false)} sx={{ color: '#64748b' }}>
							Skip
						</Button>
					) : (
						<Button
							size='small'
							startIcon={<ArrowBackRoundedIcon fontSize='small' />}
							onClick={() => setIndex(index - 1)}
							sx={{ color: '#64748b' }}
						>
							Back
						</Button>
					)}
					<Button
						size='small'
						variant='contained'
						disableElevation
						endIcon={isLast ? undefined : <ArrowForwardRoundedIcon fontSize='small' />}
						onClick={() => (isLast ? close(true) : setIndex(index + 1))}
						sx={{ bgcolor: '#1d4ed8', '&:hover': { bgcolor: '#1a43b8' } }}
					>
						{isLast ? 'Got it' : 'Next'}
					</Button>
				</Box>
			</Paper>
		</ThemeProvider>
	)
}

export default Tour
