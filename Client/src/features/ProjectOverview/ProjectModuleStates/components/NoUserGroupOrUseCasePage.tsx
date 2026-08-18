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
 * Shown when everything needed exists, but no use case / user group has been
 * picked yet. The "why" lives in the module tour; this only says what to do.
 */
const NoUserGroupOrUseCasePage = () => (
	<EmptyState
		title='Configure permissions'
		description='Pick a UseCase and a user group on the left to set read and write access per property.'
	/>
)

export default NoUserGroupOrUseCasePage
