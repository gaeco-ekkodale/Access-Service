// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

import { AccessRightDTO, PropertyRight } from '../api/AccessService'

type AccessRightIdentity = Pick<AccessRightDTO, 'guidelineClassificationId' | 'guidlineClassificationPropertyId'>

export function buildAccessRightKey(accessRight: AccessRightIdentity): string {
	return `${accessRight.guidelineClassificationId}::${accessRight.guidlineClassificationPropertyId}`
}

export function sortAccessRights(accessRights: AccessRightDTO[]): AccessRightDTO[] {
	return [...accessRights]
		.filter(accessRight => accessRight.right !== PropertyRight.NONE)
		.map(accessRight => ({ ...accessRight, id: accessRight.id || '' }))
		.sort((left, right) => {
			const classificationCompare = left.guidelineClassificationId.localeCompare(right.guidelineClassificationId)

			if (classificationCompare !== 0) {
				return classificationCompare
			}

			return left.guidlineClassificationPropertyId.localeCompare(right.guidlineClassificationPropertyId)
		})
}

export function areAccessRightsEqual(left: AccessRightDTO[], right: AccessRightDTO[]): boolean {
	const normalizedLeft = sortAccessRights(left)
	const normalizedRight = sortAccessRights(right)

	if (normalizedLeft.length !== normalizedRight.length) {
		return false
	}

	return normalizedLeft.every((accessRight, index) => {
		const other = normalizedRight[index]

		return accessRight.id === other.id
			&& accessRight.name === other.name
			&& accessRight.guidelineClassificationId === other.guidelineClassificationId
			&& accessRight.userGroupId === other.userGroupId
			&& accessRight.useCaseId === other.useCaseId
			&& accessRight.guidlineClassificationPropertyId === other.guidlineClassificationPropertyId
			&& accessRight.right === other.right
	})
}

export function upsertAccessRightsDraft(currentDraft: AccessRightDTO[], changedAccessRights: AccessRightDTO[]): AccessRightDTO[] {
	const draftMap = new Map(sortAccessRights(currentDraft).map(accessRight => [buildAccessRightKey(accessRight), accessRight]))

	changedAccessRights.forEach(accessRight => {
		const key = buildAccessRightKey(accessRight)

		if (accessRight.right === PropertyRight.NONE) {
			draftMap.delete(key)
			return
		}

		const existing = draftMap.get(key)
		draftMap.set(key, {
			...existing,
			...accessRight,
			id: accessRight.id || existing?.id || '',
		})
	})

	return sortAccessRights([...draftMap.values()])
}

export function countAccessRightChanges(originalAccessRights: AccessRightDTO[], draftAccessRights: AccessRightDTO[]): number {
	const originalMap = new Map(sortAccessRights(originalAccessRights).map(accessRight => [buildAccessRightKey(accessRight), accessRight]))
	const draftMap = new Map(sortAccessRights(draftAccessRights).map(accessRight => [buildAccessRightKey(accessRight), accessRight]))
	const allKeys = new Set([...originalMap.keys(), ...draftMap.keys()])

	let changes = 0

	allKeys.forEach(key => {
		const original = originalMap.get(key)
		const draft = draftMap.get(key)

		if (!original || !draft) {
			changes += 1
			return
		}

		if (
			original.id !== draft.id
			|| original.name !== draft.name
			|| original.guidelineClassificationId !== draft.guidelineClassificationId
			|| original.userGroupId !== draft.userGroupId
			|| original.useCaseId !== draft.useCaseId
			|| original.guidlineClassificationPropertyId !== draft.guidlineClassificationPropertyId
			|| original.right !== draft.right
		) {
			changes += 1
		}
	})

	return changes
}
