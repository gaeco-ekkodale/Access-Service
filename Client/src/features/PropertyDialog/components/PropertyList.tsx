// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

import ExpandMoreIcon from '@mui/icons-material/ExpandMore'
import SearchIcon from '@mui/icons-material/Search'
import Accordion from '@mui/material/Accordion'
import AccordionDetails from '@mui/material/AccordionDetails'
import AccordionSummary from '@mui/material/AccordionSummary'
import Box from '@mui/material/Box'
import Chip from '@mui/material/Chip'
import InputAdornment from '@mui/material/InputAdornment'
import List from '@mui/material/List'
import ListItem from '@mui/material/ListItem'
import ListItemText from '@mui/material/ListItemText'
import TextField from '@mui/material/TextField'
import Typography from '@mui/material/Typography'
import { useEffect, useState } from 'react'
import { AccessRightDTO, PropertyRight } from '../../../api/AccessService'
import { AccessRightSelectBase as AccessRightSelect } from './AccessRightSelectBar'

export type PropertyGroup = {
	id: string
	name: string
	properties: AccessRightDTO[]
}

type PropertyListProps = {
	propertySets: PropertyGroup[]
	standaloneProperties: AccessRightDTO[]
	isAuthorised: boolean
	canAssignNone: boolean
	onPropertyRightChange: (property: AccessRightDTO, right: PropertyRight) => void
}

function PropertyRow({
	property,
	isAuthorised,
	canAssignNone,
	onPropertyRightChange,
}: {
	property: AccessRightDTO
	isAuthorised: boolean
	canAssignNone: boolean
	onPropertyRightChange: (property: AccessRightDTO, right: PropertyRight) => void
}) {
	return (
		<ListItem
			disableGutters
			divider
			sx={{ pl: 3, pr: 2, minHeight: 64, display: 'flex', alignItems: 'center' }}
			secondaryAction={
				<Box sx={{ pr: 1 }}>
					<AccessRightSelect
						loadedAccessRight={property}
						isAuthorised={isAuthorised}
						canAssignNone={canAssignNone}
						isMassApply={false}
						onSelectAccessRight={right => onPropertyRightChange(property, right)}
					/>
				</Box>
			}
		>
			<ListItemText
				primary={property.name}
				slotProps={{ primary: { variant: 'body2' } }}
				sx={{ pr: 20 }}
			/>
		</ListItem>
	)
}

function SetAccordion({
	id,
	name,
	properties,
	isAuthorised,
	canAssignNone,
	onPropertyRightChange,
}: PropertyGroup & Omit<PropertyListProps, 'propertySets' | 'standaloneProperties'>) {
	return (
		<Accordion
			key={id}
			disableGutters
			elevation={0}
			sx={{
				mb: 1,
				borderRadius: 1,
				overflow: 'hidden',
				border: '1px solid',
				borderColor: 'divider',
				'&:before': { display: 'none' },
			}}
		>
			<AccordionSummary
				expandIcon={<ExpandMoreIcon />}
				sx={{
					backgroundColor: 'grey.100',
					minHeight: 52,
					px: 2,
					'& .MuiAccordionSummary-content': { alignItems: 'center', gap: 1.5 },
					'&:hover': { backgroundColor: 'grey.200' },
					transition: 'background-color 0.15s',
				}}
			>
				<Typography variant='subtitle1' fontWeight={600} lineHeight={1}>
					{name}
				</Typography>
				<Chip label={properties.length} size='small' sx={{ height: 20, fontSize: '0.7rem' }} />
			</AccordionSummary>
			<AccordionDetails sx={{ p: 0 }}>
				<List disablePadding>
					{properties.map(prop => (
						<PropertyRow
							key={prop.guidlineClassificationPropertyId}
							property={prop}
							isAuthorised={isAuthorised}
							canAssignNone={canAssignNone}
							onPropertyRightChange={onPropertyRightChange}
						/>
					))}
				</List>
			</AccordionDetails>
		</Accordion>
	)
}

export default function PropertyList({
	propertySets,
	standaloneProperties,
	isAuthorised,
	canAssignNone,
	onPropertyRightChange,
}: PropertyListProps) {
	const [searchInput, setSearchInput] = useState('')
	const [searchQuery, setSearchQuery] = useState('')

	useEffect(() => {
		const timer = setTimeout(() => setSearchQuery(searchInput), 250)
		return () => clearTimeout(timer)
	}, [searchInput])

	const query = searchQuery.toLowerCase()

	const filteredSets = propertySets
		.map(set => ({
			...set,
			properties: set.properties.filter(p => !query || (p.name ?? '').toLowerCase().includes(query)),
		}))
		.filter(set => set.properties.length > 0)

	const filteredStandalone = standaloneProperties.filter(
		p => !query || (p.name ?? '').toLowerCase().includes(query),
	)

	const hasResults = filteredSets.length > 0 || filteredStandalone.length > 0
	const hasSets = propertySets.length > 0

	return (
		<Box className='min-w-125'>
			<TextField
				size='small'
				fullWidth
				placeholder='Search properties…'
				value={searchInput}
				onChange={e => setSearchInput(e.target.value)}
				sx={{ mb: 2 }}
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

			{!hasResults ? (
				<Typography variant='body2' color='text.secondary' sx={{ py: 3, textAlign: 'center' }}>
					No properties match your search.
				</Typography>
			) : hasSets ? (
				<>
					{filteredSets.map(set => (
						<SetAccordion
							key={set.id}
							{...set}
							isAuthorised={isAuthorised}
							canAssignNone={canAssignNone}
							onPropertyRightChange={onPropertyRightChange}
						/>
					))}

					{filteredStandalone.length > 0 && (
						<SetAccordion
							id='__standalone__'
							name='Other Properties'
							properties={filteredStandalone}
							isAuthorised={isAuthorised}
							canAssignNone={canAssignNone}
							onPropertyRightChange={onPropertyRightChange}
						/>
					)}
				</>
			) : (
				<List disablePadding>
					{filteredStandalone.length > 0 ? (
						filteredStandalone.map(prop => (
							<PropertyRow
								key={prop.guidlineClassificationPropertyId}
								property={prop}
								isAuthorised={isAuthorised}
								canAssignNone={canAssignNone}
								onPropertyRightChange={onPropertyRightChange}
							/>
						))
					) : (
						<Typography variant='body2' color='text.secondary' sx={{ py: 3, textAlign: 'center' }}>
							There are no properties available for this classification.
						</Typography>
					)}
				</List>
			)}
		</Box>
	)
}
