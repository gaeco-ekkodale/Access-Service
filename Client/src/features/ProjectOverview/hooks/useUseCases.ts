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
import { UseCasesService } from '../../../api/UseCaseService/services/UseCasesService'

/**
 * API call to retrieve all Use Cases.
 */
export function useUseCases() {
	return useQuery({
		queryKey: ['useCases'],
		queryFn: () => UseCasesService.getApiUseCases(),
		staleTime: 5 * 60 * 1000,
		retry: false,
	})
}
