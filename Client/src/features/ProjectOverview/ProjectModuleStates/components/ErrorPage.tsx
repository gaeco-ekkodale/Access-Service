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
 * Shown when loading failed. Kept distinct from the empty states on purpose:
 * this is a reachability problem, not missing setup.
 */
const ErrorPage = () => (
	<EmptyState
		tone='error'
		title='Could not load the data'
		description='The data model or the list of UseCases could not be loaded.'
		footnote='A connection problem, not missing setup. Check that the services are running, then reload.'
	/>
)

export default ErrorPage
