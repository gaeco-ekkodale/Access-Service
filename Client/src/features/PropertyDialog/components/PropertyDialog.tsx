// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

import { Box } from '@mui/material'
import Button from '@mui/material/Button'
import CircularProgress from '@mui/material/CircularProgress'
import Dialog from '@mui/material/Dialog'
import DialogActions from '@mui/material/DialogActions'
import DialogContent from '@mui/material/DialogContent'
import DialogTitle from '@mui/material/DialogTitle'
import { styled } from '@mui/material/styles'
import { useMemo, useState } from 'react'
import { toast } from 'sonner'
import { AccessRightDTO, ClassificationList, PropertyRight } from '../../../api/AccessService'
import useCheckUserRoles from '../../../hooks/useCheckUserRoles'
import useIsAdmin from '../../../hooks/useIsAdmin'
import { buildAccessRightKey } from '../../../utils/accessRightsDraft'
import { useClassificationProperties } from '../../ClassificationPanel/hooks/useClassificationProperties'
import { AccessRightSelectBase as SelectBar } from './AccessRightSelectBar'
import PropertyList, { PropertyGroup } from './PropertyList'

type PropertyDialogProps = {
	classification: ClassificationList
	accessRights: AccessRightDTO[]
	userGroupId: string
	useCaseId: string
	open: boolean
	onClose: () => void
	onAccessRightsChange: (accessRights: AccessRightDTO[]) => void
}

const BootstrapDialog = styled(Dialog)(({ theme }) => ({
	'& .MuiDialogContent-root': {
		padding: theme.spacing(2),
	},
	'& .MuiDialogActions-root': {
		padding: theme.spacing(1),
	},
	'& .MuiDialog-paper': {
		minWidth: '700px',
		maxWidth: '900px',
		width: '80vw',
	},
}))

export default function PropertyDialog({
	classification,
	accessRights,
	userGroupId,
	useCaseId,
	open,
	onClose,
	onAccessRightsChange,
}: PropertyDialogProps) {
	const [selectedAccessRight, setSelectedAccessRight] = useState<PropertyRight>(PropertyRight.NONE)
	const isAuthorised = useCheckUserRoles([import.meta.env.VITE_ADMIN_ROLE_NAME])
	const isAdmin = useIsAdmin()
	const {
		data: propertiesData,
		isPending: isLoadingProperties,
		isFetching: isRefreshingProperties,
	} = useClassificationProperties(classification.id, open)
	const canEditProperties = isAuthorised && Boolean(useCaseId) && Boolean(userGroupId)

	const accessRightsMap = useMemo(
		() => new Map(accessRights.map(accessRight => [buildAccessRightKey(accessRight), accessRight])),
		[accessRights],
	)

	const mergedProperties = useMemo(() => {
		if (!propertiesData || propertiesData.length === 0) {
			return []
		}

		return propertiesData.map(property => {
			const stub: AccessRightDTO = {
				id: '',
				name: property.name ?? '',
				guidelineClassificationId: classification.id ?? '',
				userGroupId,
				useCaseId,
				guidlineClassificationPropertyId: property.id ?? '',
				right: PropertyRight.NONE,
			}

			const found = accessRightsMap.get(buildAccessRightKey(stub))

			return found ? { ...stub, ...found } : stub
		})
	}, [accessRightsMap, propertiesData, classification.id, userGroupId, useCaseId])

	const groupedProperties = useMemo<{ sets: PropertyGroup[]; standalone: AccessRightDTO[] }>(() => {
		if (!propertiesData || mergedProperties.length === 0) {
			return { sets: [], standalone: [] }
		}

		const propToSet = new Map(
			propertiesData.map(p => [p.id ?? '', { id: p.propertySetId ?? '', name: p.propertySetName ?? '' }]),
		)

		const setMap = new Map<string, PropertyGroup>()
		const standalone: AccessRightDTO[] = []

		for (const prop of mergedProperties) {
			const setInfo = propToSet.get(prop.guidlineClassificationPropertyId ?? '')
			if (setInfo?.id) {
				if (!setMap.has(setInfo.id)) {
					setMap.set(setInfo.id, { id: setInfo.id, name: setInfo.name, properties: [] })
				}
				setMap.get(setInfo.id)!.properties.push(prop)
			} else {
				standalone.push(prop)
			}
		}

		const sets = Array.from(setMap.values())
			.sort((a, b) => a.name.localeCompare(b.name))
			.map(s => ({ ...s, properties: [...s.properties].sort((a, b) => (a.name ?? '').localeCompare(b.name ?? '')) }))

		return {
			sets,
			standalone: [...standalone].sort((a, b) => (a.name ?? '').localeCompare(b.name ?? '')),
		}
	}, [propertiesData, mergedProperties])

	const handleAccessRightChange = (selectedRight: PropertyRight) => {
		setSelectedAccessRight(selectedRight)
	}

	const handlePropertyRightChange = (property: AccessRightDTO, right: PropertyRight) => {
		onAccessRightsChange([
			{
				...property,
				right,
				id: property.id || '',
			},
		])
	}

	const handleMassApply = () => {
		const changedProperties = mergedProperties
			.filter(property => property.right !== selectedAccessRight)
			.map(property => ({
				...property,
				right: selectedAccessRight,
				id: property.id || '',
			}))

		if (changedProperties.length === 0) {
			toast.info('No changes to apply')
			return
		}

		onAccessRightsChange(changedProperties)
		toast.success(`Staged ${changedProperties.length} properties`)
	}

	return (
		<BootstrapDialog onClose={onClose} aria-labelledby='customized-dialog-title' open={open}>
			<DialogTitle sx={{ m: 0, p: 2 }} id='customized-dialog-title'>
				{classification.name}
				<div className='flex justify-end'>
					{mergedProperties.length > 0 && canEditProperties ? (
						<Box display='flex'>
							<Button
								variant='contained'
								className='min-w-[15%] justify-end'
								onClick={handleMassApply}
							>
								MassApply
							</Button>
							<Box ml={2}>
								<SelectBar
									loadedAccessRight={{
										id: '',
										name: '',
										guidelineClassificationId: '',
										userGroupId: '',
										useCaseId: '',
										guidlineClassificationPropertyId: '',
										right: selectedAccessRight,
									}}
									isAuthorised={canEditProperties}
									canAssignNone={isAdmin}
									isMassApply={true}
									onSelectAccessRight={handleAccessRightChange}
								/>
							</Box>
						</Box>
					) : null}
				</div>
			</DialogTitle>
			<DialogContent dividers>
				{isLoadingProperties ? (
					<Box display='flex' flexDirection='column' alignItems='center' gap={2} p={4}>
						<CircularProgress size={32} />
						<span className='text-sm text-gray-500'>Loading properties...</span>
					</Box>
				) : useCaseId && userGroupId ? (
					<Box className='relative'>
						{isRefreshingProperties ? (
							<Box className='pointer-events-none absolute top-2 right-2 z-10 rounded-full bg-white/80 p-1'>
								<CircularProgress size={18} />
							</Box>
						) : null}
						<PropertyList
							propertySets={groupedProperties.sets}
							standaloneProperties={groupedProperties.standalone}
							isAuthorised={canEditProperties}
							canAssignNone={isAdmin}
							onPropertyRightChange={handlePropertyRightChange}
						/>
					</Box>
				) : (
					'Please select a UseCase and a user group'
				)}
			</DialogContent>
			<DialogActions>
				<Button autoFocus onClick={onClose}>
					Close
				</Button>
			</DialogActions>
		</BootstrapDialog>
	)
}
