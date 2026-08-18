// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

/**
 * AccessRightSelect
 * -----------------
 * Select bar component to view and change the access right for a property.
 * Handles selecting an access right for a property draft entry.
 *
 * The parent component owns the draft state and decides how to persist it.
 *
 * Props:
 * - loadedAccessRight: The property and its current right (might be right=NONE if not in DB)
 * - isMassApply: Marks this select as the shared mass-apply selector
 * - onSelectAccessRight: Callback for propagating the selected right
 */

import Box from '@mui/material/Box'
import FormControl from '@mui/material/FormControl'
import MenuItem from '@mui/material/MenuItem'
import Select, { SelectChangeEvent } from '@mui/material/Select'
import { toast } from 'sonner'
import { AccessRightDTO, PropertyRight } from '../../../api/AccessService'
import useCheckUserRoles from '../../../hooks/useCheckUserRoles'
import useIsAdmin from '../../../hooks/useIsAdmin'

type AccessRightSelectProps = {
	loadedAccessRight: AccessRightDTO
	isMassApply: boolean
	onSelectAccessRight?: (right: PropertyRight) => void
	isAuthorised: boolean
	canAssignNone: boolean
}

export function AccessRightSelectBase({
	loadedAccessRight,
	isAuthorised,
	canAssignNone,
	onSelectAccessRight,
}: AccessRightSelectProps) {
	/**
	 * Handles a change in the dropdown selection.
	 * Propagates the selected right to the parent draft state.
	 */
	const handleChange = (event: SelectChangeEvent) => {
		// Determine which PropertyRight the user selected
		let newRight: PropertyRight | undefined = undefined
		switch (event.target.value) {
			case PropertyRight.READ:
				newRight = PropertyRight.READ
				break
			case PropertyRight.WRITE:
				newRight = PropertyRight.WRITE
				break
			case PropertyRight.NONE:
				newRight = PropertyRight.NONE
				break
			default:
				toast.error('Invalid access right: ' + event.target.value)
				return
		}

		// Only admins can remove access rights
		if (newRight === PropertyRight.NONE && !canAssignNone) {
			toast.error('Only admins can set access rights to "None".')
			return
		}

		onSelectAccessRight?.(newRight)
	}

	return (
		<div className='min-w-25'>
			<Box>
				<FormControl fullWidth>
					<Select
						className={
							loadedAccessRight.right === PropertyRight.NONE
								? 'bg-none-background text-none-text'
								: loadedAccessRight.right === PropertyRight.READ
									? 'bg-read-background text-read-text'
									: loadedAccessRight.right === PropertyRight.WRITE
										? 'bg-write-background text-write-text'
										: ''
						}
						labelId='access-right-select-label'
						id='access-right-select'
						value={loadedAccessRight.right}
						renderValue={accessRight => accessRight}
						onChange={handleChange}
						displayEmpty
						disabled={!isAuthorised}
					>
						{/* List all available access rights (enum pattern) */}
						{Object.keys(PropertyRight)
							.filter(k => isNaN(Number(k)))
							.map(key => (
								<MenuItem key={key} value={PropertyRight[key as keyof typeof PropertyRight]}>
									{PropertyRight[key as keyof typeof PropertyRight]}
								</MenuItem>
							))}
					</Select>
				</FormControl>
			</Box>
		</div>
	)
}

type AccessRightSelectWithAuthProps = Omit<AccessRightSelectProps, 'isAuthorised' | 'canAssignNone'>

export default function AccessRightSelect(props: AccessRightSelectWithAuthProps) {
	const isAuthorised = useCheckUserRoles([import.meta.env.VITE_ADMIN_ROLE_NAME])
	const isAdmin = useIsAdmin()

	return <AccessRightSelectBase {...props} isAuthorised={isAuthorised} canAssignNone={isAdmin} />
}
