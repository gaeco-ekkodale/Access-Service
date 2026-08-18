// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

import { useQuery } from '@tanstack/react-query'
import { AccessRightsService } from '../../../api/AccessService'

/**
 * API call to retrieve all acess rights for the use case and user group.
 */
export function useAccessRights(useCaseId: string, userGroupId: string) {
	return useQuery({
		queryKey: ['accessRight', userGroupId, useCaseId],
		queryFn: () => AccessRightsService.getAccessRightsByUseCaseUserGroupAsync(useCaseId, userGroupId),
		enabled: !!useCaseId && !!userGroupId,
	})
}
