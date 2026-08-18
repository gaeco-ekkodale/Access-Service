// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

import { TourPanel } from './Tour'

export const TOUR_KEY = 'access'
export const TOUR_MODULE_NAME = 'Access Rights'

/**
 * Describes this module and its place in gaeco - nothing beyond it. No pointers to other
 * modules or to tools outside the platform.
 *
 * Kept as data, not JSX, so the wording can be revised without touching a component.
 */
export const TOUR_PANELS: TourPanel[] = [
	{
		title: 'Who sees what',
		body: 'Here you decide which parts of the data model a user group may see and change, within one UseCase. Access is granted per classification and per individual property.',
	},
	{
		title: 'Set the context first',
		body: 'Choose a UseCase and a user group on the left. Nothing appears until both are set, because permissions always apply to that exact pair.',
	},
	{
		title: 'Then work through the classifications',
		body: 'Open a classification to see its properties and set each one to Read, Write or None: Read shows the property, Write also allows editing it, None keeps it hidden from the group. The guideline selector shortens a long list.',
	},
	{
		title: 'Save when you are done',
		body: 'Your edits are collected first, and the number of pending ones is shown next to “Save changes”. Choosing it writes them all at once, so a whole classification can be worked through before anything takes effect.',
	},
]
