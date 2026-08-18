// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

import Chip from '@mui/material/Chip'
import { ClassificationRight, getRightLabel } from '../models/ClassificationRight'
import { PropertyRight } from '../api/AccessService/models/PropertyRight'

interface RightChipProps {
	classificationRight: ClassificationRight
	selectedRights?: PropertyRight[]
	handleOnChange?: (right: ClassificationRight) => void
}

/**
 * Maps Right to ClassificationRight
 * @param right
 * @returns ClassificationRight
 */
const mapRightToClassificationRight = (right: PropertyRight): ClassificationRight => {
	switch (right) {
		case PropertyRight.NONE:
			return ClassificationRight.None
		case PropertyRight.READ:
			return ClassificationRight.Read
		case PropertyRight.WRITE:
			return ClassificationRight.Write
		default:
			return ClassificationRight.None
	}
}

export default function RightChip({ classificationRight, selectedRights, handleOnChange }: RightChipProps) {
	const label = getRightLabel(classificationRight)
	const isClickable = !!handleOnChange
	const mappedSelectedRights = selectedRights?.map(mapRightToClassificationRight)
	const isSelected = mappedSelectedRights?.includes(classificationRight)
	const backgroundColor = isClickable ? (isSelected ? classificationRight : '#e0e0e0') : classificationRight

	const handleClick = () => {
		if (handleOnChange) {
			handleOnChange(classificationRight)
		}
	}

	return (
		<div>
			<Chip label={label} clickable={isClickable} onClick={isClickable ? handleClick : undefined} style={{ backgroundColor }} />
		</div>
	)
}
