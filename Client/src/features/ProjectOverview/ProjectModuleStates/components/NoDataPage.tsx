// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

import EmptyState from '../../../../components/EmptyState'

/**
 * Type definition for data checks.
 * Each item names a prerequisite and whether it is present.
 */
export type DataCheck = {
	/**
	 * Human-readable name of the prerequisite, used inside a sentence
	 * (e.g. "no data model and no use cases have been created yet").
	 */
	dataType: string
	/**
	 * A boolean indicating if the data is available.
	 */
	isAvailable: boolean
}

export type NoDataPageProps = {
	/**
	 * The prerequisites this screen depends on, and their availability.
	 */
	dataChecks: DataCheck[]
}

/** Joins names into "a", "a and b", "a, b and c". */
const joinNames = (names: string[]): string => {
	if (names.length <= 1) return names[0] ?? ''
	return `${names.slice(0, -1).join(', ')} and ${names[names.length - 1]}`
}

/**
 * Shown when permissions cannot be configured yet because a prerequisite from
 * another part of the platform is still missing. Names what is missing and
 * where it comes from, instead of reporting the bare data types.
 */
const NoDataPage = ({ dataChecks }: NoDataPageProps) => {
	const missing = dataChecks
		.filter(dataCheck => !dataCheck.isAvailable)
		.map(dataCheck => dataCheck.dataType)

	// Base case: this page should not be reached with nothing missing.
	if (missing.length === 0) {
		return (
			<EmptyState
				title='Nothing to show'
				description='No object types came back for the current selection. Try a different guideline.'
			/>
		)
	}

	return (
		<EmptyState
			title='Nothing to configure yet'
			description={`Still missing: ${joinNames(missing)}.`}
			footnote='The data model is uploaded in Platform Config; UseCases are created in the UseCases module.'
		/>
	)
}

export default NoDataPage
