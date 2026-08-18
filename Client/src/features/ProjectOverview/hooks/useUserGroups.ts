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
import { UserGroupsService } from '../../../api/AccessService'

/**
 * API call to retrieve all User Groups.
 */
export function useUserGroups() {
	return useQuery({
		queryKey: ['userGroup'],
		queryFn: () => UserGroupsService.getKeycloakGroups(),
		staleTime: 5 * 60 * 1000,
		retry: false,
	})
}
