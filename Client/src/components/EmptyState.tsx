// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

import { ReactNode } from 'react'

export type EmptyStateProps = {
	/** What this screen is for. */
	title: string
	/** Why it is empty and what the user should do next. */
	description: ReactNode
	/** Optional primary action. */
	action?: ReactNode
	/** Optional prerequisite or background note, rendered smaller. */
	footnote?: ReactNode
	/** Renders the title in an error tone instead of neutral. */
	tone?: 'neutral' | 'error'
}

/**
 * Shared presentation for the "nothing to show" states of this client.
 * Follows the platform pattern: what the screen is, why it is empty,
 * the one action to take, and any prerequisite worth naming.
 */
const EmptyState = ({
	title,
	description,
	action,
	footnote,
	tone = 'neutral',
}: EmptyStateProps) => (
	<div className='flex h-full items-center justify-center p-8'>
		<div className='max-w-md text-center'>
			<h2
				className={`text-lg font-semibold ${
					tone === 'error' ? 'text-red-700' : 'text-gray-800'
				}`}
			>
				{title}
			</h2>
			<p className='mt-2 text-sm text-gray-600'>{description}</p>
			{action ? <div className='mt-4'>{action}</div> : null}
			{footnote ? (
				<p className='mt-4 text-xs text-gray-500'>{footnote}</p>
			) : null}
		</div>
	</div>
)

export default EmptyState
