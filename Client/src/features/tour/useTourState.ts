// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

import { useCallback, useEffect, useState } from 'react'

export type TourStatus = 'unseen' | 'completed' | 'skipped'

/**
 * Bump when the panels of a tour change substantially. Testers who already
 * dismissed the old version would otherwise never see the improved one.
 */
export const TOUR_VERSION = 2

const storageKeyFor = (tourKey: string) => `gaeco.tour.${tourKey}.v${TOUR_VERSION}`

/** localStorage can be unavailable (private mode, blocked storage). */
const readStatus = (key: string): TourStatus => {
	try {
		const raw = window.localStorage.getItem(key)
		return raw === 'completed' || raw === 'skipped' ? raw : 'unseen'
	} catch {
		return 'unseen'
	}
}

const writeStatus = (key: string, status: TourStatus): void => {
	try {
		window.localStorage.setItem(key, status)
	} catch {
		// Not being able to remember is acceptable; the tour stays re-openable.
	}
}

/**
 * Per-module tour state. Only the user's *choice* is persisted — never any
 * notion of progress through the platform, which is always derived from data.
 */
export const useTourState = (tourKey: string) => {
	const key = storageKeyFor(tourKey)
	const [status, setStatus] = useState<TourStatus>('completed')

	// Read after mount so a blocked localStorage cannot break the first render.
	useEffect(() => {
		setStatus(readStatus(key))
	}, [key])

	const persist = useCallback(
		(next: TourStatus) => {
			setStatus(next)
			writeStatus(key, next)
		},
		[key]
	)

	return {
		status,
		markCompleted: useCallback(() => persist('completed'), [persist]),
		markSkipped: useCallback(() => persist('skipped'), [persist]),
	}
}
