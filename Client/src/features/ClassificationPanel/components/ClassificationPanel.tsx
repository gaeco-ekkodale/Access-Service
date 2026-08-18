// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

import SearchIcon from '@mui/icons-material/Search'
import Box from '@mui/material/Box'
import InputAdornment from '@mui/material/InputAdornment'
import TextField from '@mui/material/TextField'
import { useEffect, useMemo, useState } from 'react'
import { FixedSizeList } from 'react-window'
import { AccessRightDTO, ClassificationList, PropertyRight } from '../../../api/AccessService'
import { ClassificationRight } from '../../../models/ClassificationRight'
import PropertyDialog from '../../PropertyDialog/components/PropertyDialog'
import ClassificationCard from './ClassificationCard'

export type ClassificationPanelProps = {
	userGroupId: string
	useCaseId: string
	classificationList: ClassificationList[]
	selectedFilter: PropertyRight[]
	accessRights: AccessRightDTO[]
	onAccessRightsChange: (accessRights: AccessRightDTO[]) => void
}

const SEARCH_BAR_HEIGHT = 64
const CLASSIFICATIONS_PER_ROW = 4

export default function ClassificationPanel({
	userGroupId,
	useCaseId,
	classificationList,
	selectedFilter,
	accessRights,
	onAccessRightsChange,
}: ClassificationPanelProps) {
	const [dialogClassification, setDialogClassification] = useState<ClassificationList | null>(null)
	const [isDialogOpen, setIsDialogOpen] = useState(false)
	const [windowSize, setWindowSize] = useState({ height: window.innerHeight })
	const [searchInput, setSearchInput] = useState('')
	const [debouncedSearch, setDebouncedSearch] = useState('')

	useEffect(() => {
		const timer = setTimeout(() => setDebouncedSearch(searchInput), 250)
		return () => clearTimeout(timer)
	}, [searchInput])

	useEffect(() => {
		const handleResize = () => setWindowSize({ height: window.innerHeight })
		window.addEventListener('resize', handleResize)
		return () => window.removeEventListener('resize', handleResize)
	}, [])

	const { classificationRightsById, classificationSummaryById } = useMemo(() => {
		const nextClassificationRightsById = new Map<string, Set<PropertyRight>>()
		const nextClassificationSummaryById = new Map<string, ClassificationRight>()

		for (const accessRight of accessRights) {
			const classificationId = accessRight.guidelineClassificationId
			if (!classificationId) continue

			const rights = nextClassificationRightsById.get(classificationId) ?? new Set<PropertyRight>()
			rights.add(accessRight.right)
			nextClassificationRightsById.set(classificationId, rights)
		}

		nextClassificationRightsById.forEach((rights, classificationId) => {
			if (rights.has(PropertyRight.READ) && rights.has(PropertyRight.WRITE)) {
				nextClassificationSummaryById.set(classificationId, ClassificationRight.Mixed)
				return
			}
			if (rights.has(PropertyRight.READ)) {
				nextClassificationSummaryById.set(classificationId, ClassificationRight.Read)
				return
			}
			if (rights.has(PropertyRight.WRITE)) {
				nextClassificationSummaryById.set(classificationId, ClassificationRight.Write)
				return
			}
			nextClassificationSummaryById.set(classificationId, ClassificationRight.None)
		})

		return { classificationRightsById: nextClassificationRightsById, classificationSummaryById: nextClassificationSummaryById }
	}, [accessRights])

	const visibleClassifications = useMemo(() => {
		const query = debouncedSearch.toLowerCase()
		return classificationList
			.filter(classification => {
				if (query && !classification.name?.toLowerCase().includes(query)) {
					return false
				}
				if (selectedFilter.length === 0) {
					return true
				}
				const rights = classificationRightsById.get(classification.id ?? '')
				return rights ? selectedFilter.some(filter => rights.has(filter)) : false
			})
			.sort((a, b) => (a.name ?? '').localeCompare(b.name ?? ''))
	}, [classificationList, debouncedSearch, selectedFilter, classificationRightsById])

	const handleCardClick = (classification: ClassificationList) => {
		setDialogClassification(classification)
		setIsDialogOpen(true)
	}

	const handleDialogClose = () => {
		setIsDialogOpen(false)
		setDialogClassification(null)
	}

	const Row = ({ index, style }: { index: number; style: React.CSSProperties }) => {
		const startIndex = index * CLASSIFICATIONS_PER_ROW
		const itemsForRow = visibleClassifications.slice(startIndex, startIndex + CLASSIFICATIONS_PER_ROW)

		return (
			<div style={style} className='grid grid-cols-4 gap-4 px-4 py-2'>
				{itemsForRow.map(classification => (
					<ClassificationCard
						key={classification.id}
						cardClassification={classification}
						classificationRight={
							classificationSummaryById.get(classification.id ?? '') ?? ClassificationRight.None
						}
						onClick={handleCardClick}
					/>
				))}
			</div>
		)
	}

	return (
		<div className='flex flex-col'>
			<Box sx={{ px: 2, pt: 2, pb: 1 }}>
				<TextField
					fullWidth
					size='small'
					placeholder='Search classifications…'
					value={searchInput}
					onChange={e => setSearchInput(e.target.value)}
					slotProps={{
						input: {
							startAdornment: (
								<InputAdornment position='start'>
									<SearchIcon fontSize='small' />
								</InputAdornment>
							),
						},
					}}
				/>
			</Box>
			<FixedSizeList
				height={windowSize.height * 0.85 - SEARCH_BAR_HEIGHT}
				width='100%'
				itemSize={170}
				itemCount={Math.ceil(visibleClassifications.length / CLASSIFICATIONS_PER_ROW)}
				overscanCount={2}
			>
				{Row}
			</FixedSizeList>
			{dialogClassification ? (
				<PropertyDialog
					classification={dialogClassification}
					accessRights={accessRights}
					userGroupId={userGroupId}
					useCaseId={useCaseId}
					open={isDialogOpen}
					onClose={handleDialogClose}
					onAccessRightsChange={onAccessRightsChange}
				/>
			) : null}
		</div>
	)
}
