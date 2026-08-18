// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

import { useState } from 'react'
import { PropertyRight } from '../../../api/AccessService'
import RightChip from '../../../components/RightsChip'
import { ClassificationRight } from '../../../models/ClassificationRight'

type RightFilterProps = {
	label: string
	onFilterChange?: (rights: PropertyRight[]) => void
}

const RightFilter = ({ label, onFilterChange }: RightFilterProps) => {
	// State of the selection.
	const [selectedRights, setSelectedRights] = useState<PropertyRight[]>([])

	/**
	 * Helper function to remove the specified access right from the array of selected rights.
	 * @param right The access right to be removed.
	 * @param rights The array of selected access rights.
	 * @returns The updated array of selected rights after removal.
	 */
	const removeRight = (right: PropertyRight, rights: PropertyRight[]): PropertyRight[] => {
		return rights.filter(r => r !== right)
	}

	/**
	 * Helper function to add the specified access right to the array of selected rights.
	 * @param right The access right to be added.
	 * @param rights The array of selected rights.
	 * @returns The updated array of selected rights after addition.
	 */
	const addRight = (right: PropertyRight, rights: PropertyRight[]): PropertyRight[] => {
		return [...rights, right]
	}

	/**
	 * Handles the change event when a chip for an access right is toggled.
	 * @param right The access right being toggled.
	 */
	const handleOnChange = (givenRight: ClassificationRight) => {
		let updatedRights: PropertyRight[] = []
		let right: PropertyRight | undefined = undefined

		if (givenRight === ClassificationRight.Read) right = PropertyRight.READ
		if (givenRight === ClassificationRight.Write) right = PropertyRight.WRITE

		if (!right) {
			return
		}

		if (selectedRights.includes(right)) {
			updatedRights = removeRight(right, selectedRights)
		} else {
			updatedRights = addRight(right, selectedRights)
		}

		setSelectedRights(updatedRights)

		if (onFilterChange) {
			onFilterChange(updatedRights)
		}
	}

	return (
		<div className='flex justify-around'>
			<div className='flex items-center'>{label}</div>
			<RightChip classificationRight={ClassificationRight.Read} handleOnChange={handleOnChange} selectedRights={selectedRights} />
			<RightChip classificationRight={ClassificationRight.Write} handleOnChange={handleOnChange} selectedRights={selectedRights} />
		</div>
	)
}

export default RightFilter
